using UnityEngine;

/// Dash ability, unlocked via the evolution "Dash" card — Left Shift bursts
/// the player forward for a short window once unlocked. Reads its own input
/// and drives the Rigidbody2D directly so it never has to touch
/// PlayerController.cs; runs after it (DefaultExecutionOrder) so the dash
/// velocity isn't immediately stomped by PlayerController's own FixedUpdate.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    private Rigidbody2D rb;
    private EvolutionSystem evolution;
    private Vector2 lastMoveDirection = Vector2.right;
    private Vector2 dashDirection;
    private float dashEndTime;
    private float nextDashTime;

    public bool IsDashing { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        evolution = GetComponent<EvolutionSystem>();
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(x, y);
        if (input.sqrMagnitude > 0.0001f)
            lastMoveDirection = input.normalized;

        bool unlocked = evolution == null || evolution.UnlockedDash;

        if (unlocked && Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime)
        {
            IsDashing = true;
            dashDirection = lastMoveDirection;
            dashEndTime = Time.time + dashDuration;
            nextDashTime = Time.time + dashCooldown;
        }

        if (IsDashing && Time.time >= dashEndTime)
            IsDashing = false;
    }

    private void FixedUpdate()
    {
        if (!IsDashing) return;
        rb.linearVelocity = dashDirection * dashSpeed;
    }
}
