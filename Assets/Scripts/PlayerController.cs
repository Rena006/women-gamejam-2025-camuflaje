using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    private Rigidbody rb;
    private Renderer sphereRenderer;
    private int totalJumps = 0;
    private bool isJumping = false; 
    private float jumpTime = 0f;    
    
    [Header("Movement Settings")]
    public float jumpForce = 8f;
    public float moveSpeed = 5f;
    
    [Header("Ground Detection")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.1f;
    
    [Header("Squash & Stretch Settings")]
    public float jumpDeformDuration = 0.5f; // Duración de la deformación por salto
    public float maxVelocityForStretch = 10f;
    public float stretchSpeed = 8f;
    
    [Header("Color Settings")]
    public Color[] jumpColors = new Color[] 
    {
        Color.white,    // Color inicial
        Color.blue,     // Después de 2 saltos
        Color.green,    // Después de 4 saltos
        Color.yellow,   // Después de 6 saltos
        Color.red       // Después de 8 saltos
    };
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool canJump = true;
    
    void Start() 
    {
        rb = GetComponent<Rigidbody>();
        sphereRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        if (sphereRenderer != null && jumpColors.Length > 0)
        {
            sphereRenderer.material.color = jumpColors[0];
        }
    }
    
    void Update() 
    {
        HandleMovement();
        HandleSquashStretch();
        UpdateJumpStatus();
        UpdateJumpTimer();
    }
    
    void HandleMovement()
    {
        // Movimiento horizontal
        float horizontal = 0f;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;
        
        Vector3 movement = new Vector3(horizontal * moveSpeed, rb.velocity.y, 0);
        rb.velocity = movement;
        
        if (Input.GetKeyDown(KeyCode.Space) && canJump && IsGrounded())
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            isJumping = true;
            jumpTime = 0f;
            
            totalJumps++;
            UpdateColor();
            canJump = false;
        }
    }
    
    void UpdateJumpTimer()
    {
        if (isJumping)
        {
            jumpTime += Time.deltaTime;
            
            if (jumpTime >= jumpDeformDuration)
            {
                isJumping = false;
            }
        }
    }
    
    void UpdateColor()
    {
        if (sphereRenderer == null || jumpColors.Length == 0) return;
        
        int colorIndex = (totalJumps / 2) % jumpColors.Length;
        sphereRenderer.material.color = jumpColors[colorIndex];
        
        Debug.Log($"Saltos totales: {totalJumps}, Color índice: {colorIndex}");
    }
    
    void HandleSquashStretch()
    {
        bool isGrounded = IsGrounded();
        
        if (isJumping && !isGrounded)
        {
            float velocityY = rb.velocity.y;
            float velocityRatio = Mathf.Clamp01(Mathf.Abs(velocityY) / maxVelocityForStretch);
            
            if (velocityY > 0) 
            {
                float stretchY = 1f + (velocityRatio * 0.4f);
                float squishXZ = 1f - (velocityRatio * 0.2f);
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,
                    originalScale.y * stretchY,
                    originalScale.z * squishXZ
                );
            }
            else // Cayendo
            {
                float stretchY = 1f + (velocityRatio * 0.5f);
                float squishXZ = 1f - (velocityRatio * 0.25f);
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,
                    originalScale.y * stretchY,
                    originalScale.z * squishXZ
                );
            }
        }
        else 
        {
            targetScale = originalScale;
        }
        
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * stretchSpeed);
    }
    
    void UpdateJumpStatus()
    {
        bool isGrounded = IsGrounded();
        
        if (isGrounded)
        {
            canJump = true;
            if (isJumping)
            {
                isJumping = false;
            }
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