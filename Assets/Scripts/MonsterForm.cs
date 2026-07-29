using System.Collections.Generic;
using UnityEngine;

/// The evolution tree: which creature the player is, what it's worth in stats,
/// and — critically — what it is allowed to become next.
///
///   TIER 1            TIER 2          TIER 3               TIER 4
///
///                 ┌─ Ogre ───────┬─ Troll ──────────── Colossus
///                 │  (bulk)      └─ Iron Golem ─────── Titan
///   Clawling ─────┤
///  (claws only)   ├─ Wraith ─────┬─ Lich ───────────── Dread Lich
///                 │  (bolts)     └─ Pyre ───────────── Red Dragon
///                 │
///                 ├─ Crawler ────┬─ Broodmother ────── Hydra
///                 │  (venom)     └─ Bile Horror ────── Gorgon
///                 │
///                 └─ Wisp ───────┬─ Rime ───────────── Frost Dragon
///                    (speed)     └─ Shade ──────────── Reaper
///
/// THE RULE: you evolve upward only, and only along your own line. A form is
/// offered as a card only when the player's *current* form is its declared
/// Parent — so you can never swap sideways within a tier, and a Tier-2 choice
/// permanently closes the three lines you didn't take.
///
/// The player never sees this tree. They see three cards. The filtering is what
/// makes a run read as a path instead of a grab bag.
public static class MonsterForm
{
    public struct Form
    {
        public string Sprite;
        public string Name;
        public string Parent;       // null for Tier 1
        public int Tier;            // 1-4, drives size
        public int BonusHealth;
        public float SpeedMultiplier;
        public float DamageMultiplier;
        public string Blurb;        // shown on the evolution card

        /// Seconds a dash-trail venom cloud lingers. 0 = this form leaves none,
        /// which is every line except Crawler's.
        public float VenomDuration;
        public bool VenomSlows;     // Bile Horror / Gorgon
        public bool VenomSpreads;   // Hydra

        // Every form's blurb promises a mechanic; these are those mechanics.
        // A form with all-zero passives is just a stat block, which is what
        // made several evolutions feel like nothing was happening.
        public float RegenSeconds;      // Troll line: heal 1 HP this often
        public int ArmorFlat;           // Golem line: flat damage reduction
        public float RaiseChance;       // Lich line: chance a kill raises an ally
        public bool BoltsExplode;       // Pyre line
        public bool SlowOnHit;          // Rime line
        public float DashInvuln;        // Shade line: i-frames after a dash
        public float DashCooldownScale; // Wisp line: <1 dashes more often
    }

    /// Every evolution is 40% bigger than the one before.
    private const float GrowthPerTier = 1.4f;

    private static readonly Dictionary<string, Form> Forms = new Dictionary<string, Form>();

    private static void Add(string sprite, string name, string parent, int tier,
                            int hp, float speed, float damage, string blurb,
                            float venom = 0f, bool venomSlows = false, bool venomSpreads = false,
                            float regen = 0f, int armor = 0, float raise = 0f,
                            bool boltsExplode = false, bool slowOnHit = false,
                            float dashInvuln = 0f, float dashCooldownScale = 1f)
    {
        Forms[sprite] = new Form
        {
            Sprite = sprite, Name = name, Parent = parent, Tier = tier,
            BonusHealth = hp, SpeedMultiplier = speed, DamageMultiplier = damage, Blurb = blurb,
            VenomDuration = venom, VenomSlows = venomSlows, VenomSpreads = venomSpreads,
            RegenSeconds = regen, ArmorFlat = armor, RaiseChance = raise,
            BoltsExplode = boltsExplode, SlowOnHit = slowOnHit,
            DashInvuln = dashInvuln, DashCooldownScale = dashCooldownScale,
        };
    }

