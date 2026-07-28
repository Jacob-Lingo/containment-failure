using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// Adds ENDLESS to the main menu: the same gameplay scene, but floors never
/// stop, there's no Warden and no Iron Key gate, and the only score is how deep
/// you got. Injected on scene load like the shop and index, so no scene edit.
///
/// Only on the main menu, not on the lose/win screens — those already have
/// their own "play again" routes, and offering a mode switch there would
/// silently change what run the player thinks they're restarting.
public class EndlessButton : MonoBehaviour
{
    private const string PlayScene = "Master";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindFirstObjectByType<MainMenu>() == null) return;
        if (FindFirstObjectByType<EndlessButton>() != null) return;

        new GameObject("EndlessButton").AddComponent<EndlessButton>();
    }

    private void Start()
    {
        string label = GameMode.BestEndlessFloor > 0
            ? $"ENDLESS (best {GameMode.BestEndlessFloor})"
            : "ENDLESS";

        MenuButtons.Clone(transform, label, 2, StartEndless);
    }

    private void StartEndless()
    {
        GameMode.SelectEndless();

        // Same reset the story-mode Play does — without it the run inherits
        // whatever floor/kills the last one ended on.
        FloorManager.ResetRun();
        RunStats.ResetRun();
        BossState.Reset();

        SceneTransition.LoadScene(PlayScene);
    }
}
