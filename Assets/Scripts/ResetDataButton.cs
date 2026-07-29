using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Wipes all persistent player data: banked gold and bought upgrades
/// (MetaProgression), lifetime card statistics and unlocked forms (MetaStats),
/// and the Endless depth record (GameMode).
///
/// Two-click confirmation, because this is the one irreversible button in the
/// game and it sits next to Play. First click arms it and relabels; a second
/// click within the timeout commits, otherwise it disarms itself. A modal would
/// be more code for a button most players press once, if ever.
public class ResetDataButton : MonoBehaviour
{
    private const float ArmedSeconds = 4f;
    private const string IdleLabel = "RESET DATA";
    private const string ArmedLabel = "CONFIRM WIPE?";

    private Button button;
    private float disarmAt;
    private bool armed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Main menu only. On the lose screen this would sit next to "play
        // again" right after a bad run, which is the worst possible moment to
        // put a wipe button under the player's cursor.
        if (FindFirstObjectByType<MainMenu>() == null) return;
        if (FindFirstObjectByType<ResetDataButton>() != null) return;

        new GameObject("ResetDataButton").AddComponent<ResetDataButton>();
    }

    private void Start()
    {
        button = MenuButtons.Clone(transform, IdleLabel, 3, OnPressed);
    }

    private void Update()
    {
        if (armed && Time.unscaledTime >= disarmAt) Disarm();
    }

    private void OnPressed()
    {
        if (!armed)
        {
            armed = true;
            disarmAt = Time.unscaledTime + ArmedSeconds;
            SetLabel(ArmedLabel);
            return;
        }

        MetaProgression.ResetAll();
        MetaStats.ResetAll();
        GameMode.ClearRecord();
        PlayerPrefs.Save();

        Disarm();
        Debug.Log("All saved progression wiped.");
    }

    private void Disarm()
    {
        armed = false;
        SetLabel(IdleLabel);
    }

    private void SetLabel(string text)
    {
        if (button == null) return;

        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) label.text = text;
        foreach (var legacy in button.GetComponentsInChildren<Text>(true)) legacy.text = text;
    }
}
