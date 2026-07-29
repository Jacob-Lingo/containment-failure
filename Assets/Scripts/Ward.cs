using UnityEngine;

/// A sigil planted on the ground that zaps whatever comes near it, then burns
/// out. The counterpart to SummonedAlly: that one chases, this one holds
/// ground, which is the whole reason both exist.
///
/// A separate script rather than a "stationary" flag on SummonedAlly — the two
/// share no behaviour beyond "expires on a timer", and a mode flag would mean
/// every read of that file asks which creature it's looking at.
///
/// Like SummonedAlly it has no GuardHealth, so nothing can attack it; it dies
/// on its timer instead, which is also what caps how many can be standing.
public class Ward : MonoBehaviour
{
    private const float ZapInterval = 0.9f;

    private int damage;
    private float range;
    private float expireTime;
    private float nextZap;
    private float nextPulse;

    public static void Spawn(Vector3 position, int damage, float range, float lifetime)
    {
        var go = new GameObject("Ward");
        go.transform.position = position;

        var ward = go.AddComponent<Ward>();
        ward.damage = damage;
        ward.range = range;
        ward.expireTime = Time.time + lifetime;

        ScreamVfx.SpawnRing(position, range, new Color(0.9f, 0.4f, 1f, 0.35f));
    }

    private void Update()
    {
        if (Time.time >= expireTime)
        {
            HitFlashFx.Spawn(transform.position, new Color(0.9f, 0.4f, 1f, 0.8f), 0.5f);
            Destroy(gameObject);
            return;
        }

        // The sigil sheet is a short loop, so it's replayed rather than left
        // running — the alternative is a persistent animator for one object.
        if (Time.time >= nextPulse)
        {
            nextPulse = Time.time + 1.4f;
            SpellFx.Play(SpellFx.Sigil, transform.position, 1.6f, 0.9f,
                         new Color(1f, 0.5f, 1f, 0.9f));
        }

        if (Time.time < nextZap) return;

        GuardHealth target = NearestGuard();
        if (target == null) return;

        nextZap = Time.time + ZapInterval;

        Vector3 offset = target.transform.position - transform.position;
        BeamVfx.Spawn(transform.position, ((Vector2)offset).normalized, offset.magnitude, 0.3f,
                      new Color(0.95f, 0.5f, 1f, 0.9f));
        HitFlashFx.Spawn(target.transform.position, new Color(1f, 0.6f, 1f, 0.9f), 0.4f);

        target.TakeDamage(damage, AttackType.Ranged);
    }

    private GuardHealth NearestGuard()
    {
        GuardHealth best = null;
        float bestDistance = float.MaxValue;

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, range))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard) || guard.Health <= 0) continue;

            float distance = Vector2.Distance(transform.position, guard.transform.position);
            if (distance < bestDistance) { bestDistance = distance; best = guard; }
        }

        return best;
    }
}
