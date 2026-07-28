using UnityEngine;

/// In-scene "you escaped" overlay shown on reaching the final floor —
/// deliberately doesn't change scenes or stop the game, so mobs keep
/// spawning underneath it and testing can continue uninterrupted. Continue
/// just hides the panel again.
public class WinPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        // Nothing resets the run here (unlike death), so the live stats are
        // still the winning run's — capture and show them in one go.
        var evolution = FindFirstObjectByType<EvolutionSystem>();
        RunSummary.Capture("ESCAPED THE DUNGEON", evolution != null ? evolution.GetSummary() : "Claws only");
        RunSummary.Show();
    }

    public void Continue()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        RunSummary.Hide();
    }
}
