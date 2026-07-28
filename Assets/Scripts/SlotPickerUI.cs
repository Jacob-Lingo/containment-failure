using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// "Your ability bar is full — which one does this replace?" Shown only when a
/// slottable active is picked with all four slots occupied.
///
/// Built in code and self-destroying, like the other runtime UI, so it needs no
/// scene wiring. Cancel is always available: the caller re-rolls the level-up
/// cards rather than burning the pick, so refusing a swap is never a dead pick.
public class SlotPickerUI : MonoBehaviour
{
    private static readonly Color Panel = new Color(0.05f, 0.05f, 0.08f, 0.96f);
    private static readonly Color Row = new Color(0.16f, 0.16f, 0.21f);
    private static readonly Color Cancel = new Color(0.24f, 0.16f, 0.16f);

    private Action<int> onReplace;
    private Action onCancel;
    private TMP_FontAsset font;

    public static void Show(AbilitySlots slots, SkillId incoming, Action<int> onReplace, Action onCancel)
    {
        var font = UiFont.Resolve();
        if (font == null)
        {
            // No font means no picker could be read, so don't trap the player
            // behind an invisible modal — fall back to overwriting slot 1.
            Debug.LogWarning("SlotPickerUI: no TMP font available; replacing slot 1.");
            onReplace?.Invoke(0);
            return;
        }

        var picker = new GameObject("SlotPickerUI").AddComponent<SlotPickerUI>();
        picker.onReplace = onReplace;
        picker.onCancel = onCancel;
        picker.font = font;
        picker.Build(slots, incoming);
    }

    private void Build(AbilitySlots slots, SkillId incoming)
    {
        var canvasGO = new GameObject("SlotPickerCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950; // above the level-up cards, below the wipe

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        var background = NewImage(canvasGO.transform, Panel);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        var title = NewLabel(background.transform, 46f, Color.white);
        title.text = $"Your abilities are full.\nReplace one with <b>{EvolutionSystem.TitleOf(incoming)}</b>?";
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1100f, 160f));

        float y = -330f;
        for (int i = 0; i < AbilitySlots.Count; i++)
        {
            string bound = slots.NameAt(i) ?? "(empty)";
            int index = i;

            var button = NewButton(background.transform, new Vector2(900f, 92f), Row);
            Anchor((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(900f, 92f));
            y -= 104f;

            var label = NewLabel(button.transform, 32f, Color.white);
            label.text = $"[{AbilitySlots.KeyLabel(index)}]   {bound}";
            Stretch(label.rectTransform);

            button.onClick.AddListener(() => Finish(() => onReplace?.Invoke(index)));
        }

        var cancel = NewButton(background.transform, new Vector2(900f, 84f), Cancel);
        Anchor((RectTransform)cancel.transform, new Vector2(0.5f, 1f), new Vector2(0f, y - 24f), new Vector2(900f, 84f));

        var cancelLabel = NewLabel(cancel.transform, 30f, new Color(0.9f, 0.8f, 0.8f));
        cancelLabel.text = "Keep what I have — show me different cards";
        Stretch(cancelLabel.rectTransform);

        cancel.onClick.AddListener(() => Finish(() => onCancel?.Invoke()));
    }

    /// Tears the picker down before running the callback: onCancel re-opens the
    /// level-up choice, and this modal must not still be sitting on top of it.
    private void Finish(Action action)
    {
        Destroy(gameObject);
        action?.Invoke();
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

    private Button NewButton(Transform parent, Vector2 size, Color color)
    {
        var image = NewImage(parent, color);
        image.rectTransform.sizeDelta = size;

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
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
