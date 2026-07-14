using System;
using UnityEngine;

public class GuardPerception : MonoBehaviour
{
    [SerializeField] private Transform target;          // Player — assigned in Inspector for now
    [SerializeField] private float detectRadius = 6f;
    [SerializeField] private float loseRadius = 8f;     // hysteresis: lose > detect
    [SerializeField, Range(1f, 360f)] private float viewAngle = 70f;  // full cone angle, centred on facing
    [SerializeField] private LayerMask obstacleMask = ~0;             // what blocks line of sight

    public event Action<Transform> TargetSpotted;
    public event Action TargetLost;

    public bool HasTarget { get; private set; }

    // Nothing rotates the guard yet; sprite faces right, so transform.right
    // is forward. Tracks rotation automatically if a facing system lands.
    private Vector2 Forward => transform.right;

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
        Vector2 origin = transform.position;
        Vector2 toTarget = (Vector2)target.position - origin;

        // hysteresis applies to range only: once spotted, sight extends to loseRadius
        float radius = HasTarget ? loseRadius : detectRadius;
        if (toTarget.sqrMagnitude > radius * radius) return false;

        if (Vector2.Angle(Forward, toTarget) > viewAngle * 0.5f) return false;

        return HasLineOfSight(origin, toTarget);
    }

    private bool HasLineOfSight(Vector2 origin, Vector2 toTarget)
    {
        // Guards, walls and player all share the Default layer, so the first
        // hit is the guard's own collider. Walk all hits and ignore ourselves
        // and the target; anything else in between blocks sight.
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, toTarget.normalized, toTarget.magnitude, obstacleMask);
        foreach (RaycastHit2D hit in hits)
        {
            Transform t = hit.transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (t == target || t.IsChildOf(target)) continue;
            return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasTarget ? Color.red : Color.white;

        Vector3 origin = transform.position;
        Gizmos.DrawWireSphere(origin, detectRadius);

        float half = viewAngle * 0.5f;
        Vector3 edge = (Vector3)(Forward * detectRadius);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, half) * edge);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, -half) * edge);

        if (HasTarget && target != null)
            Gizmos.DrawLine(origin, target.position);
    }
}
