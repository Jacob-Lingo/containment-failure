using UnityEngine;

/// In-memory per-run combat stats: kills and attack usage broken down by
/// AttackType. Feeds EvolutionSystem's dominant-track choice and matches the
/// "combat actions / enemies killed by type" data collection from the GDD.
public static class RunStats
{
    /// Reset run-scoped static state on every play-mode entry / player launch.
    /// Static state survives editor Play sessions when Domain Reload is
    /// disabled (Unity 6 default), so a run left mid-progress (e.g. stopping
    /// play on floor 3) would carry that floor/kills/boss state into the next
    /// Play. Runs before the first scene loads, guaranteeing a clean run.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => ResetRun();

    public static int MeleeKills { get; private set; }
    public static int RangedKills { get; private set; }
    public static int ScreamKills { get; private set; }

    public static int MeleeAttacksUsed { get; private set; }
    public static int RangedAttacksUsed { get; private set; }
    public static int ScreamAttacksUsed { get; private set; }

    public static int TotalKills => MeleeKills + RangedKills + ScreamKills;

    /// Kills on the current floor only — drives SpawnDirector's auto-advance
    /// (FloorManager.KillQuota). Reset per-floor (ResetFloorKills) and per-run.
    public static int FloorKills { get; private set; }

    /// Experience from the yellow ExpPickup orbs scattered around the map —
    /// added to TotalKills by EvolutionSystem's level-up counter, so orbs are
    /// a genuine second XP source independent of kills.
    public static int BonusExp { get; private set; }

    public static void RegisterBonusExp(int amount) => BonusExp += amount;

    public static void RegisterAttackUsed(AttackType type)
    {
        switch (type)
        {
            case AttackType.Melee: MeleeAttacksUsed++; break;
            case AttackType.Ranged: RangedAttacksUsed++; break;
            case AttackType.Scream: ScreamAttacksUsed++; break;
        }
    }

    public static void RegisterKill(AttackType type)
    {
        switch (type)
        {
            case AttackType.Melee: MeleeKills++; break;
            case AttackType.Ranged: RangedKills++; break;
            case AttackType.Scream: ScreamKills++; break;
        }
        FloorKills++;
    }

    /// Called by SpawnDirector once a floor's kill quota is met and it
    /// auto-advances — distinct from ResetRun, which also fires this.
    public static void ResetFloorKills() => FloorKills = 0;

    public static AttackType DominantKillType()
    {
        if (MeleeKills >= RangedKills && MeleeKills >= ScreamKills) return AttackType.Melee;
        if (RangedKills >= ScreamKills) return AttackType.Ranged;
        return AttackType.Scream;
    }

    public static void ResetRun()
    {
        MeleeKills = RangedKills = ScreamKills = 0;
        MeleeAttacksUsed = RangedAttacksUsed = ScreamAttacksUsed = 0;
        FloorKills = 0;
        BonusExp = 0;
    }
}
