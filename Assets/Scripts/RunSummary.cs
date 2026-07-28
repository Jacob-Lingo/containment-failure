using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// End-of-run recap: "Floor 3 · 84 kills · 4:12" plus the build you took.
/// Every number already exists in RunStats/FloorManager/EvolutionSystem — the
/// only real work is snapshotting them *before* FloorRunWatcher wipes the run,
/// and surviving the scene load into LoseScreen.
///
/// The overlay is built in code (canvas + one TMP label) rather than added to
/// a scene, both because Master.unity is integration-only and because the
/// lose/win screens are separate scenes; same approach as HitFlashFx/ExpOrb.
public static class RunSummary
{
    public static string Text { get; private set; }

    private static GameObject overlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Text = null;
        overlay = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// Snapshot the run. MUST be called before RunStats.ResetRun() — the whole
    /// point is that death resets the stats we want to report.
    public static void Capture(string outcome, string build)
    {
        int minutes = Mathf.FloorToInt(RunStats.Elapsed / 60f);
        int seconds = Mathf.FloorToInt(RunStats.Elapsed % 60f);

        Text =
            $"{outcome}\n" +
            $"Depth {FloorManager.CurrentFloor}   ·   {RunStats.TotalKills} slain   ·   {minutes}:{seconds:00}\n" +
            $"<color=#FFD933>+{RunStats.CoinsThisRun} gold</color>   <size=70%>({MetaProgression.Coins} hoarded)</size>\n" +
            $"<size=70%>Claw {RunStats.MeleeKills}   Bolt {RunStats.RangedKills}   Scream {RunStats.ScreamKills}</size>\n\n" +
            $"<size=75%>{build}</size>";
    }

    /// The lose screen is its own scene, so there's nothing there to call
    /// Show() — hook the load instead and the recap appears by itself.
    /// Detected by the RestartGame component rather than the scene name, so a
    /// renamed or duplicated lose screen still gets the recap.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        overlay = null; // destroyed along with the previous scene

        if (Object.FindFirstObjectByType<RestartGame>() != null) Show();
    }

    public static void Show()
    {
        if (string.IsNullOrEmpty(Text) || overlay != null) return;

        TMP_FontAsset font = UiFont.Resolve();
        if (font == null)
        {
            Debug.LogWarning("RunSummary: no TMP font available, skipping the recap overlay.");
            return;
        }

        overlay = new GameObject("RunSummary");

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above the lose screen's own UI

        var scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var labelObject = new GameObject("Text");
        labelObject.transform.SetParent(overlay.transform, false);

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = Text;
        label.fontSize = 34f;
        label.alignment = TextAlignmentOptions.Top;
        label.color = new Color(0.92f, 0.92f, 0.95f);
        label.raycastTarget = false; // never eat clicks on the Play Again button

        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 500f);
        rect.anchoredPosition = new Vector2(0f, -40f);
    }

    public static void Hide()
    {
        if (overlay == null) return;

        Object.Destroy(overlay);
        overlay = null;
    }
}
