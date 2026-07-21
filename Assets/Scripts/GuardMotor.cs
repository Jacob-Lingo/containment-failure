using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GuardMotor : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float arriveRadius = 1.5f;
    
    private Rigidbody2D rb;
    private Vector2? seekTarget;  //null = no movement order
    private Vector2 knockbackVelocity;
    private float knockbackEndTime;
    private float slowMultiplier = 1f;
    private float slowEndTime;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

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
        if (Time.time < knockbackEndTime)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        if (seekTarget == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = seekTarget.Value - rb.position;
        float distance = toTarget.magnitude;

        // Arrive; ramp speed down inside the radius so the guard
        // doesn't overshoot and orbit the player. Outside it, pure seek.
        float speed = distance < arriveRadius
            ? maxSpeed * (distance / arriveRadius)
            : maxSpeed;

        if (Time.time < slowEndTime) speed *= slowMultiplier;

        rb.linearVelocity = toTarget.normalized * speed;
        
        UpdateFacing();
    }

    [Header("Facing")] 
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float faceVelocityThreshold = 0.05f;

    private void UpdateFacing()
    {
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < faceVelocityThreshold * faceVelocityThreshold)
            return;

        float targetAngle = Mathf.Atan2(v.y, v.x) *Mathf.Rad2Deg;
        float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newAngle);
    }
    
}

