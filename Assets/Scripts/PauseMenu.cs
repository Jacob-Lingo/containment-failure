using UnityEngine;

/// Basic roguelike-run QoL: Escape toggles a pause panel with Resume,
/// Restart Run, and Quit. Restart deliberately resets everything in place
/// (floor, stats, skills, guards, ammo, health) rather than reloading the
/// scene, so the player never leaves the screen they were testing in.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    private bool isPaused;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void RestartRun()
    {
        FloorManager.ResetRun();
        RunStats.ResetRun();
        BossState.Reset();

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            if (player.TryGetComponent<PlayerHealth>(out var health)) health.ResetToStartingHealth();
            if (player.TryGetComponent<EvolutionSystem>(out var evolution)) evolution.ResetProgress();
            if (player.TryGetComponent<PlayerCombat>(out var combat)) combat.ResetCombatState();
        }

        var spawner = FindFirstObjectByType<SpawnDirector>();
        if (spawner != null) spawner.ResetForNewRun();

        var winPanel = FindFirstObjectByType<WinPanelUI>();
        if (winPanel != null) winPanel.Continue();

        Resume();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
