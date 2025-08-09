using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    private Rigidbody rb;
    private int jumpCount = 0;
    
    [Header("Movement Settings")]
    public float jumpForce = 10f;
    public float moveSpeed = 5f;
    public int maxJumps = 2;
    
    [Header("Ground Detection")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.1f;
    
    [Header("Squash & Stretch Settings")]
    public float maxVelocityForStretch = 15f;  
    public float stretchSpeed = 8f;           
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    
    void Start() 
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }
    
    void Update() 
    {
        HandleMovement();
        HandleSquashStretch();
        UpdateJumpCount();
    }
    
    void HandleMovement()
    {
        // Movimiento horizontal
        float horizontal = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(horizontal * moveSpeed, rb.velocity.y, 0);
        rb.velocity = movement;
        
        // Salto con doble salto
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCount++;
        }
    }
    
    void HandleSquashStretch()
    {
        bool isGrounded = IsGrounded();
        float velocityY = rb.velocity.y;
        
        if (!isGrounded) 
        {
            float velocityRatio = Mathf.Clamp01(Mathf.Abs(velocityY) / maxVelocityForStretch);
            
            if (velocityY > 0.5f) 
            {
                float stretchY = 1f + (velocityRatio * 0.6f);  
                float squishXZ = 1f - (velocityRatio * 0.25f); 
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,
                    originalScale.y * stretchY,
                    originalScale.z * squishXZ
                );
            }
            else if (velocityY < -0.5f) 
            {
                
                float stretchY = 1f + (velocityRatio * 0.7f);  
                float squishXZ = 1f - (velocityRatio * 0.3f);  
                
                targetScale = new Vector3(
                    originalScale.x * squishXZ,
                    originalScale.y * stretchY,
                    originalScale.z * squishXZ
                );
            }
            else 
            {
                targetScale = Vector3.Lerp(targetScale, originalScale, Time.deltaTime * stretchSpeed);
            }
        }
        else 
        {
            targetScale = originalScale;
        }
        
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * stretchSpeed);
    }
    
    void UpdateJumpCount()
    {
        if (IsGrounded())
        {
            jumpCount = 0;
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
}