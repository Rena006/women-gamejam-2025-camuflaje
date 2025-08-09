using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    private Animator animator;
    private Rigidbody rb;
    private bool wasGrounded = false;
    
    [Header("Movement Settings")]
    public float jumpForce = 10f;
    public float moveSpeed = 5f;
    
    [Header("Ground Detection")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.1f;
    
    void Start() 
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }
    
    void Update() 
    {
        HandleMovement();
        CheckGroundImpact();
    }
    
    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(horizontal * moveSpeed, rb.velocity.y, 0);
        rb.velocity = movement;
        
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    
    void CheckGroundImpact() 
    {
        bool isGrounded = IsGrounded();
        
        if (isGrounded && !wasGrounded && rb.velocity.y < -1f) 
        {
            if (animator != null)
            {
                animator.Play("SquashStretch");
            }
        }
        
        wasGrounded = isGrounded;
    }
    
    bool IsGrounded()
    {
        Vector3 spherePosition = transform.position;
        float sphereRadius = GetComponent<SphereCollider>().radius * transform.localScale.y;
        
        return Physics.CheckSphere(
            spherePosition - Vector3.up * (sphereRadius + groundCheckDistance), 
            sphereRadius * 0.9f, 
            groundLayer
        );
    }
    
    void OnCollisionEnter(Collision collision) 
    {
        if (Vector3.Dot(collision.contacts[0].normal, Vector3.up) > 0.7f)
        {
            if (animator != null && rb.velocity.y < -1f)
            {
                animator.Play("SquashStretch");
            }
        }
    }
}