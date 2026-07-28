using UnityEngine;

/// Which way the current run is being played. Story mode is the authored
/// descent that ends at FloorManager.TotalFloors with the Warden and the Iron
/// Key; Endless never ends — floors keep going and the only score is how deep
/// you got before dying.
///
/// Deliberately a static rather than a field on anything: FloorManager,
/// SpawnDirector, FloorExitDoor and FloorHUD all need to know, and they have no
/// shared owner. Reset on play-mode entry like the other run-scoped statics.
public static class GameMode
{
    private const string BestFloorKey = "endless.bestFloor";

    public static bool IsEndless { get; private set; }

    /// Deepest floor ever reached in Endless. Persisted, unlike everything
    /// else about a run — it's the mode's entire scoreboard.
    public static int BestEndlessFloor => PlayerPrefs.GetInt(BestFloorKey, 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => IsEndless = false;

    public static void SelectEndless() => IsEndless = true;

    public static void SelectStory() => IsEndless = false;

    public static void ClearRecord() => PlayerPrefs.DeleteKey(BestFloorKey);

    /// Called as each floor is reached, not at death — a run that crashes or is
    /// quit mid-floor should still keep the depth it actually earned.
    public static void RecordDepth(int floor)
    {
        if (!IsEndless || floor <= BestEndlessFloor) return;

        PlayerPrefs.SetInt(BestFloorKey, floor);
        PlayerPrefs.Save();
    }
}
