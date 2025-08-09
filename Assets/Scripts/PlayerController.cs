using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    private Rigidbody rb;
    private Renderer sphereRenderer;
    private int totalJumps = 0;
    private bool hasJumped = false;
    
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float moveSpeed = 5f;
    
    [Header("Ground Detection")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.1f;
    public float timeToAllowNextJump = 0.2f;
    
    [Header("Squash & Stretch Settings")]
    public float maxVelocityForStretch = 8f;
    public float stretchSpeed = 8f;
    public float minVelocityToDeform = 0.5f;
    public float landingSquashDuration = 0.4f;
    
    [Header("Color Settings")]
    public Color[] jumpColors = new Color[] 
    {
        Color.white,    
        Color.blue,     
        Color.green,    
        Color.yellow,   
        Color.red       
    };
    
    [Header("Color Pattern Targets")]
    public GameObject[] colorTargets;
    
    private Vector3 originalScale;
    private float timeGrounded = 0f;
    private bool shouldDeform = false;
    private bool isLandingSquash = false;
    private float landingSquashTimer = 0f;
    
    void Start() 
    {
        rb = GetComponent<Rigidbody>();
        sphereRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            Debug.Log("Animator desactivado");
        }
        
        if (sphereRenderer != null && jumpColors.Length > 0)
        {
            sphereRenderer.material.color = jumpColors[0];
        }
        
        if (colorTargets == null || colorTargets.Length == 0)
        {
            FindColorTargets();
        }
        
        transform.localScale = originalScale;
    }
    
    void Update() 
    {
        HandleMovement3D();
        HandleSquashStretch();
        UpdateGroundStatus();
        UpdateLandingSquash();
        CheckColorMatching();
    }
    
    void HandleMovement3D()
    {
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical = -1f;
        
        Vector3 movement = new Vector3(horizontal * moveSpeed, rb.linearVelocity.y, vertical * moveSpeed);
        rb.linearVelocity = movement;
        
        if (Input.GetKeyDown(KeyCode.Space) && 
            IsGrounded() && 
            !hasJumped && 
            timeGrounded > timeToAllowNextJump &&
            Mathf.Abs(rb.linearVelocity.y) < 0.1f &&
            !isLandingSquash)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            hasJumped = true;
            timeGrounded = 0f;
            shouldDeform = true;
            
            totalJumps++;
            UpdateColor();
            
            Debug.Log($"SALTO #{totalJumps} - Deformación VERTICAL ACTIVADA");
        }
    }
    
    void UpdateLandingSquash()
    {
        if (isLandingSquash)
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
                Debug.Log($"Squash aterrizaje: Y={landSquash.y:F2} (aplastado)");
            }
            else 
            {
                float returnProgress = (progress - 0.3f) / 0.7f;
                Vector3 currentSquash = new Vector3(
                    originalScale.x * 1.8f,
                    originalScale.y * 0.3f,
                    originalScale.z * 1.8f
                );
                transform.localScale = Vector3.Lerp(currentSquash, originalScale, returnProgress);
            }
            
            if (progress >= 1f)
            {
                isLandingSquash = false;
                landingSquashTimer = 0f;
                transform.localScale = originalScale;
                Debug.Log("Squash vertical completado");
            }
        }
    }
    
    void UpdateColor()
    {
        if (sphereRenderer == null || jumpColors.Length == 0) return;
        
        int colorIndex = (totalJumps / 2) % jumpColors.Length;
        sphereRenderer.material.color = jumpColors[colorIndex];
    }
    
    Color GetCurrentColor()
    {
        if (sphereRenderer != null)
            return sphereRenderer.material.color;
        return Color.white;
    }
    
    void CheckColorMatching()
    {
        if (colorTargets == null) return;
        
        Color currentColor = GetCurrentColor();
        
        foreach (GameObject target in colorTargets)
        {
            if (target == null) continue;
            
            Renderer targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer == null) continue;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance < 2f)
            {
                Color targetColor = targetRenderer.material.color;
                
                if (ColorsMatch(currentColor, targetColor))
                {
                    OnColorMatch(target);
                }
                else
                {
                    OnColorMismatch(target);
                }
            }
        }
    }
    
    bool ColorsMatch(Color color1, Color color2)
    {
        float tolerance = 0.1f;
        return Mathf.Abs(color1.r - color2.r) < tolerance &&
               Mathf.Abs(color1.g - color2.g) < tolerance &&
               Mathf.Abs(color1.b - color2.b) < tolerance;
    }
    
    void OnColorMatch(GameObject target)
    {
        Debug.Log($"¡COLOR CORRECTO! Pelota {GetCurrentColor()} coincide con objetivo {target.name}");
        CreateMatchEffect(target.transform.position);
    }
    
    void OnColorMismatch(GameObject target)
    {
        Renderer targetRenderer = target.GetComponent<Renderer>();
        Debug.Log($"Color incorrecto. Pelota: {GetCurrentColor()}, Objetivo: {targetRenderer.material.color}");
    }
    
    void CreateMatchEffect(Vector3 position)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.transform.position = position + Vector3.up;
        effect.transform.localScale = Vector3.one * 0.5f;
        effect.GetComponent<Renderer>().material.color = Color.yellow;
        
        Destroy(effect, 1f);
    }
    
    void FindColorTargets()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("ColorTarget");
        colorTargets = targets;
        
        Debug.Log($"Encontrados {targets.Length} objetivos de color");
    }
    
    void HandleSquashStretch()
    {
        if (isLandingSquash) return;
        
        bool isGrounded = IsGrounded();
        float velocityY = rb.linearVelocity.y;
        float absVelocityY = Mathf.Abs(velocityY);
        
        if (shouldDeform && !isGrounded && absVelocityY > minVelocityToDeform)
        {
            float velocityRatio = Mathf.Clamp01(absVelocityY / maxVelocityForStretch);
            
            Vector3 targetScale;
            
            if (velocityY > 0) 
            {
                float stretchY = 1f + (velocityRatio * 1.5f);  
                float squishXZ = 1f - (velocityRatio * 0.4f);   
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,  
                    originalScale.y * stretchY,   
                    originalScale.z * squishXZ    
                );
                
                Debug.Log($"SUBIENDO VERTICAL - Alto: {stretchY:F2}, Delgado: {squishXZ:F2}");
            }
            else // Cayendo - Se estira AÚN MÁS verticalmente
            {
                float stretchY = 1f + (velocityRatio * 2.0f);  
                float squishXZ = 1f - (velocityRatio * 0.5f);  
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,   
                    originalScale.y * stretchY,   
                    originalScale.z * squishXZ    
                );
                
                Debug.Log($"CAYENDO VERTICAL - Alto: {stretchY:F2}, Delgado: {squishXZ:F2}");
            }
            
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * stretchSpeed);
        }
        else if (shouldDeform && isGrounded) 
        {
            shouldDeform = false;
            isLandingSquash = true;
            landingSquashTimer = 0f;
            
            Debug.Log("Aterrizó - Iniciando SQUASH VERTICAL (aplastado)");
        }
        else if (!shouldDeform && !isLandingSquash)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * stretchSpeed);
            
            if (Vector3.Distance(transform.localScale, originalScale) < 0.01f)
            {
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
        
        return Physics.CheckSphere(
            spherePosition - Vector3.up * (sphereRadius + groundCheckDistance), 
            sphereRadius * 0.9f, 
            groundLayer
        );
    }
    
    public void ResetJumpCounter()
    {
        totalJumps = 0;
        UpdateColor();
    }
}