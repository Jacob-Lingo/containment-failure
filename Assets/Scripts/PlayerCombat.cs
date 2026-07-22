using System.Collections;
using UnityEngine;

/// Player attacks: left click (M1) is the primary attack — melee claw by
/// default, but permanently replaced by the ranged shot once the player
/// commits to the Ranged Attack evolution card (the two are mutually
/// exclusive archetype choices; see EvolutionSystem). Space lets out a
/// scream AOE once unlocked, independent of the M1 build choice. R/E/F are
/// three further unlockable actives (Beam/Cyclone/Berserk) independent of
/// M1/Scream/Dash. Reads EvolutionSystem for passive upgrades but never
/// assumes it's present, so this still works standalone.
public class PlayerCombat : MonoBehaviour
{
    [Header("Melee (Claw)")]
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private float meleeArc = 90f;
    [SerializeField] private int meleeDamage = 2;
    [SerializeField] private float meleeCooldown = 0.4f;

    [Header("Ranged (Bullet) — replaces melee on M1 once unlocked")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int rangedDamage = 1;
    [SerializeField] private float rangedCooldown = 0.5f;

    [Header("Scream")]
    [SerializeField] private float screamRange = 3f;
    [SerializeField] private float screamArc = 100f;
    [SerializeField] private int screamDamage = 1;
    [SerializeField] private float screamCooldown = 3f;

    [SerializeField] private LayerMask guardLayer = ~0;

    [Header("Kamehameha Beam (R)")]
    [SerializeField] private float beamLength = 8f;
    [SerializeField] private float beamWidth = 0.8f;
    [SerializeField] private int beamDamage = 6;
    [SerializeField] private float beamCooldown = 4f;

    [Header("Cyclone (E)")]
    [SerializeField] private float cycloneRadius = 2f;
    [SerializeField] private int cycloneDamagePerTick = 1;
    [SerializeField] private float cycloneDuration = 1f;
    [SerializeField] private float cycloneTickInterval = 0.25f;
    [SerializeField] private float cycloneCooldown = 6f;

    [Header("Berserk (F) — reckless auto-pilot, loses manual control")]
    [SerializeField] private float berserkCooldown = 45f; // starts counting only once the state ends, not on activation
    [SerializeField] private float berserkDamageMultiplier = 3.5f;
    [SerializeField] private float berserkChaseSpeed = 7f;
    [SerializeField] private float berserkAttackRange = 1.5f;
    [SerializeField] private float berserkAttackCooldown = 0.4f;
    [SerializeField] private float berserkRetargetInterval = 0.3f;
    [SerializeField] private float berserkSearchRadius = 40f;

    [Header("Knockback On Hit")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Scream Slow")]
    [SerializeField] private float screamSlowMultiplier = 0.5f;
    [SerializeField] private float screamSlowDuration = 2f;

    private EvolutionSystem evolution;
    private SpriteRenderer sr;
    private Color baseColor;
    private float nextMeleeTime, nextRangedTime, nextScreamTime;
    private float nextBeamTime, nextCycloneTime, nextBerserkTime;
    private float lastMeleeCooldownUsed = 1f, lastRangedCooldownUsed = 1f, lastScreamCooldownUsed = 1f;
    private float lastBeamCooldownUsed = 1f, lastCycloneCooldownUsed = 1f, lastBerserkCooldownUsed = 1f;
    private float berserkEndTime;
    private Vector2? berserkAimOverride;

    /// Whether M1 currently fires the ranged shot instead of the claw — read
    /// by AbilityBarUI to pick which icon to show for the M1 slot.
    public bool RangedIsPrimary => evolution != null && evolution.UnlockedRanged;

    /// 0 = ready, 1 = just used. Whichever attack M1 currently maps to.
    public float M1CooldownFraction =>
        RangedIsPrimary
            ? Mathf.Clamp01((nextRangedTime - Time.time) / Mathf.Max(0.0001f, lastRangedCooldownUsed))
            : Mathf.Clamp01((nextMeleeTime - Time.time) / Mathf.Max(0.0001f, lastMeleeCooldownUsed));

    public float ScreamCooldownFraction => Mathf.Clamp01((nextScreamTime - Time.time) / Mathf.Max(0.0001f, lastScreamCooldownUsed));
    public float BeamCooldownFraction => Mathf.Clamp01((nextBeamTime - Time.time) / Mathf.Max(0.0001f, lastBeamCooldownUsed));
    public float CycloneCooldownFraction => Mathf.Clamp01((nextCycloneTime - Time.time) / Mathf.Max(0.0001f, lastCycloneCooldownUsed));
    public float BerserkCooldownFraction => Mathf.Clamp01((nextBerserkTime - Time.time) / Mathf.Max(0.0001f, lastBerserkCooldownUsed));

    /// While true, the player has no manual control — PlayerDash also checks
    /// this to suspend its own input handling during the reckless auto-pilot.
    public bool IsBerserking => Time.time < berserkEndTime;

    private void Awake()
    {
        evolution = GetComponent<EvolutionSystem>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        AbilityBarUI.EnsureInstance(this, GetComponent<PlayerDash>());
    }

    /// In-place "Restart Run" hook (PauseMenu) — clears cooldowns left over
    /// from the previous run.
    public void ResetCombatState()
    {
        StopAllCoroutines(); // cancels any in-flight Cyclone/Berserk if restarting mid-ability

        nextMeleeTime = 0f;
        nextRangedTime = 0f;
        nextScreamTime = 0f;
        nextBeamTime = 0f;
        nextCycloneTime = 0f;
        nextBerserkTime = 0f;
        berserkEndTime = 0f;
        berserkAimOverride = null;
        if (sr != null) sr.color = baseColor;

        // Berserk disables PlayerController for its duration — if a restart
        // interrupts it mid-flight, make sure control actually comes back.
        if (TryGetComponent<PlayerController>(out var controller)) controller.enabled = true;
    }

    private void Update()
    {
        // No attacks while the game is frozen (level-up card / pause menu):
        // physics queries still work at timeScale 0, so without this a click
        // on a UI card also swings the claw and pollutes RunStats.
        if (GameState.IsFrozen) return;

        // Berserk suspends all manual attack input — BerserkRoutine drives
        // movement and attacks directly until the state ends.
        if (!IsBerserking)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (RangedIsPrimary) TryRanged();
                else TryMelee();
            }

            if (Input.GetKeyDown(KeyCode.Space)) TryScream();
            if (Input.GetKeyDown(KeyCode.R)) TryBeam();
            if (Input.GetKeyDown(KeyCode.E)) TryCyclone();
        }

        if (Input.GetKeyDown(KeyCode.F)) TryBerserk();
    }

