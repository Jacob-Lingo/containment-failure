using System.Collections;
using UnityEngine;

/// Gap-closer bolted onto a normal melee guard: when the player is out of
/// baton reach but inside leapRange, the brute telegraphs, launches itself the
/// whole distance and damages everything it lands on.
///
/// Added at runtime by SpawnDirector (AddComponent) alongside the prefab's own
/// GuardBrain rather than replacing it — GuardBrain keeps doing the chasing and
/// the ordinary melee swing, this only adds the jump.
///
/// The leap itself is GuardMotor.ApplyKnockback: it already overrides seek for
/// a duration and auto-expires, which is exactly a leap arc. There is
/// deliberately no second movement-override path.
[RequireComponent(typeof(GuardMotor))]
public class GuardLeap : MonoBehaviour
{
    [Tooltip("Inside this the brute just uses GuardBrain's normal melee swing; the leap is for closing a gap.")]
    [SerializeField] private float minLeapRange = 2.5f;
    [SerializeField] private float maxLeapRange = 7f;

    [Tooltip("Telegraph before launch, same contract as GuardBrain's swing windup: tint first, so the leap is dodgeable.")]
    [SerializeField] private float windup = 0.5f;

    [SerializeField] private float leapDuration = 0.3f;
    [SerializeField] private float cooldown = 4f;
    [SerializeField] private float slamRadius = 2f;
    [SerializeField] private int slamDamage = 3;

    private static readonly Color WindupColor = new Color(1f, 0.3f, 0.15f);

    private GuardMotor motor;
    private GuardHealth health;
    private SpriteRenderer sr;
    private Transform target;
    private float nextLeapTime;
    private Coroutine leap;

    private void Awake()
    {
        motor = GetComponent<GuardMotor>();
        health = GetComponent<GuardHealth>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void Configure(Transform player)
    {
        target = player;
        nextLeapTime = Time.time + cooldown * 0.5f;
    }

    /// Called by SpawnDirector after the tier profile, matching
    /// GuardBrain.ScaleDamageForFloor — without it the leap would be the one
    /// enemy attack in the game that never gets deadlier with depth.
    public void ScaleDamageForFloor(float multiplier)
    {
        slamDamage = Mathf.Max(1, Mathf.RoundToInt(slamDamage * multiplier));
    }

    public void SetSlamProfile(int damage, float leapCooldown)
    {
        slamDamage = damage;
        cooldown = leapCooldown;
    }

    private void Update()
    {
        if (target == null || leap != null || Time.time < nextLeapTime) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance < minLeapRange || distance > maxLeapRange) return;

        nextLeapTime = Time.time + cooldown;
        leap = StartCoroutine(LeapRoutine());
    }

    private IEnumerator LeapRoutine()
    {
        if (sr != null) sr.color = WindupColor;

        // Hold still while telegraphing. GuardBrain re-issues motor.Seek every
        // Update, so a plain motor.Stop() would be overwritten next frame —
        // a zero-velocity knockback wins because FixedUpdate checks it first.
        float elapsed = 0f;
        while (elapsed < windup)
        {
            motor.ApplyKnockback(Vector2.zero, 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // GuardHealth's hit flash may have repainted us mid-windup; revert to
        // its cached base colour, not whatever is on the renderer right now.
        if (sr != null) sr.color = health != null ? health.BaseColor : Color.white;

        if (target == null) { leap = null; yield break; }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float distance = Mathf.Min(toTarget.magnitude, maxLeapRange);
        if (toTarget.sqrMagnitude > 0.0001f)
            motor.ApplyKnockback(toTarget.normalized * (distance / leapDuration), leapDuration);

        yield return new WaitForSeconds(leapDuration);

        Slam();
        leap = null;
    }

    private void Slam()
    {
        Sfx.PlayRandom("guard_baton_hit", 3, transform.position);
        HitFlashFx.Spawn(transform.position, new Color(1f, 0.6f, 0.3f, 0.9f), 1.2f);
        ScreamVfx.SpawnRing(transform.position, slamRadius, new Color(1f, 0.5f, 0.2f, 0.8f));

        // Player only: an untagged OverlapCircle here would have the brute
        // slamming its own swarm, which no other enemy attack in the game does.
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, slamRadius))
        {
            if (hit == null || !hit.CompareTag("Player")) continue;
            if (hit.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(slamDamage);
        }
    }
}
