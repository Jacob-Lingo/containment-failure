using UnityEngine;

public enum MetaUpgradeId
{
    Vitality,
    Swiftness,
    Greed,
    Magnetism,
    HeadStart,
    Salvage,
    IronKey
}

/// Between-run progression: coins banked from the yellow Coin pickups, and the
/// permanent upgrades bought with them. Unlike RunStats/FloorManager (which are
/// deliberately wiped every run) this is the one piece of state that is
/// *supposed* to survive, so it's PlayerPrefs-backed rather than in-memory.
///
/// PlayerPrefs on purpose: this is six ints. A save file, JSON serialization
/// and a write scheduler would all be real code to maintain for data that fits
/// in the registry, and PlayerPrefs already handles the cross-platform paths.
/// If saves ever need to be portable or tamper-resistant, that's the point to
/// switch — not before.
public static class MetaProgression
{
    private const string CoinsKey = "meta.coins";
    private const string UpgradeKeyPrefix = "meta.upgrade.";

    public struct UpgradeInfo
    {
        public MetaUpgradeId Id;
        public string Title;
        public string Description;
        public int MaxLevel;
        public int BaseCost;
    }

    /// Cost of the *next* level scales linearly: BaseCost * (owned + 1). Cheap
    /// first steps, meaningful last ones, no formula anyone has to reverse
    /// engineer from a spreadsheet.
    public static readonly UpgradeInfo[] Upgrades =
    {
        new UpgradeInfo { Id = MetaUpgradeId.Vitality,  Title = "Scaled Hide", Description = "Begin every descent with +2 max health.",           MaxLevel = 5, BaseCost = 20 },
        new UpgradeInfo { Id = MetaUpgradeId.Swiftness, Title = "Fleet Hooves",        Description = "Begin every descent 6% faster.",                    MaxLevel = 5, BaseCost = 25 },
        new UpgradeInfo { Id = MetaUpgradeId.Greed,     Title = "Hoarder's Instinct",  Description = "Gold is worth +1 per piece.",                      MaxLevel = 4, BaseCost = 30 },
        new UpgradeInfo { Id = MetaUpgradeId.Magnetism, Title = "Greedy Aura",   Description = "Draw gold and essence in from 1.5 units further.", MaxLevel = 4, BaseCost = 20 },
        new UpgradeInfo { Id = MetaUpgradeId.HeadStart, Title = "Ancient Memory",      Description = "Begin each descent one evolution closer.",            MaxLevel = 3, BaseCost = 40 },
        new UpgradeInfo { Id = MetaUpgradeId.IronKey,   Title = "Iron Key",        Description = "The gate out of the deepest vault is locked. Without this you cannot escape, however far you get.", MaxLevel = 1, BaseCost = 150 },
        new UpgradeInfo { Id = MetaUpgradeId.Salvage,   Title = "Grave Robber",     Description = "The slain drop 10% more gold.",                    MaxLevel = 3, BaseCost = 35 },
    };

    public static int Coins
    {
        get => PlayerPrefs.GetInt(CoinsKey, 0);
        private set { PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int LevelOf(MetaUpgradeId id) => PlayerPrefs.GetInt(UpgradeKeyPrefix + id, 0);

    public static UpgradeInfo InfoFor(MetaUpgradeId id)
    {
        foreach (var upgrade in Upgrades)
            if (upgrade.Id == id) return upgrade;
        return default;
    }

    /// Returns -1 when the upgrade is maxed, so callers can tell "can't afford"
    /// (a real cost you haven't reached) apart from "nothing left to buy".
    public static int CostOf(MetaUpgradeId id)
    {
        var info = InfoFor(id);
        int owned = LevelOf(id);
        return owned >= info.MaxLevel ? -1 : info.BaseCost * (owned + 1);
    }

    public static bool CanAfford(MetaUpgradeId id)
    {
        int cost = CostOf(id);
        return cost >= 0 && Coins >= cost;
    }

    public static bool Buy(MetaUpgradeId id)
    {
        if (!CanAfford(id)) return false;

        Coins -= CostOf(id);
        PlayerPrefs.SetInt(UpgradeKeyPrefix + id, LevelOf(id) + 1);
        PlayerPrefs.Save();
        return true;
    }

    /// Banked the instant a coin is picked up rather than at the end of a run,
    /// so dying never costs the player what they already collected — the whole
    /// point of a meta-currency is that a bad run still moves you forward.
    public static void AddCoins(int amount)
    {
        if (amount > 0) Coins += amount;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(CoinsKey);
        foreach (var upgrade in Upgrades)
            PlayerPrefs.DeleteKey(UpgradeKeyPrefix + upgrade.Id);
        PlayerPrefs.Save();
    }

    // --- Derived values, read by the systems each upgrade affects ---

    public static int BonusStartingHealth => LevelOf(MetaUpgradeId.Vitality) * 2;

    public static float StartingSpeedMultiplier => 1f + 0.06f * LevelOf(MetaUpgradeId.Swiftness);

    public static int CoinValue => 1 + LevelOf(MetaUpgradeId.Greed);

    public static float BonusMagnetRadius => 1.5f * LevelOf(MetaUpgradeId.Magnetism);

    public static int HeadStartLevels => LevelOf(MetaUpgradeId.HeadStart);

    /// The run-gate: the final floor's exit stays sealed until this is bought,
    /// so beating the game requires banking gold across several runs. Checked
    /// by FloorExitDoor and surfaced by FloorHUD.
    public static bool HasIronKey => LevelOf(MetaUpgradeId.IronKey) > 0;

    /// Extra chance for a guard to drop a coin on death, on top of the base
    /// drop rate in GuardHealth.
    public static float BonusCoinDropChance => 0.10f * LevelOf(MetaUpgradeId.Salvage);
}
