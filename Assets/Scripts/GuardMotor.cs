using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GuardMotor : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float arriveRadius = 1.5f;
    
    [Header("Swarm steering")]
    [Tooltip("Guards inside this range of each other push apart. Stops the pack collapsing into one conga line.")]
    [SerializeField] private float separationRadius = 1.1f;
    [SerializeField] private float separationWeight = 1.5f;
    [Tooltip("Sideways drift once close to the target, so the pack wraps around instead of trailing behind.")]
    [SerializeField] private float encircleWeight = 0.85f;
    [Tooltip("Distance at which encircling fully takes over from straight pursuit.")]
    [SerializeField] private float encircleRadius = 4f;

    // Shared so 45 guards don't each allocate a results array every physics tick.
    private static readonly Collider2D[] NeighbourBuffer = new Collider2D[12];

    private Rigidbody2D rb;
    private Vector2? seekTarget;  //null = no movement order

    /// +1 or -1, fixed per guard: half the pack wraps clockwise and half
    /// anticlockwise, which is what closes a circle instead of forming a
    /// single orbiting arc.
    private float orbitSign;
    private Vector2 knockbackVelocity;
    private float knockbackEndTime;
    private float slowMultiplier = 1f;
    private float slowEndTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        orbitSign = Random.value < 0.5f ? -1f : 1f;
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

        Vector2 heading = toTarget.normalized;

        // Straight pursuit makes the whole pack trail the player in a line that
        // is trivial to outrun. Two steering forces fix that:
        //   encircle  — the closer a guard gets, the more it drifts sideways,
        //               so the pack wraps around the player instead of queueing
        //               behind them;
        //   separate  — guards push out of each other's personal space, which
        //               spreads that wrap into an even ring rather than a clump.
        heading += Encircle(heading, distance) * encircleWeight;
        heading += Separation() * separationWeight;

        if (heading.sqrMagnitude > 0.0001f) heading.Normalize();

        rb.linearVelocity = heading * speed;

        UpdateFacing();
    }

    /// Sideways component, perpendicular to the approach. Zero at range (so
    /// guards still close the gap) and strongest up close (so they slide around
    /// the target rather than shoving into its back).
    private Vector2 Encircle(Vector2 heading, float distance)
    {
        if (distance > encircleRadius) return Vector2.zero;

        float closeness = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, encircleRadius));
        Vector2 tangent = new Vector2(-heading.y, heading.x) * orbitSign;
        return tangent * closeness;
    }

    /// Boids separation over nearby guards. Weighted by how deep the overlap
    /// is, so touching guards push hard and distant ones barely at all.
    private Vector2 Separation()
    {
        int found = Physics2D.OverlapCircleNonAlloc(rb.position, separationRadius, NeighbourBuffer);
        Vector2 push = Vector2.zero;

        for (int i = 0; i < found; i++)
        {
            Collider2D other = NeighbourBuffer[i];
            if (other == null || other.attachedRigidbody == rb) continue;
            if (!other.TryGetComponent<GuardMotor>(out _)) continue; // ignore walls, pickups, the player

            Vector2 away = rb.position - (Vector2)other.transform.position;
            float distance = away.magnitude;
            if (distance < 0.0001f)
            {
                // Perfectly overlapping guards have no away-vector to use;
                // nudge them apart in an arbitrary but stable direction.
                push += new Vector2(orbitSign, 0f);
                continue;
            }

            push += away / distance * (1f - distance / separationRadius);
        }

        return push;
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

