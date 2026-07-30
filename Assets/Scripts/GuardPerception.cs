using System;
using UnityEngine;

public class GuardPerception : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // Player

    [Header("Detection Ranges")]
    [SerializeField] private float closeDetectRadius = 3f;
    [SerializeField] private float forwardDetectRadius = 8f;
    [SerializeField] private float loseRadius = 10f; // Hysteresis: lose > detect

    [Header("Detection Angle")]
    [SerializeField, Range(1f, 360f)] private float viewAngle = 90f; // Full cone angle

    [Header("Line of Sight")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    public event Action<Transform> TargetSpotted;
    public event Action TargetLost;

    public bool HasTarget { get; private set; }

    private Animator animator;

    private Vector2 Forward
    {
        get
        {
            if (animator == null) return transform.right; // Fallback for editor
            
            float lastMoveX = animator.GetFloat("LastMoveX");
            float lastMoveY = animator.GetFloat("LastMoveY");

            // If the guard has never moved, default to down.
            if (Mathf.Approximately(lastMoveX, 0f) && Mathf.Approximately(lastMoveY, 0f))
            {
                return Vector2.down;
            }
            
            return new Vector2(lastMoveX, lastMoveY).normalized;
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (target == null) return;

        bool seen = CanSeeTarget();

        if (!HasTarget && seen)
        {
            HasTarget = true;
            TargetSpotted?.Invoke(target);
        }
        else if (HasTarget && !seen)
        {
            HasTarget = false;
            TargetLost?.Invoke();
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    private bool CanSeeTarget()
    {
        if (target == null) return false;

        Vector2 origin = transform.position;
        Vector2 toTarget = (Vector2)target.position - origin;
        float distance = toTarget.magnitude;

        // Condition to lose target: must be already tracking and either out of lose radius OR line of sight is broken.
        if (HasTarget)
        {
            if (distance > loseRadius || !HasLineOfSight(origin, toTarget))
            {
                return false;
            }
            return true; // Target is kept.
        }

        // Conditions to acquire a new target (not currently tracking).
        
        // If outside the maximum possible detection range, no need to check further.
        if (distance > forwardDetectRadius) return false;

        // Line of sight is mandatory for acquiring a new target.
        if (!HasLineOfSight(origin, toTarget)) return false;

        // 1. Close-range omni-directional check.
        if (distance <= closeDetectRadius)
        {
            return true; // Acquired via close range.
        }

        // 2. Forward-range directional check.
        // We already know distance is > closeDetectRadius and <= forwardDetectRadius.
        if (Vector2.Angle(Forward, toTarget) <= viewAngle * 0.5f)
        {
            return true; // Acquired via forward cone.
        }

        return false; // Target not acquired.
    }

    private bool HasLineOfSight(Vector2 origin, Vector2 toTarget)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, toTarget.normalized, toTarget.magnitude, obstacleMask);
        foreach (RaycastHit2D hit in hits)
        {
            Transform t = hit.transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (t == target || t.IsChildOf(target)) continue;
            return false; // An obstacle is in the way.
        }
        return true; // Clear line of sight.
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;

        // Draw detection radii
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(origin, closeDetectRadius);
        Gizmos.DrawWireSphere(origin, forwardDetectRadius);

        // Draw lose radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, loseRadius);

        // Draw view cone
        Gizmos.color = HasTarget ? Color.red : Color.cyan;
        float halfAngle = viewAngle * 0.5f;
        Vector3 forward = Forward;
        Vector3 rightEdge = Quaternion.Euler(0, 0, halfAngle) * forward * forwardDetectRadius;
        Vector3 leftEdge = Quaternion.Euler(0, 0, -halfAngle) * forward * forwardDetectRadius;
        Gizmos.DrawLine(origin, origin + rightEdge);
        Gizmos.DrawLine(origin, origin + leftEdge);

        // Draw line to target if seen
        if (HasTarget && target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, target.position);
        }
    }
}