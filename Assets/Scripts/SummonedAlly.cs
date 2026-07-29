using UnityEngine;

/// A risen skeleton that fights on the player's side — the Lich line's payoff.
///
/// Deliberately has **no GuardHealth**: the player's attacks all look for that
/// component, so an ally without one simply can't be hit by you, and guards
/// target the player's transform directly so they ignore it too. That sidesteps
/// needing a faction system for one feature. It dies on a timer instead, which
/// also caps how many can pile up.
///
/// Built in code (no prefab), same pattern as ExpOrb and Coin.
public class SummonedAlly : MonoBehaviour
{
    private const float Speed = 4.5f;
    private const float AttackRange = 0.9f;
    private const float AttackCooldown = 0.8f;
    private const float SearchInterval = 0.4f;

    private int damage;
    private float expireTime;
    private float nextAttack;
    private float nextSearch;
    private Transform target;

    public static void Spawn(Vector3 position, int damage, float lifetime)
    {
        var go = new GameObject("SummonedAlly");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.8f;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Bestiary.Get(Bestiary.Skeleton);
        renderer.color = new Color(0.6f, 0.9f, 0.7f); // sickly green: reads as yours, not theirs
        renderer.sortingOrder = 9;

        var ally = go.AddComponent<SummonedAlly>();
        ally.damage = damage;
        ally.expireTime = Time.time + lifetime;

        SpellFx.Play(SpellFx.Phantom, position, 1.6f, 1.8f);
    }

    private void Update()
    {
        if (Time.time >= expireTime)
        {
            HitFlashFx.Spawn(transform.position, new Color(0.6f, 0.9f, 0.7f, 0.8f), 0.5f);
            Destroy(gameObject);
            return;
        }

        if (target == null && Time.time >= nextSearch)
        {
            nextSearch = Time.time + SearchInterval;
            target = FindNearestGuard();
        }

        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > AttackRange)
        {
            // Rigidbody-less runtime object, so the repo's Rigidbody2D movement
            // rule doesn't apply here — same as OrbMagnet.
            transform.position = Vector3.MoveTowards(
                transform.position, target.position, Speed * Time.deltaTime);
            return;
        }

        if (Time.time < nextAttack) return;
        nextAttack = Time.time + AttackCooldown;

        if (target.TryGetComponent<GuardHealth>(out var guard))
        {
            guard.TakeDamage(damage, AttackType.Melee);
            HitFlashFx.Spawn(target.position, new Color(0.6f, 0.9f, 0.7f, 0.85f), 0.3f);
        }
    }

    private Transform FindNearestGuard()
    {
        Transform best = null;
        float bestDistance = float.MaxValue;

        foreach (var guard in FindObjectsByType<GuardHealth>(FindObjectsSortMode.None))
        {
            if (guard.Health <= 0) continue;

            float distance = Vector2.Distance(transform.position, guard.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = guard.transform;
            }
        }

        return best;
    }
}