    private Vector2 AimDirection()
    {
        // During Berserk, aim follows the tracked target instead of the
        // (unread, since the player has no manual control) mouse cursor.
        if (berserkAimOverride.HasValue) return berserkAimOverride.Value;

        if (Camera.main == null) return Vector2.right;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    /// Adrenaline (universal) stacks with an ability-specific cooldown-down
    /// card, combined additively and capped so cooldowns can't reach zero.
    private float ApplyCooldownReduction(float baseCooldown, float extraReduction)
    {
        if (evolution == null) return baseCooldown;
        float total = Mathf.Clamp01(evolution.AdrenalineReduction + extraReduction);
        return baseCooldown * (1f - total);
    }

    /// Crit roll, then Berserk's flat multiplier if its window is active —
    /// shared by every damage source (melee, ranged, scream, beam, cyclone).
    private int ApplyDamageModifiers(int damage)
    {
        int result = evolution != null && evolution.RollCrit() ? damage * 2 : damage;
        if (Time.time < berserkEndTime) result = Mathf.RoundToInt(result * berserkDamageMultiplier);
        return result;
    }

    private void ApplyKnockbackIfUnlocked(GuardHealth guard, Vector2 aim)
    {
        if (evolution == null || !evolution.KnockbackOnHit) return;
        if (guard.TryGetComponent<GuardMotor>(out var motor))
            motor.ApplyKnockback(aim * knockbackForce, knockbackDuration);
    }

    private void TryMelee()
    {
        if (Time.time < nextMeleeTime) return;
        lastMeleeCooldownUsed = ApplyCooldownReduction(meleeCooldown, 0f);
        nextMeleeTime = Time.time + lastMeleeCooldownUsed;
        RunStats.RegisterAttackUsed(AttackType.Melee);

        bool biggerClaw = evolution != null && evolution.BiggerClaw;
        bool titan = evolution != null && evolution.Titan;
        float range = (biggerClaw ? meleeRange * 3f : meleeRange) * (titan ? evolution.TitanSizeMultiplier : 1f);
        int baseDamage = (biggerClaw ? meleeDamage * 2 : meleeDamage) + (evolution != null ? evolution.MeleeDamageBonus : 0);
        if (titan) baseDamage = Mathf.RoundToInt(baseDamage * evolution.TitanDamageMultiplier);
        int damage = ApplyDamageModifiers(baseDamage);

        Vector2 aim = AimDirection();
        SpawnClawEffect(aim, range);
        Sfx.PlayRandom("player_melee_swing", 3, transform.position);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, guardLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;

            Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            if (Vector2.Angle(aim, toTarget) > meleeArc * 0.5f) continue;

            guard.TakeDamage(damage, AttackType.Melee);
            if (guard.Health <= 0) evolution?.NotifyKill();
            else ApplyKnockbackIfUnlocked(guard, toTarget.normalized);
        }
    }

