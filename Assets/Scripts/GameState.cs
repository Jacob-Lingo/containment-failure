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