    static MonsterForm()
    {
        Add(Bestiary.FormCrab, "Clawling", null, 1, 0, 1.00f, 1.00f, "A small thing with claws.");

        // OGRE — stand and fight. Slowest, toughest, hits hardest.
        Add(Bestiary.FormOgre, "Ogre", Bestiary.FormCrab, 2, 4, 0.90f, 1.30f, "Claws triple in reach. Slow but immense.");
        Add(Bestiary.FormTroll, "Troll", Bestiary.FormOgre, 3, 8, 0.85f, 1.40f, "Flesh knits itself back together: heal steadily, always.", regen: 3f);
        Add(Bestiary.FormGolem, "Iron Golem", Bestiary.FormOgre, 3, 12, 0.70f, 1.35f, "Iron hide: every blow against you is blunted.", armor: 2);
        Add(Bestiary.FormColossus, "Colossus", Bestiary.FormTroll, 4, 16, 0.65f, 1.90f, "Vast and furious. Heals fast and shrugs off blows.", regen: 2f, armor: 1);
        Add(Bestiary.FormTitan, "Titan", Bestiary.FormGolem, 4, 20, 0.60f, 1.70f, "A walking fortress. Almost nothing gets through.", armor: 4, regen: 5f);

        // ARCANE — kite at range. Fragile, fast, ranged.
        Add(Bestiary.FormWraith2, "Wraith", Bestiary.FormCrab, 2, 0, 1.15f, 1.10f, "Claws become bolts of raw magic.");
        Add(Bestiary.FormLich, "Lich", Bestiary.FormWraith2, 3, 2, 1.05f, 1.25f, "The dead answer: your kills sometimes rise to fight for you.", raise: 0.25f);
        Add(Bestiary.FormPyre, "Pyre", Bestiary.FormWraith2, 3, 2, 1.15f, 1.30f, "Living flame: every bolt you fire detonates on impact.", boltsExplode: true);
        Add(Bestiary.FormLichDread, "Dread Lich", Bestiary.FormLich, 4, 6, 1.00f, 1.55f, "Command over death: most of what you kill rises again for you.", raise: 0.55f);
        Add(Bestiary.FormRedDragon, "Red Dragon", Bestiary.FormPyre, 4, 6, 1.00f, 1.60f, "A furnace within: bolts detonate, and scales blunt every blow.", boltsExplode: true, armor: 1);

        // VENOM — hit and run. Damage comes from where you have been.
        Add(Bestiary.FormCrawler, "Crawler", Bestiary.FormCrab, 2, 0, 1.30f, 0.85f, "Dashing leaves a trail of venom.", venom: 2.5f);
        Add(Bestiary.FormBroodmother, "Broodmother", Bestiary.FormCrawler, 3, 4, 1.15f, 0.90f, "Your venom lingers far longer.", venom: 5f);
        Add(Bestiary.FormBileHorror, "Bile Horror", Bestiary.FormCrawler, 3, 2, 1.20f, 1.00f, "Venom eats away at speed as well as flesh.", venom: 3f, venomSlows: true);
        Add(Bestiary.FormHydra, "Hydra", Bestiary.FormBroodmother, 4, 8, 1.05f, 1.30f, "Poison leaps from the dying to the living.", venom: 6f, venomSpreads: true);
        Add(Bestiary.FormGorgon, "Gorgon", Bestiary.FormBileHorror, 4, 6, 1.10f, 1.35f, "Your venom all but stops the swarm dead.", venom: 4f, venomSlows: true);

        // WISP — speed and evasion. Weakest hits, never gets caught.
        Add(Bestiary.FormWisp, "Wisp", Bestiary.FormCrab, 2, -2, 1.45f, 0.70f, "Barely there. Blindingly quick, and you dash twice as often.", dashCooldownScale: 0.5f);
        Add(Bestiary.FormRime, "Rime", Bestiary.FormWisp, 3, 2, 1.30f, 0.85f, "Everything you strike is left crawling.", slowOnHit: true, dashCooldownScale: 0.5f);
        Add(Bestiary.FormShade, "Shade", Bestiary.FormWisp, 3, 0, 1.40f, 0.95f, "Step out of the world: dashing leaves you untouchable.", dashInvuln: 0.6f, dashCooldownScale: 0.5f);
        Add(Bestiary.FormFrostDragon, "Frost Dragon", Bestiary.FormRime, 4, 6, 1.25f, 1.30f, "A winter that hunts: your blows freeze, your hide holds.", slowOnHit: true, armor: 1, dashCooldownScale: 0.5f);
        Add(Bestiary.FormReaper, "Reaper", Bestiary.FormShade, 4, 4, 1.35f, 1.45f, "Death on time: untouchable when you move, and it rises for you.", dashInvuln: 0.8f, dashCooldownScale: 0.4f, raise: 0.3f);
    }

    public static Form Info(string sprite) =>
        Forms.TryGetValue(sprite, out var form) ? form : Forms[Bestiary.FormCrab];

    public static IEnumerable<Form> All => Forms.Values;

    public static string DisplayName(string sprite) => Info(sprite).Name;

    public static float ScaleFor(string sprite) => Mathf.Pow(GrowthPerTier, Info(sprite).Tier - 1);

    /// Can the player evolve into `target` right now? Only if it grows directly
    /// from what they already are. This single check is the whole tier rule.
    public static bool CanEvolveTo(string current, string target)
    {
        var info = Info(target);
        return info.Parent != null && info.Parent == current;
    }

    /// Applies the form: sprite, size, bulk, speed and damage all at once.
    public static void Apply(GameObject player, string sprite)
    {
        if (player == null) return;

        var art = Bestiary.Get(sprite);
        if (art != null)
        {
            var renderer = player.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = art;
        }

        // Forms are single front-facing frames, so the prefab's four-direction
        // swapping would fight this every movement frame.
        if (player.TryGetComponent<PlayerController>(out var controller))
            controller.UseDirectionalSprites = false;

        if (player.TryGetComponent<EvolutionSystem>(out var evolution))
            evolution.ApplyFormStats(sprite, Info(sprite), ScaleFor(sprite));
    }

    /// Preview sprite for a card, or null when it isn't an evolution.
    public static Sprite PreviewFor(EvolutionSystem evolution, SkillId candidate)
    {
        string target = evolution != null ? evolution.EvolutionTargetOf(candidate) : null;
        return target == null ? null : Bestiary.Get(target);
    }

    /// Ladder order, for the index screen.
    public static readonly string[] AllForms =
    {
        Bestiary.FormCrab,
        Bestiary.FormOgre, Bestiary.FormTroll, Bestiary.FormColossus, Bestiary.FormGolem, Bestiary.FormTitan,
        Bestiary.FormWraith2, Bestiary.FormLich, Bestiary.FormLichDread, Bestiary.FormPyre, Bestiary.FormRedDragon,
        Bestiary.FormCrawler, Bestiary.FormBroodmother, Bestiary.FormHydra, Bestiary.FormBileHorror, Bestiary.FormGorgon,
        Bestiary.FormWisp, Bestiary.FormRime, Bestiary.FormFrostDragon, Bestiary.FormShade, Bestiary.FormReaper,
    };
}
