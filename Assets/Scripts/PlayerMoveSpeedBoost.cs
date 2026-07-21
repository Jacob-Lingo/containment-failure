using UnityEngine;

/// Applies the evolution system's "Swift" passive without touching
/// PlayerController.cs (owned by Jacob): runs its FixedUpdate after
/// PlayerController's default-order one via DefaultExecutionOrder, then
/// scales whatever velocity PlayerController already set that tick.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMoveSpeedBoost : MonoBehaviour
{
    public float SpeedMultiplier = 1f;

    /// Separate from SpeedMultiplier (Swift's stacking boost) so the "Titan"
    /// card's permanent slowdown combines with it rather than overwriting it.
    public float TitanMultiplier = 1f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        float combined = SpeedMultiplier * TitanMultiplier;
        if (combined == 1f) return;
        rb.linearVelocity *= combined;
    }
}
