using UnityEngine;
using System.Linq;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private Renderer sphereRenderer;
    private int totalJumps = 0;

    [Header("Movement Settings")]
    public float jumpForce = 6f;
    public float moveDistance = 1f;
    public float moveSpeed = 5f;
    public float moveDuration = 0.3f;

    [Header("Ground Detection")]
    public LayerMask groundLayer = -1;
    public float groundCheckDistance = 0.1f;
    public float timeToAllowNextJump = 0.2f;

    [Header("Collision Detection")]
    public float raycastDistance = 1.2f;     // Distancia para chequear obstáculo delante
    public float playerRadius = 0.5f;        // Radio "virtual" del jugador
    public bool showDebugRays = true;

    [Header("World Bounds")]
    public float worldSize = 12f;
    public float fallLimit = -5f;
    public bool enforceWorldBounds = true;   // ⬅ NUEVO
    public float worldBoundaryBuffer = 0.25f;// ⬅ NUEVO

    [Header("Collision Layers")]
    public LayerMask wallLayer = ~0;         // ⬅ NUEVO: por defecto todas las capas

    [Header("Squash & Stretch")]
    public float maxVelocityForStretch = 8f;
    public float stretchSpeed = 20f;
    public float minVelocityToDeform = 1f;
    public float landingSquashDuration = 0.3f;

    [Header("Materials")]
    public Material[] colorMaterials = new Material[5];

    [Header("Shape Mechanics")]
    public GameObject[] colorTargets;
    public float detectionRange = 3f;

    [Header("Debug")]
    public bool debugMovement = true;

    // Privados
    private Vector3 originalScale;
    private float timeGrounded = 0f;
    private bool shouldDeform = false;
    private bool isLandingSquash = false;
    private float landingSquashTimer = 0f;
    private bool isTransformed = false;
    private bool isJumping = false;
    private bool isMoving = false;
    private Vector3 moveStartPos;
    private Vector3 moveTargetPos;
    private float moveTimer = 0f;
    private GameObject currentHideTarget = null;

    // Eventos
    public System.Action<bool> OnTransformationChanged;
    public System.Action<int> OnColorChanged;

    void Start()
    {
        InitializeComponents();
        SetupRigidbody();
        InitializePosition();
        FindColorTargets();
        SetupArcadeColliders();
        Debug.Log("*** PLAYER INITIALIZED - RAYCAST COLLISION SYSTEM ***");
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        sphereRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;

        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;  // solo para evitar warnings si se mueve por script
        rb.useGravity = false;

        var anim = GetComponent<Animator>();
        if (anim) DestroyImmediate(anim);

        if (colorMaterials != null && colorMaterials.Length > 0 && colorMaterials[0] != null)
            sphereRenderer.material = colorMaterials[0];
    }

    void SetupRigidbody()
    {
        rb.mass = 1f;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void InitializePosition()
    {
        transform.position = new Vector3(0f, -0.5f, 0f);
    }

    void SetupArcadeColliders()
    {
        var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in all)
        {
            if (!IsArcadeMachine(obj)) continue;
            if (obj.GetComponent<Collider>() != null) continue;

            var col = obj.AddComponent<BoxCollider>();
            var r = obj.GetComponent<Renderer>();
            if (r)
            {
                var b = r.bounds;
                col.size = b.size;
                col.center = b.center - obj.transform.position;
            }
        }
    }

    bool IsArcadeMachine(GameObject obj)
    {
        string n = obj.name.ToLower();
        return n.Contains("maquina") || n.Contains("arcade") || n.Contains("machine");
    }

    void FindColorTargets()
    {
        var rends = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (var r in rends)
        {
            if (IsValidTarget(r.gameObject)) list.Add(r.gameObject);
        }
        colorTargets = list.ToArray();
    }

    bool IsValidTarget(GameObject obj)
    {
        return obj != gameObject &&
               !obj.name.Contains("Camera") &&
               !obj.name.Contains("Light") &&
               !obj.name.Contains("Directional") &&
               !obj.name.Contains("Plane") &&
               (obj.name.Contains("Cube") || obj.name.Contains("Target"));
    }

    void Update()
    {
        ConstrainToGroundLevel();
        HandleInput();
        HandleSmoothMovement();
        UpdateGroundStatus();
        CheckWorldBounds();
        CheckShapeMechanics();
        HandleSquashStretch();
        UpdateLandingSquash();
    }

    void ConstrainToGroundLevel()
    {
        if (!isJumping && Mathf.Abs(transform.position.y - (-0.5f)) > 0.0001f)
        {
            var p = transform.position; p.y = -0.5f; transform.position = p;
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R)) ResetPlayer();

        for (int i = 1; i <= 5; i++)
            if (Input.GetKeyDown(KeyCode.Alpha0 + i)) SetBallMaterial(i - 1);

        HandleMovementInput();

        if (Input.GetKeyDown(KeyCode.T))
            ToggleTransformation();
    }

    void HandleMovementInput()
    {
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            StartSimpleJump();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            Vector3 dir = new Vector3(h, 0, v).normalized;
            if (CanMoveInDirection(dir)) StartSmoothMove(dir);
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) && CanMoveInDirection(Vector3.left)) StartSmoothMove(Vector3.left);
        if (Input.GetKeyDown(KeyCode.D) && CanMoveInDirection(Vector3.right)) StartSmoothMove(Vector3.right);
        if (Input.GetKeyDown(KeyCode.W) && CanMoveInDirection(Vector3.forward)) StartSmoothMove(Vector3.forward);
        if (Input.GetKeyDown(KeyCode.S) && CanMoveInDirection(Vector3.back)) StartSmoothMove(Vector3.back);
        if (Input.GetKeyDown(KeyCode.LeftArrow) && CanMoveInDirection(Vector3.left)) StartSmoothMove(Vector3.left);
        if (Input.GetKeyDown(KeyCode.RightArrow) && CanMoveInDirection(Vector3.right)) StartSmoothMove(Vector3.right);
        if (Input.GetKeyDown(KeyCode.UpArrow) && CanMoveInDirection(Vector3.forward)) StartSmoothMove(Vector3.forward);
        if (Input.GetKeyDown(KeyCode.DownArrow) && CanMoveInDirection(Vector3.back)) StartSmoothMove(Vector3.back);
    }

    bool CanMoveInDirection(Vector3 direction)
    {
        Vector3 start = transform.position + Vector3.up * 0.1f;

        // 1) límites primero
        if (enforceWorldBounds && WouldExceedWorldBounds(direction)) return false;

        // 2) rayos centrales + laterales
        if (Physics.Raycast(start, direction, out RaycastHit hit, raycastDistance, wallLayer.value))
            if (hit.collider.gameObject != gameObject) return false;

        Vector3 side = Vector3.Cross(direction, Vector3.up).normalized * (playerRadius * 0.8f);
        if (Physics.Raycast(start + side, direction, out hit, raycastDistance, wallLayer.value))
            if (hit.collider.gameObject != gameObject) return false;
        if (Physics.Raycast(start - side, direction, out hit, raycastDistance, wallLayer.value))
            if (hit.collider.gameObject != gameObject) return false;

        // 3) sphere cast
        if (Physics.SphereCast(start, playerRadius * 0.7f, direction, out hit, moveDistance + 0.2f, wallLayer.value))
            if (hit.collider.gameObject != gameObject) return false;

        return true;
    }

    bool WouldExceedWorldBounds(Vector3 direction)
    {
        Vector3 future = transform.position + direction * moveDistance;
        float b = worldSize - worldBoundaryBuffer;
        return (future.x < -b || future.x > b || future.z < -b || future.z > b);
    }

    void StartSmoothMove(Vector3 direction)
    {
        if (isMoving || isJumping) return;
        isMoving = true;
        moveTimer = 0f;
        moveStartPos = transform.position;
        moveTargetPos = moveStartPos + direction * moveDistance;
        moveTargetPos.y = -0.5f;
    }

    void HandleSmoothMovement()
    {
        if (!isMoving) return;

        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / moveDuration);
        float eased = (t < 0.5f) ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        transform.position = Vector3.Lerp(moveStartPos, moveTargetPos, eased);

        if (t >= 1f)
        {
            transform.position = moveTargetPos;
            transform.localScale = originalScale;
            isMoving = false;
        }
        else
        {
            HandleMovementDeformation(t);
        }
    }

    void HandleMovementDeformation(float progress)
    {
        if (isTransformed) return;
        Vector3 s = originalScale;

        if (progress < 0.1f)
        {
            float a = (progress / 0.1f) * 0.05f;
            s.y *= 1f - a; s.x *= 1f + a * 0.5f; s.z *= 1f + a * 0.5f;
        }
        else if (progress > 0.9f)
        {
            float a = ((progress - 0.9f) / 0.1f) * 0.05f;
            s.y *= 1f - a; s.x *= 1f + a * 0.5f; s.z *= 1f + a * 0.5f;
        }
        transform.localScale = s;
    }

    void OnDrawGizmos()
    {
        if (!showDebugRays || !Application.isPlaying) return;
        Vector3 pos = transform.position + Vector3.up * 0.1f;
        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var d in dirs) Gizmos.DrawRay(pos, d * raycastDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerRadius);

        if (enforceWorldBounds)
        {
            Gizmos.color = Color.magenta;
            float b = worldSize - worldBoundaryBuffer;
            Vector3 tl = new Vector3(-b, transform.position.y, b);
            Vector3 tr = new Vector3(b, transform.position.y, b);
            Vector3 bl = new Vector3(-b, transform.position.y, -b);
            Vector3 br = new Vector3(b, transform.position.y, -b);
            Gizmos.DrawLine(tl, tr); Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl); Gizmos.DrawLine(bl, tl);
        }
    }

    void SetBallMaterial(int idx)
    {
        if (colorMaterials == null || idx < 0 || idx >= colorMaterials.Length || colorMaterials[idx] == null) return;
        sphereRenderer.material = colorMaterials[idx];
        OnColorChanged?.Invoke(idx);
    }

    // ===== Camouflage =====
    void CheckShapeMechanics()
    {
        if (colorTargets == null || colorTargets.Length == 0) return;

        string ballMat = GetCleanMaterialName(sphereRenderer.material.name);
        bool match = false;
        GameObject matched = null;

        foreach (var t in colorTargets)
        {
            if (t == null) continue;
            if (Vector3.Distance(transform.position, t.transform.position) >= detectionRange) continue;

            var r = t.GetComponent<Renderer>(); if (!r) continue;
            string targetMat = GetCleanMaterialName(r.material.name);

            if (DoMaterialsMatch(ballMat, targetMat))
            {
                match = true; matched = t; break;
            }
        }

        UpdateHideStatus(match, matched);
    }

    void UpdateHideStatus(bool found, GameObject matched)
    {
        if (found)
        {
            currentHideTarget = matched;
            if (!isTransformed) TransformToCube();
        }
        else
        {
            if (currentHideTarget != null)
            {
                float d = Vector3.Distance(transform.position, currentHideTarget.transform.position);
                if (d > detectionRange + 0.25f) currentHideTarget = null;
            }
            if (isTransformed && currentHideTarget == null) TransformToSphere();
        }
    }

    string GetCleanMaterialName(string n) => n.Replace(" (Instance)", "").Trim().ToLower();

    bool DoMaterialsMatch(string a, string b)
    {
        if (a == b) return true;

        string na = new string(a.Where(char.IsDigit).ToArray());
        string nb = new string(b.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(na) && na == nb) return true;

        string[] colors = { "red","blue","green","yellow","white","orange","purple","black",
                            "azul","verde","rojo","amarillo","blanco","naranja","morado","negro" };
        foreach (var c in colors) if (a.Contains(c) && b.Contains(c)) return true;

        return false;
    }

    void ToggleTransformation() { if (!isTransformed) TransformToCube(); else TransformToSphere(); }

    void TransformToCube()
    {
        isTransformed = true;
        transform.localScale = originalScale * 0.7f;
        OnTransformationChanged?.Invoke(true);
    }

    void TransformToSphere()
    {
        isTransformed = false;
        transform.localScale = originalScale;
        OnTransformationChanged?.Invoke(false);
    }

    // ===== Jump / squash =====
    void StartSimpleJump()
    {
        if (isJumping) return;
        isJumping = true; totalJumps++; UpdateColor();
        StartCoroutine(SimpleJumpAnimation());
    }

    IEnumerator SimpleJumpAnimation()
    {
        float dur = 0.8f, height = 1.5f, t = 0f;
        Vector3 start = transform.position, baseScale = transform.localScale;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float h = Mathf.Sin(p * Mathf.PI) * height;
            Vector3 pos = start; pos.y = -0.5f + h; transform.position = pos;
            transform.localScale = CalculateJumpScale(p, baseScale);
            yield return null;
        }

        transform.position = new Vector3(start.x, -0.5f, start.z);
        transform.localScale = baseScale;
        isJumping = false;
    }

    Vector3 CalculateJumpScale(float p, Vector3 s)
    {
        Vector3 r = s;
        if (p < 0.2f)
        {
            float k = 1f - (p / 0.2f) * 0.3f;
            r.y *= k; r.x *= 1f + (1f - k) * 0.5f; r.z *= 1f + (1f - k) * 0.5f;
        }
        else if (p > 0.8f)
        {
            float k = ((p - 0.8f) / 0.2f) * 0.4f;
            r.y *= 1f - k; r.x *= 1f + k; r.z *= 1f + k;
        }
        else
        {
            float k = Mathf.Sin((p - 0.2f) / 0.6f * Mathf.PI) * 0.2f;
            r.y *= 1f + k; r.x *= 1f - k * 0.3f; r.z *= 1f - k * 0.3f;
        }
        return r;
    }

    void UpdateColor()
    {
        int idx = (totalJumps / 2) % Mathf.Max(1, colorMaterials.Length);
        SetBallMaterial(idx);
    }

    void HandleSquashStretch()
    {
        if (isLandingSquash || isTransformed) return;
        bool grounded = IsGrounded();

        if (shouldDeform && !grounded)
        {
            float a = 0.2f;
            Vector3 s = new Vector3(
                originalScale.x * (1f - a * 0.3f),
                originalScale.y * (1f + a),
                originalScale.z * (1f - a * 0.3f)
            );
            transform.localScale = Vector3.Lerp(transform.localScale, s, Time.deltaTime * stretchSpeed);
        }
        else if (shouldDeform && grounded)
        {
            shouldDeform = false;
            isLandingSquash = true;
            landingSquashTimer = 0f;
        }
        else if (!shouldDeform && !isLandingSquash)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * stretchSpeed);
        }
    }

    void UpdateLandingSquash()
    {
        if (!isLandingSquash || isTransformed) return;

        landingSquashTimer += Time.deltaTime;
        float p = landingSquashTimer / landingSquashDuration;

        if (p < 0.3f)
        {
            float q = p / 0.3f;
            Vector3 s = new Vector3(originalScale.x * Mathf.Lerp(1f, 1.8f, q),
                                    originalScale.y * Mathf.Lerp(1f, 0.3f, q),
                                    originalScale.z * Mathf.Lerp(1f, 1.8f, q));
            transform.localScale = s;
        }
        else
        {
            float q = (p - 0.3f) / 0.7f;
            Vector3 maxS = new Vector3(originalScale.x * 1.8f, originalScale.y * 0.3f, originalScale.z * 1.8f);
            transform.localScale = Vector3.Lerp(maxS, originalScale, q);
        }

        if (p >= 1f)
        {
            isLandingSquash = false;
            transform.localScale = originalScale;
        }
    }

    void UpdateGroundStatus()
    {
        if (IsGrounded()) timeGrounded += Time.deltaTime; else timeGrounded = 0f;
    }

    void CheckWorldBounds()
    {
        Vector3 p = transform.position; bool fix = false;
        if (p.x < -worldSize) { p.x = -worldSize + 0.1f; fix = true; }
        if (p.x > worldSize) { p.x = worldSize - 0.1f; fix = true; }
        if (p.z < -worldSize) { p.z = -worldSize + 0.1f; fix = true; }
        if (p.z > worldSize) { p.z = worldSize - 0.1f; fix = true; }
        if (p.y < fallLimit) { ResetPlayer(); return; }
        if (fix) transform.position = p;
    }

    bool IsGrounded() => transform.position.y <= -0.4f;

    public void ResetPlayer()
    {
        totalJumps = 0;
        SetBallMaterial(0);
        isTransformed = false;
        currentHideTarget = null;
        InitializePosition();
        transform.localScale = originalScale;
        OnTransformationChanged?.Invoke(false);
        OnColorChanged?.Invoke(0);
    }

    // API público para el NPC
    public bool IsPlayerTransformed() => isTransformed;
    public bool IsHiddenForNPC()
    {
        if (!isTransformed || currentHideTarget == null) return false;
        return Vector3.Distance(transform.position, currentHideTarget.transform.position) <= detectionRange + 0.05f;
    }
}
