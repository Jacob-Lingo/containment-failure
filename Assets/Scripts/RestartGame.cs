using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    public void Restart()
    {
        Time.timeScale = 1f;

        FloorManager.ResetRun();
        RunStats.ResetRun();
        BossState.Reset();

        SceneManager.LoadScene(GameManager.LastLevelIndex);
    }
}