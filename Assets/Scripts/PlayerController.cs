// This program was written with AI assistance

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite spriteDown;
    [SerializeField] private Sprite spriteUp;
    [SerializeField] private Sprite spriteRight;
    [SerializeField] private Sprite spriteLeft;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        
    }

    void Update()
    {
        // Don't read input if the game is frozen (e.g. paused or level-up screen)
        if (GameState.IsFrozen) 
        {
            moveInput = Vector2.zero;
            return;
        }

        // Read input every rendered frame
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        
        moveInput = new Vector2(x, y).normalized;

        UpdateDirectionalSprite();
    }
    
    private void FixedUpdate()
    {
        if (GameState.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Apply movement on the physics tick, through the rigidbody
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void UpdateDirectionalSprite()
    {
        if (moveInput == Vector2.zero) return;

        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            if (moveInput.x > 0)
            {
                if (spriteRight != null) spriteRenderer.sprite = spriteRight;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (spriteLeft != null)
                {
                    spriteRenderer.sprite = spriteLeft;
                    spriteRenderer.flipX = false;
                }
                else
                {
                    if (spriteRight != null)
                    {
                        spriteRenderer.sprite = spriteRight;
                        spriteRenderer.flipX = true;
                    }
                }
            }
        }
        else
        {
            spriteRenderer.flipX = false;

            if (moveInput.y > 0)
            {
                if (spriteUp != null) spriteRenderer.sprite = spriteUp;
            }
            else
            {
                if (spriteDown != null) spriteRenderer.sprite = spriteDown;
            }
        }
    }
}