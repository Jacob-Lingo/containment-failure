using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Full-screen black block-wipe between scenes (menu -> floor -> next floor
/// -> win/lose), so floor changes don't hard-cut. Builds its UI at runtime
/// (a grid of plain UI Images) rather than being hand-authored per scene,
/// and persists via DontDestroyOnLoad so the cover stays up while the next
/// scene loads underneath it. Call SceneTransition.LoadScene(name) instead
/// of SceneManager.LoadScene(name) anywhere a smooth change is wanted.
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    private const int Columns = 12;
    private const int Rows = 7;
    private const float StepDuration = 0.45f;

    private const float CardHoldSeconds = 0.9f;

    private GameObject canvasGO;
    private Image[] cells;
    private TMP_Text card;

    public static void LoadScene(string sceneName)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.DoTransition(sceneName));
    }

    /// Same wipe, no scene change: covers the screen, runs `atFullCover`
    /// (where the world is rearranged unseen), holds a title card, uncovers.
    /// Used for in-scene floor advances, which previously hard-cut — the new
    /// floor's rescale/respawn popped in with the player watching.
    public static void Interstitial(string label, Action atFullCover)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.DoInterstitial(label, atFullCover));
    }

    private IEnumerator DoInterstitial(string label, Action atFullCover)
    {
        // Nothing should be shooting the player from behind a black screen.
        // If something else already holds the freeze (pause menu), skip
        // freezing rather than fighting it — the wipe runs unscaled anyway.
        bool frozen = GameState.TryFreeze(GameState.FreezeReason.FloorTransition);

        canvasGO.SetActive(true);
        yield return Wipe(cover: true);

        atFullCover?.Invoke();

        ShowCard(label);
        yield return new WaitForSecondsRealtime(CardHoldSeconds);
        ShowCard(null);

        yield return Wipe(cover: false);
        canvasGO.SetActive(false);

        if (frozen) GameState.Release(GameState.FreezeReason.FloorTransition);
    }

    private void ShowCard(string label)
    {
        if (card == null)
        {
            if (string.IsNullOrEmpty(label)) return;

            var font = UiFont.Resolve();
            if (font == null) return;

            var cardGO = new GameObject("FloorCard");
            cardGO.transform.SetParent(canvasGO.transform, false);

            card = cardGO.AddComponent<TextMeshProUGUI>();
            card.font = font;
            card.fontSize = 90f;
            card.alignment = TextAlignmentOptions.Center;
            card.raycastTarget = false;

            var rect = card.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        card.text = label ?? string.Empty;
        // Sits above the wipe cells, which are drawn first in the same canvas.
        card.transform.SetAsLastSibling();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneTransition>();
        instance.BuildUI();
    }

    private void BuildUI()
    {
        canvasGO = new GameObject("TransitionCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();

        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(canvasGO.transform, false);
        var gridRect = gridGO.AddComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;

        var layout = gridGO.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = Columns;
        layout.spacing = Vector2.zero;
        layout.cellSize = new Vector2(
            Mathf.Max(1f, Screen.width) / Columns,
            Mathf.Max(1f, Screen.height) / Rows);

        cells = new Image[Columns * Rows];
        for (int i = 0; i < cells.Length; i++)
        {
            var cellGO = new GameObject("Cell" + i);
            cellGO.transform.SetParent(gridGO.transform, false);
            var img = cellGO.AddComponent<Image>();
            img.color = Color.black;
            cellGO.transform.localScale = Vector3.zero;
            cells[i] = img;
        }

        canvasGO.SetActive(false);
    }

    private IEnumerator DoTransition(string sceneName)
    {
        canvasGO.SetActive(true);

        yield return Wipe(cover: true);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
            yield return null;

        yield return Wipe(cover: false);

        canvasGO.SetActive(false);
    }

    private IEnumerator Wipe(bool cover)
    {
        float t = 0f;
        float perCellDelay = StepDuration * 0.5f / Columns;
        float perCellDuration = StepDuration * 0.5f;

        while (t < StepDuration)
        {
            t += Time.unscaledDeltaTime;

            for (int i = 0; i < cells.Length; i++)
            {
                float delay = (i % Columns) * perCellDelay;
                float local = Mathf.Clamp01((t - delay) / perCellDuration);
                float scale = cover ? local : 1f - local;
                cells[i].transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        float finalScale = cover ? 1f : 0f;
        foreach (var cell in cells)
            cell.transform.localScale = Vector3.one * finalScale;
    }
}
