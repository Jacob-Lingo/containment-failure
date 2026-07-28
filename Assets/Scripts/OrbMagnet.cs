using UnityEngine;

/// Pulls a pickup toward the player once they're close enough, so collecting
/// is a reward rather than a chore of walking onto a 0.5-unit collider. Shared
/// by both pickup types (ExpOrb, Coin) — they add it in their Spawn().
///
/// Deliberately moves via transform: these are trigger-only, rigidbody-less
/// runtime objects, not physics bodies, so the repo's Rigidbody2D movement
/// rule doesn't apply.
public class OrbMagnet : MonoBehaviour
{
    private const float BaseRadius = 3f;
    private const float Speed = 9f;

    private Transform player;
    private float searchTimer;

    /// Widened by the Static Charge meta upgrade. Read per-frame rather than
    /// cached so an in-place restart after a shop purchase picks it up.
    private static float Radius => BaseRadius + MetaProgression.BonusMagnetRadius;

    private void Update()
    {
        if (GameState.IsFrozen) return;

        if (player == null)
        {
            // Re-find rather than caching once: the player can be destroyed and
            // the run restarted in place (PauseMenu). Throttled — this runs on
            // every loose orb in the level.
            searchTimer -= Time.deltaTime;
            if (searchTimer > 0f) return;

            searchTimer = 0.25f;
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found == null) return;
            player = found.transform;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > Radius) return;

        // Accelerate as it closes, so the last stretch snaps in.
        float pull = Speed * Mathf.Lerp(2f, 0.5f, distance / Radius);
        transform.position = Vector3.MoveTowards(
            transform.position, player.position, pull * Time.deltaTime);
    }
}
