using System;
using UnityEngine;

/// Shared projectile for both the player's ranged attack and ranged guards.
/// isPlayerBullet decides who it can hurt, since guards aren't tagged
/// distinctly from each other — cheaper and more robust than layer/tag
/// juggling for a two-faction game.
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f;

    [Header("Explosive Rounds")]
    [SerializeField] private float explosionRadius = 2.8f;
    [SerializeField] private int explosionDamage = 1;
    [Tooltip("Direct-hit damage multiplier for explosive rounds. Splash targets take explosionDamage instead.")]
    [SerializeField] private float explosiveDirectMultiplier = 2f;
    [SerializeField] private LayerMask guardLayer = ~0;

    [Header("Ricochet")]
    [SerializeField] private float ricochetRadius = 4f;

    private int damage;
    private bool isPlayerBullet;
    private int pierceRemaining;
    private float scaleMultiplier = 1f;
    private bool explosive;
    private bool ricochet;
    private bool hasRicocheted;
    private Action onKill;
    private Rigidbody2D rb;
    private Vector2 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.useFullKinematicContacts = true; // kinematic bodies don't report contacts vs static colliders (walls) without this

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = Resources.Load<Sprite>(isPlayerBullet ? "Combat/bullet_player" : "Combat/bullet_guard");
    }

    public void Init(Vector2 dir, int dmg, bool playerOwned, int pierceCount = 0, float scale = 1f, bool isExplosive = false, Action onKillCallback = null, bool canRicochet = false)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        damage = dmg;
        isPlayerBullet = playerOwned;
        pierceRemaining = pierceCount;
        scaleMultiplier = scale;
        explosive = isExplosive;
        onKill = onKillCallback;
        ricochet = canRicochet;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Bigger Bullets swaps to a plain circle instead of just scaling the
            // normal bullet sprite up — the source art reads oddly stretched
            // when enlarged, a circle reads cleanly as "bigger" at any scale.
            // Tinted per faction since the circle has no baked-in art color.
            if (scaleMultiplier > 1.01f)
            {
                sr.sprite = GetCircleSprite();
                sr.color = isPlayerBullet ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.5f, 0.2f);
            }
            else
            {
                sr.sprite = Resources.Load<Sprite>(isPlayerBullet ? "Combat/bullet_player" : "Combat/bullet_guard");
                sr.color = Color.white;
            }
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one * scaleMultiplier;

        Destroy(gameObject, lifeTime);
    }

    private static Sprite circleSprite;
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, dist <= radius ? 1f : 0f));
            }
        }
        tex.Apply();

        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerBullet)
        {
            if (other.CompareTag("Player")) return;

            if (other.TryGetComponent<GuardHealth>(out var guard))
            {
                // Explosive rounds reward landing the shot: the guard you
                // actually hit takes boosted impact damage, everyone else in
                // the blast takes the (much smaller) splash.
                int impact = explosive
                    ? Mathf.Max(1, Mathf.RoundToInt(damage * explosiveDirectMultiplier))
                    : damage;

                guard.TakeDamage(impact, AttackType.Ranged);
                if (guard.Health <= 0) onKill?.Invoke();

                if (explosive) Explode(guard);

                if (pierceRemaining > 0)
                {
                    pierceRemaining--;
                }
                else if (ricochet && !hasRicocheted && TryRicochet(other))
                {
                    hasRicocheted = true;
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }
        }
        else
        {
            if (other.TryGetComponent<GuardHealth>(out _)) return; // ignore other guards

            if (other.TryGetComponent<PlayerHealth>(out var player))
            {
                player.TakeDamage(damage);
                if (explosive) PlayExplosionFx(); // military/Tank bazooka rounds — impact juice only, no AoE (only one player to hit)
                Destroy(gameObject);
                return;
            }
        }

        // Fell through the target checks. Everything the bullet should pass
        // through (pickups, exp orbs, exit doors, other bullets) is a trigger,
        // so any solid collider left is a wall/obstacle — pop against it
        // instead of gliding through until lifeTime expires.
        if (!other.isTrigger) PopOnWall();
    }

    private void PopOnWall()
    {
        if (explosive)
        {
            // Detonate against the wall. Player rounds still catch nearby guards
            // in the blast; guard rounds get impact juice only (mirrors the
            // no-AoE handling when they strike the player).
            if (isPlayerBullet) Explode();
            else PlayExplosionFx();
        }
        else
        {
            Color spark = isPlayerBullet ? new Color(0.6f, 0.85f, 1f, 0.9f) : new Color(1f, 0.6f, 0.3f, 0.9f);
            HitFlashFx.Spawn(transform.position, spark, 0.35f);
        }

        Destroy(gameObject);
    }

    /// Redirects toward the nearest other guard within ricochetRadius. Returns
    /// false (letting the caller destroy the bullet as normal) if none found.
    private bool TryRicochet(Collider2D justHit)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, ricochetRadius, guardLayer);
        Collider2D nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == justHit || !hit.TryGetComponent<GuardHealth>(out _)) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = hit;
            }
        }

        if (nearest == null) return false;

        direction = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        return true;
    }

    private void PlayExplosionFx()
    {
        Sfx.PlayRandom("explosion", 3, transform.position, 0.8f);

        // Scales with the blast so the flash keeps reading as the real
        // footprint after explosionRadius was widened.
        HitFlashFx.Spawn(transform.position, new Color(1f, 0.5f, 0.1f, 0.85f), explosionRadius * 0.6f);
    }

    /// `directHit` is the guard the bullet physically struck, if any — it
    /// already took the boosted impact damage above, so it's excluded here
    /// rather than double-dipping on its own explosion.
    private void Explode(GuardHealth directHit = null)
    {
        PlayExplosionFx();
        Juice.Boom();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, guardLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            if (guard == directHit) continue;

            guard.TakeDamage(explosionDamage, AttackType.Ranged);
            if (guard.Health <= 0) onKill?.Invoke();
        }
    }
}
