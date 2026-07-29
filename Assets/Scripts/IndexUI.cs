using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// The index, in three scrollable tabs:
///   MONSTERS — every evolution form, laid out as the actual tree
///   BESTIARY — every NPC, with how many you've killed
///   CARDS    — sortable pick-rate table
///
/// All three scroll, because the tree alone is 21 entries and a fixed panel
/// could only ever show a slice of it.
public class IndexUI : MonoBehaviour
{
    private enum Tab { Monsters, Bestiary, Cards }
    private enum Sort { PickRate, Picked, Seen, Used, Name }

    private static readonly Color Panel = new Color(0.06f, 0.06f, 0.09f, 0.97f);
    private static readonly Color Row = new Color(0.15f, 0.15f, 0.20f);
    private static readonly Color RowAlt = new Color(0.11f, 0.11f, 0.15f);
    private static readonly Color Header = new Color(0.22f, 0.22f, 0.30f);
    private static readonly Color HeaderOn = new Color(0.36f, 0.30f, 0.14f);
    private static readonly Color Gold = new Color(1f, 0.85f, 0.2f);
    private static readonly Color Dim = new Color(0.45f, 0.45f, 0.5f);

    private GameObject root;
    private TMP_FontAsset font;
    private Tab tab = Tab.Monsters;
    private Sort sort = Sort.PickRate;

    private RectTransform content;
    private readonly List<Button> tabButtons = new List<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool betweenRuns = FindFirstObjectByType<MainMenu>() != null
                        || FindFirstObjectByType<RestartGame>() != null
                        || FindFirstObjectByType<PlayAgainButton>() != null;
        if (!betweenRuns || FindFirstObjectByType<IndexUI>() != null) return;

