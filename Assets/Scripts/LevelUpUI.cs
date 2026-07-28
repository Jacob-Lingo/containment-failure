using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// The 3-card level-up panel from the GDD: pauses the game (Time.timeScale),
/// shows up to 3 choices with title/description, and reports back which
/// index was clicked. Card content is set at runtime by EvolutionSystem
/// since which 3 skills are on offer is randomized each level-up.
public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardTitles;
    [SerializeField] private TMP_Text[] cardDescriptions;

    [Header("Rarity (display only — all optional, everything falls back at runtime)")]
    [Tooltip("Card background Image per slot. Leave empty to use each card Button's own Image.")]
    [SerializeField] private Image[] cardBackgrounds;
    [Tooltip("Rarity name shown above each card. Leave empty to prefix the title instead.")]
    [SerializeField] private TMP_Text[] cardRarityLabels;
    [Tooltip("Card art in Rarity enum order. Empty slots load from Resources/Cards/<Rarity>Card.")]
    [SerializeField] private Sprite[] rarityArt = new Sprite[RarityCount];

    [Tooltip("Multiplies each card's size and spacing at startup. 1 = leave the scene layout alone.")]
    [SerializeField] private float cardScale = 2f;

    private const int RarityCount = 5;

    private static readonly Color[] RarityColors =
    {
        new Color(0.78f, 0.78f, 0.78f), // Common
        new Color(0.40f, 0.85f, 0.40f), // Uncommon
        new Color(0.35f, 0.60f, 1.00f), // Rare
        new Color(0.70f, 0.40f, 1.00f), // Epic
        new Color(1.00f, 0.65f, 0.15f), // Legendary
    };

    private Action<int> onChosen;
    private Image[] formPreviews;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        LoadMissingRarityArt();
        ResizeCards();
    }

    /// The card PNGs live in Resources/Cards so the panel works without any
    /// Inspector wiring. They're imported as Sprite Mode: Multiple (the art is
    /// a slice of a larger sheet), so the sprite is a sub-asset — LoadAll, not
    /// Load. Anything dragged in via the Inspector wins over this.
    private void LoadMissingRarityArt()
    {
        if (rarityArt == null || rarityArt.Length < RarityCount)
            System.Array.Resize(ref rarityArt, RarityCount);

        for (int r = 0; r < RarityCount; r++)
        {
            if (rarityArt[r] != null) continue;

            string path = "Cards/" + (Rarity)r + "Card";
            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites.Length > 0)
                rarityArt[r] = sprites[0];
            else
                Debug.LogWarning($"LevelUpUI: no sprite found at Resources/{path}.");
        }
    }

    /// Grows the cards from code instead of in Master.unity (integration-only
    /// scene). Spacing scales with the size so the cards don't grow into each
    /// other. Awake-only — running this per Show() would compound.
    private void ResizeCards()
    {
        if (Mathf.Approximately(cardScale, 1f)) return;

        // Width comes from the card art's aspect so the frame fills the rect
        // instead of stretching.
        float aspect = rarityArt[0] != null
            ? rarityArt[0].rect.width / rarityArt[0].rect.height
            : 0f;

        foreach (var button in cardButtons)
        {
            var rect = (RectTransform)button.transform;
            Vector2 size = rect.sizeDelta * cardScale;
            if (aspect > 0f) size.x = size.y * aspect;

            rect.sizeDelta = size;
            rect.anchoredPosition *= cardScale;

            ScaleText(button);
        }
    }

    /// Text doesn't ride along with the card rect, so scale it explicitly:
    /// font size, plus the height and vertical offset of any label whose rect
    /// is a fixed height (the Title). Full-stretch labels (the Description)
    /// already grow with the card, so touching their sizeDelta — which is an
    /// inset, not a size — would shrink them instead.
    private void ScaleText(Button card)
    {
        foreach (var text in card.GetComponentsInChildren<TMP_Text>(true))
        {
            text.fontSize *= cardScale;

            var rect = text.rectTransform;
            if (!Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y)) continue;

            Vector2 size = rect.sizeDelta;
            size.y *= cardScale;
            rect.sizeDelta = size;

            Vector2 position = rect.anchoredPosition;
            position.y *= cardScale;
            rect.anchoredPosition = position;
        }
    }

    public void Show(string[] titles, string[] descriptions, Rarity[] rarities, Sprite[] previews, Action<int> chosen)
    {
        onChosen = chosen;

        for (int i = 0; i < cardButtons.Length; i++)
        {
            bool active = i < titles.Length && titles[i] != null;
            cardButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            cardTitles[i].text = titles[i];
            cardDescriptions[i].text = descriptions[i];
            ApplyRarity(i, rarities[i]);
            ShowFormPreview(i, previews != null && i < previews.Length ? previews[i] : null);

            int index = i;
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => Choose(index));
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// Shows what you'd turn into above the card, when that card changes your
    /// form. Built lazily per slot and reused; hidden for cards that don't
    /// transform you, so the badge itself signals "this is a permanent shape
    /// change" without needing extra text.
    private void ShowFormPreview(int i, Sprite preview)
    {
        if (formPreviews == null || formPreviews.Length != cardButtons.Length)
            formPreviews = new Image[cardButtons.Length];

        if (formPreviews[i] == null)
        {
            if (preview == null) return; // nothing to show and nothing built yet

            var go = new GameObject("FormPreview");
            go.transform.SetParent(cardButtons[i].transform, false);

            var image = go.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(96f, 96f);
            rect.anchoredPosition = new Vector2(0f, 104f); // sits above the card, not on it

            formPreviews[i] = image;
        }

        formPreviews[i].sprite = preview;
        formPreviews[i].gameObject.SetActive(preview != null);
    }

    /// Labels how strong the card is. Display only — rarity has no effect on
    /// which cards are offered (see the Rarity enum). With nothing wired in the
    /// Inspector this repaints the card Button's own Image with the rarity
    /// frame and prepends a coloured rarity line to the title.
    private void ApplyRarity(int i, Rarity rarity)
    {
        int r = (int)rarity;

        Image background = cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null
            ? cardBackgrounds[i]
            : cardButtons[i].image;

        if (background != null && rarityArt[r] != null)
        {
            background.sprite = rarityArt[r];
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.color = Color.white; // the scene's card box is tinted grey
        }

        string rarityText = rarity.ToString().ToUpperInvariant();

        if (cardRarityLabels != null && i < cardRarityLabels.Length && cardRarityLabels[i] != null)
        {
            cardRarityLabels[i].text = rarityText;
            cardRarityLabels[i].color = RarityColors[r];
        }
        else
        {
            string hex = ColorUtility.ToHtmlStringRGB(RarityColors[r]);
            cardTitles[i].text = $"<color=#{hex}>{rarityText}</color>\n{cardTitles[i].text}";
        }
    }

    /// Dismiss without choosing (run reset while a card was pending) —
    /// clears the callback so a stale click can't grant an old skill.
    public void Hide()
    {
        onChosen = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Choose(int index)
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        onChosen?.Invoke(index);
    }
}
