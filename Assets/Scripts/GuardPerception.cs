using System;
using UnityEngine;

/// Tells the brains (GuardBrain / GuardRangedBrain) who to hunt.
///
/// This used to be a stealth-game sense: a 6-unit detect radius, a 70-degree
/// view cone and a line-of-sight raycast. In a swarm game that combination
/// meant guards mostly never woke up at all —
///   * nothing in the project rotates a guard, so `transform.right` was always
///     world +X and the cone only ever faced right, blinding them to roughly
///     five directions out of six;
///   * SpawnDirector spawns up to `maxDistanceFromPlayer` (12) away, well past
///     the 6-unit detect radius, and an unaware guard doesn't move, so it could
///     never close the gap to become aware;
///   * guards share the Default layer with walls, so in a crowd the raycast hit
///     another guard first and reported "blocked".
///
/// The design is "a dungeon full of things that want you dead", not stealth, so
/// awareness is now unconditional: a guard hunts its target from the instant
/// SpawnDirector hands it one. `aggroRadius` exists only as a safety valve and
/// is deliberately huge — it is a new serialized field, so the stale
/// detectRadius/loseRadius/viewAngle values still baked into Guard.prefab and
/// GuardRanged.prefab are ignored rather than silently overriding this.
public class GuardPerception : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Tooltip("Guards hunt from anywhere on the floor. Only lower this if you want them to go dormant when far away.")]
    [SerializeField] private float aggroRadius = 1000f;

    public event Action<Transform> TargetSpotted;
    public event Action TargetLost;

    public bool HasTarget { get; private set; }

    private void Update()
    {
        if (target == null)
        {
            // The player can be destroyed mid-run; drop the lock so the brain
            // stops chasing a corpse.
            if (HasTarget)
            {
                HasTarget = false;
                TargetLost?.Invoke();
            }
            return;
        }

        bool seen = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude
                    <= aggroRadius * aggroRadius;

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

    private void OnDrawGizmosSelected()
    {
        if (!HasTarget || target == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, target.position);
    }
}
