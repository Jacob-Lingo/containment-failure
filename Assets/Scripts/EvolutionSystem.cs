using System.Collections;
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
    Titan,

    // Evolutions. One per form; eligibility is decided entirely by
    // MonsterForm.CanEvolveTo (current form must be the target's parent).
    EvolveOgre, EvolveTroll, EvolveGolem, EvolveColossus, EvolveTitan,
    EvolveWraith, EvolveLich, EvolvePyre, EvolveDreadLich, EvolveRedDragon,
    EvolveCrawler, EvolveBroodmother, EvolveBileHorror, EvolveHydra, EvolveGorgon,
    EvolveWisp, EvolveRime, EvolveShade, EvolveFrostDragon, EvolveReaper,

    // Line-specific actives. Each is gated to the branch whose fantasy it
    // belongs to, so a line's cards are part of what makes it play differently.
    GroundSlam, Meteor, WebTrap, FrostNova, Blink,
    LeapSmash, ChainLightning, VenomBurst, Warp, Bulwark, Terrify,
    Maelstrom, FlameWheel, Starfall, BroodCall,
    Overcharge, Ward, Empower,

    // Passives that fire on their own — no slot, no key.
    FrostAura, ManaShell, SoulHarvest, Bloodlust
}

/// How strong a card is, shown on the level-up card (art + coloured label in
/// LevelUpUI). Purely a readability aid: rarity does NOT affect how often a
/// card is drawn. That was tried on 2026-07-28 and reverted — weighting the
/// draw by rarity cut across the archetype prerequisites and made the
/// evolution path incoherent. Common = small stacking stat, Legendary =
/// rewrites how you play.
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// Drives the "evolution track" from the GDD: kills feed a level counter,
/// and each level-up offers 3 skill cards (LevelUpUI), pausing the game
/// until the player picks one. The run starts with basic claws only.
///
/// Every level-up draws 3 at random from the eligible pool — there is no forced
/// archetype choice at level 1 (removed 2026-07-28), so a run can take passives
/// first and stay in its base form for several levels before committing.
/// Greater Claws and Arcane Bolt are still mutually exclusive forever once
/// either is taken, since both define what M1 does (see PlayerCombat).
///
/// IsAvailable is the skill tree. The player never sees it — they just get
/// three cards — but underneath, committing to a path opens some cards and
/// permanently closes others. See the branch comments in IsAvailable.
/// Falls back to auto-granting the first option if no LevelUpUI exists in
/// the scene, so progression never stalls during a headless/dev-scene test.
public class EvolutionSystem : MonoBehaviour
{
    [Tooltip("Exp needed for the first level-up.")]
    [SerializeField] private int killsPerLevel = 5;

    [Tooltip("Extra exp each level costs over the previous one. 0 = flat curve.")]
    [SerializeField] private int levelCostGrowth = 3;

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
    /// Blood Frenzy takes manual control away entirely, so this is the length
    /// of time the player is *watching* rather than playing. Nerfed 25s -> 8s
    /// on 2026-07-28: at 25s it cleared whole waves by itself and the run
    /// stopped being a game for a third of a minute. One central constant —
    /// the stack bonus and ResetProgress both derive from it.
    private const float BaseBerserkDuration = 8f;

    public float BerserkDuration { get; private set; } = BaseBerserkDuration;
    public bool Ricochet { get; private set; }
    private int armorFromCards;

    /// Stone Hide stacks plus whatever the current form's hide is worth.
    public int ArmorFlat => armorFromCards + Form.ArmorFlat;
    public bool KnockbackOnHit { get; private set; }
    public bool ScreamSlow { get; private set; }

    /// Passives that run themselves. FrostAura ticks in Update, SoulHarvest and
    /// Bloodlust hang off NotifyKill, ManaShell is read by PlayerHealth — none
    /// of them touch a slot or a key.
    public bool FrostAura { get; private set; }
    public bool ManaShell { get; private set; }
    public bool SoulHarvest { get; private set; }
    private bool bloodlust;
    private int bloodlustStacks;
    private float bloodlustExpiry;
    private int killsTowardHarvest;
    private float frostAuraTimer;

    private const float FrostAuraRadius = 3f;
    private const float FrostAuraInterval = 0.4f;
    private const float BloodlustPerStack = 0.08f;
    private const int BloodlustMaxStacks = 5;
    private const float BloodlustWindow = 4f;
    private const int HarvestEveryNKills = 5;
    private const float HarvestRadius = 3.2f;

    /// Decays by falling off a cliff rather than fading gradually — the player
    /// needs to be able to feel whether it's up, and a smooth ramp reads as noise.
    public float BloodlustReduction =>
        Time.time < bloodlustExpiry ? BloodlustPerStack * bloodlustStacks : 0f;

    public bool Titan { get; private set; }
    public float TitanDamageMultiplier { get; private set; } = 1f;

    /// Applied to melee/scream/cyclone reach (claw range, scream range,
    /// cyclone radius) — "physical" attacks, not the ranged/beam archetype.
    public float TitanSizeMultiplier { get; private set; } = 1f;
    private bool secondWindUnlocked;
    private bool secondWindUsed;

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

        // Lich line: the dead get back up on your side.
        var form = Form;
        if (form.RaiseChance > 0f && Random.value < form.RaiseChance)
            SummonedAlly.Spawn(lastKillPosition, Mathf.Max(1, 1 + MeleeDamageBonus), 12f);

