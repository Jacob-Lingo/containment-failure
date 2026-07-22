using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    public void Restart()
    {
        GameState.ForceUnfreeze();

        FloorManager.ResetRun();
        RunStats.ResetRun();
        BossState.Reset();

        SceneManager.LoadScene(GameManager.LastLevelIndex);
    }
}