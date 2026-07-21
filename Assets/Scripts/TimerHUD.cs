using UnityEngine;
using TMPro;

/// Displays SpawnDirector's floor countdown. The color shift under 10s is
/// the player-facing pressure cue for the GDD's "escape before containment
/// systems activate" rule. Wire in Master: TMP text under the HUD canvas,
/// drag this text + the SpawnDirector in.
public class TimerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private SpawnDirector spawnDirector;

    private void Update()
    {
        if (timerText == null || spawnDirector == null) return;

        float t = Mathf.Max(0f, spawnDirector.CurrentTime);
        timerText.text = $"{Mathf.FloorToInt(t / 60):0}:{Mathf.FloorToInt(t % 60):00}";
        timerText.color = t <= 10f ? Color.red : Color.white;
    }
}
