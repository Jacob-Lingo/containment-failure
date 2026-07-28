using System.Collections.Generic;
using UnityEngine;

/// Sprite lookup for every themed creature in the game, loaded from
/// Assets/Resources/Bestiary (Kenney Tiny Dungeon, CC0). Central so the enemy
/// tiers in SpawnDirector and the player's evolution forms both name their art
/// in one place instead of scattering Resources.Load calls.
///
/// Cached: Resources.Load hits the asset database, and SpawnDirector calls this
/// on every spawn.
public static class Bestiary
{
    // Enemies — humans hunting the escaped monster.
    public const string Knight = "knight";
    public const string KnightCaptain = "knight_captain";
    public const string Archer = "archer";
    public const string Mage = "mage";
    public const string Skeleton = "skeleton";
    public const string Warden = "warden";
    public const string Cultist = "cultist";
    public const string Squire = "squire";
    public const string Elder = "elder";
    public const string Brigand = "brigand";
    public const string Acolyte = "acolyte";
    public const string Slime = "slime";
    public const string Wolf = "wolf";
    public const string Rat = "rat";
    public const string Lizardman = "lizardman";
    public const string Imp = "imp";
    public const string Ent = "ent";
    public const string Zombie = "zombie";
    public const string Bat = "bat";
    public const string SkeletonWarrior = "skeleton_warrior";

    /// Same behaviour, different faces. Every skin in a group uses the exact
    /// same brain and stats — it's purely so a swarm doesn't look like forty
    /// copies of one knight.
    ///
    /// Brigand and Cultist are deliberately NOT in these pools: they are the
    /// reserved faces of the Leaping Brute and the Necromancer. A tier the
    /// player has to recognise on sight (one telegraphs a slam, the other must
    /// be rushed down) can't share a face with the filler swarm.
    private static readonly string[] MeleeSkins = { Knight, Skeleton, Squire, Slime, Zombie, SkeletonWarrior };
    private static readonly string[] RangedSkins = { Archer, Elder, Lizardman };
    // Brigand and Cultist are deliberately absent: they're reserved as the
    // recognisable faces of the Leaping Brute and Necromancer tiers. A tier you
    // must identify on sight can't share a face with the filler swarm.
    private static readonly string[] CasterSkins = { Mage, Acolyte, Imp };

    public static string RandomMeleeSkin() => MeleeSkins[Random.Range(0, MeleeSkins.Length)];
    public static string RandomRangedSkin() => RangedSkins[Random.Range(0, RangedSkins.Length)];
    public static string RandomCasterSkin() => CasterSkins[Random.Range(0, CasterSkins.Length)];

    // Player evolution forms, see MonsterForm.
    public const string FormCrab = "form_crab";
    public const string FormColossus = "form_colossus";

    // Tier 2-4 evolution forms (TinyCreatures pack).
    public const string FormOgre = "form_ogre";
    public const string FormTroll = "form_troll";
    public const string FormGolem = "form_golem";
    public const string FormTitan = "form_titan";

    public const string FormWraith2 = "form_wraith2";
    public const string FormLich = "form_lich";
    public const string FormPyre = "form_pyre";
    public const string FormRedDragon = "form_reddragon";
    public const string FormLichDread = "form_dreadlich";

    public const string FormCrawler = "form_crawler";
    public const string FormBroodmother = "form_broodmother";
    public const string FormBileHorror = "form_bilehorror";
    public const string FormHydra = "form_hydra";
    public const string FormGorgon = "form_gorgon";

    public const string FormWisp = "form_wisp";
    public const string FormRime = "form_rime";
    public const string FormShade = "form_shade";
    public const string FormFrostDragon = "form_frostdragon";
    public const string FormReaper = "form_reaper";

    /// Everything that can show up as an NPC, for the bestiary screen.
    public static readonly string[] AllEnemies =
    {
        Knight, KnightCaptain, Squire, Skeleton, SkeletonWarrior, Zombie, Slime,
        Archer, Elder, Lizardman, Mage, Acolyte, Imp,
        Wolf, Rat, Ent, Brigand, Cultist, Warden,
    };

    /// Human-readable names for the bestiary.
    public static string DisplayName(string enemy)
    {
        switch (enemy)
        {
            case Knight: return "Knight";
            case KnightCaptain: return "Knight Captain";
            case Squire: return "Squire";
            case Skeleton: return "Skeleton";
            case SkeletonWarrior: return "Skeleton Warrior";
            case Zombie: return "Zombie";
            case Slime: return "Slime";
            case Archer: return "Archer";
            case Elder: return "Elder";
            case Lizardman: return "Lizardman";
            case Mage: return "Mage";
            case Acolyte: return "Acolyte";
            case Imp: return "Imp";
            case Wolf: return "Wolf";
            case Rat: return "Giant Rat";
            case Ent: return "Ent";
            case Brigand: return "Leaping Brute";
            case Cultist: return "Necromancer";
            case Warden: return "The Warden";
            default: return enemy;
        }
    }

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string name)
    {
        if (cache.TryGetValue(name, out var cached)) return cached;

        var sprite = Resources.Load<Sprite>("Bestiary/" + name);
        if (sprite == null)
            Debug.LogWarning($"Bestiary: no sprite at Resources/Bestiary/{name}.");

        cache[name] = sprite;
        return sprite;
    }

    /// Applies a bestiary sprite to whatever renderer a spawned creature has,
    /// and clears any tint — the old tier system tinted a plain white sprite to
    /// tell variants apart, which would discolour the real art. Also tells
    /// GuardHealth what to revert to after a hit flash.
    public static void Apply(GameObject creature, string spriteName)
    {
        var sprite = Get(spriteName);
        if (sprite == null) return;

        var renderer = creature.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null) return;

        renderer.sprite = sprite;
        renderer.color = Color.white;

        MetaStats.RecordEnemy(spriteName);

        if (creature.TryGetComponent<GuardHealth>(out var health))
            health.SetBaseColor(Color.white);

        // The Tiny Dungeon characters are drawn holding their own weapons, so
        // the separate WeaponIcon child (a sci-fi gun/baton) would sit on top
        // of the art as a second, wrong weapon.
        var weaponIcon = creature.GetComponentInChildren<WeaponIcon>();
        if (weaponIcon != null) weaponIcon.gameObject.SetActive(false);
    }
}
