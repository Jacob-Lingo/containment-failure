using UnityEngine;

public enum MetaUpgradeId
{
    Vitality,
    Swiftness,
    Greed,
    Magnetism,
    HeadStart,
    Salvage,
    Luck
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
        new UpgradeInfo { Id = MetaUpgradeId.Vitality,  Title = "Reinforced Hide", Description = "Start every run with +2 max health.",           MaxLevel = 5, BaseCost = 20 },
        new UpgradeInfo { Id = MetaUpgradeId.Swiftness, Title = "Lab Legs",        Description = "Start every run 6% faster.",                    MaxLevel = 5, BaseCost = 25 },
        new UpgradeInfo { Id = MetaUpgradeId.Greed,     Title = "Sticky Fingers",  Description = "Coins are worth +1 each.",                      MaxLevel = 4, BaseCost = 30 },
        new UpgradeInfo { Id = MetaUpgradeId.Magnetism, Title = "Static Charge",   Description = "Pull coins and orbs in from 1.5 units further.", MaxLevel = 4, BaseCost = 20 },
        new UpgradeInfo { Id = MetaUpgradeId.HeadStart, Title = "Head Start",      Description = "Begin each run one level-up closer.",            MaxLevel = 3, BaseCost = 40 },
        new UpgradeInfo { Id = MetaUpgradeId.Salvage,   Title = "Salvage Rig",     Description = "Guards drop 10% more coins.",                    MaxLevel = 3, BaseCost = 35 },
        new UpgradeInfo { Id = MetaUpgradeId.Luck,      Title = "Lucky Streak",    Description = "Rarer level-up cards show up more often.",       MaxLevel = 5, BaseCost = 45 },
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

    /// Extra chance for a guard to drop a coin on death, on top of the base
    /// drop rate in GuardHealth.
    public static float BonusCoinDropChance => 0.10f * LevelOf(MetaUpgradeId.Salvage);

    /// Luck multiplier applied to a card's base draw weight in
    /// EvolutionSystem.DrawWeighted. Scales with how rare the card is, so
    /// Common (tier 0) is untouched and Legendary (tier 4) gains the most —
    /// luck should shift the *shape* of the pool, not just inflate everything.
    /// At max level a Legendary's weight is 3.4x its base.
    public static float DrawWeightMultiplier(int rarityTier)
    {
        return 1f + 0.12f * rarityTier * LevelOf(MetaUpgradeId.Luck);
    }
}
