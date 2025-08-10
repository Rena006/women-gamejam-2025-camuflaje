using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    private Rigidbody rb;
    private Renderer sphereRenderer;
    private int totalJumps = 0;
    private bool hasJumped = false;
    
    [Header("Movement Settings")]
    public float jumpForce = 6f;
    public float moveSpeed = 5f;
    
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
    
    void Start() 
    {
        rb = GetComponent<Rigidbody>();
        sphereRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        
        // Desactivar animator
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
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
        
        transform.position = new Vector3(0, 0.1f, 0);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Debug.Log($"Pelota posicionada en Y = 0.5");
    }
    
    void FindColorTargets()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("ColorTarget");
        if (targets.Length > 0)
        {
            colorTargets = targets;
            Debug.Log($"Encontrados {targets.Length} cubos objetivo");
            
            foreach (GameObject cube in targets)
            {
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                if (cubeRenderer != null)
                {
                    Debug.Log($"Cubo {cube.name}: Material = {cubeRenderer.material.name}");
                }
            }
        }
        else
        {
            Debug.LogError("No se encontraron cubos con tag ColorTarget");
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
        HandleMovement3D();
        
        if (!isTransformed)
        {
            HandleSquashStretch();
        }
        
        UpdateGroundStatus();
        UpdateLandingSquash();
        CheckWorldBounds();
        CheckShapeMechanics();
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            DebugComplete();
        }
        
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                transform.position += Vector3.down * 0.05f;
                Debug.Log($"Pelota bajada a Y: {transform.position.y:F3}");
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                transform.position += Vector3.up * 0.05f;
                Debug.Log($"Pelota subida a Y: {transform.position.y:F3}");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPlayer();
        }
        
        // TEST materiales
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetBallMaterial(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetBallMaterial(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetBallMaterial(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetBallMaterial(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetBallMaterial(4);
        
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
        Debug.Log($"En suelo: {IsGrounded()}");
        Debug.Log($"Transformado: {isTransformed}");
        Debug.Log($"Material actual: {sphereRenderer.material.name}");
        
        if (colorTargets != null)
        {
            foreach (GameObject cube in colorTargets)
            {
                if (cube != null)
                {
                    float dist = Vector3.Distance(transform.position, cube.transform.position);
                    string cubeMaterial = cube.GetComponent<Renderer>().material.name;
                    Debug.Log($"{cube.name}: Distancia={dist:F2}, Material={cubeMaterial}");
                }
            }
        }
    }
    
    void CheckShapeMechanics()
    {
        if (colorTargets == null || colorTargets.Length == 0) return;
        
        string ballMaterial = sphereRenderer.material.name;
        bool foundMatch = false;
        
        foreach (GameObject target in colorTargets)
        {
            if (target == null) continue;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance < detectionRange)
            {
                string targetMaterial = target.GetComponent<Renderer>().material.name;
                
                bool isMatch = ballMaterial.Contains("Example") && targetMaterial.Contains("Example");
                
                if (isMatch)
                {
                    string ballNumber = GetMaterialNumber(ballMaterial);
                    string targetNumber = GetMaterialNumber(targetMaterial);
                    
                    if (ballNumber == targetNumber && ballNumber != "")
                    {
                        foundMatch = true;
                        Debug.Log($"*** MATCH ENCONTRADO: {ballMaterial} == {targetMaterial} ***");
                        
                        if (!isTransformed)
                        {
                            TransformToCube();
                        }
                        break;
                    }
                }
            }
        }
        
        if (!foundMatch && isTransformed)
        {
            TransformToSphere();
        }
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
        
        if (materialName.Contains("Example") && !materialName.Contains("1") && !materialName.Contains("2") && !materialName.Contains("3") && !materialName.Contains("4"))
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
        
        Destroy(effect.GetComponent<Collider>());
        
        Destroy(effect, 2f);
    }
    
    void CheckWorldBounds()
    {
        Vector3 pos = transform.position;
        
        if (pos.x < -worldSize || pos.x > worldSize || 
            pos.z < -worldSize || pos.z > worldSize || 
            pos.y < fallLimit)
        {
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
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = -1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical = -1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical = 1f;
        
        Vector3 movement = new Vector3(horizontal * moveSpeed, rb.linearVelocity.y, vertical * moveSpeed);
        rb.linearVelocity = movement;
        
        // SALTO
        if (Input.GetKeyDown(KeyCode.Space) && 
            IsGrounded() && 
            !hasJumped && 
            timeGrounded > timeToAllowNextJump &&
            Mathf.Abs(rb.linearVelocity.y) < 0.5f &&
            !isLandingSquash)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            hasJumped = true;
            timeGrounded = 0f;
            
            if (!isTransformed)
            {
                shouldDeform = true;
            }
            
            totalJumps++;
            UpdateColor();
            
            Debug.Log($"*** SALTO #{totalJumps} EJECUTADO ***");
        }
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
        Vector3 spherePosition = transform.position;
        float sphereRadius = GetComponent<SphereCollider>().radius;
        
        // DETECCIÓN SIMPLE
        bool grounded = Physics.CheckSphere(
            spherePosition - Vector3.up * (sphereRadius * 0.8f), 
            sphereRadius * 0.3f, 
            groundLayer
        );
        
        return grounded;
    }
}