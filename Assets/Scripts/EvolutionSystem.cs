using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum SkillId
{
    UnlockRanged,
    UnlockScream,
    UnlockDash,
    BiggerClaw,
    DoubleShot,
    TripleShot,
    RadiusScream,
    Vitality,
    Swift,
    DashPhase,
    DashLunge,
    MeleeDamageUp,
    RangedDamageUp,
    ScreamDamageUp,
    RangedPierce,
    ScreamCooldownDown,
    Adrenaline,
    Regeneration,
    DashCooldownDown,
    CritChance,
    BiggerBullets,
    ExplosiveRounds,
    LifeSteal,
    Thorns,
    UnlockBeam,
    BeamDamageUp,
    UnlockCyclone,
    CycloneDamageUp,
    UnlockBerserk,
    BerserkDurationUp,
    Ricochet,
    Armor,
    SecondWind,
    KnockbackOnHit,
    ScreamSlow,
    Titan
}

/// Drives the "evolution track" from the GDD: kills feed a level counter,
/// and each level-up offers 3 skill cards (LevelUpUI), pausing the game
/// until the player picks one. The run starts with melee claw only. The
/// very first level-up is NOT random — it always offers exactly Dash,
/// Bigger Claw, and Ranged Attack, so every run picks an archetype: mobility
/// (dash), melee (bigger claw), or ranged. Bigger Claw and Ranged Attack are
/// mutually exclusive forever after (they both define what M1 does — see
/// PlayerCombat), while Dash always stays available alongside either. Later
/// levels draw randomly from the remaining pool; upgrade cards (Double Shot,
/// Radius Scream) only enter the pool once their base ability is unlocked.
/// Falls back to auto-granting the first option if no LevelUpUI exists in
/// the scene, so progression never stalls during a headless/dev-scene test.
public class EvolutionSystem : MonoBehaviour
{
    [SerializeField] private int killsPerLevel = 5;

    public bool UnlockedRanged { get; private set; }
    public bool UnlockedScream { get; private set; }
    public bool UnlockedDash { get; private set; }
    public bool BiggerClaw { get; private set; }
    public bool DoubleShot { get; private set; }
    public bool TripleShot { get; private set; }
    public bool RadiusScream { get; private set; }
    public bool DashPhase { get; private set; }
    public bool DashLunge { get; private set; }
    public int MeleeDamageBonus { get; private set; }
    public int RangedDamageBonus { get; private set; }
    public int ScreamDamageBonus { get; private set; }
    public bool RangedPierce { get; private set; }
    public float ScreamCooldownReduction { get; private set; }
    public float AdrenalineReduction { get; private set; }
    public float DashCooldownReduction { get; private set; }
    public int CritChancePercent { get; private set; }
    public float BulletScale { get; private set; } = 1f;
    public bool ExplosiveRounds { get; private set; }
    public int LifeStealAmount { get; private set; }
    public int ThornsDamage { get; private set; }
    public bool UnlockedBeam { get; private set; }
    public int BeamDamageBonus { get; private set; }
    public bool UnlockedCyclone { get; private set; }
    public int CycloneDamageBonus { get; private set; }
    public bool UnlockedBerserk { get; private set; }
    public float BerserkDuration { get; private set; } = 25f;
    public bool Ricochet { get; private set; }
    public int ArmorFlat { get; private set; }
    public bool KnockbackOnHit { get; private set; }
    public bool ScreamSlow { get; private set; }
    public bool Titan { get; private set; }
    public float TitanDamageMultiplier { get; private set; } = 1f;

    /// Applied to melee/scream/cyclone reach (claw range, scream range,
    /// cyclone radius) — "physical" attacks, not the ranged/beam archetype.
    public float TitanSizeMultiplier { get; private set; } = 1f;
    private bool secondWindUnlocked;
    private bool secondWindUsed;

    private const float TitanScale = 1.6f;
    private const float TitanSpeedMultiplier = 0.6f;
    private const float TitanDamageMultiplierValue = 2f;
    private const float TitanSizeMultiplierValue = 1.5f;

