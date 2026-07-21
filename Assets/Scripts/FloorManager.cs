/// Tracks progress through the facility's 10 floors across scene reloads.
/// Plain static state (like GameManager) rather than a MonoBehaviour/
/// DontDestroyOnLoad object, so it survives SceneManager.LoadScene calls
/// with no extra wiring.
public static class FloorManager
{
    public const int TotalFloors = 3;

    public static int CurrentFloor { get; private set; } = 1;

    public static bool IsFinalFloor => CurrentFloor >= TotalFloors;

    /// Multiplier applied to guard stats for the current floor — a flat 15%
    /// bump per floor past the first, so floor 10 guards are noticeably
    /// tougher than floor 1.
    public static float DifficultyMultiplier => 1f + 0.15f * (CurrentFloor - 1);

    /// Kills required on the current floor before SpawnDirector auto-advances
    /// before the exit door unlocks (the final floor is Tank-gated instead — see BossState).
    public static int KillQuota => 30 + (CurrentFloor - 1) * 8;

    public static void AdvanceFloor()
    {
        if (!IsFinalFloor)
            CurrentFloor++;
    }

    public static void ResetRun()
    {
        CurrentFloor = 1;
    }
}
