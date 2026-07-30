using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GuardMotor : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float arriveRadius = 1.5f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2? seekTarget;  //null = no movement order
    private Vector2 knockbackVelocity;
    private float knockbackEndTime;
    private float slowMultiplier = 1f;
    private float slowEndTime;
    private float lastMoveX;
    private float lastMoveY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;
    }

    public void Seek(Vector2 worldPosition) => seekTarget = worldPosition;

    public void Stop() => seekTarget = null;

    /// Called by SpawnDirector for military/boss tiers (e.g. a slower, heavier Tank).
    public void SetMaxSpeed(float speed) => maxSpeed = speed;

    /// Overrides normal seek/stop for `duration` seconds — used by Lunge Dash
    /// to shove a guard away without the perception/brain layer knowing
    /// anything happened; seeking resumes automatically once it expires.
    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVelocity = velocity;
        knockbackEndTime = Time.time + duration;
    }

    /// Temporary move-speed reduction (e.g. the "Scream Slow" card) — same
    /// auto-expiring-override shape as ApplyKnockback, checked alongside it.
    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowEndTime = Time.time + duration;
    }

    private void FixedUpdate()
    {
        Vector2 velocity;
        if (Time.time < knockbackEndTime)
        {
            velocity = knockbackVelocity;
        }
        else if (seekTarget == null)
        {
            velocity = Vector2.zero;
        }
        else
        {
            Vector2 toTarget = seekTarget.Value - rb.position;
            float distance = toTarget.magnitude;

            // Arrive; ramp speed down inside the radius so the guard
            // doesn't overshoot and orbit the player. Outside it, pure seek.
            float speed = distance < arriveRadius
                ? maxSpeed * (distance / arriveRadius)
                : maxSpeed;

            if (Time.time < slowEndTime) speed *= slowMultiplier;

            velocity = toTarget.normalized * speed;
        }

        rb.linearVelocity = velocity;
        UpdateAnimator(velocity);
    }

    private void UpdateAnimator(Vector2 velocity)
    {
        animator.SetFloat("MoveX", velocity.x);
        animator.SetFloat("MoveY", velocity.y);
        animator.SetFloat("Speed", velocity.sqrMagnitude);

        if (velocity.sqrMagnitude > 0.01f)
        {
            lastMoveX = velocity.x;
            lastMoveY = velocity.y;
        }
        
        animator.SetFloat("LastMoveX", lastMoveX);
        animator.SetFloat("LastMoveY", lastMoveY);
    }
}