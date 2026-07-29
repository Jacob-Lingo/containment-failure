using System.Collections.Generic;
using UnityEngine;

/// A patch of ground that hurts whatever stands in it: venom trails, web traps,
/// lingering fire. This is the mechanic the venom line is built on, and it's
/// genuinely different from every other damage source in the game — it persists
/// after you've left, so it's about *where you route* rather than what you hit.
///
/// Deliberately one component rather than a zone plus a per-guard "poisoned"
/// status: the zone re-checks who's inside on every tick, which gives lingering
/// damage, re-application and expiry for free, and reuses GuardMotor.ApplySlow
/// for the slowing variants instead of inventing a second slow.
public class HazardZone : MonoBehaviour
{
    private const float TickInterval = 0.4f;

    // Shared so a trail of a dozen zones doesn't allocate every tick.
    private static readonly Collider2D[] Buffer = new Collider2D[32];

    private float radius;
    private int damagePerTick;
    private float expireTime;
    private float nextTick;
    private float slowMultiplier;   // 1 = no slow
    private bool spreads;
    private Color tint;

    /// `spreads` is the Hydra's payoff: anything that dies inside the cloud
    /// bursts into a smaller one, so a dense swarm chain-reacts.
    public static HazardZone Spawn(Vector3 position, float radius, float duration,
                                   int damagePerTick, Color tint,
                                   float slowMultiplier = 1f, bool spreads = false)
    {
        var go = new GameObject("HazardZone");
        go.transform.position = position;

        var zone = go.AddComponent<HazardZone>();
        zone.radius = radius;
        zone.damagePerTick = damagePerTick;
        zone.expireTime = Time.time + duration;
        zone.slowMultiplier = slowMultiplier;
        zone.spreads = spreads;
        zone.tint = tint;
        zone.BuildFootprint();

        return zone;
    }

    /// A patch that persists for the zone's whole life.
    ///
    /// This used to re-spawn a ScreamVfx ring every tick, which was wrong in
    /// two ways: that ring is a 0.3s *expanding shockwave*, so a lingering
    /// puddle rendered as a pulse — with a 0.1s gap of nothing between ticks.
    /// A zone that outlives its own telegraph is unreadable, and venom was
    /// effectively invisible because of it.
    private void BuildFootprint()
    {
        var go = new GameObject("Footprint");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * (radius * 2f);

        footprint = go.AddComponent<SpriteRenderer>();
        footprint.sprite = SpellFx.StaticFrame(SpellFx.FelSpell, PatchFrame);
        footprint.color = tint;
        footprint.sortingOrder = 3; // on the ground, under creatures
    }

    /// A frame of the felspell sheet held still: a dense speckled circle that
    /// already reads as bubbling liquid. Real art recoloured per hazard beats a
    /// procedural circle, which always looks like a debug gizmo.
    ///
    /// Mid-sheet, where the burst is at full spread — the early and late frames
    /// are partial and would read as a hole in the patch.
    ///
    /// The source art is green/yellow and the tint multiplies it, so warm and
    /// green hazards recolour correctly but a *blue* one would come out near
    /// black. Every caller is green venom today; a cold hazard needs a
    /// different sheet rather than a different tint.
    private const int PatchFrame = 45;

    private SpriteRenderer footprint;

    private void Update()
    {
        if (Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }

        // Bubbling: the patch breathes so it reads as alive, and fades out over
        // its last second so expiry is something you can see coming.
        if (footprint != null)
        {
            float remaining = expireTime - Time.time;
            float fade = Mathf.Clamp01(remaining);
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
            footprint.color = new Color(tint.r, tint.g, tint.b, tint.a * fade * pulse);
        }

        if (Time.time < nextTick) return;
        nextTick = Time.time + TickInterval;

        int found = Physics2D.OverlapCircleNonAlloc(transform.position, radius, Buffer);
        var dying = new List<Vector3>();

        for (int i = 0; i < found; i++)
        {
            var hit = Buffer[i];
            if (hit == null || !hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            if (guard.Health <= 0) continue;

            guard.TakeDamage(damagePerTick, AttackType.Melee);

            if (slowMultiplier < 1f && hit.TryGetComponent<GuardMotor>(out var motor))
                motor.ApplySlow(slowMultiplier, TickInterval * 1.5f);

            if (spreads && guard.Health <= 0) dying.Add(hit.transform.position);
        }

        // Spawned after the loop: seeding zones mid-iteration would have them
        // picked up by this same tick and cascade in one frame.
        foreach (var position in dying)
            Spawn(position, radius * 0.6f, 1.5f, damagePerTick, tint, slowMultiplier);
    }
}
