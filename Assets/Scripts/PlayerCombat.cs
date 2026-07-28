using System.Collections;
using System.Collections.Generic;
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

    [Header("Line abilities")]
    [SerializeField] private float slamRadius = 3.5f;
    [SerializeField] private int slamDamage = 4;
    [SerializeField] private float slamCooldown = 7f;
    [SerializeField] private float meteorRadius = 2.6f;
    [SerializeField] private int meteorDamage = 7;
    [SerializeField] private float meteorDelay = 0.7f;
    [SerializeField] private float meteorCooldown = 8f;
    [SerializeField] private float webRadius = 2.4f;
    [SerializeField] private float webDuration = 6f;
    [SerializeField] private float webCooldown = 9f;
    [SerializeField] private float novaRadius = 4f;
    [SerializeField] private float novaCooldown = 8f;
    [SerializeField] private float blinkRange = 6f;
    [SerializeField] private float blinkInvuln = 0.35f;
    [SerializeField] private float blinkCooldown = 5f;
    [SerializeField] private float leapRange = 7f;
    [SerializeField] private float leapTravel = 0.28f;
    [SerializeField] private float leapRadius = 3.2f;
    [SerializeField] private int leapDamage = 6;
    [SerializeField] private float leapCooldown = 8f;
    [SerializeField] private int chainDamage = 4;
    [SerializeField] private int chainJumps = 5;
    [SerializeField] private float chainHopRange = 5f;
    [SerializeField] private float chainCooldown = 7f;
    [SerializeField] private float venomBurstRadius = 3.5f;
    [SerializeField] private float venomBurstCooldown = 9f;
    [SerializeField] private float warpCooldown = 6f;
    [SerializeField] private float bulwarkSeconds = 2f;
    [SerializeField] private float bulwarkCooldown = 14f;
    [SerializeField] private float terrifyRadius = 5f;
    [SerializeField] private float terrifyCooldown = 10f;
    [SerializeField] private float maelstromRadius = 5.5f;
    [SerializeField] private int maelstromDamage = 3;
    [SerializeField] private float maelstromCooldown = 9f;
    [SerializeField] private float flameWheelRadius = 2.8f;
    [SerializeField] private int flameWheelDamage = 2;
    [SerializeField] private float flameWheelDuration = 4f;
    [SerializeField] private float flameWheelTick = 0.5f;
    [SerializeField] private float flameWheelCooldown = 11f;
    [SerializeField] private float starfallSpread = 3f;
    [SerializeField] private float starfallRadius = 1.8f;
    [SerializeField] private int starfallDamage = 5;
    [SerializeField] private float starfallCooldown = 10f;
    [SerializeField] private float broodCooldown = 14f;
    [SerializeField] private float overchargeMaxCharge = 1.6f;
    [SerializeField] private float overchargeRadius = 4.5f;
    [SerializeField] private int overchargeMaxDamage = 14;
    [SerializeField] private float overchargeCooldown = 12f;
    [SerializeField] private int wardDamage = 3;
    [SerializeField] private float wardRange = 5f;
    [SerializeField] private float wardLifetime = 12f;
    [SerializeField] private float wardCooldown = 15f;
    [SerializeField] private int empowerCharges = 5;
    [SerializeField] private float empowerCooldown = 16f;

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
    private float nextSlamTime, nextMeteorTime, nextWebTime, nextNovaTime, nextBlinkTime;
    private float nextLeapTime, nextChainTime, nextVenomBurstTime, nextWarpTime, nextBulwarkTime, nextTerrifyTime;
    private float nextMaelstromTime, nextFlameWheelTime, nextStarfallTime, nextBroodTime;
    private float lastMaelstromCooldownUsed = 1f, lastFlameWheelCooldownUsed = 1f;
    private float lastStarfallCooldownUsed = 1f, lastBroodCooldownUsed = 1f;
    private float nextOverchargeTime, nextWardTime, nextEmpowerTime;
    private float lastOverchargeCooldownUsed = 1f, lastWardCooldownUsed = 1f, lastEmpowerCooldownUsed = 1f;
    private bool channelling;
    private int empowerRemaining;
    private float lastLeapCooldownUsed = 1f, lastChainCooldownUsed = 1f, lastVenomBurstCooldownUsed = 1f;
    private float lastWarpCooldownUsed = 1f, lastBulwarkCooldownUsed = 1f, lastTerrifyCooldownUsed = 1f;
    private float lastSlamCooldownUsed = 1f, lastMeteorCooldownUsed = 1f;
    private float lastWebCooldownUsed = 1f, lastNovaCooldownUsed = 1f, lastBlinkCooldownUsed = 1f;
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
    public float SlamCooldownFraction => Mathf.Clamp01((nextSlamTime - Time.time) / Mathf.Max(0.0001f, lastSlamCooldownUsed));
    public float MeteorCooldownFraction => Mathf.Clamp01((nextMeteorTime - Time.time) / Mathf.Max(0.0001f, lastMeteorCooldownUsed));
    public float WebCooldownFraction => Mathf.Clamp01((nextWebTime - Time.time) / Mathf.Max(0.0001f, lastWebCooldownUsed));
    public float BlinkCooldownFraction => Mathf.Clamp01((nextBlinkTime - Time.time) / Mathf.Max(0.0001f, lastBlinkCooldownUsed));
    public float LeapCooldownFraction => Mathf.Clamp01((nextLeapTime - Time.time) / Mathf.Max(0.0001f, lastLeapCooldownUsed));
    public float ChainCooldownFraction => Mathf.Clamp01((nextChainTime - Time.time) / Mathf.Max(0.0001f, lastChainCooldownUsed));
    public float VenomBurstCooldownFraction => Mathf.Clamp01((nextVenomBurstTime - Time.time) / Mathf.Max(0.0001f, lastVenomBurstCooldownUsed));
    public float WarpCooldownFraction => Mathf.Clamp01((nextWarpTime - Time.time) / Mathf.Max(0.0001f, lastWarpCooldownUsed));
    public float BulwarkCooldownFraction => Mathf.Clamp01((nextBulwarkTime - Time.time) / Mathf.Max(0.0001f, lastBulwarkCooldownUsed));
    public float TerrifyCooldownFraction => Mathf.Clamp01((nextTerrifyTime - Time.time) / Mathf.Max(0.0001f, lastTerrifyCooldownUsed));
    public float MaelstromCooldownFraction => Mathf.Clamp01((nextMaelstromTime - Time.time) / Mathf.Max(0.0001f, lastMaelstromCooldownUsed));
    public float FlameWheelCooldownFraction => Mathf.Clamp01((nextFlameWheelTime - Time.time) / Mathf.Max(0.0001f, lastFlameWheelCooldownUsed));
    public float StarfallCooldownFraction => Mathf.Clamp01((nextStarfallTime - Time.time) / Mathf.Max(0.0001f, lastStarfallCooldownUsed));
    public float BroodCooldownFraction => Mathf.Clamp01((nextBroodTime - Time.time) / Mathf.Max(0.0001f, lastBroodCooldownUsed));
    public float OverchargeCooldownFraction => Mathf.Clamp01((nextOverchargeTime - Time.time) / Mathf.Max(0.0001f, lastOverchargeCooldownUsed));
    public float WardCooldownFraction => Mathf.Clamp01((nextWardTime - Time.time) / Mathf.Max(0.0001f, lastWardCooldownUsed));
    public float EmpowerCooldownFraction => Mathf.Clamp01((nextEmpowerTime - Time.time) / Mathf.Max(0.0001f, lastEmpowerCooldownUsed));

    /// How many empowered M1 attacks are banked, for the HUD.
    public int EmpowerRemaining => empowerRemaining;

    public float NovaCooldownFraction => Mathf.Clamp01((nextNovaTime - Time.time) / Mathf.Max(0.0001f, lastNovaCooldownUsed));

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
        channelling = false;
        empowerRemaining = 0;
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

            ReadAbilitySlots();
        }
        else
        {
            // Blood Frenzy suspends manual attacks, but pressing its own key
            // again must stay harmless rather than silently queueing a recast.
            ReadAbilitySlots(berserkOnly: true);
        }
    }

    /// Keys 1-4 fire whatever the player has bound to that slot. Abilities no
    /// longer own a fixed key — see AbilitySlots. Each Try* method still
    /// self-gates on its own unlock and cooldown, so an empty or stale binding
    /// is a no-op rather than a special case here.
    private void ReadAbilitySlots(bool berserkOnly = false)
    {
        if (evolution == null) return;

        for (int i = 0; i < AbilitySlots.Count; i++)
        {
            if (!Input.GetKeyDown(AbilitySlots.KeyFor(i))) continue;

            SkillId? bound = evolution.Slots.At(i);
            if (!bound.HasValue) continue;
            if (berserkOnly && bound.Value != SkillId.UnlockBerserk) continue;

            Activate(bound.Value);
        }
    }

    private void Activate(SkillId id)
    {
        MetaStats.RecordUsed(id);

        switch (id)
        {
            case SkillId.UnlockScream: TryScream(); break;
            case SkillId.UnlockBeam: TryBeam(); break;
            case SkillId.UnlockCyclone: TryCyclone(); break;
            case SkillId.UnlockBerserk: TryBerserk(); break;
            case SkillId.GroundSlam: TryGroundSlam(); break;
            case SkillId.Meteor: TryMeteor(); break;
            case SkillId.WebTrap: TryWebTrap(); break;
            case SkillId.FrostNova: TryFrostNova(); break;
            case SkillId.Blink: TryBlink(); break;
            case SkillId.LeapSmash: TryLeapSmash(); break;
            case SkillId.ChainLightning: TryChainLightning(); break;
            case SkillId.VenomBurst: TryVenomBurst(); break;
            case SkillId.Warp: TryWarp(); break;
            case SkillId.Bulwark: TryBulwark(); break;
            case SkillId.Terrify: TryTerrify(); break;
            case SkillId.Maelstrom: TryMaelstrom(); break;
            case SkillId.FlameWheel: TryFlameWheel(); break;
            case SkillId.Starfall: TryStarfall(); break;
            case SkillId.BroodCall: TryBroodCall(); break;
            case SkillId.Overcharge: TryOvercharge(); break;
            case SkillId.Ward: TryWard(); break;
            case SkillId.Empower: TryEmpower(); break;
        }
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
    private float ApplyCooldownReduction(float baseCooldown, float extraReduction = 0f)
    {
        if (evolution == null) return baseCooldown;
        float total = Mathf.Clamp01(evolution.AdrenalineReduction + evolution.BloodlustReduction + extraReduction);
        return baseCooldown * (1f - total);
    }

    /// Crit roll, then Berserk's flat multiplier if its window is active —
    /// shared by every damage source (melee, ranged, scream, beam, cyclone).
    /// Ogre line. A hard shove plus damage — the bruiser's answer to being
    /// surrounded, which is exactly the situation that line courts.
    private void TryGroundSlam()
    {
        if (Time.time < nextSlamTime) return;
        lastSlamCooldownUsed = ApplyCooldownReduction(slamCooldown);
        nextSlamTime = Time.time + lastSlamCooldownUsed;

        ScreamVfx.SpawnRing(transform.position, slamRadius, new Color(0.9f, 0.7f, 0.35f, 0.6f));
        SpellFx.Play(SpellFx.BrightFire, transform.position, slamRadius * 1.8f, 1.6f);
        Juice.Boom();

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, slamRadius, guardLayer))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;

            guard.TakeDamage(ApplyDamageModifiers(slamDamage), AttackType.Melee);
            if (guard.Health <= 0) evolution?.NotifyKill();

            if (hit.TryGetComponent<GuardMotor>(out var motor))
            {
                Vector2 away = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                motor.ApplyKnockback(away * knockbackForce * 1.6f, knockbackDuration * 1.5f);
            }
        }
    }

    /// Wraith line. Lands where you aimed after a delay, so it has to be led —
    /// the ranged line's reward for holding distance and reading the swarm.
    private void TryMeteor()
    {
        if (Time.time < nextMeteorTime) return;
        lastMeteorCooldownUsed = ApplyCooldownReduction(meteorCooldown);
        nextMeteorTime = Time.time + lastMeteorCooldownUsed;

        StartCoroutine(MeteorRoutine(AimPoint()));
    }

    private IEnumerator MeteorRoutine(Vector3 target)
    {
        // Telegraph first: the player (and anything watching) can see where it
        // will land, same contract the guards' attack windup gives the player.
        ScreamVfx.SpawnRing(target, meteorRadius, new Color(1f, 0.5f, 0.1f, 0.5f));

        yield return new WaitForSeconds(meteorDelay);

        SpellFx.Play(SpellFx.Fire, target, meteorRadius * 2.2f, 1.5f);
        Juice.Boom();

        foreach (var hit in Physics2D.OverlapCircleAll(target, meteorRadius, guardLayer))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            guard.TakeDamage(ApplyDamageModifiers(meteorDamage), AttackType.Ranged);
            if (guard.Health <= 0) evolution?.NotifyKill();
        }
    }

    /// Crawler line. A hazard you place deliberately, rather than one you leave
    /// behind by moving — the same mechanic used with intent.
    private void TryWebTrap()
    {
        if (Time.time < nextWebTime) return;
        lastWebCooldownUsed = ApplyCooldownReduction(webCooldown);
        nextWebTime = Time.time + lastWebCooldownUsed;

        var form = evolution != null ? evolution.Form : default;
        HazardZone.Spawn(AimPoint(), webRadius, webDuration, 1,
                         new Color(0.55f, 0.9f, 0.35f, 0.55f),
                         slowMultiplier: 0.35f,
                         spreads: form.VenomSpreads);

        SpellFx.Play(SpellFx.FelSpell, AimPoint(), webRadius * 2f, 1.2f);
    }

    /// Wisp line. No damage at all — it buys distance, which is the only
    /// defence a form with 0.70x damage and paper health actually has.
    private void TryFrostNova()
    {
        if (Time.time < nextNovaTime) return;
        lastNovaCooldownUsed = ApplyCooldownReduction(novaCooldown);
        nextNovaTime = Time.time + lastNovaCooldownUsed;

        ScreamVfx.SpawnRing(transform.position, novaRadius, new Color(0.5f, 0.85f, 1f, 0.6f));
        SpellFx.Play(SpellFx.Freezing, transform.position, novaRadius * 1.9f, 1.4f);

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, novaRadius, guardLayer))
        {
            if (hit.TryGetComponent<GuardMotor>(out var motor))
                motor.ApplySlow(0.3f, 3f);
        }
    }

    /// Wisp line. A hard reposition rather than a dash: no travel, so it goes
    /// through anything in the way. The protection-circle effect plays at both
    /// ends, which reads as a portal opening and closing.
    private void TryBlink()
    {
        if (Time.time < nextBlinkTime) return;
        lastBlinkCooldownUsed = ApplyCooldownReduction(blinkCooldown);
        nextBlinkTime = Time.time + lastBlinkCooldownUsed;

        Vector3 from = transform.position;
        Vector3 to = AimPoint();

        // Clamp to range so it can't be used to cross the whole floor.
        Vector3 offset = to - from;
        if (offset.magnitude > blinkRange) to = from + offset.normalized * blinkRange;
        to.z = from.z;

        SpellFx.Play(SpellFx.ProtectionCircle, from, 3f, 1.6f);

        // Movement goes through the rigidbody, per the repo's movement rule —
        // writing transform.position would desync the physics body.
        if (TryGetComponent<Rigidbody2D>(out var body)) body.position = to;
        else transform.position = to;

        SpellFx.Play(SpellFx.ProtectionCircle, to, 3f, 1.6f);
        StartCoroutine(BlinkInvulnerability());
    }

    private IEnumerator BlinkInvulnerability()
    {
        if (!TryGetComponent<PlayerHealth>(out var health)) yield break;

        health.Invulnerable = true;
        yield return new WaitForSeconds(blinkInvuln);

        // PlayerDash owns invulnerability during a Phase Dash; don't clear it
        // out from under an overlapping dash.
        var dash = GetComponent<PlayerDash>();
        if (dash == null || !dash.IsDashing) health.Invulnerable = false;
    }

    /// Ogre line. Travels through the air rather than teleporting, so it can be
    /// read and reacted to, and lands as a slam — the bruiser's way of choosing
    /// a fight instead of waiting for one.
    private void TryLeapSmash()
    {
        if (Time.time < nextLeapTime) return;
        lastLeapCooldownUsed = ApplyCooldownReduction(leapCooldown);
        nextLeapTime = Time.time + lastLeapCooldownUsed;

        StartCoroutine(LeapRoutine(ClampToRange(AimPoint(), leapRange)));
    }

    private IEnumerator LeapRoutine(Vector3 target)
    {
        var body = GetComponent<Rigidbody2D>();
        Vector2 from = body != null ? body.position : (Vector2)transform.position;

        // Airborne and untouchable, which is what makes leaping *into* a crowd
        // a real option rather than suicide.
        TryGetComponent<PlayerHealth>(out var health);
        if (health != null) health.Invulnerable = true;

        float elapsed = 0f;
        while (elapsed < leapTravel)
        {
            elapsed += Time.deltaTime;
            Vector2 position = Vector2.Lerp(from, target, Mathf.Clamp01(elapsed / leapTravel));

            if (body != null) body.MovePosition(position);
            else transform.position = position;

            yield return null;
        }

        var dash = GetComponent<PlayerDash>();
        if (health != null && (dash == null || !dash.IsDashing)) health.Invulnerable = false;

        ScreamVfx.SpawnRing(target, leapRadius, new Color(0.9f, 0.65f, 0.3f, 0.65f));
        SpellFx.Play(SpellFx.BrightFire, target, leapRadius * 1.9f, 1.7f);
        Juice.Boom();

        foreach (var hit in Physics2D.OverlapCircleAll(target, leapRadius, guardLayer))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;

            guard.TakeDamage(ApplyDamageModifiers(leapDamage), AttackType.Melee);
            if (guard.Health <= 0) evolution?.NotifyKill();

            if (hit.TryGetComponent<GuardMotor>(out var motor))
            {
                Vector2 away = ((Vector2)hit.transform.position - (Vector2)target).normalized;
                motor.ApplyKnockback(away * knockbackForce * 1.8f, knockbackDuration * 1.5f);
            }
        }
    }

    /// Wraith line. Hops outward from the nearest foe and never revisits one,
    /// so it rewards a packed crowd — the situation a kiting build usually flees.
    private void TryChainLightning()
    {
        if (Time.time < nextChainTime) return;
        lastChainCooldownUsed = ApplyCooldownReduction(chainCooldown);
        nextChainTime = Time.time + lastChainCooldownUsed;

        var struck = new List<GuardHealth>();
        Vector3 from = transform.position;

        for (int jump = 0; jump < chainJumps; jump++)
        {
            GuardHealth next = NearestGuardExcluding(from, chainHopRange, struck);
            if (next == null) break;

            Vector3 offset = next.transform.position - from;
            BeamVfx.Spawn(from, ((Vector2)offset).normalized, offset.magnitude, 0.35f,
                          new Color(0.6f, 0.8f, 1f, 0.9f));
            HitFlashFx.Spawn(next.transform.position, new Color(0.7f, 0.85f, 1f, 0.9f), 0.5f);

            next.TakeDamage(ApplyDamageModifiers(chainDamage), AttackType.Ranged);
            if (next.Health <= 0) evolution?.NotifyKill();

            struck.Add(next);
            from = next.transform.position;
        }
    }

    private GuardHealth NearestGuardExcluding(Vector3 origin, float range, List<GuardHealth> exclude)
    {
        GuardHealth best = null;
        float bestDistance = float.MaxValue;

        foreach (var hit in Physics2D.OverlapCircleAll(origin, range, guardLayer))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            if (guard.Health <= 0 || exclude.Contains(guard)) continue;

            float distance = Vector2.Distance(origin, guard.transform.position);
            if (distance < bestDistance) { bestDistance = distance; best = guard; }
        }

        return best;
    }

    /// Crawler line. Places the line's hazard deliberately and all at once,
    /// rather than trailing it behind you a puff at a time.
    private void TryVenomBurst()
    {
        if (Time.time < nextVenomBurstTime) return;
        lastVenomBurstCooldownUsed = ApplyCooldownReduction(venomBurstCooldown);
        nextVenomBurstTime = Time.time + lastVenomBurstCooldownUsed;

        var form = evolution != null ? evolution.Form : default;
        float duration = Mathf.Max(3f, form.VenomDuration);

        const int Clouds = 6;
        for (int i = 0; i < Clouds; i++)
        {
            float angle = (i / (float)Clouds) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * venomBurstRadius * 0.7f;
            HazardZone.Spawn(transform.position + offset, 1.4f, duration, 1,
                             new Color(0.45f, 0.9f, 0.25f, 0.55f),
                             form.VenomSlows ? 0.45f : 1f, form.VenomSpreads);
        }

        SpellFx.Play(SpellFx.FelSpell, transform.position, venomBurstRadius * 2f, 1.3f);
    }

    /// Wisp line. Unlike Blink you do NOT choose where — it's an escape, not a
    /// repositioning tool, and the randomness is what pays for the short cooldown.
    private void TryWarp()
    {
        if (Time.time < nextWarpTime) return;
        lastWarpCooldownUsed = ApplyCooldownReduction(warpCooldown);
        nextWarpTime = Time.time + lastWarpCooldownUsed;

        Vector3 from = transform.position;
        Vector3 to = FindWarpDestination(from);

        SpellFx.Play(SpellFx.Phantom, from, 3f, 1.7f);

        if (TryGetComponent<Rigidbody2D>(out var body)) body.position = to;
        else transform.position = to;

        SpellFx.Play(SpellFx.ProtectionCircle, to, 3f, 1.6f);
        StartCoroutine(BlinkInvulnerability());
    }

    /// Random point 8-16 units away that isn't inside geometry. Falls back to
    /// standing still rather than warping into a wall.
    private Vector3 FindWarpDestination(Vector3 from)
    {
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(8f, 16f);
            Vector3 candidate = from + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;
            candidate.z = from.z;

            bool blocked = false;
            foreach (var hit in Physics2D.OverlapCircleAll(candidate, 0.6f))
            {
                if (hit.isTrigger) continue;                            // pickups, doors
                if (hit.TryGetComponent<GuardHealth>(out _)) continue;   // landing on a guard is fine
                if (hit.transform == transform) continue;
                blocked = true;
                break;
            }

            if (!blocked) return candidate;
        }

        return from;
    }

    /// Universal. No damage, no movement — two seconds where nothing lands.
    private void TryBulwark()
    {
        if (Time.time < nextBulwarkTime) return;
        lastBulwarkCooldownUsed = ApplyCooldownReduction(bulwarkCooldown);
        nextBulwarkTime = Time.time + lastBulwarkCooldownUsed;

        StartCoroutine(BulwarkRoutine());
    }

    private IEnumerator BulwarkRoutine()
    {
        if (!TryGetComponent<PlayerHealth>(out var health)) yield break;

        health.Invulnerable = true;
        SpellFx.Play(SpellFx.ProtectionCircle, transform.position, 3.5f, 0.8f);

        yield return new WaitForSeconds(bulwarkSeconds);

        var dash = GetComponent<PlayerDash>();
        if (dash == null || !dash.IsDashing) health.Invulnerable = false;
    }

    /// Universal panic button. Pure knockback, so every line has a way out of a
    /// pile-up even if its own kit has no answer to being surrounded.
    private void TryTerrify()
    {
        if (Time.time < nextTerrifyTime) return;
        lastTerrifyCooldownUsed = ApplyCooldownReduction(terrifyCooldown);
        nextTerrifyTime = Time.time + lastTerrifyCooldownUsed;

        ScreamVfx.SpawnRing(transform.position, terrifyRadius, new Color(0.8f, 0.4f, 1f, 0.6f));
        SpellFx.Play(SpellFx.Midnight, transform.position, terrifyRadius * 1.8f, 1.5f);

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, terrifyRadius, guardLayer))
        {
            if (!hit.TryGetComponent<GuardMotor>(out var motor)) continue;

            Vector2 away = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            motor.ApplyKnockback(away * knockbackForce * 2.2f, knockbackDuration * 2f);
        }
    }

    /// Ogre line. Knockback with the sign flipped — it drags everything into
    /// claw range instead of pushing it out. The bruiser's problem is never
    /// being surrounded, it's things refusing to come close.
    private void TryMaelstrom()
    {
        if (Time.time < nextMaelstromTime) return;
        lastMaelstromCooldownUsed = ApplyCooldownReduction(maelstromCooldown);
        nextMaelstromTime = Time.time + lastMaelstromCooldownUsed;

        SpellFx.Play(SpellFx.Vortex, transform.position, maelstromRadius * 1.6f, 1.1f);
        ScreamVfx.SpawnRing(transform.position, maelstromRadius, new Color(0.4f, 0.5f, 0.9f, 0.5f));

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, maelstromRadius, guardLayer))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;

            guard.TakeDamage(ApplyDamageModifiers(maelstromDamage), AttackType.Melee);
            if (guard.Health <= 0) { evolution?.NotifyKill(); continue; }

            if (hit.TryGetComponent<GuardMotor>(out var motor))
            {
                Vector2 inward = ((Vector2)transform.position - (Vector2)hit.transform.position).normalized;
                motor.ApplyKnockback(inward * knockbackForce * 1.4f, knockbackDuration * 2f);
            }
        }
    }

    /// Wraith line. The only thing on the bolt line that rewards standing in the
    /// middle of things, so it's a deliberate counterweight to pure kiting.
    private void TryFlameWheel()
    {
        if (Time.time < nextFlameWheelTime) return;
        lastFlameWheelCooldownUsed = ApplyCooldownReduction(flameWheelCooldown);
        nextFlameWheelTime = Time.time + lastFlameWheelCooldownUsed;

        StartCoroutine(FlameWheelRoutine());
    }

    private IEnumerator FlameWheelRoutine()
    {
        float elapsed = 0f;

        while (elapsed < flameWheelDuration)
        {
            // Follows the player rather than burning where it was cast — that's
            // what makes it a movement tool and not a second Web Trap.
            SpellFx.Play(SpellFx.FireSpin, transform.position, flameWheelRadius * 2f, 1.4f);

            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, flameWheelRadius, guardLayer))
            {
                if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
                guard.TakeDamage(ApplyDamageModifiers(flameWheelDamage), AttackType.Ranged);
                if (guard.Health <= 0) evolution?.NotifyKill();
            }

            elapsed += flameWheelTick;
            yield return new WaitForSeconds(flameWheelTick);
        }
    }

    /// Wisp line. Three small impacts scattered around the cursor rather than
    /// one big one on it — it covers ground instead of demanding precision,
    /// which is the opposite trade to Meteor.
    private void TryStarfall()
    {
        if (Time.time < nextStarfallTime) return;
        lastStarfallCooldownUsed = ApplyCooldownReduction(starfallCooldown);
        nextStarfallTime = Time.time + lastStarfallCooldownUsed;

        Vector3 center = AimPoint();
        for (int i = 0; i < 3; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * starfallSpread;
            StartCoroutine(StarfallImpact(center + new Vector3(scatter.x, scatter.y, 0f), 0.25f * i));
        }
    }

    private IEnumerator StarfallImpact(Vector3 target, float delay)
    {
        yield return new WaitForSeconds(delay);

        ScreamVfx.SpawnRing(target, starfallRadius, new Color(0.6f, 0.6f, 1f, 0.5f));
        yield return new WaitForSeconds(0.45f);

        SpellFx.Play(SpellFx.Nebula, target, starfallRadius * 2.4f, 1.5f);
        Juice.Boom();

        foreach (var hit in Physics2D.OverlapCircleAll(target, starfallRadius, guardLayer))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            guard.TakeDamage(ApplyDamageModifiers(starfallDamage), AttackType.Ranged);
            if (guard.Health <= 0) evolution?.NotifyKill();
        }
    }

    /// Crawler line. Reuses the Lich's ally wholesale — the Broodmother fantasy
    /// is the same mechanic with a different reason for existing, and a second
    /// spawnable minion type would be two systems doing one job.
    private void TryBroodCall()
    {
        if (Time.time < nextBroodTime) return;
        lastBroodCooldownUsed = ApplyCooldownReduction(broodCooldown);
        nextBroodTime = Time.time + lastBroodCooldownUsed;

        Vector3 origin = ClampToRange(AimPoint(), 6f);
        SpellFx.Play(SpellFx.MagicSpell, origin, 3.5f, 1.3f);

        for (int i = 0; i < 3; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * 1.2f;
            SummonedAlly.Spawn(origin + new Vector3(scatter.x, scatter.y, 0f),
                               Mathf.Max(1, 1 + (evolution != null ? evolution.MeleeDamageBonus : 0)), 14f);
        }
    }

    /// Wraith line, and the only ability in the game you *hold*. Damage and
    /// radius scale with how long you stand still charging, so it asks for the
    /// one thing the swarm never gives you: a few seconds of not moving.
    private void TryOvercharge()
    {
        if (channelling || Time.time < nextOverchargeTime) return;
        lastOverchargeCooldownUsed = ApplyCooldownReduction(overchargeCooldown);
        nextOverchargeTime = Time.time + lastOverchargeCooldownUsed;

        StartCoroutine(OverchargeRoutine());
    }

    private IEnumerator OverchargeRoutine()
    {
        channelling = true;

        // Whichever key the player bound it to — the ability doesn't own a key,
        // so the hold has to be read from the current binding.
        int slot = evolution != null ? evolution.Slots.IndexOf(SkillId.Overcharge) : -1;
        KeyCode key = slot >= 0 ? AbilitySlots.KeyFor(slot) : KeyCode.None;

        var body = GetComponent<Rigidbody2D>();
        float charge = 0f;
        float nextRing = 0f;

        while (charge < overchargeMaxCharge)
        {
            // Releasing or moving doesn't waste the cast, it just fires early at
            // whatever power was built. Punishing the interrupt would make this
            // unusable in the only situation it's for.
            if (key != KeyCode.None && !Input.GetKey(key)) break;
            if (body != null && body.linearVelocity.magnitude > 0.5f) break;

            charge += Time.deltaTime;

            if (Time.time >= nextRing)
            {
                nextRing = Time.time + 0.4f;
                float grown = Mathf.Lerp(1.2f, overchargeRadius * 1.4f, charge / overchargeMaxCharge);
                SpellFx.Play(SpellFx.ChannelRing, transform.position, grown, 1.1f,
                             new Color(0.7f, 0.5f, 1f, 0.8f));
            }

            yield return null;
        }

        channelling = false;

        // A tap still does something, it just isn't worth the cooldown.
        float power = Mathf.Clamp01(charge / overchargeMaxCharge);
        float radius = overchargeRadius * Mathf.Lerp(0.45f, 1f, power);
        int damage = Mathf.Max(1, Mathf.RoundToInt(overchargeMaxDamage * Mathf.Lerp(0.25f, 1f, power)));

        Vector3 target = ClampToRange(AimPoint(), 9f);
        ScreamVfx.SpawnRing(target, radius, new Color(0.75f, 0.5f, 1f, 0.6f));
        SpellFx.Play(SpellFx.Nebula, target, radius * 2.2f, 1.4f);
        Juice.Boom();

        foreach (var hit in Physics2D.OverlapCircleAll(target, radius, guardLayer))
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;
            guard.TakeDamage(ApplyDamageModifiers(damage), AttackType.Ranged);
            if (guard.Health <= 0) evolution?.NotifyKill();
        }
    }

    /// Universal. Holds ground instead of chasing, which is what separates it
    /// from Brood Call — it defends a spot you intend to come back to.
    private void TryWard()
    {
        if (Time.time < nextWardTime) return;
        lastWardCooldownUsed = ApplyCooldownReduction(wardCooldown);
        nextWardTime = Time.time + lastWardCooldownUsed;

        Ward.Spawn(ClampToRange(AimPoint(), 6f),
                   ApplyDamageModifiers(wardDamage), wardRange, wardLifetime);
    }

    /// Universal. Charges are spent per *attack*, not per enemy struck — an
    /// arcing claw that catches three guards would otherwise eat the whole buff
    /// in one swing.
    private void TryEmpower()
    {
        if (Time.time < nextEmpowerTime) return;
        lastEmpowerCooldownUsed = ApplyCooldownReduction(empowerCooldown);
        nextEmpowerTime = Time.time + lastEmpowerCooldownUsed;

        empowerRemaining = empowerCharges;
        SpellFx.Play(SpellFx.MagicSpell, transform.position, 3f, 1.4f,
                     new Color(1f, 0.85f, 0.4f, 0.9f));
    }

    /// Consumes one charge if any are banked. Called by TryMelee/TryRanged
    /// only — the two things M1 can be.
    private bool ConsumeEmpower()
    {
        if (empowerRemaining <= 0) return false;
        empowerRemaining--;
        return true;
    }

    private Vector3 ClampToRange(Vector3 target, float range)
    {
        Vector3 offset = target - transform.position;
        if (offset.magnitude > range) target = transform.position + offset.normalized * range;
        target.z = transform.position.z;
        return target;
    }

    /// World point under the cursor, on the play plane.
    private Vector3 AimPoint()
    {
        if (Camera.main == null) return transform.position + (Vector3)AimDirection() * 4f;

        Vector3 point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        point.z = transform.position.z;
        return point;
    }

    private int ApplyDamageModifiers(int damage)
    {
        int result = evolution != null && evolution.RollCrit() ? damage * 2 : damage;

        // Each evolution form hits for its own amount (MonsterForm.Stats) —
        // the claw line trades speed for damage, the dash line the reverse.
        if (evolution != null)
            result = Mathf.RoundToInt(result * evolution.FormDamageMultiplier);

        if (Time.time < berserkEndTime) result = Mathf.RoundToInt(result * berserkDamageMultiplier);
        return Mathf.Max(1, result);
    }

    /// Rime line: everything you strike is left crawling. Applied wherever a
    /// hit lands, alongside the knockback check, so it covers claws, scream,
    /// whirlwind and the line abilities without each one knowing about forms.
    private void ApplyFormSlow(GuardHealth guard)
    {
        if (evolution == null || !evolution.Form.SlowOnHit) return;
        if (guard.TryGetComponent<GuardMotor>(out var motor))
            motor.ApplySlow(0.5f, 2f);
    }

    private void ApplyKnockbackIfUnlocked(GuardHealth guard, Vector2 aim)
    {
        ApplyFormSlow(guard);

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

        if (ConsumeEmpower())
        {
            damage *= 2;
            SpellFx.Play(SpellFx.WeaponHit, (Vector2)transform.position + aim * (range * 0.7f),
                         range * 1.3f, 1.8f, new Color(1f, 0.9f, 0.5f, 0.95f));
        }

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
        // Jittered angle and a mirrored slash half the time so a flurry of
        // Greater Claws swipes doesn't stamp the same decal repeatedly.
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + Random.Range(-10f, 10f);
        fx.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Combat/claw_slash");
        sr.color = new Color(1f, 0.5f, 0.55f, 0.95f); // blood-tinged talons rather than the original steel/gold
        sr.flipY = Random.value < 0.5f;
        sr.sortingOrder = 11;
        fx.transform.localScale = Vector3.one * (range * 0.5f * Random.Range(0.92f, 1.1f));

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

        // One charge per volley, not per bolt — Triple Bolt is still a single
        // attack, and spending three charges on it would make the two cards
        // actively fight each other.
        bool empowered = ConsumeEmpower();
        if (empowered)
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            SpellFx.Play(SpellFx.MagickaHit, origin, 2.2f, 1.8f, new Color(1f, 0.85f, 0.5f, 0.95f));
        }

        if (evolution.TripleShot)
        {
            const float spread = 12f;
            SpawnBullet(aim, empowered);
            SpawnBullet(Quaternion.Euler(0, 0, spread) * aim, empowered);
            SpawnBullet(Quaternion.Euler(0, 0, -spread) * aim, empowered);
        }
        else if (evolution.DoubleShot)
        {
            const float spread = 8f;
            SpawnBullet(Quaternion.Euler(0, 0, spread) * aim, empowered);
            SpawnBullet(Quaternion.Euler(0, 0, -spread) * aim, empowered);
        }
        else
        {
            SpawnBullet(aim, empowered);
        }
    }

    private void SpawnBullet(Vector2 dir, bool empowered = false)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        GameObject go = Instantiate(bulletPrefab, origin, Quaternion.identity);
        int damage = ApplyDamageModifiers(rangedDamage + (evolution != null ? evolution.RangedDamageBonus : 0));
        if (empowered) damage *= 2;
        int pierce = evolution != null && evolution.RangedPierce ? 1 : 0;
        float scale = evolution != null ? evolution.BulletScale : 1f;
        bool explosive = evolution != null && (evolution.ExplosiveRounds || evolution.Form.BoltsExplode);
        bool ricochet = evolution != null && evolution.Ricochet;

        // Cast spark at the hand — the Arcane Bolt otherwise appears from nothing.
        HitFlashFx.Spawn(origin, new Color(0.72f, 0.45f, 1f, 0.8f), 0.3f);

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

        var screamColor = new Color(0.45f, 1f, 0.65f, 0.55f); // ghostly green Scream
        if (radial) ScreamVfx.SpawnRing(transform.position, range, screamColor);
        else ScreamVfx.SpawnCone(transform.position, aim, screamArc, range, screamColor);

        // Animated art on top of the procedural shape; the shape still shows
        // the true hitbox, this just sells it.
        SpellFx.Play(evolution != null && evolution.ScreamSlow ? SpellFx.Freezing : SpellFx.SunBurn,
                     transform.position, range * 1.6f, 1.5f);

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
        BeamVfx.Spawn(transform.position, aim, beamLength, beamWidth, new Color(1f, 0.42f, 0.1f, 0.8f)); // Dragonfire Beam plume

        float beamAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        SpellFx.Play(SpellFx.FlameLash, transform.position + (Vector3)(aim * beamLength * 0.45f),
                     beamLength * 0.9f, 1.6f, rotation: beamAngle);

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