        new GameObject("IndexUI").AddComponent<IndexUI>();
    }

    private void Start()
    {
        font = UiFont.Resolve();
        if (font == null)
        {
            Debug.LogWarning("IndexUI: no TMP font available, skipping the index.");
            return;
        }

        if (MenuButtons.Clone(transform, "INDEX", 1, Open) == null)
        {
            var canvas = NewCanvas("IndexButtonCanvas", 500);
            var fallback = NewButton(canvas.transform, new Vector2(320f, 90f), Row, "INDEX", 32f);
            var rect = (RectTransform)fallback.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            fallback.onClick.AddListener(Open);
        }

        BuildPanel();
        root.SetActive(false);
    }

    private void BuildPanel()
    {
        var canvas = NewCanvas("IndexCanvas", 900);
        root = canvas.gameObject;

        var background = NewImage(canvas.transform, Panel);
        Stretch(background.rectTransform);

        var title = NewLabel(background.transform, 52f, Color.white);
        title.text = "INDEX";
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(900f, 64f));

        BuildTabs(background.transform);
        BuildScrollArea(background.transform);

        var close = NewButton(background.transform, new Vector2(260f, 74f), Row, "BACK", 30f);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 30f);
        close.onClick.AddListener(Close);
    }

    private void BuildTabs(Transform parent)
    {
        var tabs = new (string label, Tab which)[]
        {
            ("MONSTERS", Tab.Monsters), ("BESTIARY", Tab.Bestiary), ("CARDS", Tab.Cards),
        };

        float x = -300f;
        foreach (var (label, which) in tabs)
        {
            var button = NewButton(parent, new Vector2(280f, 58f), Header, label, 26f);
            Anchor((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(x, -120f), new Vector2(280f, 58f));
            x += 300f;

            Tab captured = which;
            button.onClick.AddListener(() => { tab = captured; Refresh(); });
            tabButtons.Add(button);
        }
    }

    /// A real ScrollRect: the monster tree is 21 entries and the card table
    /// grows with every skill, so a fixed viewport can only ever show a slice.
    private void BuildScrollArea(Transform parent)
    {
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(parent, false);
        var viewport = viewportGO.AddComponent<RectTransform>();
        Anchor(viewport, new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(1500f, 690f));

        var mask = viewportGO.AddComponent<Image>();
        mask.color = new Color(0f, 0f, 0f, 0.25f);
        viewportGO.AddComponent<Mask>().showMaskGraphic = true;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        content = contentGO.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var scroll = parent.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
    }

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

    private void Refresh()
    {
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        for (int i = 0; i < tabButtons.Count; i++)
            tabButtons[i].GetComponent<Image>().color = (int)tab == i ? HeaderOn : Header;

        float used = tab == Tab.Monsters ? BuildMonsters()
                   : tab == Tab.Bestiary ? BuildBestiary()
                   : BuildCards();

        // Content height drives whether the ScrollRect can scroll at all.
        content.sizeDelta = new Vector2(0f, used);
    }

    /// The tree, drawn as the tree: one row per tier chain, indented by tier so
    /// the branch structure is visible rather than implied.
    private float BuildMonsters()
    {
        float y = -10f;

        foreach (string form in MonsterForm.AllForms)
        {
            var info = MonsterForm.Info(form);
            bool unlocked = MetaStats.HasUnlockedForm(form);

            var row = NewImage(content, info.Tier % 2 == 0 ? Row : RowAlt);
            Anchor(row.rectTransform, new Vector2(0.5f, 1f), new Vector2((info.Tier - 1) * 60f, y), new Vector2(1300f, 96f));
            y -= 104f;

            var portrait = NewImage(row.transform, unlocked ? Color.white : new Color(0.16f, 0.16f, 0.2f));
            portrait.sprite = Bestiary.Get(form);
            portrait.preserveAspect = true;
            Anchor(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(76f, 76f));

            var text = NewLabel(row.transform, 24f, unlocked ? Color.white : Dim);
            text.alignment = TextAlignmentOptions.Left;
            text.text = unlocked
                ? $"<b>{info.Name}</b>  <size=75%>Tier {info.Tier}</size>\n<size=72%>{info.Blurb}</size>"
                : $"<b>???</b>  <size=75%>Tier {info.Tier}</size>\n<size=72%>Not yet evolved into.</size>";
            Anchor(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(1100f, 90f));
        }

        return -y + 20f;
    }

    private float BuildBestiary()
    {
        float y = -10f;

        foreach (string enemy in Bestiary.AllEnemies)
        {
            bool seen = MetaStats.HasSeenEnemy(enemy);
            int slain = MetaStats.EnemiesSlain(enemy);

            var row = NewImage(content, Row);
            Anchor(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1300f, 80f));
            y -= 88f;

            var portrait = NewImage(row.transform, seen ? Color.white : new Color(0.16f, 0.16f, 0.2f));
            portrait.sprite = Bestiary.Get(enemy);
            portrait.preserveAspect = true;
            Anchor(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(64f, 64f));

            var text = NewLabel(row.transform, 26f, seen ? Color.white : Dim);
            text.alignment = TextAlignmentOptions.Left;
            text.text = seen ? Bestiary.DisplayName(enemy) : "???";
            Anchor(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(700f, 70f));

            var count = NewLabel(row.transform, 24f, seen ? Gold : Dim);
            count.alignment = TextAlignmentOptions.Right;
            count.text = seen ? $"encountered {slain}" : "never encountered";
            Anchor(count.rectTransform, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(420f, 70f));
        }

        return -y + 20f;
    }

    private float BuildCards()
    {
        var options = new (string label, Sort mode)[]
        {
            ("Pick rate", Sort.PickRate), ("Picked", Sort.Picked),
            ("Offered", Sort.Seen), ("Cast", Sort.Used), ("Name", Sort.Name),
        };

        float x = -560f;
        foreach (var (label, mode) in options)
        {
            var button = NewButton(content, new Vector2(210f, 46f), sort == mode ? HeaderOn : Header, label, 20f);
            Anchor((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(x, -10f), new Vector2(210f, 46f));
            x += 224f;

            Sort captured = mode;
            button.onClick.AddListener(() => { sort = captured; Refresh(); });
        }

        var ids = new List<SkillId>();
        foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
            if (MetaStats.Seen(id) > 0) ids.Add(id);
        ids.Sort(Compare);

        float y = -70f;
        if (ids.Count == 0)
        {
            var empty = NewLabel(content, 26f, Dim);
            empty.text = "No cards seen yet — finish a run to fill this in.";
            Anchor(empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1200f, 40f));
            return 140f;
        }

        for (int i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            var row = NewImage(content, i % 2 == 0 ? Row : RowAlt);
            Anchor(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1400f, 40f));
            y -= 44f;

            var name = NewLabel(row.transform, 23f, Color.white);
            name.alignment = TextAlignmentOptions.Left;
            name.text = EvolutionSystem.TitleOf(id);
            Anchor(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(560f, 40f));

            var stats = NewLabel(row.transform, 23f, Gold);
            stats.alignment = TextAlignmentOptions.Right;
            stats.text = $"{MetaStats.PickRate(id) * 100f:0}%   ·   picked {MetaStats.Picked(id)}/{MetaStats.Seen(id)}   ·   cast {MetaStats.Used(id)}";
            Anchor(stats.rectTransform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(780f, 40f));
        }

        return -y + 20f;
    }

    private int Compare(SkillId a, SkillId b)
    {
        switch (sort)
        {
            case Sort.Picked: return MetaStats.Picked(b).CompareTo(MetaStats.Picked(a));
            case Sort.Seen: return MetaStats.Seen(b).CompareTo(MetaStats.Seen(a));
            case Sort.Used: return MetaStats.Used(b).CompareTo(MetaStats.Used(a));
            case Sort.Name: return string.Compare(EvolutionSystem.TitleOf(a), EvolutionSystem.TitleOf(b), StringComparison.Ordinal);
            default: return MetaStats.PickRate(b).CompareTo(MetaStats.PickRate(a));
        }
    }

    // --- builders ---

    private Canvas NewCanvas(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Image NewImage(Transform parent, Color color)
    {
        var go = new GameObject("Image");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text NewLabel(Transform parent, float size, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private Button NewButton(Transform parent, Vector2 size, Color color, string text, float fontSize)
    {
        var image = NewImage(parent, color);
        image.rectTransform.sizeDelta = size;

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var label = NewLabel(image.transform, fontSize, Color.white);
        label.text = text;
        Stretch(label.rectTransform);

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
