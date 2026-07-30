// This program was written with AI assistance

using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private float lastMoveX;
    private float lastMoveY;
    private bool isKnockedBack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isKnockedBack) return;

        // Don't read input if the game is frozen (e.g. paused or level-up screen)
        if (GameState.IsFrozen)
        {
            movement = Vector2.zero;
            return;
        }

        // Read input every rendered frame
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("MoveX", movement.x);
        animator.SetFloat("MoveY", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);

        if (movement.sqrMagnitude > 0)
        {
            lastMoveX = movement.x;
            lastMoveY = movement.y;
        }
        animator.SetFloat("LastMoveX", lastMoveX);
        animator.SetFloat("LastMoveY", lastMoveY);
    }
    
    private void FixedUpdate()
    {
        if (isKnockedBack) return;

        if (GameState.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Apply movement on the physics tick, through the rigidbody
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    public void ApplyKnockback(Vector2 force, float duration)
    {
        StartCoroutine(KnockbackRoutine(force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 force, float duration)
    {
        isKnockedBack = true;
        rb.AddForce(force, ForceMode2D.Impulse);
        yield return new WaitForSeconds(duration);
        isKnockedBack = false;
    }
}