/// Tracks the floor-10 Tank boss across a run, mirroring FloorManager/RunStats'
/// plain-static pattern. Set by SpawnDirector (Spawned) and BossMarker
/// (Defeated); read by FloorExitDoor to gate the escape scene.
public static class BossState
{
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
