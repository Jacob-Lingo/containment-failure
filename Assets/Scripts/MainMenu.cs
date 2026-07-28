using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private string playSceneName = "Master";
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject leaderboardPanel;
#pragma warning restore 0649

    public void PlayGame()
    {
        // Also restarts the run clock (RunStats.Elapsed) — without this the
        // end-of-run summary counts however long the menu was left open.
        RunStats.ResetRun();
        SceneTransition.LoadScene(playSceneName);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void OpenLeaderboard()
    {
        leaderboardPanel.SetActive(true);
    }

    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }
}
