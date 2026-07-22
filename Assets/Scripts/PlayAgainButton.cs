using UnityEngine;

/// Wired to the win screen's "Play Again" button: clears run-scoped state so
/// the next playthrough starts clean at floor 1 with no leftover skills, and
/// drops the player straight back into gameplay rather than the main menu.
public class PlayAgainButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Master";

    public void PlayAgain()
    {
        FloorManager.ResetRun();
        RunStats.ResetRun();
        BossState.Reset();
        GameState.ForceUnfreeze();
        SceneTransition.LoadScene(targetSceneName);
    }
}