    /// Rolled by whichever attack lands (Melee/Ranged/Scream) right before
    /// applying damage. Returns true ~CritChancePercent% of the time.
    public bool RollCrit() => CritChancePercent > 0 && Random.value * 100f < CritChancePercent;

    /// Called by PlayerCombat (melee/scream) and Bullet's onKill callback
    /// (ranged) right after confirming a kill — heals per Life Steal stack.
    public void NotifyKill()
    {
        if (LifeStealAmount > 0 && playerHealth != null) playerHealth.Heal(LifeStealAmount);
    }

    /// Called by PlayerHealth when a hit would otherwise be lethal. Returns
    /// true (and consumes the once-per-run charge) if Second Wind is
    /// unlocked and hasn't already saved the player this run.
    public bool TryConsumeSecondWind()
    {
        if (!secondWindUnlocked || secondWindUsed) return false;
        secondWindUsed = true;
        return true;
    }

    private struct Skill
    {
        public SkillId Id;
        public string Title;
        public string Description;
        public int MaxStacks;
    }

    private static readonly Skill[] AllSkills =
    {
        new Skill { Id = SkillId.UnlockRanged, Title = "Ranged Attack", Description = "Replaces your claw with a ranged energy shot on left-click. Permanent — retires melee for this run.", MaxStacks = 1 },
        new Skill { Id = SkillId.UnlockScream, Title = "Scream", Description = "Unlocks a shockwave attack (space).", MaxStacks = 1 },
        new Skill { Id = SkillId.UnlockDash, Title = "Dash", Description = "Unlocks a quick burst of speed (left shift).", MaxStacks = 1 },
        new Skill { Id = SkillId.BiggerClaw, Title = "Bigger Claw", Description = "Claw triples in range, damage doubles. Permanent — retires the ranged path for this run.", MaxStacks = 1 },
        new Skill { Id = SkillId.DoubleShot, Title = "Double Shot", Description = "Ranged attack fires 2 bullets in a narrow spread.", MaxStacks = 1 },
        new Skill { Id = SkillId.TripleShot, Title = "Triple Shot", Description = "Ranged attack fires 3 bullets: one straight, two angled.", MaxStacks = 1 },
        new Skill { Id = SkillId.RadiusScream, Title = "Radius Scream", Description = "Scream hits all around you, not just in front.", MaxStacks = 1 },
        new Skill { Id = SkillId.Vitality, Title = "Vitality", Description = "+2 max health.", MaxStacks = 5 },
        new Skill { Id = SkillId.Swift, Title = "Swift", Description = "+15% move speed.", MaxStacks = 5 },
        new Skill { Id = SkillId.DashPhase, Title = "Phase Dash", Description = "Dash grants brief invulnerability and a see-through sprite. Permanent — retires Lunge Dash for this run.", MaxStacks = 1 },
        new Skill { Id = SkillId.DashLunge, Title = "Lunge Dash", Description = "Dash knocks back and damages enemies in your path. You take a small amount of damage too (capped at 50% of current HP). Permanent — retires Phase Dash for this run.", MaxStacks = 1 },
        new Skill { Id = SkillId.MeleeDamageUp, Title = "Claw Sharpness", Description = "+1 claw damage.", MaxStacks = 3 },
        new Skill { Id = SkillId.RangedDamageUp, Title = "Overcharged Rounds", Description = "+1 ranged shot damage.", MaxStacks = 3 },
        new Skill { Id = SkillId.ScreamDamageUp, Title = "Piercing Scream", Description = "+1 scream damage.", MaxStacks = 3 },
        new Skill { Id = SkillId.RangedPierce, Title = "Ranged Pierce", Description = "Ranged shots pass through the first enemy hit instead of stopping.", MaxStacks = 1 },
        new Skill { Id = SkillId.ScreamCooldownDown, Title = "Screaming Fury", Description = "Scream cooldown -15%.", MaxStacks = 3 },
        new Skill { Id = SkillId.Adrenaline, Title = "Adrenaline", Description = "All attack cooldowns -10%.", MaxStacks = 3 },
        new Skill { Id = SkillId.Regeneration, Title = "Regeneration", Description = "Slowly regenerate 1 health every few seconds.", MaxStacks = 3 },
        new Skill { Id = SkillId.DashCooldownDown, Title = "Restless", Description = "Dash cooldown -15%.", MaxStacks = 3 },
        new Skill { Id = SkillId.CritChance, Title = "Precision", Description = "+10% chance for any attack to deal double damage.", MaxStacks = 3 },
        new Skill { Id = SkillId.BiggerBullets, Title = "Bigger Bullets", Description = "Bullets are 25% bigger, and hit a wider area.", MaxStacks = 3 },
        new Skill { Id = SkillId.ExplosiveRounds, Title = "Explosive Rounds", Description = "Ranged shots explode on impact, damaging nearby enemies too.", MaxStacks = 1 },
        new Skill { Id = SkillId.LifeSteal, Title = "Life Steal", Description = "Heal 1 HP on every kill.", MaxStacks = 3 },
        new Skill { Id = SkillId.Thorns, Title = "Thorns", Description = "Getting hit sends out a retaliation shockwave, damaging nearby enemies.", MaxStacks = 3 },
        new Skill { Id = SkillId.UnlockBeam, Title = "Kamehameha Beam", Description = "Unlocks a devastating charged beam attack (R).", MaxStacks = 1 },
        new Skill { Id = SkillId.BeamDamageUp, Title = "Overcharged Beam", Description = "+2 beam damage.", MaxStacks = 3 },
        new Skill { Id = SkillId.UnlockCyclone, Title = "Cyclone", Description = "Unlocks a spinning claw whirlwind (E). Requires Bigger Claw.", MaxStacks = 1 },
        new Skill { Id = SkillId.CycloneDamageUp, Title = "Razor Cyclone", Description = "+1 cyclone damage per tick.", MaxStacks = 3 },
        new Skill { Id = SkillId.UnlockBerserk, Title = "Berserk", Description = "Unlocks a reckless rage state (F): you lose manual control for 25s while auto-attacking the nearest enemy (and auto-casting Scream/Beam/Cyclone if unlocked) with hugely boosted damage.", MaxStacks = 1 },
        new Skill { Id = SkillId.BerserkDurationUp, Title = "Sustained Rage", Description = "+1s Berserk duration.", MaxStacks = 3 },
        new Skill { Id = SkillId.Ricochet, Title = "Ricochet", Description = "Ranged shots that would stop instead bounce to a nearby enemy.", MaxStacks = 1 },
        new Skill { Id = SkillId.Armor, Title = "Armor", Description = "-1 flat damage taken from every hit (never below 1).", MaxStacks = 3 },
        new Skill { Id = SkillId.SecondWind, Title = "Second Wind", Description = "The first hit that would kill you instead leaves you at 1 HP. Once per run.", MaxStacks = 1 },
        new Skill { Id = SkillId.KnockbackOnHit, Title = "Bludgeon", Description = "Melee and Scream hits knock enemies back.", MaxStacks = 1 },
        new Skill { Id = SkillId.ScreamSlow, Title = "Chilling Scream", Description = "Scream slows every enemy it hits.", MaxStacks = 1 },
        new Skill { Id = SkillId.Titan, Title = "Titan", Description = "Grow massive: +100% damage and +50% reach on claw, scream, and cyclone. Move speed -40%. Permanent — requires Bigger Claw.", MaxStacks = 1 },
    };

