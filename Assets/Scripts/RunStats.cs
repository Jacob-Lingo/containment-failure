/// In-memory per-run combat stats: kills and attack usage broken down by
/// AttackType. Feeds EvolutionSystem's dominant-track choice and matches the
/// "combat actions / enemies killed by type" data collection from the GDD.
public static class RunStats
{
    public static int MeleeKills { get; private set; }
    public static int RangedKills { get; private set; }
    public static int ScreamKills { get; private set; }

    public static int MeleeAttacksUsed { get; private set; }
    public static int RangedAttacksUsed { get; private set; }
    public static int ScreamAttacksUsed { get; private set; }

    public static int TotalKills => MeleeKills + RangedKills + ScreamKills;

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
    }

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
    }
}