    private void SpawnClawEffect(Vector2 aim, float range)
    {
        GameObject fx = new GameObject("ClawFX");
        fx.transform.position = (Vector2)transform.position + aim * (range * 0.6f);
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        fx.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Combat/claw_slash");
        sr.sortingOrder = 11;
        fx.transform.localScale = Vector3.one * (range * 0.5f);

        Destroy(fx, 0.15f);
    }

    private void TryRanged()
    {
        if (evolution == null || !evolution.UnlockedRanged) return;
        if (Time.time < nextRangedTime || bulletPrefab == null) return;
        lastRangedCooldownUsed = ApplyCooldownReduction(rangedCooldown, 0f);
        nextRangedTime = Time.time + lastRangedCooldownUsed;
        RunStats.RegisterAttackUsed(AttackType.Ranged);

        Vector2 aim = AimDirection();

        if (evolution.TripleShot)
        {
            const float spread = 12f;
            SpawnBullet(aim);
            SpawnBullet(Quaternion.Euler(0, 0, spread) * aim);
            SpawnBullet(Quaternion.Euler(0, 0, -spread) * aim);
        }
        else if (evolution.DoubleShot)
        {
            const float spread = 8f;
            SpawnBullet(Quaternion.Euler(0, 0, spread) * aim);
            SpawnBullet(Quaternion.Euler(0, 0, -spread) * aim);
        }
        else
        {
            SpawnBullet(aim);
        }
    }

    private void SpawnBullet(Vector2 dir)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        GameObject go = Instantiate(bulletPrefab, origin, Quaternion.identity);
        int damage = ApplyDamageModifiers(rangedDamage + (evolution != null ? evolution.RangedDamageBonus : 0));
        int pierce = evolution != null && evolution.RangedPierce ? 1 : 0;
        float scale = evolution != null ? evolution.BulletScale : 1f;
        bool explosive = evolution != null && evolution.ExplosiveRounds;
        bool ricochet = evolution != null && evolution.Ricochet;

