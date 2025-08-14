using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EnemyNPC : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;
    public PlayerController playerController;

    [Header("Movement Settings")]
    public float patrolSpeed = 1f;
    public float chaseSpeed = 2f;
    public float rotationSpeed = 2f;
    public float stoppingDistance = 1.2f;

    [Header("Detection Settings")]
    public float detectionRange = 7f;
    public float loseTargetRange = 10f;
    public float detectionAngle = 90f;
    public LayerMask obstacleLayer = ~0;
    public float detectionDelay = 3f;     // tiempo para escapar tras ser visto

    [Header("Game Start Settings")]
    public float gameStartDelay = 3f;     // delay antes de activar la detección

    [Header("Audio")]
    public AudioSource audioSource;
    public float warningDistance = 4f;
    public float maxWarningVolume = 0.7f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    public bool randomPatrol = false;
    public float randomPatrolRange = 5f;

    [Header("Visual Feedback")]
    public Renderer enemyRenderer;
    public Material normalMaterial;
    public Material alertMaterial;
    public Material searchingMaterial;

    [Header("End States")]
    public float captureDistance = 1f;
    public float gameOverDelay = 1f;

    [Header("Collision System")]
    public float movementRayDistance = 1.5f;
    public bool enableCollisionAvoidance = true;

    [Header("Escape Conditions")]
    public float escapeDistance = 12f;
    public float escapeTime = 10f;
    public bool enableEscapeByDistance = true;
    public bool enableEscapeByTime = true;

    public enum EnemyState { Patrolling, Chasing, Searching, Attacking, GameOver }
    private EnemyState currentState = EnemyState.Patrolling;

    private int currentPatrolIndex = 0;
    private float patrolTimer = 0f;
    private Vector3 randomPatrolTarget;
    private Vector3 startPosition;

    private Vector3 lastPlayerPosition;
    private float searchTimer = 0f;
    private float searchDuration = 8f;

    private float escapeTimer = 0f;
    private bool isPlayerEscaping = false;

    private float detectionCheckInterval = 0.15f;
    private float lastDetectionCheck = 0f;

    private float detectionTimer = 0f;
    private bool isInDetectionDelay = false;

    private bool hasLineOfSight = false;
    private bool gameIsOver = false;

    void Start()
    {
        InitializeComponents();
        SetupAudio();
        SetInitialState();
    }

    void InitializeComponents()
    {
        startPosition = transform.position;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (!playerObj) playerObj = GameObject.Find("Player");
            if (!playerObj)
            {
                playerController = FindFirstObjectByType<PlayerController>();
                if (playerController) playerObj = playerController.gameObject;
            }
            if (playerObj) { player = playerObj.transform; if (!playerController) playerController = playerObj.GetComponent<PlayerController>(); }
        }

        if (enemyRenderer == null) enemyRenderer = GetComponent<Renderer>();

        var pos = transform.position; pos.y = -0.3f; transform.position = pos;
    }

    void SetupAudio()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = warningDistance * 2f;
    }

    void SetInitialState()
    {
        currentState = EnemyState.Patrolling;
        isInDetectionDelay = false;
        detectionTimer = 0f;
        hasLineOfSight = false;

        if (randomPatrol && (patrolPoints == null || patrolPoints.Length == 0))
            GenerateRandomPatrolTarget();
    }

    void Update()
    {
        if (gameIsOver || player == null) return;

        UpdateDetection();
        UpdateStateBehavior();
        UpdateVisualFeedback();
        UpdateWarningAudio();
        CheckEscapeConditions();
    }

    void UpdateDetection()
    {
        if (Time.time < gameStartDelay) return;

        if (isInDetectionDelay) detectionTimer += Time.deltaTime;

        if (Time.time - lastDetectionCheck < detectionCheckInterval) return;
        lastDetectionCheck = Time.time;

        float dist = Vector3.Distance(transform.position, player.position);
        bool hidden = IsPlayerHidden();
        bool inRange = dist <= detectionRange;
        bool inFOV = IsPlayerInFieldOfView();
        bool los = HasDirectLineOfSight();

        hasLineOfSight = inRange && inFOV && los && !hidden;

        if (currentState != EnemyState.Attacking && currentState != EnemyState.GameOver)
        {
            if (hasLineOfSight && (currentState == EnemyState.Patrolling || currentState == EnemyState.Searching))
            {
                OnPlayerDetected();
            }
            else if (!hasLineOfSight && currentState == EnemyState.Chasing)
            {
                if (dist > loseTargetRange) OnPlayerLost();
            }
            else if (!hasLineOfSight && isInDetectionDelay)
            {
                isInDetectionDelay = false;
                detectionTimer = 0f;
            }
        }
    }

    void UpdateStateBehavior()
    {
        float d = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Patrolling: HandlePatrolling(); break;
            case EnemyState.Chasing: HandleChasing(d); break;
            case EnemyState.Searching: HandleSearching(); break;
        }
    }

    void HandlePatrolling()
    {
        if (patrolPoints != null && patrolPoints.Length > 0 && !randomPatrol)
        {
            Transform target = patrolPoints[currentPatrolIndex];
            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > 0.8f)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                MoveInDirection(dir, patrolSpeed);
                RotateTowards(dir);
            }
            else
            {
                patrolTimer += Time.deltaTime;
                if (patrolTimer >= patrolWaitTime)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    patrolTimer = 0f;
                }
            }
        }
        else
        {
            RandomPatrol();
        }
    }

    void HandleChasing(float distToPlayer)
    {
        if (distToPlayer <= captureDistance) { CapturePlayer(); return; }

        if (!hasLineOfSight && distToPlayer > loseTargetRange) { OnPlayerLost(); return; }

        if (currentState == EnemyState.Chasing)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            MoveInDirection(dir, chaseSpeed);
            RotateTowards(dir);
            lastPlayerPosition = player.position;
        }
    }

    void HandleSearching()
    {
        searchTimer += Time.deltaTime;

        if (hasLineOfSight) { OnPlayerDetected(); return; }

        if (searchTimer >= searchDuration)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }

        float dist = Vector3.Distance(transform.position, lastPlayerPosition);
        if (dist > 0.8f)
        {
            Vector3 dir = (lastPlayerPosition - transform.position).normalized;
            MoveInDirection(dir, patrolSpeed);
            RotateTowards(dir);
        }
        else
        {
            transform.Rotate(0, 60f * Time.deltaTime, 0);
        }
    }

    void OnPlayerDetected()
    {
        if (!isInDetectionDelay)
        {
            isInDetectionDelay = true;
            detectionTimer = 0f;
            CreateDetectionEffect();
            return;
        }

        if (detectionTimer >= detectionDelay)
        {
            ChangeState(EnemyState.Chasing);
            lastPlayerPosition = player.position;
            isInDetectionDelay = false;
        }
    }

    void OnPlayerLost()
    {
        ChangeState(EnemyState.Searching);
        searchTimer = 0f;
    }

    void CapturePlayer()
    {
        if (gameIsOver) return;
        ChangeState(EnemyState.Attacking);
        StartCoroutine(GameOverSequence());
    }

    bool IsPlayerInFieldOfView()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toPlayer);
        return angle <= detectionAngle * 0.5f;
    }

    bool HasDirectLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = (player.position - origin).normalized;
        float dist = Vector3.Distance(transform.position, player.position);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleLayer))
            return hit.collider.transform == player;

        return true;
    }

    bool IsPlayerHidden() => playerController != null && playerController.IsHiddenForNPC();

    void MoveInDirection(Vector3 direction, float speed)
    {
        Vector3 finalDir = direction;

        if (enableCollisionAvoidance)
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, movementRayDistance))
            {
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
                Vector3 left = -right;

                if (!Physics.Raycast(origin, right, movementRayDistance)) finalDir = right;
                else if (!Physics.Raycast(origin, left, movementRayDistance)) finalDir = left;
                else finalDir = -direction;
            }
        }

        Vector3 target = transform.position + finalDir * speed * Time.deltaTime;
        target.y = -0.3f;
        transform.position = target;
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion q = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, rotationSpeed * Time.deltaTime);
    }

    void RandomPatrol()
    {
        float dist = Vector3.Distance(transform.position, randomPatrolTarget);
        if (dist > 0.8f)
        {
            Vector3 dir = (randomPatrolTarget - transform.position).normalized;
            MoveInDirection(dir, patrolSpeed);
            RotateTowards(dir);
        }
        else
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime)
            {
                GenerateRandomPatrolTarget();
                patrolTimer = 0f;
            }
        }
    }

    void GenerateRandomPatrolTarget()
    {
        Vector3 rnd = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        randomPatrolTarget = startPosition + rnd * Random.Range(2f, randomPatrolRange);
        randomPatrolTarget.y = -0.3f;
    }

    void UpdateVisualFeedback()
    {
        if (!enemyRenderer) return;
        Material m = null;
        switch (currentState)
        {
            case EnemyState.Patrolling: m = normalMaterial; break;
            case EnemyState.Chasing:
            case EnemyState.Attacking: m = alertMaterial; break;
            case EnemyState.Searching: m = searchingMaterial; break;
        }
        if (m && enemyRenderer.material != m) enemyRenderer.material = m;
    }

    void UpdateWarningAudio()
    {
        if (!audioSource || !player) return;
        float d = Vector3.Distance(transform.position, player.position);

        if (currentState == EnemyState.Chasing && hasLineOfSight && d <= warningDistance)
        {
            if (!audioSource.isPlaying) audioSource.Play();
            float vol = 1f - (d / warningDistance);
            audioSource.volume = vol * maxWarningVolume;
        }
        else
        {
            if (audioSource.isPlaying) audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    void CheckEscapeConditions()
    {
        if (!enableEscapeByDistance && !enableEscapeByTime) return;

        float d = Vector3.Distance(transform.position, player.position);

        if (enableEscapeByDistance && d >= escapeDistance)
        {
            if (!isPlayerEscaping) { isPlayerEscaping = true; escapeTimer = 0f; }
            escapeTimer += Time.deltaTime;

            if ((enableEscapeByTime && escapeTimer >= escapeTime) || !enableEscapeByTime)
            {
                PlayerEscaped();
            }
        }
        else
        {
            if (isPlayerEscaping) { isPlayerEscaping = false; escapeTimer = 0f; }
        }
    }

    void PlayerEscaped()
    {
        if (gameIsOver) return;
        gameIsOver = true;
        StartCoroutine(VictorySequence());
    }

    void CreateDetectionEffect()
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.transform.position = transform.position + Vector3.up * 1.5f;
        fx.transform.localScale = Vector3.one * 0.5f;
        fx.GetComponent<Renderer>().material.color = Color.red;
        Destroy(fx.GetComponent<Collider>());
        Destroy(fx, 1f);
    }

    IEnumerator GameOverSequence()
    {
        gameIsOver = true;
        ChangeState(EnemyState.GameOver);
        if (playerController) playerController.enabled = false;
        yield return new WaitForSeconds(gameOverDelay);

        // Mostrar panel de Game Over en la misma escena (GameManager lo gestiona)
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.TriggerGameOver();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator VictorySequence()
    {
        ChangeState(EnemyState.GameOver);
        yield return new WaitForSeconds(gameOverDelay);

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.TriggerVictory();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ChangeState(EnemyState s) { if (currentState != s) currentState = s; }

    // API pública
    public bool IsPlayerDetected() => hasLineOfSight;
    public EnemyState GetCurrentState() => currentState;
    public float GetDistanceToPlayer() => player ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
    public float GetEscapeTimer() => escapeTimer;
    public bool IsPlayerEscaping() => isPlayerEscaping;
    public bool IsInDetectionDelay() => isInDetectionDelay;
    public float GetDetectionCountdown() => Mathf.Max(0, detectionDelay - detectionTimer);
    public int GetDetectionSecondsLeft() => Mathf.CeilToInt(GetDetectionCountdown());
    public bool IsNPCActive() => Time.time >= gameStartDelay;
    public float GetActivationCountdown() => Mathf.Max(0, gameStartDelay - Time.time);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, warningDistance);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        Gizmos.color = Color.green;
        Vector3 f = transform.forward;
        Vector3 l = Quaternion.Euler(0, -detectionAngle / 2, 0) * f;
        Vector3 r = Quaternion.Euler(0, detectionAngle / 2, 0) * f;
        Gizmos.DrawRay(transform.position, l * detectionRange);
        Gizmos.DrawRay(transform.position, r * detectionRange);
    }
}
