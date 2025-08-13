using UnityEngine;
using UnityEngine.SceneManagement;

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
    public float detectionRange = 10f;
    public float loseTargetRange = 12f;
    public float detectionAngle = 180f;
    public LayerMask obstacleLayer = ~0;
    public float detectionDelay = 0.05f;  // pequeño para pruebas
    
    [Header("Collision System")]
    public float movementRayDistance = 1.5f;
    public bool enableCollisionAvoidance = true;
    
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
    
    [Header("Game Over Settings")]
    public float captureDistance = 1f;
    public float gameOverDelay = 1f;
    
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

    private GameManager gm;
    
    void Start() 
    {
        InitializeComponents();
        SetupAudio();
        SetInitialState();
        gm = FindFirstObjectByType<GameManager>();
        Debug.Log("🤖 Enemy NPC initialized - Ready to hunt!");
    }
    
    void InitializeComponents()
    {
        startPosition = transform.position;
        
        if (player == null) FindPlayer();
        if (enemyRenderer == null) enemyRenderer = GetComponent<Renderer>();
        
        var pos = transform.position; pos.y = -0.3f; transform.position = pos;
    }
    
    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null) playerObj = playerController.gameObject;
        }
        if (playerObj == null)
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("player") || 
                    (obj.GetComponent<Renderer>() != null && 
                     obj.GetComponent<Rigidbody>() != null && 
                     obj.name.Contains("Sphere")))
                {
                    playerObj = obj; break;
                }
            }
        }
        if (playerObj != null)
        {
            player = playerObj.transform;
            if (playerController == null) playerController = playerObj.GetComponent<PlayerController>();
            Debug.Log($"✅ Enemy found player: {playerObj.name}");
        }
        else Debug.LogError("❌ Enemy could not find player! Make sure player exists.");
    }
    
    void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = warningDistance * 2f;
    }
    
    void SetInitialState()
    {
        ChangeState(EnemyState.Patrolling);
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
        if (isInDetectionDelay) detectionTimer += Time.deltaTime;
        if (Time.time - lastDetectionCheck < detectionCheckInterval) return;
        lastDetectionCheck = Time.time;
        
        float dist = Vector3.Distance(transform.position, player.position);
        bool playerIsHidden = IsPlayerHidden();
        bool inRange = dist <= detectionRange;
        bool inFov = IsPlayerInFieldOfView();
        bool directLoS = HasDirectLineOfSight();
        hasLineOfSight = inRange && inFov && directLoS && !playerIsHidden;
        
        if (currentState != EnemyState.Attacking && currentState != EnemyState.GameOver)
        {
            if (hasLineOfSight && (currentState == EnemyState.Patrolling || currentState == EnemyState.Searching))
                OnPlayerDetected();
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
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        switch (currentState)
        {
            case EnemyState.Patrolling: HandlePatrolling(); break;
            case EnemyState.Chasing:    HandleChasing(distanceToPlayer); break;
            case EnemyState.Searching:  HandleSearching(); break;
        }
    }
    
    void HandlePatrolling()
    {
        if (patrolPoints != null && patrolPoints.Length > 0 && !randomPatrol)
            PatrolBetweenPoints();
        else
            RandomPatrol();
    }
    
    void HandleChasing(float distanceToPlayer)
    {
        if (distanceToPlayer <= captureDistance) { CapturePlayer(); return; }
        if (!hasLineOfSight && distanceToPlayer > loseTargetRange) { OnPlayerLost(); return; }
        ChasePlayer();
    }
    
    void HandleSearching()
    {
        searchTimer += Time.deltaTime;
        if (hasLineOfSight) { OnPlayerDetected(); return; }
        if (searchTimer >= searchDuration) { ChangeState(EnemyState.Patrolling); return; }
        SearchLastPosition();
    }
    
    void OnPlayerDetected()
    {
        lastPlayerPosition = player.position;

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
        if (player == null) return false;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 targetPos = player.position + Vector3.up * 0.2f;
        Vector3 dir = (targetPos - origin).normalized;
        float dist = Vector3.Distance(origin, targetPos);

        int playerLayer = player.gameObject.layer;
        int blockers = ~(1 << playerLayer);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, blockers, QueryTriggerInteraction.Ignore))
            return hit.collider.transform == player;
        return true;
    }
    
    bool IsPlayerHidden() => playerController != null && playerController.IsHiddenForNPC();
    
    void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        MoveInDirection(dir, chaseSpeed);
        RotateTowards(dir);
        lastPlayerPosition = player.position;
    }
    
    void SearchLastPosition()
    {
        float dist = Vector3.Distance(transform.position, lastPlayerPosition);
        if (dist > 0.8f)
        {
            Vector3 dir = (lastPlayerPosition - transform.position).normalized;
            MoveInDirection(dir, patrolSpeed);
            RotateTowards(dir);
        }
        else transform.Rotate(0, 60f * Time.deltaTime, 0);
    }
    
    void PatrolBetweenPoints()
    {
        if (patrolPoints.Length == 0) return;
        Transform p = patrolPoints[currentPatrolIndex];
        float dist = Vector3.Distance(transform.position, p.position);
        
        if (dist > 0.8f)
        {
            Vector3 dir = (p.position - transform.position).normalized;
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
        Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        randomPatrolTarget = startPosition + dir * Random.Range(2f, randomPatrolRange);
        randomPatrolTarget.y = -0.3f;
    }
    
    void MoveInDirection(Vector3 direction, float speed)
    {
        Vector3 finalDir = direction;
        if (enableCollisionAvoidance) finalDir = GetCollisionAvoidanceDirection(direction);
        Vector3 pos = transform.position + finalDir * speed * Time.deltaTime;
        pos.y = -0.3f;
        transform.position = pos;
    }
    
    Vector3 GetCollisionAvoidanceDirection(Vector3 originalDirection)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayOrigin, originalDirection, out RaycastHit hit, movementRayDistance))
        {
            if (IsArcadeMachine(hit.collider.gameObject))
            {
                Vector3 right = Vector3.Cross(originalDirection, Vector3.up).normalized;
                Vector3 left = -right;
                if (!Physics.Raycast(rayOrigin, right, movementRayDistance)) return right;
                if (!Physics.Raycast(rayOrigin, left, movementRayDistance)) return left;
                return -originalDirection;
            }
        }

        Vector3 side = Vector3.Cross(originalDirection, Vector3.up).normalized * 0.5f;
        bool rightBlocked = Physics.Raycast(rayOrigin + side, originalDirection, movementRayDistance);
        bool leftBlocked = Physics.Raycast(rayOrigin - side, originalDirection, movementRayDistance);
        if (rightBlocked && !leftBlocked)  return (originalDirection - side * 0.5f).normalized;
        if (leftBlocked && !rightBlocked)  return (originalDirection + side * 0.5f).normalized;
        return originalDirection;
    }
    
    bool IsArcadeMachine(GameObject obj)
    {
        string n = obj.name.ToLower();
        return n.Contains("maquina") || n.Contains("arcade") || n.Contains("machine");
    }
    
    void RotateTowards(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        Quaternion q = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, rotationSpeed * Time.deltaTime);
    }
    
    void ChangeState(EnemyState s) { if (currentState != s) currentState = s; }
    
    void UpdateVisualFeedback()
    {
        if (enemyRenderer == null) return;
        Material m = null;
        switch (currentState)
        {
            case EnemyState.Patrolling: m = normalMaterial; break;
            case EnemyState.Chasing:
            case EnemyState.Attacking:  m = alertMaterial; break;
            case EnemyState.Searching:  m = searchingMaterial; break;
        }
        if (m != null && enemyRenderer.material != m) enemyRenderer.material = m;
    }
    
    void UpdateWarningAudio()
    {
        if (audioSource == null || player == null) return;
        float d = Vector3.Distance(transform.position, player.position);
        if (currentState == EnemyState.Chasing && hasLineOfSight && d <= warningDistance)
        {
            if (!audioSource.isPlaying) audioSource.Play();
            float t = 1f - (d / warningDistance);
            audioSource.volume = t * maxWarningVolume;
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
            if (enableEscapeByTime && escapeTimer >= escapeTime) PlayerEscaped();
            else if (!enableEscapeByTime) PlayerEscaped();
        }
        else if (isPlayerEscaping)
        {
            isPlayerEscaping = false; escapeTimer = 0f;
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
        var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        e.transform.position = transform.position + Vector3.up * 1.5f;
        e.transform.localScale = Vector3.one * 0.5f;
        e.GetComponent<Renderer>().material.color = Color.red;
        Destroy(e.GetComponent<Collider>());
        Destroy(e, 1f);
    }
    
    System.Collections.IEnumerator GameOverSequence()
    {
        gameIsOver = true;
        ChangeState(EnemyState.GameOver);
        if (playerController != null) playerController.enabled = false;
        yield return new WaitForSeconds(gameOverDelay);
        if (gm != null) gm.TriggerGameOver();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    System.Collections.IEnumerator VictorySequence()
    {
        ChangeState(EnemyState.GameOver);
        yield return new WaitForSeconds(gameOverDelay);
        if (gm != null) gm.TriggerVictory();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // API pública
    public bool IsPlayerDetected() => hasLineOfSight;
    public EnemyState GetCurrentState() => currentState;
    public float GetDistanceToPlayer() => player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
    public float GetEscapeTimer() => escapeTimer;
    public bool IsPlayerEscaping() => isPlayerEscaping;
}
