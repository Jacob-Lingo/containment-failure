using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    [SerializeField] private string FallbackScene = "Master";

    public void Restart()
    {
        GameState.ForceUnfreeze();

        FloorManager.ResetRun();
        RunStats.ResetRun();
        BossState.Reset();

        // LastLevelIndex is a build index, and it's -1 when the run started
        // from a scene that isn't in the build settings (e.g. play-testing
        // Dev_Levels straight from the Project window). Fall back to the real
        // gameplay scene rather than throwing on LoadScene(-1).
        if (GameManager.LastLevelIndex < 0)
            SceneManager.LoadScene(FallbackScene);
        else
            SceneManager.LoadScene(GameManager.LastLevelIndex);
    }
}