        if (bloodlust)
        {
            bloodlustStacks = Mathf.Min(BloodlustMaxStacks, bloodlustStacks + 1);
            bloodlustExpiry = Time.time + BloodlustWindow;
        }

        if (!SoulHarvest) return;

        if (++killsTowardHarvest < HarvestEveryNKills) return;
        killsTowardHarvest = 0;

        SpellFx.Play(SpellFx.Casting, lastKillPosition, HarvestRadius * 1.8f, 1.5f);
        ScreamVfx.SpawnRing(lastKillPosition, HarvestRadius, new Color(0.7f, 0.3f, 1f, 0.55f));

        foreach (var hit in Physics2D.OverlapCircleAll(lastKillPosition, HarvestRadius))
        {
            if (hit.TryGetComponent<GuardHealth>(out var guard))
                guard.TakeDamage(3, AttackType.Scream);
        }
    }

    /// Where the most recent kill happened, so a raised ally appears on the
    /// corpse rather than on the player. Set by whoever confirms the kill.
    public void NoteKillPosition(Vector3 position) => lastKillPosition = position;

    private Vector3 lastKillPosition;

    /// Called by PlayerHealth when a hit would otherwise be lethal. Returns
    /// true (and consumes the once-per-run charge) if Second Wind is
    /// unlocked and hasn't already saved the player this run.
    public bool TryConsumeSecondWind()
    {
        if (!secondWindUnlocked || secondWindUsed) return false;
        secondWindUsed = true;
        return true;
    }

    /// Evolution cards and the form each becomes. A card in this table is
    /// offered only when MonsterForm says the player's current form can grow
    /// into it — that check IS the tier rule.
    private static readonly Dictionary<SkillId, string> EvolutionTargets = new Dictionary<SkillId, string>
    {
        { SkillId.EvolveOgre, Bestiary.FormOgre },
        { SkillId.EvolveTroll, Bestiary.FormTroll },
        { SkillId.EvolveGolem, Bestiary.FormGolem },
        { SkillId.EvolveColossus, Bestiary.FormColossus },
        { SkillId.EvolveTitan, Bestiary.FormTitan },

        { SkillId.EvolveWraith, Bestiary.FormWraith2 },
        { SkillId.EvolveLich, Bestiary.FormLich },
        { SkillId.EvolvePyre, Bestiary.FormPyre },
        { SkillId.EvolveDreadLich, Bestiary.FormLichDread },
        { SkillId.EvolveRedDragon, Bestiary.FormRedDragon },

        { SkillId.EvolveCrawler, Bestiary.FormCrawler },
        { SkillId.EvolveBroodmother, Bestiary.FormBroodmother },
        { SkillId.EvolveBileHorror, Bestiary.FormBileHorror },
        { SkillId.EvolveHydra, Bestiary.FormHydra },
        { SkillId.EvolveGorgon, Bestiary.FormGorgon },

        { SkillId.EvolveWisp, Bestiary.FormWisp },
        { SkillId.EvolveRime, Bestiary.FormRime },
        { SkillId.EvolveShade, Bestiary.FormShade },
        { SkillId.EvolveFrostDragon, Bestiary.FormFrostDragon },
        { SkillId.EvolveReaper, Bestiary.FormReaper },
    };

    /// The form the player currently wears. Everything about size, stats and
    /// which evolutions are offered hangs off this one field.
    public string CurrentFormSprite { get; private set; } = Bestiary.FormCrab;

    /// Venom trail properties of the current form, read by PlayerDash. Zero
    /// duration means this form leaves no trail at all.
    public MonsterForm.Form Form => MonsterForm.Info(CurrentFormSprite);

    private struct Skill
    {
        public SkillId Id;
        public string Title;
        public string Description;
        public int MaxStacks;
        public Rarity Rarity;
    }

    private static readonly Skill[] AllSkills =
    {
        new Skill { Id = SkillId.UnlockRanged, Title = "Arcane Bolt", Description = "Left-click fires a magic bolt instead of clawing. Permanent — gives up claws for this descent.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.UnlockScream, Title = "Scream", Description = "A shockwave scream that damages everything in front of you.", MaxStacks = 1, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.UnlockDash, Title = "Dash", Description = "Dash a short distance (left shift). Movement only — never takes an ability slot.", MaxStacks = 1, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.BiggerClaw, Title = "Greater Claws", Description = "Claws triple in reach and double in damage. Permanent — gives up magic bolts for this descent.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.DoubleShot, Title = "Double Bolt", Description = "Fire 2 bolts in a narrow spread.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.TripleShot, Title = "Triple Bolt", Description = "Fire 3 bolts: one straight, two angled.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.RadiusScream, Title = "Radius Scream", Description = "Scream hits all around you, not just in front.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.Vitality, Title = "Troll Blood", Description = "+2 max health.", MaxStacks = 5, Rarity = Rarity.Common },
        new Skill { Id = SkillId.Swift, Title = "Feral Speed", Description = "+15% move speed.", MaxStacks = 5, Rarity = Rarity.Common },
        new Skill { Id = SkillId.DashPhase, Title = "Phase Dash", Description = "Dash makes you briefly untouchable. Permanent — gives up Lunge Dash for this descent.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.DashLunge, Title = "Lunge Dash", Description = "Dash damages and knocks back anything you pass through. It wounds you a little too (capped at 50% of current HP). Permanent — gives up Phase Dash for this descent.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.MeleeDamageUp, Title = "Sharper Claws", Description = "+1 claw damage.", MaxStacks = 3, Rarity = Rarity.Common },
        new Skill { Id = SkillId.RangedDamageUp, Title = "Stronger Bolts", Description = "+1 bolt damage.", MaxStacks = 3, Rarity = Rarity.Common },
        new Skill { Id = SkillId.ScreamDamageUp, Title = "Stronger Scream", Description = "+1 scream damage.", MaxStacks = 3, Rarity = Rarity.Common },
        new Skill { Id = SkillId.RangedPierce, Title = "Piercing Bolts", Description = "Bolts pass through the first enemy hit instead of stopping.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.ScreamCooldownDown, Title = "Faster Scream", Description = "Scream cooldown -15%.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.Adrenaline, Title = "Haste", Description = "All attack cooldowns -10%.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.Regeneration, Title = "Regeneration", Description = "Slowly regenerate 1 health every few seconds.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.DashCooldownDown, Title = "Faster Dash", Description = "Dash cooldown -15%.", MaxStacks = 3, Rarity = Rarity.Common },
        new Skill { Id = SkillId.CritChance, Title = "Critical Strike", Description = "+10% chance for any attack to deal double damage.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.BiggerBullets, Title = "Bigger Bolts", Description = "Bolts are 25% bigger and hit a wider area.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.ExplosiveRounds, Title = "Explosive Bolts", Description = "Bolts deal double damage on a direct hit and explode, damaging everything nearby.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.LifeSteal, Title = "Vampirism", Description = "Heal 1 HP on every kill.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.Thorns, Title = "Spiked Hide", Description = "Getting hit sends out a shockwave, damaging nearby enemies.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.UnlockBeam, Title = "Dragonfire Beam", Description = "Breathe a searing beam of flame that damages everything in a line.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.BeamDamageUp, Title = "Stronger Dragonfire", Description = "+2 Dragonfire Beam damage.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.UnlockCyclone, Title = "Whirlwind", Description = "Spin in place, shredding everything around you. Requires Greater Claws.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.CycloneDamageUp, Title = "Stronger Whirlwind", Description = "+1 Whirlwind damage per tick.", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.UnlockBerserk, Title = "Blood Frenzy", Description = "Lose all control for 8s while you auto-attack the nearest enemy (and auto-cast your other abilities) at hugely boosted damage.", MaxStacks = 1, Rarity = Rarity.Legendary },
        new Skill { Id = SkillId.BerserkDurationUp, Title = "Longer Blood Frenzy", Description = "+1s Blood Frenzy duration.", MaxStacks = 3, Rarity = Rarity.Common },
        new Skill { Id = SkillId.Ricochet, Title = "Bouncing Bolts", Description = "A bolt that would stop instead bounces to a nearby enemy.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.Armor, Title = "Stone Hide", Description = "-1 flat damage from every blow (never below 1).", MaxStacks = 3, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.SecondWind, Title = "Undying", Description = "The first hit that would kill you instead leaves you at 1 HP. Once per descent.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.KnockbackOnHit, Title = "Crushing Blow", Description = "Claw and scream hits knock enemies back.", MaxStacks = 1, Rarity = Rarity.Uncommon },
        new Skill { Id = SkillId.ScreamSlow, Title = "Freezing Scream", Description = "Scream slows every enemy it hits.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.GroundSlam, Title = "Ground Slam", Description = "Smash the ground, damaging and hurling back everything around you. Ogre line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.Meteor, Title = "Meteor", Description = "Call down a burning rock where you aim. It lands a moment later, so lead your target. Wraith line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.WebTrap, Title = "Web Trap", Description = "Fling a patch of clinging web that slows and poisons anything standing in it. Crawler line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.LeapSmash, Title = "Leap Smash", Description = "Hurl yourself at where you aim and land like a dropped anvil, damaging and scattering everything nearby. Ogre line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.ChainLightning, Title = "Chain Lightning", Description = "A bolt that leaps from foe to foe, striking up to five. Wraith line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.VenomBurst, Title = "Venom Burst", Description = "Erupt in a ring of venom clouds centred on you. Crawler line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.Warp, Title = "Warp", Description = "Tear yourself somewhere else entirely. You do not choose where. Wisp line only.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.Bulwark, Title = "Bulwark", Description = "Nothing can touch you for two seconds.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.Terrify, Title = "Terrify", Description = "A soundless scream that hurls everything around you away. No damage — just distance.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.Blink, Title = "Blink", Description = "Vanish and reappear where you aim, untouchable for the instant between. Wisp line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.FrostNova, Title = "Frost Nova", Description = "A burst of cold that slows every enemy around you to a crawl. Wisp line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.Maelstrom, Title = "Maelstrom", Description = "Open a whirlpool that drags everything nearby into your reach, then batters it. Ogre line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.FlameWheel, Title = "Flame Wheel", Description = "Wreathe yourself in turning fire for a few seconds, burning everything you brush past. Wraith line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.Starfall, Title = "Starfall", Description = "Three falling stars scatter around where you aim. Wisp line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.BroodCall, Title = "Brood Call", Description = "Tear open the ground and let three of your young out to hunt for you. Crawler line only.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.Overcharge, Title = "Overcharge", Description = "Hold the key to gather power — the longer you stand still, the wider and deadlier the blast. Moving or letting go fires it early. Wraith line only.", MaxStacks = 1, Rarity = Rarity.Legendary },
        new Skill { Id = SkillId.Ward, Title = "Ward", Description = "Plant a sigil that strikes anything that comes near it, until it burns out.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.Empower, Title = "Empower", Description = "Your next 5 attacks land for double. A volley of bolts counts as one.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.FrostAura, Title = "Frost Aura", Description = "The air around you bites. Anything that gets close is slowed for as long as it stays.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.ManaShell, Title = "Mana Shell", Description = "Go a few seconds unhurt and a shell forms around you. It eats the next blow entirely.", MaxStacks = 1, Rarity = Rarity.Epic },
        new Skill { Id = SkillId.SoulHarvest, Title = "Soul Harvest", Description = "Every fifth kill bursts, wounding everything near the corpse.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.Bloodlust, Title = "Bloodlust", Description = "Each kill quickens you: -8% on every cooldown, stacking up to 5, fading a few seconds after you stop killing.", MaxStacks = 1, Rarity = Rarity.Rare },
        new Skill { Id = SkillId.Titan, Title = "Colossus", Description = "Grow massive: +100% damage and +50% reach on claws, scream and Whirlwind. Move speed -40%. Permanent — requires Greater Claws.", MaxStacks = 1, Rarity = Rarity.Legendary },
    };

    /// Card title for a skill, for the ability HUD and the slot picker. Reads
    /// the one card table so a rename can't desync them.
    public static string TitleOf(SkillId id) => FindSkill(id).Title ?? id.ToString();

    /// Evolution cards aren't hand-written into AllSkills: their titles and
    /// descriptions come straight from MonsterForm, so a form renamed there
    /// can't drift from the card that grants it.
    private static Skill EvolutionCard(SkillId id)
    {
        var info = MonsterForm.Info(EvolutionTargets[id]);
        return new Skill
        {
            Id = id,
            Title = "Evolve: " + info.Name,
            Description = info.Blurb,
            MaxStacks = 1,
            Rarity = info.Tier >= 4 ? Rarity.Legendary : info.Tier == 3 ? Rarity.Epic : Rarity.Rare,
        };
    }

    private static bool IsEvolution(SkillId id) => EvolutionTargets.ContainsKey(id);

    /// The form a card evolves into, or null if it isn't an evolution card.
    /// Used by the level-up panel to show what you'd become.
    public string EvolutionTargetOf(SkillId id) =>
        EvolutionTargets.TryGetValue(id, out var form) ? form : null;

    private static Skill FindSkill(SkillId id)
    {
        if (IsEvolution(id)) return EvolutionCard(id);

        foreach (var s in AllSkills)
            if (s.Id == id) return s;
        return default;
    }

    /// Keys 1-4. See AbilitySlots for why M1 and dash stay off the bar.
    public AbilitySlots Slots { get; } = new AbilitySlots();

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
    private float formRegenTimer;
    private bool choicePending;

    private const float RegenTickSeconds = 5f;

    private ExpBar expBar;
    private LevelUpUI levelUpUI;
    private PlayerHealth playerHealth;
    private PlayerMoveSpeedBoost moveSpeedBoost;

    /// MonsterForm scales the player per evolution stage, relative to this —
    /// the prefab's own starting scale, which is not assumed to be 1.
    private Vector3 baseScale;

    /// The prefab's own starting scale. MonsterForm multiplies its per-form
    /// scale by this, so it stays the single source of truth for player size.
    public Vector3 BaseScale => baseScale;

    /// The current form's damage multiplier, read by PlayerCombat alongside
    /// TitanDamageMultiplier. 1 for the base Clawling.
    public float FormDamageMultiplier { get; private set; } = 1f;

    /// Health already granted by the current form, so a change applies only the
    /// difference rather than stacking every bonus the run has passed through.
    private int appliedFormHealth;

    /// Called by MonsterForm whenever the form changes. Size, bulk, speed and
    /// damage all arrive together — a form is a creature, not a sprite swap.
    public void ApplyFormStats(string form, MonsterForm.Form stats, float scale)
    {
        transform.localScale = baseScale * scale;

        FormDamageMultiplier = stats.DamageMultiplier;

        if (moveSpeedBoost != null)
            moveSpeedBoost.FormMultiplier = stats.SpeedMultiplier;

        int delta = stats.BonusHealth - appliedFormHealth;
        if (delta > 0 && playerHealth != null) playerHealth.IncreaseMaxHealth(delta);
        appliedFormHealth = stats.BonusHealth;
    }

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        moveSpeedBoost = GetComponent<PlayerMoveSpeedBoost>();
        baseScale = transform.localScale;
    }

    private void Start()
    {
        expBar = FindFirstObjectByType<ExpBar>();
        levelUpUI = FindFirstObjectByType<LevelUpUI>();

        if (expBar != null)
            expBar.SetMaxExp(killsPerLevel);
        else
            Debug.LogWarning("No ExpBar was found in the scene.");

        ApplyMetaUpgrades();
        MonsterForm.Apply(gameObject, CurrentFormSprite);
        formOnLastRefresh = CurrentFormSprite;
        previousBodySprite = GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().sprite : null;
    }

    /// Permanent between-run upgrades bought in the shop (MetaProgression).
    /// Applied here rather than in PlayerHealth/PlayerMoveSpeedBoost so all
    /// run-start stat setup lives in one place, and so the in-place restart
    /// (ResetProgress) can re-apply them the same way.
    private void ApplyMetaUpgrades()
    {
        int bonusHealth = MetaProgression.BonusStartingHealth;
        if (bonusHealth > 0 && playerHealth != null)
            playerHealth.IncreaseMaxHealth(bonusHealth);

        if (moveSpeedBoost != null)
            moveSpeedBoost.SpeedMultiplier = MetaProgression.StartingSpeedMultiplier;
    }

    /// Exp cost of the level *after* `level` (0-indexed): killsPerLevel, then
    /// +levelCostGrowth each time. With the defaults that's 5, 8, 11, 14, 17...
    /// Late-run kills come thick and fast, so a flat cost meant levels arrived
    /// in a constant stream by the last floor and the card choice stopped
    /// feeling like a decision.
    private int CostOfLevel(int level) => killsPerLevel + levelCostGrowth * level;

    /// Walks the cost curve to turn a total exp figure into a level, plus how
    /// far into the current level the player is and what the next one costs
    /// (both for the ExpBar). Recomputed from the total rather than tracked
    /// incrementally, so it stays correct across every reset path.
    private int LevelForExp(int totalExp, out int intoLevel, out int nextCost)
    {
        int level = 0;
        int remaining = Mathf.Max(0, totalExp);

        while (true)
        {
            int cost = Mathf.Max(1, CostOfLevel(level));
            if (remaining < cost)
            {
                intoLevel = remaining;
                nextCost = cost;
                return level;
            }

            remaining -= cost;
            level++;
        }
    }

    private string formOnLastRefresh;
    private Coroutine transformRoutine;

    /// Flickers between the old and new body a few times before settling, with
    /// a burst of sparks each swap. Unscaled time, so a hitstop or a pause
    /// opened mid-transformation can never strand the player on the old sprite
    /// — that is exactly how the sprite-ownership bug used to look — a hard cut reads as a glitch (which is
    /// exactly how the sprite-ownership bug looked), whereas a stutter reads as
    /// the change being violent and deliberate.
    private void StartTransformation()
    {
        if (transformRoutine != null) StopCoroutine(transformRoutine);
        transformRoutine = StartCoroutine(TransformRoutine());
    }

    private IEnumerator TransformRoutine()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;

        Sprite becoming = renderer.sprite;   // Refresh already applied the new form
        Sprite leaving = previousBodySprite ?? becoming;
        previousBodySprite = becoming;

        const int Flickers = 7;
        const float Step = 0.07f;

        Juice.Shake(0.4f);

        for (int i = 0; i < Flickers; i++)
        {
            renderer.sprite = (i % 2 == 0) ? leaving : becoming;
            BurstSparks(0.6f + 0.12f * i);
            yield return new WaitForSecondsRealtime(Step);
        }

        renderer.sprite = becoming;
        BurstSparks(1.4f);
        Juice.Shake(0.5f);
        transformRoutine = null;
    }

    /// Ring of arcane sparks around the player, reusing the existing impact FX
    /// rather than adding a particle system.
    private void BurstSparks(float radius)
    {
        const int Count = 8;
        var tint = new Color(0.75f, 0.5f, 1f, 0.85f);

        for (int i = 0; i < Count; i++)
        {
            float angle = (i / (float)Count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            HitFlashFx.Spawn(transform.position + offset, tint, 0.3f);
        }
    }

    private Sprite previousBodySprite;

    private void Update()
    {
        // Defer level-up offers while anything else holds the freeze (pause
        // menu open) — the kill that earned it is still banked in RunStats,
        // so the offer fires on the first unfrozen frame instead.
        if (!choicePending && !GameState.IsFrozen)
        {
            // Kills plus green exp orbs. Gold is currency only (MetaProgression)
            // and deliberately does NOT feed this.
            int totalExp = RunStats.TotalKills + RunStats.BonusExp;
            int targetLevel = LevelForExp(totalExp, out _, out _) + MetaProgression.HeadStartLevels;
            if (level < targetLevel)
            {
                level++;
                OfferChoice();
            }
        }

        if (expBar != null)
        {
            LevelForExp(RunStats.TotalKills + RunStats.BonusExp, out int intoLevel, out int nextCost);
            expBar.SetMaxExp(nextCost);
            expBar.SetExp(intoLevel);
        }

        if (regenStacks > 0 && playerHealth != null)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= RegenTickSeconds)
            {
                regenTimer -= RegenTickSeconds;
                playerHealth.Heal(regenStacks);
            }
        }

        // Frost Aura re-applies a short slow on a slow tick rather than tracking
        // who is inside it: the slow lapses on its own the moment something
        // leaves, so there is no exit bookkeeping to get wrong.
        if (FrostAura)
        {
            frostAuraTimer += Time.deltaTime;
            if (frostAuraTimer >= FrostAuraInterval)
            {
                frostAuraTimer = 0f;

                // ponytail: a persistent aura sprite would need its own
                // lifecycle wired into every reset path. A cheap puff on some
                // ticks reads as a shimmer and cleans itself up.
                if (Random.value < 0.35f)
                    SpellFx.Play(SpellFx.BlueFire, transform.position, FrostAuraRadius * 1.6f, 1.2f,
                                 new Color(0.7f, 0.9f, 1f, 0.5f));

                foreach (var hit in Physics2D.OverlapCircleAll(transform.position, FrostAuraRadius))
                {
                    if (hit.TryGetComponent<GuardMotor>(out var motor))
                        motor.ApplySlow(0.55f, FrostAuraInterval * 1.6f);
                }
            }
        }

        // Troll line regenerates on its own clock, independent of the card.
        float formRegen = Form.RegenSeconds;
        if (formRegen > 0f && playerHealth != null)
        {
            formRegenTimer += Time.deltaTime;
            if (formRegenTimer >= formRegen)
            {
                formRegenTimer -= formRegen;
                playerHealth.Heal(1);
            }
        }
    }

    /// True when the current form descends from `root` — i.e. the player is on
    /// that branch, at any tier. Walks parents rather than listing every form,
    /// so adding a Tier-4 doesn't mean revisiting every gate.
    private bool IsOnLine(string root)
    {
        string form = CurrentFormSprite;
        while (form != null)
        {
            if (form == root) return true;
            form = MonsterForm.Info(form).Parent;
        }
        return false;
    }

    /// Cards that only do something if M1 fires bolts.
    private static bool IsBoltCard(SkillId id) =>
        id == SkillId.RangedDamageUp || id == SkillId.RangedPierce ||
        id == SkillId.Ricochet || id == SkillId.BiggerBullets ||
        id == SkillId.ExplosiveRounds || id == SkillId.DoubleShot ||
        id == SkillId.TripleShot || id == SkillId.UnlockBeam ||
        id == SkillId.BeamDamageUp;

    /// Cards that only do something if M1 swings claws.
    private static bool IsClawCard(SkillId id) =>
        id == SkillId.MeleeDamageUp || id == SkillId.KnockbackOnHit ||
        id == SkillId.UnlockCyclone || id == SkillId.CycloneDamageUp ||
        id == SkillId.UnlockBerserk || id == SkillId.BerserkDurationUp;

    private bool IsAvailable(SkillId id)
    {
        // ARCHETYPE LOCK. Only the Wraith line replaces M1 with bolts; every
        // other line (including the base Clawling) still claws. So bolt cards
        // are useless off that line and claw cards are useless on it — and
        // offering either was the "increase claw damage while shooting bolts"
        // problem. Gate on the line, not on loose unlock flags.
        bool bolts = IsOnLine(Bestiary.FormWraith2);

        if (!bolts && IsBoltCard(id)) return false;
        if (bolts && IsClawCard(id)) return false;

        if (id == SkillId.GroundSlam && !IsOnLine(Bestiary.FormOgre)) return false;
        if (id == SkillId.Meteor && !IsOnLine(Bestiary.FormWraith2)) return false;
        if (id == SkillId.WebTrap && !IsOnLine(Bestiary.FormCrawler)) return false;
        if (id == SkillId.FrostNova && !IsOnLine(Bestiary.FormWisp)) return false;
        if (id == SkillId.Blink && !IsOnLine(Bestiary.FormWisp)) return false;
        if (id == SkillId.Warp && !IsOnLine(Bestiary.FormWisp)) return false;
        if (id == SkillId.LeapSmash && !IsOnLine(Bestiary.FormOgre)) return false;
        if (id == SkillId.ChainLightning && !IsOnLine(Bestiary.FormWraith2)) return false;
        if (id == SkillId.VenomBurst && !IsOnLine(Bestiary.FormCrawler)) return false;
        if (id == SkillId.Maelstrom && !IsOnLine(Bestiary.FormOgre)) return false;
        if (id == SkillId.FlameWheel && !IsOnLine(Bestiary.FormWraith2)) return false;
        if (id == SkillId.Starfall && !IsOnLine(Bestiary.FormWisp)) return false;
        if (id == SkillId.BroodCall && !IsOnLine(Bestiary.FormCrawler)) return false;
        if (id == SkillId.Overcharge && !IsOnLine(Bestiary.FormWraith2)) return false;
        // Ward and Empower join Bulwark/Terrify as line-agnostic utility.
        // Bulwark and Terrify are deliberately ungated: every line needs at
        // least one panic button, and a run that draws neither of its own
        // abilities early should still have something to reach for.

        // Superseded by the evolution tree: Ogre and Wraith now grant these.
        // Left in the enum because PlayerCombat and the upgrade cards still read
        // the flags, but never offered as cards of their own.
        if (id == SkillId.BiggerClaw || id == SkillId.UnlockRanged) return false;
        if (id == SkillId.Titan) return false; // Colossus/Titan are evolutions now

        // An unlock card is normally kept off the pool by its own MaxStacks
        // once taken — but evolutions grant some of these flags directly
        // (Crawler grants Dash, Ogre grants claws, Wraith grants bolts) without
        // going through `stacks`. Without this the card is still offered and
        // does literally nothing when picked, which is the clearest possible
        // version of "this card doesn't work".
        if (id == SkillId.UnlockDash && UnlockedDash) return false;
        if (id == SkillId.UnlockScream && UnlockedScream) return false;
        if (id == SkillId.UnlockBeam && UnlockedBeam) return false;
        if (id == SkillId.UnlockCyclone && UnlockedCyclone) return false;
        if (id == SkillId.UnlockBerserk && UnlockedBerserk) return false;

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

        // Branch capstones. The player never sees a tree — they just get three
        // cards — but the pool underneath is one: committing to claws or bolts
        // at level 1 decides which capstones can ever appear, so a run reads as
        // a path rather than a grab bag.
        //
        //   CLAWS -> Whirlwind -> Colossus
        //   BOLTS -> Dragonfire Beam
        //   either (once committed) -> Blood Frenzy
        if (id == SkillId.UnlockBeam && !UnlockedRanged) return false;       // bolt capstone
        if (id == SkillId.UnlockBerserk && !BiggerClaw) return false;        // claw capstone: it auto-attacks with claws

        // The bolt branch forks. Piercing and Explosive are opposite answers to
        // "what happens when a bolt lands", so taking either permanently closes
        // the other and its follow-up — same shape as the Phase/Lunge dash split.
        //   PIERCE  -> Bouncing Bolts
        //   EXPLODE -> Bigger Bolts
        if (id == SkillId.RangedPierce && ExplosiveRounds) return false;
        if (id == SkillId.ExplosiveRounds && RangedPierce) return false;
        if (id == SkillId.Ricochet && !RangedPierce) return false;
        if (id == SkillId.BiggerBullets && !ExplosiveRounds) return false;

        // New active abilities and their upgrade cards.
        if (id == SkillId.BeamDamageUp && !UnlockedBeam) return false;
        if (id == SkillId.UnlockCyclone && !BiggerClaw) return false; // melee archetype capstone
        if (id == SkillId.CycloneDamageUp && !UnlockedCyclone) return false;
        if (id == SkillId.BerserkDurationUp && !UnlockedBerserk) return false;
        if (id == SkillId.Titan && !BiggerClaw) return false; // melee archetype capstone, same gate as Cyclone

        return true;
    }

    /// Every level-up draws 3 from the same pool — there is no longer a forced
    /// archetype choice at level 1. Claws/Bolts/Dash are ordinary cards, so a
    /// run can spend its first few levels on passives and stay in its base form
    /// longer before committing to a path.
    private void OfferChoice()
    {
        var pool = new List<Skill>();
        foreach (var skill in AllSkills)
        {
            if (!IsAvailable(skill.Id)) continue;
            int taken = stacks.TryGetValue(skill.Id, out int n) ? n : 0;
            if (taken < skill.MaxStacks) pool.Add(skill);
        }

        // Only evolutions that grow directly from the current form. At most two
        // are ever eligible, which is what makes a Tier-3 choice a real fork
        // rather than a menu.
        foreach (var pair in EvolutionTargets)
        {
            if (stacks.ContainsKey(pair.Key)) continue;
            if (!MonsterForm.CanEvolveTo(CurrentFormSprite, pair.Value)) continue;
            pool.Add(EvolutionCard(pair.Key));
        }

        if (pool.Count == 0) return; // everything maxed out

        var choices = new List<Skill>();
        while (choices.Count < 3 && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            choices.Add(pool[i]);
            pool.RemoveAt(i);
        }

        if (levelUpUI != null)
        {
            choicePending = true;
            GameState.TryFreeze(GameState.FreezeReason.LevelUpChoice);

            var titles = new string[3];
            var descriptions = new string[3];
            var rarities = new Rarity[3];
            var previews = new Sprite[3];
            for (int i = 0; i < choices.Count; i++)
            {
                titles[i] = choices[i].Title;
                descriptions[i] = choices[i].Description + KeyHintFor(choices[i].Id);
                rarities[i] = choices[i].Rarity;
                previews[i] = MonsterForm.PreviewFor(this, choices[i].Id);
                MetaStats.RecordOffered(choices[i].Id);
            }

            levelUpUI.Show(titles, descriptions, rarities, previews, index => ChooseSkill(choices[index].Id));
        }
        else
        {
            ChooseSkill(choices[0].Id);
        }
    }

    /// Which key this card will be castable on, appended to its description.
    ///
    /// Actives bind to keys 1-4 at pick time, but nothing said so anywhere —
    /// the ability bar shows the binding afterwards in 11pt text at the bottom
    /// of the screen, which is not where anyone is looking while choosing a
    /// card. A player who doesn't know an ability needs a keypress reasonably
    /// concludes the card did nothing.
    private string KeyHintFor(SkillId id)
    {
        if (id == SkillId.UnlockDash) return "\n<b>Press SHIFT to dash.</b>";
        if (!AbilitySlots.IsSlottable(id)) return "";

        int bound = Slots.IndexOf(id);
        if (bound >= 0) return $"\n<b>Already on key {AbilitySlots.KeyLabel(bound)}.</b>";

        int free = Slots.FirstEmpty();
        return free >= 0
            ? $"\n<b>Cast with key {AbilitySlots.KeyLabel(free)}.</b>"
            : "\n<b>Your slots are full — you'll pick one to replace.</b>";
    }

    /// Entry point from the level-up card. A slottable active with a full bar
    /// detours through the slot picker *before* anything is granted, so
    /// declining costs nothing — the level-up simply re-rolls a fresh set of
    /// cards rather than burning the pick on an ability with nowhere to go.
    private void ChooseSkill(SkillId id)
    {
        if (AbilitySlots.IsSlottable(id) && Slots.IndexOf(id) < 0 && Slots.IsFull)
        {
            SlotPickerUI.Show(
                Slots,
                incoming: id,
                onReplace: index =>
                {
                    Slots.BindTo(index, id);
                    ApplySkill(id);
                },
                onCancel: OfferChoice);
            return;
        }

        ApplySkill(id);
    }

    /// Commits an evolution. The Ogre and Wraith lines redefine what M1 does,
    /// so they set the same BiggerClaw/UnlockedRanged flags PlayerCombat has
    /// always read — the combat code needs no knowledge of forms.
    private void Evolve(string form)
    {
        CurrentFormSprite = form;

        if (form == Bestiary.FormOgre) BiggerClaw = true;
        if (form == Bestiary.FormWraith2) UnlockedRanged = true;

        // The venom trail only drops while dashing (PlayerDash.DropVenom), so
        // without this the Crawler line's entire identity — the thing every one
        // of its blurbs promises — was silently absent for any run that never
        // happened to be offered the Dash card. A line must not depend on an
        // unrelated draw to function.
        if (form == Bestiary.FormCrawler) UnlockedDash = true;

        string previous = formOnLastRefresh;
        MonsterForm.Apply(gameObject, form);
        formOnLastRefresh = form;
        MetaStats.RecordForm(form);

        if (previous != null && previous != form) StartTransformation();

        if (choicePending)
        {
            GameState.Release(GameState.FreezeReason.LevelUpChoice);
            choicePending = false;
        }
    }

    private void ApplySkill(SkillId id)
    {
        MetaStats.RecordPicked(id);
        stacks[id] = (stacks.TryGetValue(id, out int n) ? n : 0) + 1;

        if (AbilitySlots.IsSlottable(id)) Slots.TryBindToEmpty(id);

        if (IsEvolution(id))
        {
            Evolve(EvolutionTargets[id]);
            return;
        }

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
                // Multiplies the shop's Lab Legs baseline rather than
                // overwriting it, same way TitanMultiplier stays separate.
                if (moveSpeedBoost != null)
                    moveSpeedBoost.SpeedMultiplier = MetaProgression.StartingSpeedMultiplier * (1f + 0.15f * swiftStacks);
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
                BerserkDuration = BaseBerserkDuration + berserkDurationStacks;
                break;
            case SkillId.Ricochet: Ricochet = true; break;
            case SkillId.Armor:
                armorStacks++;
                armorFromCards = armorStacks;
                break;
            case SkillId.SecondWind: secondWindUnlocked = true; break;
            case SkillId.KnockbackOnHit: KnockbackOnHit = true; break;
            case SkillId.ScreamSlow: ScreamSlow = true; break;
            case SkillId.FrostAura: FrostAura = true; break;
            case SkillId.ManaShell: ManaShell = true; break;
            case SkillId.SoulHarvest: SoulHarvest = true; break;
            case SkillId.Bloodlust: bloodlust = true; break;
            case SkillId.Titan:
                Titan = true;
                TitanDamageMultiplier = TitanDamageMultiplierValue;
                TitanSizeMultiplier = TitanSizeMultiplierValue;
                if (moveSpeedBoost != null) moveSpeedBoost.TitanMultiplier = TitanSpeedMultiplier;
                break;
        }


        if (choicePending)
        {
            GameState.Release(GameState.FreezeReason.LevelUpChoice);
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
        BerserkDuration = BaseBerserkDuration;
        Ricochet = false;
        armorFromCards = 0;
        KnockbackOnHit = false;
        ScreamSlow = false;
        FrostAura = false;
        ManaShell = false;
        SoulHarvest = false;
        bloodlust = false;
        bloodlustStacks = 0;
        bloodlustExpiry = 0f;
        killsTowardHarvest = 0;
        frostAuraTimer = 0f;
        Titan = false;
        TitanDamageMultiplier = 1f;
        TitanSizeMultiplier = 1f;
        secondWindUnlocked = false;
        secondWindUsed = false;

        Slots.Clear();
        appliedFormHealth = 0;
        FormDamageMultiplier = 1f;
        if (moveSpeedBoost != null) moveSpeedBoost.FormMultiplier = 1f;

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
        GameState.Release(GameState.FreezeReason.LevelUpChoice);
        if (levelUpUI != null) levelUpUI.Hide(); // stale card would grant a pre-reset skill

        if (moveSpeedBoost != null) moveSpeedBoost.TitanMultiplier = 1f;
        if (expBar != null) expBar.SetMaxExp(killsPerLevel);

        // PauseMenu calls PlayerHealth.ResetToStartingHealth() before this, so
        // re-applying here restores the shop's flat bonuses on top of a clean
        // slate rather than stacking onto the old run's totals.
        ApplyMetaUpgrades();
        CurrentFormSprite = Bestiary.FormCrab;
        MonsterForm.Apply(gameObject, CurrentFormSprite);
        formOnLastRefresh = CurrentFormSprite;
        previousBodySprite = null;
        if (transformRoutine != null) { StopCoroutine(transformRoutine); transformRoutine = null; }
    }

    /// Human-readable list of acquired skills, for the HUD's skill tracker and
    /// the end-of-run summary. Reads titles straight out of AllSkills via the
    /// `stacks` dictionary — this used to be 36 hand-written lines that had to
    /// be kept in sync with the card table by hand, and drifted every time a
    /// card was renamed.
    public string GetSummary()
    {
        var sb = new StringBuilder();

        foreach (var skill in AllSkills)
        {
            if (!stacks.TryGetValue(skill.Id, out int taken) || taken <= 0) continue;

            sb.Append(skill.Title);
            if (skill.MaxStacks > 1) sb.Append($" x{taken}");
            if (skill.Id == SkillId.SecondWind && secondWindUsed) sb.Append(" (spent)");
            sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "Claws only";
    }
}
