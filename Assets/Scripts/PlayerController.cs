using UnityEngine;
using System.Linq;

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
    
    [Header("Collision Detection - NUEVO SISTEMA")]
    public float raycastDistance = 1.2f; // Distancia de raycast para detectar obstáculos
    public float playerRadius = 0.5f; // Radio del jugador
    public bool showDebugRays = true; // Mostrar rayos de debug
    
    [Header("Squash & Stretch Settings")]
    public float maxVelocityForStretch = 8f;
    public float stretchSpeed = 20f;
    public float minVelocityToDeform = 1f;
    public float landingSquashDuration = 0.3f;
    
    [Header("World Bounds")]
    public float worldSize = 12f;
    public float fallLimit = -5f;
    
    [Header("Materials")]
    public Material[] colorMaterials = new Material[5];
    
    [Header("Shape Mechanics")]
    public GameObject[] colorTargets;
    public float detectionRange = 3f;
    
    [Header("Debug")]
    public bool debugMovement = true;
    
    // Private fields
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
    private Vector3 lastValidPosition;
    
    // Events
    public System.Action<bool> OnTransformationChanged;
    public System.Action<int> OnColorChanged;
    
    void Start() 
    {
        InitializeComponents();
        SetupRigidbody();
        InitializePosition();
        FindColorTargets();
        SetupArcadeColliders();
        
        lastValidPosition = transform.position;
        Debug.Log("*** PLAYER INITIALIZED - RAYCAST COLLISION SYSTEM ***");
    }
    
    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        sphereRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("Rigidbody added automatically");
        }
        
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            DestroyImmediate(animator);
            Debug.Log("Animator removed");
        }
        
        if (colorMaterials != null && colorMaterials.Length > 0 && colorMaterials[0] != null)
        {
            sphereRenderer.material = colorMaterials[0];
        }
    }
    
    void SetupRigidbody()
    {
        // SISTEMA SIMPLIFICADO - Sin rigidbody para física
        rb.mass = 1f;
        rb.isKinematic = true; // Kinematic pero solo para evitar warnings
        rb.useGravity = false;
        
        Debug.Log("Rigidbody configured as kinematic for collision detection");
    }
    
    void InitializePosition()
    {
        float correctHeight = -0.5f;
        Vector3 correctPosition = new Vector3(0, correctHeight, 0);
        transform.position = correctPosition;
        
        Debug.Log($"Ball positioned at correct height: Y = {correctHeight}");
    }
    
    void SetupArcadeColliders()
    {
        Debug.Log("Setting up arcade machine colliders...");
        
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int collidersAdded = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (IsArcadeMachine(obj))
            {
                if (obj.GetComponent<Collider>() == null)
                {
                    BoxCollider collider = obj.AddComponent<BoxCollider>();
                    
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Bounds bounds = renderer.bounds;
                        // Usar bounds del mundo, no locales
                        collider.size = bounds.size;
                        collider.center = bounds.center - obj.transform.position;
                    }
                    
                    collidersAdded++;
                    Debug.Log($"✅ Added collider to: {obj.name}");
                }
                else
                {
                    Debug.Log($"⚠️ Arcade machine {obj.name} already has collider");
                }
            }
        }
        
        Debug.Log($"🎮 Added {collidersAdded} colliders to arcade machines");
    }
    
    bool IsArcadeMachine(GameObject obj)
    {
        string objName = obj.name.ToLower();
        return objName.Contains("maquina") || objName.Contains("arcade") || objName.Contains("machine");
    }
    
    void FindColorTargets()
    {
        Debug.Log("Searching for camouflage objects...");
        
        Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        System.Collections.Generic.List<GameObject> targetsList = new System.Collections.Generic.List<GameObject>();
        
        foreach (Renderer renderer in allRenderers)
        {
            GameObject obj = renderer.gameObject;
            if (IsValidTarget(obj))
            {
                targetsList.Add(obj);
            }
        }
        
        colorTargets = targetsList.ToArray();
        Debug.Log($"Found {colorTargets.Length} camouflage objects");
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
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log($"=== MOVEMENT DEBUG ===");
            Debug.Log($"isMoving: {isMoving}");
            Debug.Log($"isJumping: {isJumping}");
            Debug.Log($"moveTimer: {moveTimer}");
            Debug.Log($"Position: {transform.position}");
        }
    }
    
    void ConstrainToGroundLevel()
    {
        if (!isJumping && transform.position.y != -0.5f)
        {
            Vector3 pos = transform.position;
            pos.y = -0.5f;
            transform.position = pos;
        }
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.I)) DebugComplete();
        if (Input.GetKeyDown(KeyCode.R)) ResetPlayer();
        
        for (int i = 1; i <= 5; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                SetBallMaterial(i - 1);
            }
        }
        
        HandleMovementInput();
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleTransformation();
        }
    }
    
    void HandleMovementInput()
    {
        if (isMoving) return;
        
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            StartSimpleJump();
        }
        
        // Input Manager (WASD configurado en Unity)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
            if (CanMoveInDirection(direction))
            {
                StartSmoothMove(direction);
                Debug.Log($"INPUT MANAGER: Moving in direction {direction}");
            }
            else
            {
                Debug.Log($"❌ Movement blocked by obstacle!");
            }
            return;
        }
        
        // Teclas directas WASD
        if (Input.GetKeyDown(KeyCode.A)) 
        {
            if (CanMoveInDirection(Vector3.left))
            {
                StartSmoothMove(Vector3.left);
                Debug.Log("✅ Moving LEFT");
            }
            else Debug.Log("❌ LEFT blocked by obstacle");
        }
        if (Input.GetKeyDown(KeyCode.D)) 
        {
            if (CanMoveInDirection(Vector3.right))
            {
                StartSmoothMove(Vector3.right);
                Debug.Log("✅ Moving RIGHT");
            }
            else Debug.Log("❌ RIGHT blocked by obstacle");
        }
        if (Input.GetKeyDown(KeyCode.W)) 
        {
            if (CanMoveInDirection(Vector3.forward))
            {
                StartSmoothMove(Vector3.forward);
                Debug.Log("✅ Moving FORWARD");
            }
            else Debug.Log("❌ FORWARD blocked by obstacle");
        }
        if (Input.GetKeyDown(KeyCode.S)) 
        {
            if (CanMoveInDirection(Vector3.back))
            {
                StartSmoothMove(Vector3.back);
                Debug.Log("✅ Moving BACK");
            }
            else Debug.Log("❌ BACK blocked by obstacle");
        }
        
        // Flechas del teclado
        if (Input.GetKeyDown(KeyCode.LeftArrow)) 
        {
            if (CanMoveInDirection(Vector3.left))
            {
                StartSmoothMove(Vector3.left);
                Debug.Log("✅ Moving LEFT (Arrow)");
            }
            else Debug.Log("❌ LEFT (Arrow) blocked");
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) 
        {
            if (CanMoveInDirection(Vector3.right))
            {
                StartSmoothMove(Vector3.right);
                Debug.Log("✅ Moving RIGHT (Arrow)");
            }
            else Debug.Log("❌ RIGHT (Arrow) blocked");
        }
        if (Input.GetKeyDown(KeyCode.UpArrow)) 
        {
            if (CanMoveInDirection(Vector3.forward))
            {
                StartSmoothMove(Vector3.forward);
                Debug.Log("✅ Moving FORWARD (Arrow)");
            }
            else Debug.Log("❌ FORWARD (Arrow) blocked");
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) 
        {
            if (CanMoveInDirection(Vector3.back))
            {
                StartSmoothMove(Vector3.back);
                Debug.Log("✅ Moving BACK (Arrow)");
            }
            else Debug.Log("❌ BACK (Arrow) blocked");
        }
    }
    
    // NUEVO SISTEMA: Verificar colisiones con Raycast ANTES de mover
    bool CanMoveInDirection(Vector3 direction)
    {
        Vector3 startPos = transform.position + Vector3.up * 0.1f; // Ligeramente elevado
        float checkDistance = raycastDistance;
        
        // Raycast principal en el centro
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, checkDistance))
        {
            if (IsArcadeMachine(hit.collider.gameObject))
            {
                Debug.Log($"🚫 Center ray blocked by: {hit.collider.gameObject.name} at distance {hit.distance:F2}");
                return false;
            }
        }
        
        // Raycasts adicionales en los lados (para mejor detección)
        Vector3 leftOffset = Vector3.Cross(direction, Vector3.up).normalized * (playerRadius * 0.8f);
        Vector3 rightOffset = -leftOffset;
        
        // Ray izquierdo
        if (Physics.Raycast(startPos + leftOffset, direction, out hit, checkDistance))
        {
            if (IsArcadeMachine(hit.collider.gameObject))
            {
                Debug.Log($"🚫 Left ray blocked by: {hit.collider.gameObject.name}");
                return false;
            }
        }
        
        // Ray derecho
        if (Physics.Raycast(startPos + rightOffset, direction, out hit, checkDistance))
        {
            if (IsArcadeMachine(hit.collider.gameObject))
            {
                Debug.Log($"🚫 Right ray blocked by: {hit.collider.gameObject.name}");
                return false;
            }
        }
        
        Debug.Log($"✅ Path clear in direction {direction}");
        return true;
    }
    
    void StartSmoothMove(Vector3 direction)
    {
        if (isMoving || isJumping) 
        {
            Debug.Log($"Cannot move: isMoving={isMoving}, isJumping={isJumping}");
            return;
        }
        
        isMoving = true;
        moveTimer = 0f;
        moveStartPos = transform.position;
        
        Vector3 targetPos = moveStartPos + (direction * moveDistance);
        targetPos.y = -0.5f;
        moveTargetPos = targetPos;
        
        Debug.Log($"🎯 Starting move from {moveStartPos} to {moveTargetPos}");
    }
    
    void HandleSmoothMovement()
    {
        if (!isMoving) return;
        
        moveTimer += Time.deltaTime;
        float progress = moveTimer / moveDuration;
        
        if (progress < 1f)
        {
            float easedProgress = EaseInOutCubic(progress);
            Vector3 newPos = Vector3.Lerp(moveStartPos, moveTargetPos, easedProgress);
            newPos.y = -0.5f;
            
            // Verificación adicional durante el movimiento
            Vector3 directionToTarget = (moveTargetPos - transform.position).normalized;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, directionToTarget, out RaycastHit hit, 0.8f))
            {
                if (IsArcadeMachine(hit.collider.gameObject))
                {
                    // Detener movimiento si encontramos obstáculo
                    isMoving = false;
                    moveTimer = 0f;
                    Debug.Log($"🛑 Movement stopped - hit {hit.collider.gameObject.name} during movement");
                    return;
                }
            }
            
            transform.position = newPos;
            HandleMovementDeformation(progress);
        }
        else
        {
            Vector3 finalPos = moveTargetPos;
            finalPos.y = -0.5f;
            transform.position = finalPos;
            
            transform.localScale = originalScale;
            isMoving = false;
            moveTimer = 0f;
            Debug.Log("✅ Movement completed");
        }
    }
    
    void HandleMovementDeformation(float progress)
    {
        if (isTransformed) return;
        
        Vector3 scale = originalScale;
        if (progress < 0.1f)
        {
            float squashAmount = (progress / 0.1f) * 0.05f;
            scale.y *= (1f - squashAmount);
            scale.x *= (1f + squashAmount * 0.5f);
            scale.z *= (1f + squashAmount * 0.5f);
        }
        else if (progress > 0.9f)
        {
            float landAmount = ((progress - 0.9f) / 0.1f) * 0.05f;
            scale.y *= (1f - landAmount);
            scale.x *= (1f + landAmount * 0.5f);
            scale.z *= (1f + landAmount * 0.5f);
        }
        transform.localScale = scale;
    }
    
    float EaseInOutCubic(float t)
    {
        if (t < 0.5f) 
            return 4f * t * t * t;
        else 
            return 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    // Visualización de debug en Scene View
    void OnDrawGizmos()
    {
        if (!showDebugRays || !Application.isPlaying) return;
        
        Vector3 pos = transform.position + Vector3.up * 0.1f;
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        Color[] colors = { Color.blue, Color.cyan, Color.red, Color.green };
        
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 dir = directions[i];
            bool canMove = CanMoveInDirection(dir);
            
            Gizmos.color = canMove ? colors[i] : Color.red;
            Gizmos.DrawRay(pos, dir * raycastDistance);
            
            // Rayos laterales
            Vector3 leftOffset = Vector3.Cross(dir, Vector3.up).normalized * (playerRadius * 0.8f);
            Vector3 rightOffset = -leftOffset;
            
            Gizmos.color = canMove ? (colors[i] * 0.7f) : (Color.red * 0.7f);
            Gizmos.DrawRay(pos + leftOffset, dir * raycastDistance);
            Gizmos.DrawRay(pos + rightOffset, dir * raycastDistance);
        }
        
        // Radio del jugador
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerRadius);
    }
    
    void SetBallMaterial(int materialIndex)
    {
        if (colorMaterials != null && materialIndex >= 0 && materialIndex < colorMaterials.Length && colorMaterials[materialIndex] != null)
        {
            sphereRenderer.material = colorMaterials[materialIndex];
            OnColorChanged?.Invoke(materialIndex);
            Debug.Log($"Material changed to: {colorMaterials[materialIndex].name}");
        }
    }
    
    void CheckShapeMechanics()
    {
        if (colorTargets == null || colorTargets.Length == 0) return;
        
        string ballMaterial = GetCleanMaterialName(sphereRenderer.material.name);
        bool foundMatch = false;
        GameObject matched = null;
        
        // DEBUG: Mostrar material actual de la pelota
        if (Time.frameCount % 120 == 0) // Cada 2 segundos aprox
        {
            Debug.Log($"🔍 Ball material: '{ballMaterial}'");
        }
        
        foreach (GameObject target in colorTargets)
        {
            if (target == null) continue;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < detectionRange)
            {
                Renderer targetRenderer = target.GetComponent<Renderer>();
                if (targetRenderer == null) continue;
                
                string targetMaterial = GetCleanMaterialName(targetRenderer.material.name);
                bool isMatch = DoMaterialsMatch(ballMaterial, targetMaterial);
                
                // DEBUG: Mostrar comparación
                Debug.Log($"🎯 Near {target.name}: Ball='{ballMaterial}' vs Target='{targetMaterial}' | Match: {isMatch} | Distance: {distance:F2}");
                
                if (isMatch)
                {
                    foundMatch = true;
                    matched = target;
                    if (!isTransformed) 
                    {
                        TransformToCube();
                        Debug.Log($"✅ CAMOUFLAGE ACTIVATED with {target.name}!");
                    }
                    break;
                }
            }
        }
        
        UpdateHideStatus(foundMatch, matched);
    }
    
    void UpdateHideStatus(bool foundMatch, GameObject matched)
    {
        if (foundMatch)
        {
            currentHideTarget = matched;
        }
        else
        {
            if (currentHideTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentHideTarget.transform.position);
                if (distance > detectionRange + 0.25f) 
                {
                    currentHideTarget = null;
                }
            }
            
            if (isTransformed && currentHideTarget == null)
            {
                TransformToSphere();
            }
        }
    }
    
    string GetCleanMaterialName(string materialName)
    {
        return materialName.Replace(" (Instance)", "").Trim().ToLower();
    }
    
    bool DoMaterialsMatch(string ballMaterial, string targetMaterial)
    {
        // Comparación exacta primero
        if (ballMaterial == targetMaterial) 
        {
            Debug.Log($"✅ EXACT MATCH: '{ballMaterial}' == '{targetMaterial}'");
            return true;
        }
        
        // Check for number matching
        string ballNumber = GetMaterialNumber(ballMaterial);
        string targetNumber = GetMaterialNumber(targetMaterial);
        if (!string.IsNullOrEmpty(ballNumber) && !string.IsNullOrEmpty(targetNumber))
        {
            bool numberMatch = ballNumber == targetNumber;
            if (numberMatch)
                Debug.Log($"✅ NUMBER MATCH: Ball#{ballNumber} == Target#{targetNumber}");
            else
                Debug.Log($"❌ Number mismatch: Ball#{ballNumber} != Target#{targetNumber}");
            return numberMatch;
        }
        
        // Check for color name matching
        string[] colors = { 
            "red", "blue", "green", "yellow", "white", "orange", "purple", "black",
            "azul", "verde", "rojo", "amarillo", "blanco", "naranja", "morado", "negro",
            "example", "color", "material"
        };
        
        foreach (string color in colors)
        {
            bool ballHasColor = ballMaterial.Contains(color);
            bool targetHasColor = targetMaterial.Contains(color);
            
            if (ballHasColor && targetHasColor)
            {
                Debug.Log($"✅ COLOR MATCH: Both contain '{color}'");
                return true;
            }
        }
        
        Debug.Log($"❌ NO MATCH: '{ballMaterial}' vs '{targetMaterial}'");
        return false;
    }
    
    string GetMaterialNumber(string materialName)
    {
        for (int i = materialName.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(materialName[i])) 
                return materialName[i].ToString();
        }
        
        if (materialName.Contains("example") && !materialName.Any(char.IsDigit)) 
            return "0";
            
        return "";
    }
    
    void ToggleTransformation()
    {
        if (!isTransformed)
            TransformToCube();
        else
            TransformToSphere();
    }
    
    void TransformToCube()
    {
        isTransformed = true;
        transform.localScale = originalScale * 0.7f; 
        CreateTransformEffect(Color.yellow);
        OnTransformationChanged?.Invoke(true);
        Debug.Log("*** TRANSFORMED TO CUBE! ***");
    }
    
    void TransformToSphere()
    {
        isTransformed = false;
        currentHideTarget = null;
        transform.localScale = originalScale;
        CreateTransformEffect(Color.white);
        OnTransformationChanged?.Invoke(false);
        Debug.Log("*** BACK TO SPHERE ***");
    }
    
    void CreateTransformEffect(Color effectColor)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.transform.position = transform.position + Vector3.up * 1f;
        effect.transform.localScale = Vector3.one * 0.8f;
        effect.GetComponent<Renderer>().material.color = effectColor;
        
        Collider effectCollider = effect.GetComponent<Collider>();
        if (effectCollider != null) Destroy(effectCollider);
        
        Destroy(effect, 2f);
    }
    
    void StartSimpleJump()
    {
        if (isJumping) return;
        
        isJumping = true;
        totalJumps++;
        UpdateColor();
        Debug.Log($"*** STARTING SIMPLE JUMP #{totalJumps} ***");
        StartCoroutine(SimpleJumpAnimation());
    }
    
    System.Collections.IEnumerator SimpleJumpAnimation()
    {
        float jumpDuration = 0.8f;
        float jumpHeight = 1.5f;
        float timer = 0f;
        
        Vector3 startPos = transform.position;
        Vector3 originalJumpScale = transform.localScale;
        
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;
            
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            Vector3 jumpPos = startPos;
            jumpPos.y = -0.5f + height;
            
            Vector3 scale = CalculateJumpScale(progress, originalJumpScale);
            
            transform.position = jumpPos;
            transform.localScale = scale;
            yield return null;
        }
        
        Vector3 finalPos = startPos;
        finalPos.y = -0.5f;
        transform.position = finalPos;
        transform.localScale = originalJumpScale;
        
        isJumping = false;
        Debug.Log("*** SIMPLE JUMP COMPLETED ***");
    }
    
    Vector3 CalculateJumpScale(float progress, Vector3 baseScale)
    {
        Vector3 scale = baseScale;
        
        if (progress < 0.2f)
        {
            float squash = 1f - (progress / 0.2f) * 0.3f;
            scale.y *= squash;
            scale.x *= (1f + (1f - squash) * 0.5f);
            scale.z *= (1f + (1f - squash) * 0.5f);
        }
        else if (progress > 0.8f)
        {
            float land = ((progress - 0.8f) / 0.2f) * 0.4f;
            scale.y *= (1f - land);
            scale.x *= (1f + land);
            scale.z *= (1f + land);
        }
        else
        {
            float stretch = Mathf.Sin((progress - 0.2f) / 0.6f * Mathf.PI) * 0.2f;
            scale.y *= (1f + stretch);
            scale.x *= (1f - stretch * 0.3f);
            scale.z *= (1f - stretch * 0.3f);
        }
        
        return scale;
    }
    
    void UpdateColor()
    {
        int materialIndex = (totalJumps / 2) % colorMaterials.Length;
        SetBallMaterial(materialIndex);
        
        string[] colorNames = {"WHITE", "BLUE", "GREEN", "RED", "YELLOW"};
        string colorName = materialIndex < colorNames.Length ? colorNames[materialIndex] : $"COLOR_{materialIndex}";
        Debug.Log($"*** CHANGED TO: {colorName} (Jumps: {totalJumps}) ***");
    }
    
    void HandleSquashStretch()
    {
        if (isLandingSquash || isTransformed) return;
        
        bool isGrounded = IsGrounded();
        
        if (shouldDeform && !isGrounded)
        {
            float stretchAmount = 0.2f;
            Vector3 stretchScale = new Vector3(
                originalScale.x * (1f - stretchAmount * 0.3f),
                originalScale.y * (1f + stretchAmount),
                originalScale.z * (1f - stretchAmount * 0.3f)
            );
            transform.localScale = Vector3.Lerp(transform.localScale, stretchScale, Time.deltaTime * stretchSpeed);
        }
        else if (shouldDeform && isGrounded) 
        {
            shouldDeform = false;
            isLandingSquash = true;
            landingSquashTimer = 0f;
        }
        else if (!shouldDeform && !isLandingSquash)
        {
            if (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * stretchSpeed);
            else
                transform.localScale = originalScale;
        }
    }
    
    void UpdateLandingSquash()
    {
        if (isLandingSquash && !isTransformed)
        {
            landingSquashTimer += Time.deltaTime;
            float progress = landingSquashTimer / landingSquashDuration;
            
            if (progress < 0.3f)
            {
                float squashProgress = progress / 0.3f;
                Vector3 landSquash = new Vector3(
                    originalScale.x * Mathf.Lerp(1f, 1.8f, squashProgress),
                    originalScale.y * Mathf.Lerp(1f, 0.3f, squashProgress),
                    originalScale.z * Mathf.Lerp(1f, 1.8f, squashProgress)
                );
                transform.localScale = landSquash;
            }
            else
            {
                float returnProgress = (progress - 0.3f) / 0.7f;
                Vector3 maxSquash = new Vector3(originalScale.x * 1.8f, originalScale.y * 0.3f, originalScale.z * 1.8f);
                transform.localScale = Vector3.Lerp(maxSquash, originalScale, returnProgress);
            }
            
            if (progress >= 1f)
            {
                isLandingSquash = false;
                landingSquashTimer = 0f;
                transform.localScale = originalScale;
            }
        }
    }
    
    void UpdateGroundStatus()
    {
        bool isGrounded = IsGrounded();
        if (isGrounded)
        {
            timeGrounded += Time.deltaTime;
        }
        else
        {
            timeGrounded = 0f;
        }
    }
    
    void CheckWorldBounds()
    {
        Vector3 pos = transform.position;
        if (pos.x < -worldSize || pos.x > worldSize || 
            pos.z < -worldSize || pos.z > worldSize || 
            pos.y < fallLimit)
        {
            Debug.Log("Out of bounds - Resetting player");
            ResetPlayer();
        }
    }
    
    bool IsGrounded()
    {
        return transform.position.y <= -0.4f;
    }
    
    public void ResetPlayer()
    {
        totalJumps = 0;
        SetBallMaterial(0);
        
        shouldDeform = false;
        isLandingSquash = false;
        isTransformed = false;
        currentHideTarget = null;
        
        InitializePosition();
        transform.localScale = originalScale;
        
        OnTransformationChanged?.Invoke(false);
        OnColorChanged?.Invoke(0);
        
        Debug.Log("*** PLAYER RESET ***");
    }
    
    void DebugComplete()
    {
        Debug.Log("=== COMPLETE DEBUG ===");
        Debug.Log($"Position: {transform.position}");
        Debug.Log($"On Ground: {IsGrounded()}");
        Debug.Log($"Transformed: {isTransformed}");
        Debug.Log($"Current Material: {sphereRenderer.material.name}");
        Debug.Log($"Total Jumps: {totalJumps}");
        if (currentHideTarget) Debug.Log($"Hidden near: {currentHideTarget.name}");
        
        if (colorTargets != null)
        {
            Debug.Log($"Target objects found: {colorTargets.Length}");
            foreach (GameObject target in colorTargets)
            {
                if (target != null)
                {
                    float dist = Vector3.Distance(transform.position, target.transform.position);
                    Renderer targetRenderer = target.GetComponent<Renderer>();
                    string targetMaterial = targetRenderer != null ? targetRenderer.material.name : "No material";
                    Debug.Log($"  {target.name}: Distance={dist:F2}, Material={targetMaterial}");
                }
            }
        }
    }
    
    // Public API for NPC
    public bool IsPlayerTransformed() => isTransformed;
    
    public bool IsHiddenForNPC()
    {
        if (!isTransformed || currentHideTarget == null) return false;
        float distance = Vector3.Distance(transform.position, currentHideTarget.transform.position);
        return distance <= detectionRange + 0.05f;
    }
    
    public GameObject GetCurrentHideTarget() => currentHideTarget;
    
    [ContextMenu("Setup Arcade Colliders")]
    public void ForceSetupArcadeColliders()
    {
        SetupArcadeColliders();
    }
    
    [ContextMenu("List Collision Objects")]
    public void ListCollisionObjects()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Debug.Log("=== COLLISION OBJECTS ===");
        
        foreach (GameObject obj in allObjects)
        {
            if (IsArcadeMachine(obj))
            {
                Collider collider = obj.GetComponent<Collider>();
                string colliderInfo = collider != null ? $"✅ {collider.GetType().Name}" : "❌ NO COLLIDER";
                Debug.Log($"{obj.name}: {colliderInfo}");
            }
        }
    }
    
    [ContextMenu("Debug Camouflage System")]
    public void DebugCamouflageSystem()
    {
        Debug.Log("=== CAMOUFLAGE DEBUG ===");
        Debug.Log($"Ball Material: '{GetCleanMaterialName(sphereRenderer.material.name)}'");
        Debug.Log($"Is Transformed: {isTransformed}");
        Debug.Log($"Detection Range: {detectionRange}");
        Debug.Log($"Color Targets Found: {(colorTargets != null ? colorTargets.Length : 0)}");
        
        if (colorTargets != null)
        {
            foreach (GameObject target in colorTargets)
            {
                if (target != null)
                {
                    float dist = Vector3.Distance(transform.position, target.transform.position);
                    Renderer targetRenderer = target.GetComponent<Renderer>();
                    if (targetRenderer != null)
                    {
                        string targetMaterial = GetCleanMaterialName(targetRenderer.material.name);
                        bool isMatch = DoMaterialsMatch(GetCleanMaterialName(sphereRenderer.material.name), targetMaterial);
                        Debug.Log($"  {target.name}: '{targetMaterial}' | Distance: {dist:F2} | Match: {isMatch}");
                    }
                }
            }
        }
    }
}