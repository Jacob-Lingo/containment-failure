// This program was written with AI assistance

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    
    private Rigidbody2D rb;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Read input every rendered frame
        float x = Input.GetAxisRaw("Horizontal"); // A/D, arrows
        float y = Input.GetAxisRaw("Vertical");   // W/S, arrows
        
        moveInput = new Vector2(x, y).normalized;
    }
    
    private void FixedUpdate()
    {
        // Apply movement on the physics tick, through the rigidbody
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
