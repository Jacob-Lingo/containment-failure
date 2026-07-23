using UnityEngine;

/// Single owner of Time.timeScale freezing. Before this existed, the
/// level-up card (EvolutionSystem) and the pause menu (PauseMenu) each set
/// timeScale directly without knowing about the other, so e.g. pausing and
/// resuming during a pending level-up card unfroze the game behind the
/// modal. Now: exactly one FreezeReason can hold the freeze at a time;
/// TryFreeze fails for anyone else until the holder releases. Gameplay
/// input (PlayerCombat, PlayerDash) checks IsFrozen so clicking UI while
/// frozen doesn't also swing attacks or pollute RunStats metrics.
public static class GameState
{
    public enum FreezeReason { None, LevelUpChoice, PauseMenu }

    public static FreezeReason Holder { get; private set; } = FreezeReason.None;

    public static bool IsFrozen => Holder != FreezeReason.None;

    /// Reset on every play-mode entry. Static state survives editor Play
    /// sessions when Domain Reload is disabled (Unity 6 default), so a test
    /// stopped while a card/pause held the freeze would leave Holder stuck
    /// non-None into the next session — freezing attacks and level-up cards
    /// from frame one. Runs before the first scene loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Holder = FreezeReason.None;
        Time.timeScale = 1f;
    }

    /// Returns true if `reason` now holds (or already held) the freeze.
    public static bool TryFreeze(FreezeReason reason)
    {
        if (Holder != FreezeReason.None && Holder != reason) return false;

        Holder = reason;
        Time.timeScale = 0f;
        return true;
    }

    /// Only the current holder can release; anyone else's stale release
    /// is ignored (prevents the old double-unfreeze bugs).
    public static void Release(FreezeReason reason)
    {
        if (Holder != reason) return;

        Holder = FreezeReason.None;
        Time.timeScale = 1f;
    }

    /// Scene-change safety valve: statics survive LoadScene, so anything
    /// that jumps scenes (lose-screen restart, play again) clears the
    /// freeze unconditionally rather than arriving in a frozen scene.
    public static void ForceUnfreeze()
    {
        Holder = FreezeReason.None;
        Time.timeScale = 1f;
    }
}
