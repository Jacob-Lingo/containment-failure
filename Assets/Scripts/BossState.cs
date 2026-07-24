using UnityEngine;

/// Tracks the floor-10 Tank boss across a run, mirroring FloorManager/RunStats'
/// plain-static pattern. Set by SpawnDirector (Spawned) and BossMarker
/// (Defeated); read by FloorExitDoor to gate the escape scene.
public static class BossState
{
    /// Reset run-scoped static state on every play-mode entry / player launch.
    /// Static state survives editor Play sessions when Domain Reload is
    /// disabled (Unity 6 default), so a run left mid-progress (e.g. stopping
    /// play on floor 3) would carry that floor/kills/boss state into the next
    /// Play. Runs before the first scene loads, guaranteeing a clean run.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => Reset();

    public static bool Spawned { get; private set; }
    public static bool Defeated { get; private set; }

    public static void MarkSpawned() => Spawned = true;
    public static void MarkDefeated() => Defeated = true;

    public static void Reset()
    {
        Spawned = false;
        Defeated = false;
    }
}
