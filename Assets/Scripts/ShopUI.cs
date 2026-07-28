using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// The between-run shop: spend banked coins (MetaProgression) on permanent
/// upgrades. Built entirely in code and injected into the main menu scene on
/// load, rather than hand-authored — Dev_MainMenu is a shared scene, and this
/// way the whole feature is one script with nothing to wire in the Inspector.
///
/// Same runtime-UI approach as SceneTransition and RunSummary; see UiFont for
/// how the text ends up matching the rest of the game's UI.
public class ShopUI : MonoBehaviour
{
    private const string MenuSceneName = "Dev_MainMenu";

    private static readonly Color Gold = new Color(1f, 0.85f, 0.2f);
    private static readonly Color Panel = new Color(0.06f, 0.06f, 0.09f, 0.97f);
    private static readonly Color CardIdle = new Color(0.16f, 0.16f, 0.21f);
    private static readonly Color CardMaxed = new Color(0.13f, 0.20f, 0.14f);
    private static readonly Color Dim = new Color(0.55f, 0.55f, 0.60f);

    private GameObject root;
    private TMP_Text coinLabel;
    private TMP_FontAsset font;

    private readonly List<MetaUpgradeId> rowIds = new List<MetaUpgradeId>();
    private readonly List<Image> rowBackgrounds = new List<Image>();
    private readonly List<TMP_Text> rowLabels = new List<TMP_Text>();
    private readonly List<TMP_Text> rowCosts = new List<TMP_Text>();

    /// Adds the shop (button + panel) to the main menu whenever it loads. No
    /// scene edit needed, so this survives anyone re-saving Dev_MainMenu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MenuSceneName) return;
        if (FindFirstObjectByType<ShopUI>() != null) return;

        new GameObject("ShopUI").AddComponent<ShopUI>();
    }

    private void Start()
    {
        font = UiFont.Resolve();
        if (font == null)
        {
            Debug.LogWarning("ShopUI: no TMP font available, skipping the shop.");
            return;
        }

        BuildOpenButton();
        BuildPanel();
        root.SetActive(false);
    }

    // --- Construction ---

    private Canvas NewCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void BuildOpenButton()
    {
        var canvas = NewCanvas("ShopButtonCanvas", 500);

        var button = NewButton(canvas.transform, "SHOP", new Vector2(320f, 90f), CardIdle);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 120f);

        button.onClick.AddListener(Open);
    }

    private void BuildPanel()
    {
        var canvas = NewCanvas("ShopCanvas", 900);
        root = canvas.gameObject;

        var background = NewImage(canvas.transform, "Background", Panel);
        Stretch(background.rectTransform);

        var title = NewLabel(background.transform, "UPGRADES", 64f, Color.white);
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 80f));

        coinLabel = NewLabel(background.transform, string.Empty, 40f, Gold);
        Anchor(coinLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 60f));

        // One row per upgrade, laid out top-down from the header.
        const float rowHeight = 96f;
        const float rowGap = 12f;
        float y = -250f;

        foreach (var upgrade in MetaProgression.Upgrades)
        {
            var row = NewButton(background.transform, string.Empty, new Vector2(1100f, rowHeight), CardIdle);
            Anchor((RectTransform)row.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1100f, rowHeight));
            y -= rowHeight + rowGap;

            var text = NewLabel(row.transform, string.Empty, 28f, Color.white);
            text.alignment = TextAlignmentOptions.Left;
            Anchor(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(780f, rowHeight));

            var cost = NewLabel(row.transform, string.Empty, 32f, Gold);
            cost.alignment = TextAlignmentOptions.Right;
            Anchor(cost.rectTransform, new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(240f, rowHeight));

            var id = upgrade.Id;
            row.onClick.AddListener(() => TryBuy(id));

            rowIds.Add(id);
            rowBackgrounds.Add(row.GetComponent<Image>());
            rowLabels.Add(text);
            rowCosts.Add(cost);
        }

        var close = NewButton(background.transform, "BACK", new Vector2(280f, 80f), CardIdle);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 70f);
        close.onClick.AddListener(Close);
    }

    // --- Behaviour ---

    public void Open()
    {
        if (root == null) return;
        root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    /// Refreshes whether or not the buy succeeded — a failed purchase still
    /// repaints, so "can't afford" reads as feedback rather than a dead button.
    private void TryBuy(MetaUpgradeId id)
    {
        MetaProgression.Buy(id);
        Refresh();
    }

    private void Refresh()
    {
        coinLabel.text = $"{MetaProgression.Coins} coins";

        for (int i = 0; i < rowIds.Count; i++)
        {
            var info = MetaProgression.InfoFor(rowIds[i]);
            int level = MetaProgression.LevelOf(rowIds[i]);
            int cost = MetaProgression.CostOf(rowIds[i]);
            bool maxed = cost < 0;

            rowLabels[i].text = $"<b>{info.Title}</b>   <size=80%>Lv {level}/{info.MaxLevel}</size>\n<size=75%>{info.Description}</size>";
            rowCosts[i].text = maxed ? "MAX" : $"{cost}";
            rowCosts[i].color = maxed ? Dim : (MetaProgression.CanAfford(rowIds[i]) ? Gold : Dim);
            rowBackgrounds[i].color = maxed ? CardMaxed : CardIdle;
        }
    }

    // --- Small UI builders (kept local; nothing else in the repo needs them) ---

    private Image NewImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text NewLabel(Transform parent, string text, float size, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private Button NewButton(Transform parent, string text, Vector2 size, Color color)
    {
        var image = NewImage(parent, "Button", color);
        image.rectTransform.sizeDelta = size;

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (!string.IsNullOrEmpty(text))
        {
            var label = NewLabel(image.transform, text, 34f, Color.white);
            Stretch(label.rectTransform);
        }

        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