        if (go.TryGetComponent<Bullet>(out var bullet))
            bullet.Init(dir, damage, true, pierce, scale, explosive, () => evolution?.NotifyKill(), ricochet);
    }

    private void TryScream()
    {
        if (evolution == null || !evolution.UnlockedScream) return;
        if (Time.time < nextScreamTime) return;
        lastScreamCooldownUsed = ApplyCooldownReduction(screamCooldown, evolution.ScreamCooldownReduction);
        nextScreamTime = Time.time + lastScreamCooldownUsed;
        RunStats.RegisterAttackUsed(AttackType.Scream);

        bool radial = evolution != null && evolution.RadiusScream;
        bool titan = evolution != null && evolution.Titan;
        float range = (radial ? screamRange * 1.3f : screamRange) * (titan ? evolution.TitanSizeMultiplier : 1f);
        int baseDamage = screamDamage + (evolution != null ? evolution.ScreamDamageBonus : 0);
        if (titan) baseDamage = Mathf.RoundToInt(baseDamage * evolution.TitanDamageMultiplier);
        int damage = ApplyDamageModifiers(baseDamage);

        Vector2 aim = AimDirection();

        var screamColor = new Color(0.6f, 0.2f, 1f, 0.5f);
        if (radial) ScreamVfx.SpawnRing(transform.position, range, screamColor);
        else ScreamVfx.SpawnCone(transform.position, aim, screamArc, range, screamColor);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, guardLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;

            Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            if (!radial && Vector2.Angle(aim, toTarget) > screamArc * 0.5f) continue;

            guard.TakeDamage(damage, AttackType.Scream);
            if (guard.Health <= 0)
            {
                evolution?.NotifyKill();
                continue;
            }

            ApplyKnockbackIfUnlocked(guard, toTarget.normalized);
            if (evolution != null && evolution.ScreamSlow && guard.TryGetComponent<GuardMotor>(out var motor))
                motor.ApplySlow(screamSlowMultiplier, screamSlowDuration);
        }
    }

    private void TryBeam()
    {
        if (evolution == null || !evolution.UnlockedBeam) return;
        if (Time.time < nextBeamTime) return;
        lastBeamCooldownUsed = ApplyCooldownReduction(beamCooldown, 0f);
        nextBeamTime = Time.time + lastBeamCooldownUsed;
        RunStats.RegisterAttackUsed(AttackType.Ranged);

        Vector2 aim = AimDirection();
        int damage = ApplyDamageModifiers(beamDamage + evolution.BeamDamageBonus);

        Vector2 center = (Vector2)transform.position + aim * (beamLength * 0.5f);
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        Sfx.PlayRandom("beam_fire", 3, transform.position);
        BeamVfx.Spawn(transform.position, aim, beamLength, beamWidth, new Color(1f, 0.9f, 0.3f, 0.8f));

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(beamLength, beamWidth), angle, guardLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            guard.TakeDamage(damage, AttackType.Ranged);
            if (guard.Health <= 0) evolution.NotifyKill();
        }
    }

    private void TryCyclone()
    {
        if (evolution == null || !evolution.UnlockedCyclone) return;
        if (Time.time < nextCycloneTime) return;
        lastCycloneCooldownUsed = ApplyCooldownReduction(cycloneCooldown, 0f);
        nextCycloneTime = Time.time + lastCycloneCooldownUsed;
        StartCoroutine(CycloneRoutine());
    }

    private IEnumerator CycloneRoutine()
    {
        float elapsed = 0f;
        float spinAngle = 0f;
        bool titan = evolution != null && evolution.Titan;
        float radius = cycloneRadius * (titan ? evolution.TitanSizeMultiplier : 1f);
        int damagePerTick = cycloneDamagePerTick + (evolution != null ? evolution.CycloneDamageBonus : 0);
        if (titan) damagePerTick = Mathf.RoundToInt(damagePerTick * evolution.TitanDamageMultiplier);

        while (elapsed < cycloneDuration)
        {
            RunStats.RegisterAttackUsed(AttackType.Melee);
            int damage = ApplyDamageModifiers(damagePerTick);

            Vector2 spinDir = Quaternion.Euler(0, 0, spinAngle) * Vector2.right;
            SpawnClawEffect(spinDir, radius);
            Sfx.PlayRandom("player_melee_swing", 3, transform.position);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, guardLayer);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
                guard.TakeDamage(damage, AttackType.Melee);
                if (guard.Health <= 0) evolution?.NotifyKill();
                else ApplyKnockbackIfUnlocked(guard, ((Vector2)hit.transform.position - (Vector2)transform.position).normalized);
            }

            spinAngle += 140f;
            elapsed += cycloneTickInterval;
            yield return new WaitForSeconds(cycloneTickInterval);
        }
    }

    private void TryBerserk()
    {
        if (evolution == null || !evolution.UnlockedBerserk) return;
        if (Time.time < nextBerserkTime) return;
        StartCoroutine(BerserkRoutine());
    }

    /// Berserk isn't a passive buff — it's a reckless auto-pilot: the player
    /// loses manual control for its duration while this drives the
    /// Rigidbody2D directly, chasing and attacking the nearest guard with
    /// hugely boosted damage. PlayerController is disabled for the duration
    /// so it can't fight this for control of movement.
    private IEnumerator BerserkRoutine()
    {
        nextBerserkTime = float.MaxValue; // can't re-trigger while active; real cooldown starts once it ends
        berserkEndTime = Time.time + evolution.BerserkDuration;
        if (sr != null) sr.color = new Color(1f, 0.15f, 0.15f, baseColor.a);

        var controller = GetComponent<PlayerController>();
        var rb = GetComponent<Rigidbody2D>();
        if (controller != null) controller.enabled = false;

        Transform target = null;
        float nextRetarget = 0f;
        float nextAttack = 0f;

        while (Time.time < berserkEndTime)
        {
            if (target == null || Time.time >= nextRetarget)
            {
                target = FindNearestGuard();
                nextRetarget = Time.time + berserkRetargetInterval;
            }

            // Reckless ability spam — fires whatever's unlocked and off
            // cooldown; each Try* method already self-gates on cooldown/unlock,
            // so calling them every frame is cheap and "auto-casts whatever
            // it has" without needing separate per-ability timers here.
            if (evolution.UnlockedCyclone) TryCyclone();

            if (target != null && rb != null)
            {
                Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
                float dist = toTarget.magnitude;
                Vector2 dir = dist > 0.01f ? toTarget.normalized : Vector2.right;
                berserkAimOverride = dir;

                if (evolution.UnlockedBeam) TryBeam();
                if (evolution.UnlockedScream && dist <= screamRange * 1.6f) TryScream();

                if (dist > berserkAttackRange)
                {
                    rb.linearVelocity = dir * berserkChaseSpeed;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                    if (Time.time >= nextAttack)
                    {
                        nextAttack = Time.time + berserkAttackCooldown;
                        BerserkStrike(dir);
                    }
                }
            }
            else if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            yield return null;
        }

        berserkAimOverride = null;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (controller != null) controller.enabled = true;
        if (sr != null) sr.color = baseColor;

        lastBerserkCooldownUsed = berserkCooldown;
        nextBerserkTime = Time.time + lastBerserkCooldownUsed;
    }

    private Transform FindNearestGuard()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, berserkSearchRadius, guardLayer);
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out _)) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    /// Reckless AoE swing around the player — hits everything in range, not
    /// just the tracked target, matching "tries to kill as many as possible."
    private void BerserkStrike(Vector2 aim)
    {
        int damage = ApplyDamageModifiers(meleeDamage);
        SpawnClawEffect(aim, berserkAttackRange);
        Sfx.PlayRandom("player_melee_swing", 3, transform.position);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, berserkAttackRange, guardLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            guard.TakeDamage(damage, AttackType.Melee);
            if (guard.Health <= 0) evolution?.NotifyKill();
            else ApplyKnockbackIfUnlocked(guard, ((Vector2)hit.transform.position - (Vector2)transform.position).normalized);
        }
    }
}