    private static Skill FindSkill(SkillId id)
    {
        foreach (var s in AllSkills)
            if (s.Id == id) return s;
        return default;
    }

    private readonly Dictionary<SkillId, int> stacks = new Dictionary<SkillId, int>();

    private int level;
    private int vitalityStacks;
    private int swiftStacks;
    private int meleeDamageStacks;
    private int rangedDamageStacks;
    private int screamDamageStacks;
    private int screamCooldownStacks;
    private int adrenalineStacks;
    private int regenStacks;
    private int dashCooldownStacks;
    private int critStacks;
    private int bulletScaleStacks;
    private int lifeStealStacks;
    private int thornsStacks;
    private int beamDamageStacks;
    private int cycloneDamageStacks;
    private int berserkDurationStacks;
    private int armorStacks;
    private float regenTimer;
    private bool choicePending;

    private const float RegenTickSeconds = 5f;

    private ExpBar expBar;
    private LevelUpUI levelUpUI;
    private PlayerHealth playerHealth;
    private PlayerMoveSpeedBoost moveSpeedBoost;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        moveSpeedBoost = GetComponent<PlayerMoveSpeedBoost>();
    }

    private void Start()
    {
        expBar = FindFirstObjectByType<ExpBar>();
        levelUpUI = FindFirstObjectByType<LevelUpUI>();

        if (expBar != null)
            expBar.SetMaxExp(killsPerLevel);
        else
            Debug.LogWarning("No ExpBar was found in the scene.");
    }

    private void Update()
    {
        if (!choicePending)
        {
            // Yellow ExpPickup orbs (RunStats.BonusExp) count toward level-ups
            // alongside raw kills — a second, kill-independent XP source.
            int totalExp = RunStats.TotalKills + RunStats.BonusExp;
            int targetLevel = totalExp / killsPerLevel;
            if (level < targetLevel)
            {
                level++;
                OfferChoice();
            }
        }

        if (expBar != null)
            expBar.SetExp((RunStats.TotalKills + RunStats.BonusExp) % killsPerLevel);

        if (regenStacks > 0 && playerHealth != null)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= RegenTickSeconds)
            {
                regenTimer -= RegenTickSeconds;
                playerHealth.Heal(regenStacks);
            }
        }
    }

    private bool IsAvailable(SkillId id)
    {
        // Upgrade cards for an ability only show up once that ability is unlocked.
        if (id == SkillId.DoubleShot && !UnlockedRanged) return false;
        if (id == SkillId.TripleShot && !DoubleShot) return false;
        if (id == SkillId.RadiusScream && !UnlockedScream) return false;
        if (id == SkillId.BiggerBullets && !UnlockedRanged) return false;
        if (id == SkillId.ExplosiveRounds && !UnlockedRanged) return false;

        // Bigger Claw and Ranged Attack both decide what M1 does (see
        // PlayerCombat) — picking either permanently retires the other for
        // this run.
        if (id == SkillId.BiggerClaw && UnlockedRanged) return false;
        if (id == SkillId.UnlockRanged && BiggerClaw) return false;

        // Phase Dash and Lunge Dash are opposite dash philosophies (safe vs.
        // risky) — mutually exclusive, and both require Dash itself first.
        if ((id == SkillId.DashPhase || id == SkillId.DashLunge) && !UnlockedDash) return false;
        if (id == SkillId.DashPhase && DashLunge) return false;
        if (id == SkillId.DashLunge && DashPhase) return false;

        // Damage-up cards only show up once their base attack is committed to.
        if (id == SkillId.MeleeDamageUp && !BiggerClaw) return false;
        if (id == SkillId.RangedDamageUp && !UnlockedRanged) return false;
        if (id == SkillId.ScreamDamageUp && !UnlockedScream) return false;
        if (id == SkillId.RangedPierce && !UnlockedRanged) return false;
        if (id == SkillId.ScreamCooldownDown && !UnlockedScream) return false;
        if (id == SkillId.DashCooldownDown && !UnlockedDash) return false;
        if (id == SkillId.Ricochet && !UnlockedRanged) return false;
        if (id == SkillId.ScreamSlow && !UnlockedScream) return false;

        // New active abilities and their upgrade cards.
        if (id == SkillId.BeamDamageUp && !UnlockedBeam) return false;
        if (id == SkillId.UnlockCyclone && !BiggerClaw) return false; // melee archetype capstone
        if (id == SkillId.CycloneDamageUp && !UnlockedCyclone) return false;
        if (id == SkillId.BerserkDurationUp && !UnlockedBerserk) return false;
        if (id == SkillId.Titan && !BiggerClaw) return false; // melee archetype capstone, same gate as Cyclone

        return true;
    }

    private void OfferChoice()
    {
        List<Skill> choices;

        if (level == 1)
        {
            // The starter choice is fixed, not random: it establishes the
            // run's archetype — mobility, melee, or ranged.
            choices = new List<Skill>
            {
                FindSkill(SkillId.UnlockDash),
                FindSkill(SkillId.BiggerClaw),
                FindSkill(SkillId.UnlockRanged),
            };
        }
        else
        {
            var pool = new List<Skill>();
            foreach (var s in AllSkills)
            {
                if (!IsAvailable(s.Id)) continue;
                int taken = stacks.TryGetValue(s.Id, out int n) ? n : 0;
                if (taken < s.MaxStacks) pool.Add(s);
            }

            if (pool.Count == 0) return; // everything maxed out

            choices = new List<Skill>();
            while (choices.Count < 3 && pool.Count > 0)
            {
                int i = Random.Range(0, pool.Count);
                choices.Add(pool[i]);
                pool.RemoveAt(i);
            }
        }

        if (levelUpUI != null)
        {
            choicePending = true;
            Time.timeScale = 0f;

            var titles = new string[3];
            var descriptions = new string[3];
            for (int i = 0; i < choices.Count; i++)
            {
                titles[i] = choices[i].Title;
                descriptions[i] = choices[i].Description;
            }

            levelUpUI.Show(titles, descriptions, index => ChooseSkill(choices[index].Id));
        }
        else
        {
            ChooseSkill(choices[0].Id);
        }
    }

    private void ChooseSkill(SkillId id)
    {
        stacks[id] = (stacks.TryGetValue(id, out int n) ? n : 0) + 1;

        switch (id)
        {
            case SkillId.UnlockRanged: UnlockedRanged = true; break;
            case SkillId.UnlockScream: UnlockedScream = true; break;
            case SkillId.UnlockDash: UnlockedDash = true; break;
            case SkillId.BiggerClaw: BiggerClaw = true; break;
            case SkillId.DoubleShot: DoubleShot = true; break;
            case SkillId.TripleShot: TripleShot = true; break;
            case SkillId.RadiusScream: RadiusScream = true; break;
            case SkillId.Vitality:
                vitalityStacks++;
                if (playerHealth != null) playerHealth.IncreaseMaxHealth(2);
                break;
            case SkillId.Swift:
                swiftStacks++;
                if (moveSpeedBoost != null) moveSpeedBoost.SpeedMultiplier = 1f + 0.15f * swiftStacks;
                break;
            case SkillId.DashPhase: DashPhase = true; break;
            case SkillId.DashLunge: DashLunge = true; break;
            case SkillId.MeleeDamageUp:
                meleeDamageStacks++;
                MeleeDamageBonus = meleeDamageStacks;
                break;
            case SkillId.RangedDamageUp:
                rangedDamageStacks++;
                RangedDamageBonus = rangedDamageStacks;
                break;
            case SkillId.ScreamDamageUp:
                screamDamageStacks++;
                ScreamDamageBonus = screamDamageStacks;
                break;
            case SkillId.RangedPierce: RangedPierce = true; break;
            case SkillId.ScreamCooldownDown:
                screamCooldownStacks++;
                ScreamCooldownReduction = 0.15f * screamCooldownStacks;
                break;
            case SkillId.Adrenaline:
                adrenalineStacks++;
                AdrenalineReduction = 0.1f * adrenalineStacks;
                break;
            case SkillId.Regeneration:
                regenStacks++;
                break;
            case SkillId.DashCooldownDown:
                dashCooldownStacks++;
                DashCooldownReduction = 0.15f * dashCooldownStacks;
                break;
            case SkillId.CritChance:
                critStacks++;
                CritChancePercent = 10 * critStacks;
                break;
            case SkillId.BiggerBullets:
                bulletScaleStacks++;
                BulletScale = 1f + 0.25f * bulletScaleStacks;
                break;
            case SkillId.ExplosiveRounds: ExplosiveRounds = true; break;
            case SkillId.LifeSteal:
                lifeStealStacks++;
                LifeStealAmount = lifeStealStacks;
                break;
            case SkillId.Thorns:
                thornsStacks++;
                ThornsDamage = thornsStacks;
                break;
            case SkillId.UnlockBeam: UnlockedBeam = true; break;
            case SkillId.BeamDamageUp:
                beamDamageStacks++;
                BeamDamageBonus = beamDamageStacks * 2;
                break;
            case SkillId.UnlockCyclone: UnlockedCyclone = true; break;
            case SkillId.CycloneDamageUp:
                cycloneDamageStacks++;
                CycloneDamageBonus = cycloneDamageStacks;
                break;
            case SkillId.UnlockBerserk: UnlockedBerserk = true; break;
            case SkillId.BerserkDurationUp:
                berserkDurationStacks++;
                BerserkDuration = 25f + berserkDurationStacks;
                break;
            case SkillId.Ricochet: Ricochet = true; break;
            case SkillId.Armor:
                armorStacks++;
                ArmorFlat = armorStacks;
                break;
            case SkillId.SecondWind: secondWindUnlocked = true; break;
            case SkillId.KnockbackOnHit: KnockbackOnHit = true; break;
            case SkillId.ScreamSlow: ScreamSlow = true; break;
            case SkillId.Titan:
                Titan = true;
                TitanDamageMultiplier = TitanDamageMultiplierValue;
                TitanSizeMultiplier = TitanSizeMultiplierValue;
                transform.localScale = Vector3.one * TitanScale;
                if (moveSpeedBoost != null) moveSpeedBoost.TitanMultiplier = TitanSpeedMultiplier;
                break;
        }

        if (choicePending)
        {
            Time.timeScale = 1f;
            choicePending = false;
        }
    }

    /// In-place "Restart Run" hook (PauseMenu) — clears every unlock/stack
    /// and level so the next run starts from claw-only again, no scene
    /// reload required.
    public void ResetProgress()
    {
        UnlockedRanged = false;
        UnlockedScream = false;
        UnlockedDash = false;
        BiggerClaw = false;
        DoubleShot = false;
        TripleShot = false;
        RadiusScream = false;
        DashPhase = false;
        DashLunge = false;
        MeleeDamageBonus = 0;
        RangedDamageBonus = 0;
        ScreamDamageBonus = 0;
        RangedPierce = false;
        ScreamCooldownReduction = 0f;
        AdrenalineReduction = 0f;
        DashCooldownReduction = 0f;
        CritChancePercent = 0;
        BulletScale = 1f;
        ExplosiveRounds = false;
        LifeStealAmount = 0;
        ThornsDamage = 0;
        UnlockedBeam = false;
        BeamDamageBonus = 0;
        UnlockedCyclone = false;
        CycloneDamageBonus = 0;
        UnlockedBerserk = false;
        BerserkDuration = 25f;
        Ricochet = false;
        ArmorFlat = 0;
        KnockbackOnHit = false;
        ScreamSlow = false;
        Titan = false;
        TitanDamageMultiplier = 1f;
        TitanSizeMultiplier = 1f;
        transform.localScale = Vector3.one;
        secondWindUnlocked = false;
        secondWindUsed = false;

        stacks.Clear();
        level = 0;
        vitalityStacks = 0;
        swiftStacks = 0;
        meleeDamageStacks = 0;
        rangedDamageStacks = 0;
        screamDamageStacks = 0;
        screamCooldownStacks = 0;
        adrenalineStacks = 0;
        regenStacks = 0;
        dashCooldownStacks = 0;
        critStacks = 0;
        bulletScaleStacks = 0;
        lifeStealStacks = 0;
        thornsStacks = 0;
        beamDamageStacks = 0;
        cycloneDamageStacks = 0;
        berserkDurationStacks = 0;
        armorStacks = 0;
        regenTimer = 0f;
        choicePending = false;
        Time.timeScale = 1f;

        if (moveSpeedBoost != null)
        {
            moveSpeedBoost.SpeedMultiplier = 1f;
            moveSpeedBoost.TitanMultiplier = 1f;
        }
        if (expBar != null) expBar.SetMaxExp(killsPerLevel);
    }

    /// Human-readable list of acquired skills, for the HUD's skill tracker.
    public string GetSummary()
    {
        var sb = new StringBuilder();
        if (UnlockedRanged) sb.AppendLine("Ranged Attack");
        if (UnlockedScream) sb.AppendLine("Scream");
        if (UnlockedDash) sb.AppendLine("Dash");
        if (BiggerClaw) sb.AppendLine("Bigger Claw");
        if (DoubleShot) sb.AppendLine("Double Shot");
        if (TripleShot) sb.AppendLine("Triple Shot");
        if (RadiusScream) sb.AppendLine("Radius Scream");
        if (vitalityStacks > 0) sb.AppendLine($"Vitality x{vitalityStacks}");
        if (swiftStacks > 0) sb.AppendLine($"Swift x{swiftStacks}");
        if (DashPhase) sb.AppendLine("Phase Dash");
        if (DashLunge) sb.AppendLine("Lunge Dash");
        if (meleeDamageStacks > 0) sb.AppendLine($"Claw Sharpness x{meleeDamageStacks}");
        if (rangedDamageStacks > 0) sb.AppendLine($"Overcharged Rounds x{rangedDamageStacks}");
        if (screamDamageStacks > 0) sb.AppendLine($"Piercing Scream x{screamDamageStacks}");
        if (RangedPierce) sb.AppendLine("Ranged Pierce");
        if (screamCooldownStacks > 0) sb.AppendLine($"Screaming Fury x{screamCooldownStacks}");
        if (adrenalineStacks > 0) sb.AppendLine($"Adrenaline x{adrenalineStacks}");
        if (regenStacks > 0) sb.AppendLine($"Regeneration x{regenStacks}");
        if (dashCooldownStacks > 0) sb.AppendLine($"Restless x{dashCooldownStacks}");
        if (critStacks > 0) sb.AppendLine($"Precision x{critStacks}");
        if (bulletScaleStacks > 0) sb.AppendLine($"Bigger Bullets x{bulletScaleStacks}");
        if (ExplosiveRounds) sb.AppendLine("Explosive Rounds");
        if (lifeStealStacks > 0) sb.AppendLine($"Life Steal x{lifeStealStacks}");
        if (thornsStacks > 0) sb.AppendLine($"Thorns x{thornsStacks}");
        if (UnlockedBeam) sb.AppendLine("Kamehameha Beam");
        if (beamDamageStacks > 0) sb.AppendLine($"Overcharged Beam x{beamDamageStacks}");
        if (UnlockedCyclone) sb.AppendLine("Cyclone");
        if (cycloneDamageStacks > 0) sb.AppendLine($"Razor Cyclone x{cycloneDamageStacks}");
        if (UnlockedBerserk) sb.AppendLine("Berserk");
        if (berserkDurationStacks > 0) sb.AppendLine($"Sustained Rage x{berserkDurationStacks}");
        if (Ricochet) sb.AppendLine("Ricochet");
        if (armorStacks > 0) sb.AppendLine($"Armor x{armorStacks}");
        if (secondWindUnlocked) sb.AppendLine(secondWindUsed ? "Second Wind (used)" : "Second Wind");
        if (KnockbackOnHit) sb.AppendLine("Bludgeon");
        if (ScreamSlow) sb.AppendLine("Chilling Scream");
        if (Titan) sb.AppendLine("Titan");

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "Claw only";
    }
}
