using UnityEngine;
using System.Linq;

public class PlayerController : MonoBehaviour 
{
    private Rigidbody rb;
    private Renderer sphereRenderer;
    private Animator playerAnimator;
    private int totalJumps = 0;
    private bool hasJumped = false;
    
    [Header("Movement Settings")]
    public float jumpForce = 6f;
    public float moveDistance = 1f; // Distancia por movimiento
    public float moveSpeed = 5f; // Velocidad de animación
    
    [Header("Ground Detection")]
    public LayerMask groundLayer = -1;
    public float groundCheckDistance = 0.1f;
    public float timeToAllowNextJump = 0.2f;
    
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
    private float moveDuration = 0.3f;
    
    void Start() 
    {
        rb = GetComponent<Rigidbody>();
        sphereRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        
        // Verificar que el Rigidbody existe
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("Rigidbody agregado automáticamente");
        }
        
        // Configurar Rigidbody
        rb.mass = 1f;
        rb.linearDamping = 1f;
        rb.freezeRotation = true;
        
        // Desactivar animator AGRESIVAMENTE
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            Debug.Log("Animator desactivado");
        }
        
        // También destruir si existe
        if (animator != null)
        {
            DestroyImmediate(animator);
            Debug.Log("Animator destruido completamente");
        }
        
        // Material inicial
        if (colorMaterials != null && colorMaterials.Length > 0 && colorMaterials[0] != null)
        {
            sphereRenderer.material = colorMaterials[0];
        }
        
        FixGroundPosition();
        FindColorTargets();
        
        Debug.Log("*** PELOTA INICIALIZADA ***");
    }
    
    void FixGroundPosition()
    {
        // ALTURA EXACTA DONDE LA PELOTA TOCA EL SUELO
        float correctHeight = -0.5f; // Altura correcta especificada
        Vector3 correctPosition = new Vector3(0, correctHeight, 0);
        transform.position = correctPosition;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log($"Pelota posicionada en altura correcta: Y = {correctHeight}");
    }
    
    void FindColorTargets()
    {
        Debug.Log("Buscando objetos para camuflaje...");
        
        // Buscar todos los objetos con Renderer directamente
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        System.Collections.Generic.List<GameObject> targets_list = new System.Collections.Generic.List<GameObject>();
        
        foreach (Renderer renderer in allRenderers)
        {
            GameObject obj = renderer.gameObject;
            // Incluir solo cubos y excluir el player y objetos del sistema
            if (obj != gameObject && 
                !obj.name.Contains("Camera") && 
                !obj.name.Contains("Light") && 
                !obj.name.Contains("Directional") &&
                !obj.name.Contains("Plane") &&
                (obj.name.Contains("Cube") || obj.name.Contains("Target")))
            {
                targets_list.Add(obj);
            }
        }
        
        colorTargets = targets_list.ToArray();
        Debug.Log($"Encontrados {colorTargets.Length} objetos para camuflaje");
        
        // Debug de los objetos encontrados
        foreach (GameObject cube in colorTargets)
        {
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                Debug.Log($"Objeto encontrado: {cube.name} - Material: {cubeRenderer.material.name}");
            }
        }
    }
    
    void SetBallMaterial(int materialIndex)
    {
        if (colorMaterials != null && materialIndex >= 0 && materialIndex < colorMaterials.Length && colorMaterials[materialIndex] != null)
        {
            sphereRenderer.material = colorMaterials[materialIndex];
            Debug.Log($"Material cambiado a: {colorMaterials[materialIndex].name}");
        }
    }
    
    void Update() 
    {
        // FORZAR POSICIÓN Y CONSTANTEMENTE (solo si no está saltando)
        if (!isJumping && transform.position.y != -0.5f)
        {
            Vector3 pos = transform.position;
            pos.y = -0.5f;
            transform.position = pos;
        }
        
        // Manejar movimiento fluido
        HandleSmoothMovement();
        
        HandleMovement3D();
        
        // Mantener las funciones existentes pero sin squash/stretch si usamos animaciones
        UpdateGroundStatus();
        CheckWorldBounds();
        CheckShapeMechanics();
        
        // DEBUG
        if (Input.GetKeyDown(KeyCode.I))
        {
            DebugComplete();
        }
        
        // RESET
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPlayer();
        }
        
        // TEST materiales Y MOVIMIENTO CON NÚMEROS
        if (Input.GetKeyDown(KeyCode.Alpha1)) 
        {
            SetBallMaterial(0);
            Debug.Log("TECLA 1 PRESIONADA");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            SetBallMaterial(1);
            Debug.Log("TECLA 2 PRESIONADA");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) 
        {
            SetBallMaterial(2);
            Debug.Log("TECLA 3 PRESIONADA");
        }
        if (Input.GetKeyDown(KeyCode.Alpha4)) 
        {
            SetBallMaterial(3);
            Debug.Log("TECLA 4 PRESIONADA");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5)) 
        {
            SetBallMaterial(4);
            Debug.Log("TECLA 5 PRESIONADA");
        }
        
        // MOVIMIENTO ALTERNATIVO CON TECLAS NUMÉRICAS
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("MOVIMIENTO: IZQUIERDA (Tecla 6)");
            StartSmoothMove(Vector3.left);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Debug.Log("MOVIMIENTO: DERECHA (Tecla 7)");
            StartSmoothMove(Vector3.right);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Debug.Log("MOVIMIENTO: ADELANTE (Tecla 8)");
            StartSmoothMove(Vector3.forward);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Debug.Log("MOVIMIENTO: ATRÁS (Tecla 9)");
            StartSmoothMove(Vector3.back);
        }
        
        // TRANSFORMACIÓN MANUAL
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("*** FORZANDO TRANSFORMACIÓN ***");
            if (!isTransformed)
            {
                TransformToCube();
            }
            else
            {
                TransformToSphere();
            }
        }
    }
    
    void DebugComplete()
    {
        Debug.Log("=== DEBUG COMPLETO ===");
        Debug.Log($"Posición: {transform.position}");
        Debug.Log($"Velocidad: {rb.linearVelocity}");
        Debug.Log($"En suelo: {IsGrounded()}");
        Debug.Log($"Transformado: {isTransformed}");
        Debug.Log($"Material actual: {sphereRenderer.material.name}");
        Debug.Log($"Total saltos: {totalJumps}");
        
        if (colorTargets != null)
        {
            Debug.Log($"Objetos objetivo encontrados: {colorTargets.Length}");
            foreach (GameObject cube in colorTargets)
            {
                if (cube != null)
                {
                    float dist = Vector3.Distance(transform.position, cube.transform.position);
                    Renderer cubeRenderer = cube.GetComponent<Renderer>();
                    string cubeMaterial = cubeRenderer != null ? cubeRenderer.material.name : "Sin material";
                    Debug.Log($"  {cube.name}: Distancia={dist:F2}, Material={cubeMaterial}");
                }
            }
        }
    }
    
    void CheckShapeMechanics()
    {
        if (colorTargets == null || colorTargets.Length == 0) return;
        
        string ballMaterial = sphereRenderer.material.name.Replace(" (Instance)", "").Trim().ToLower();
        bool foundMatch = false;
        
        foreach (GameObject target in colorTargets)
        {
            if (target == null) continue;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance < detectionRange)
            {
                Renderer targetRenderer = target.GetComponent<Renderer>();
                if (targetRenderer == null) continue;
                
                string targetMaterial = targetRenderer.material.name.Replace(" (Instance)", "").Trim().ToLower();
                
                bool isMatch = DoMaterialsMatch(ballMaterial, targetMaterial);
                
                if (isMatch)
                {
                    foundMatch = true;
                    Debug.Log($"*** MATCH ENCONTRADO: '{ballMaterial}' == '{targetMaterial}' ***");
                    
                    if (!isTransformed)
                    {
                        TransformToCube();
                    }
                    break;
                }
                else
                {
                    Debug.Log($"Cerca de {target.name}: '{ballMaterial}' != '{targetMaterial}' (dist: {distance:F2})");
                }
            }
        }
        
        if (!foundMatch && isTransformed)
        {
            TransformToSphere();
        }
    }
    
    bool DoMaterialsMatch(string ballMaterial, string targetMaterial)
    {
        // Comparación directa
        if (ballMaterial == targetMaterial) return true;
        
        // Buscar números en los nombres
        string ballNumber = GetMaterialNumber(ballMaterial);
        string targetNumber = GetMaterialNumber(targetMaterial);
        
        if (!string.IsNullOrEmpty(ballNumber) && !string.IsNullOrEmpty(targetNumber))
        {
            return ballNumber == targetNumber;
        }
        
        // Comparación por colores comunes
        string[] colors = {"red", "blue", "green", "yellow", "white", "azul", "verde", "rojo", "amarillo", "blanco", "example"};
        
        foreach (string color in colors)
        {
            if (ballMaterial.Contains(color) && targetMaterial.Contains(color))
            {
                return true;
            }
        }
        
        return false;
    }
    
    string GetMaterialNumber(string materialName)
    {
        for (int i = materialName.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(materialName[i]))
            {
                return materialName[i].ToString();
            }
        }
        
        // Si contiene "example" pero no tiene número, asumimos que es el 0
        if (materialName.Contains("example") && !materialName.Any(char.IsDigit))
        {
            return "0"; 
        }
        
        return "";
    }
    
    void TransformToCube()
    {
        isTransformed = true;
        transform.localScale = originalScale * 0.7f; 
        CreateTransformEffect(Color.yellow);
        Debug.Log("*** ¡¡¡TRANSFORMADO A CUBO!!! ***");
    }
    
    void TransformToSphere()
    {
        isTransformed = false;
        transform.localScale = originalScale;
        CreateTransformEffect(Color.white);
        Debug.Log("*** VUELTO A ESFERA ***");
    }
    
    void CreateTransformEffect(Color effectColor)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.transform.position = transform.position + Vector3.up * 1f;
        effect.transform.localScale = Vector3.one * 0.8f;
        effect.GetComponent<Renderer>().material.color = effectColor;
        
        // Limpiar collider del efecto
        Collider effectCollider = effect.GetComponent<Collider>();
        if (effectCollider != null) Destroy(effectCollider);
        
        Destroy(effect, 2f);
    }
    
    void CheckWorldBounds()
    {
        Vector3 pos = transform.position;
        
        if (pos.x < -worldSize || pos.x > worldSize || 
            pos.z < -worldSize || pos.z > worldSize || 
            pos.y < fallLimit)
        {
            Debug.Log("Fuera de límites - Reseteando jugador");
            ResetPlayer();
        }
    }
    
    void ResetPlayer()
    {
        totalJumps = 0;
        SetBallMaterial(0);
        
        shouldDeform = false;
        isLandingSquash = false;
        hasJumped = false;
        isTransformed = false;
        
        FixGroundPosition();
        transform.localScale = originalScale;
        
        Debug.Log("*** PELOTA RESETEADA ***");
    }
    
    void HandleMovement3D()
    {
        // No permitir nuevo movimiento si ya se está moviendo
        if (isMoving) return;
        
        // SALTO CON ANIMACIÓN SIMPLE (SIN ANIMATOR)
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            StartSimpleJump();
        }
        
        // MOVIMIENTO (resto del código igual)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
            StartSmoothMove(direction);
            Debug.Log($"INPUT MANAGER: Movimiento en dirección {direction}");
            return;
        }
        
        // KeyCodes backup
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartSmoothMove(Vector3.left);
            Debug.Log("TECLA A DETECTADA - MOVIENDO IZQUIERDA");
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            StartSmoothMove(Vector3.right);
            Debug.Log("TECLA D DETECTADA - MOVIENDO DERECHA");
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            StartSmoothMove(Vector3.forward);
            Debug.Log("TECLA W DETECTADA - MOVIENDO ADELANTE");
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            StartSmoothMove(Vector3.back);
            Debug.Log("TECLA S DETECTADA - MOVIENDO ATRÁS");
            return;
        }
        
        // Flechas backup
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StartSmoothMove(Vector3.left);
            Debug.Log("FLECHA IZQUIERDA DETECTADA");
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartSmoothMove(Vector3.right);
            Debug.Log("FLECHA DERECHA DETECTADA");
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            StartSmoothMove(Vector3.forward);
            Debug.Log("FLECHA ARRIBA DETECTADA");
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            StartSmoothMove(Vector3.back);
            Debug.Log("FLECHA ABAJO DETECTADA");
            return;
        }
    }
    
    void StartSimpleJump()
    {
        if (isJumping) return;
        
        isJumping = true;
        totalJumps++;
        UpdateColor();
        
        Debug.Log($"*** INICIANDO SALTO SIMPLE #{totalJumps} ***");
        
        // Iniciar animación de salto simple
        StartCoroutine(SimpleJumpAnimation());
    }
    
    System.Collections.IEnumerator SimpleJumpAnimation()
    {
        float jumpDuration = 0.8f; // Duración total del salto
        float jumpHeight = 1.5f;   // Altura del salto
        float timer = 0f;
        
        Vector3 startPos = transform.position;
        Vector3 originalScale = transform.localScale;
        
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;
            
            // PARÁBOLA DE SALTO (matemáticas simples)
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            Vector3 jumpPos = startPos;
            jumpPos.y = -0.5f + height; // Base + altura de salto
            
            // SQUASH & STRETCH SIMPLE
            Vector3 scale = originalScale;
            if (progress < 0.2f)
            {
                // Compresión inicial
                float squash = 1f - (progress / 0.2f) * 0.3f;
                scale.y *= squash;
                scale.x *= (1f + (1f - squash) * 0.5f);
                scale.z *= (1f + (1f - squash) * 0.5f);
            }
            else if (progress > 0.8f)
            {
                // Compresión final
                float land = ((progress - 0.8f) / 0.2f) * 0.4f;
                scale.y *= (1f - land);
                scale.x *= (1f + land);
                scale.z *= (1f + land);
            }
            else
            {
                // Estiramiento en el aire
                float stretch = Mathf.Sin((progress - 0.2f) / 0.6f * Mathf.PI) * 0.2f;
                scale.y *= (1f + stretch);
                scale.x *= (1f - stretch * 0.3f);
                scale.z *= (1f - stretch * 0.3f);
            }
            
            // Aplicar posición y escala
            transform.position = jumpPos;
            transform.localScale = scale;
            
            yield return null; // Esperar al siguiente frame
        }
        
        // Finalizar salto
        Vector3 finalPos = startPos;
        finalPos.y = -0.5f; // Volver al suelo
        transform.position = finalPos;
        transform.localScale = originalScale;
        
        isJumping = false;
        Debug.Log("*** SALTO SIMPLE COMPLETADO ***");
    }
    
    void StartSmoothMove(Vector3 direction)
    {
        if (isMoving || isJumping) return;
        
        isMoving = true;
        moveTimer = 0f;
        moveStartPos = transform.position;
        
        Vector3 targetPos = moveStartPos + (direction * moveDistance);
        targetPos.y = 0.25f; // Mantener altura fija
        
        moveTargetPos = targetPos;
        
        Debug.Log($"Iniciando movimiento desde {moveStartPos} hacia {moveTargetPos}");
        
        // Activar animación de anticipación si existe
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Anticipacion");
        }
    }
    
    void HandleSmoothMovement()
    {
        if (!isMoving) return;
        
        moveTimer += Time.deltaTime;
        float progress = moveTimer / moveDuration;
        
        if (progress < 1f)
        {
            // Movimiento suave con curva easing PERO SIN CAMBIAR Y
            float easedProgress = EaseInOutCubic(progress);
            Vector3 newPos = Vector3.Lerp(moveStartPos, moveTargetPos, easedProgress);
            newPos.y = -0.5f; // FORZAR Y = -0.5 SIEMPRE
            transform.position = newPos;
        }
        else
        {
            // Completar movimiento con Y fija
            Vector3 finalPos = moveTargetPos;
            finalPos.y = -0.5f; // FORZAR Y = -0.5
            transform.position = finalPos;
            transform.localScale = originalScale; // Restaurar escala sin lerp
            isMoving = false;
            moveTimer = 0f;
            Debug.Log($"Movimiento completado en posición: {transform.position}");
        }
    }
    
    void HandleMovementDeformation(float progress)
    {
        if (isTransformed) return; // No deformar si está transformado
        
        // Deformación MUY simple y sutil
        Vector3 scale = originalScale;
        
        // Solo un squash muy ligero al inicio y final
        if (progress < 0.1f)
        {
            // Squash inicial muy sutil
            float squashAmount = (progress / 0.1f) * 0.05f; // Muy pequeño
            scale.y *= (1f - squashAmount);
            scale.x *= (1f + squashAmount * 0.5f);
            scale.z *= (1f + squashAmount * 0.5f);
        }
        else if (progress > 0.9f)
        {
            // Squash final muy sutil
            float landAmount = ((progress - 0.9f) / 0.1f) * 0.05f; // Muy pequeño
            scale.y *= (1f - landAmount);
            scale.x *= (1f + landAmount * 0.5f);
            scale.z *= (1f + landAmount * 0.5f);
        }
        // No hay stretch en el medio, solo movimiento natural
        
        // Aplicar escala de forma INMEDIATA (sin lerp para evitar jitter)
        transform.localScale = scale;
    }
    
    float EaseInOutCubic(float t)
    {
        if (t < 0.5f)
            return 4f * t * t * t;
        else
            return 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    void UpdateColor()
    {
        int materialIndex = (totalJumps / 2) % colorMaterials.Length;
        SetBallMaterial(materialIndex);
        
        string[] colorNames = {"BLANCO", "AZUL", "VERDE", "ROJO", "AMARILLO"};
        string colorName = materialIndex < colorNames.Length ? colorNames[materialIndex] : $"COLOR_{materialIndex}";
        
        Debug.Log($"*** CAMBIO A: {colorName} (Saltos: {totalJumps}) ***");
    }
    
    void HandleSquashStretch()
    {
        if (isLandingSquash || isTransformed) return;
        
        bool isGrounded = IsGrounded();
        float velocityY = rb.linearVelocity.y;
        float absVelocityY = Mathf.Abs(velocityY);
        
        if (shouldDeform && !isGrounded && absVelocityY > minVelocityToDeform)
        {
            float velocityRatio = Mathf.Clamp01(absVelocityY / maxVelocityForStretch);
            Vector3 targetScale;
            
            if (velocityY > 0)
            {
                // Subiendo - estirar hacia arriba
                float stretchY = 1f + (velocityRatio * 2.0f);
                float squishXZ = 1f - (velocityRatio * 0.6f);
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,  
                    originalScale.y * stretchY,   
                    originalScale.z * squishXZ    
                );
            }
            else
            {
                // Bajando - estirar hacia abajo
                float stretchY = 1f + (velocityRatio * 2.5f);
                float squishXZ = 1f - (velocityRatio * 0.7f);
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,   
                    originalScale.y * stretchY,   
                    originalScale.z * squishXZ    
                );
            }
            
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * stretchSpeed);
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
            {
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * stretchSpeed);
            }
            else
            {
                transform.localScale = originalScale;
            }
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
                // Fase de squash
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
                // Fase de recuperación
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
            
            if (timeGrounded > timeToAllowNextJump)
            {
                hasJumped = false;
            }
        }
        else
        {
            timeGrounded = 0f;
        }
    }
    
    bool IsGrounded()
    {
        // DETECCIÓN SUPER SIMPLE - siempre en el suelo para debugging
        return true; // Temporalmente siempre en suelo para que pueda saltar
    }
}