using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Adds a button to a menu by cloning one that's already there, so runtime UI
/// (SHOP, INDEX, ENDLESS) inherits the scene's own sprite, colour, size and
/// font instead of inventing a look that doesn't match.
///
/// Extracted once there were three callers doing the same thing. It also keeps
/// the stacking rule in one place: clones go below the lowest existing button,
/// offset by how many we've already added, so they don't land on top of each
/// other.
public static class MenuButtons
{
    /// Returns null when the scene has no button to copy — callers should fall
    /// back to building their own so the feature stays reachable.
    public static Button Clone(Transform owner, string label, int stackIndex, Action onClick)
    {
        Button template = FindTemplate(owner);
        if (template == null) return null;

        var clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
        clone.name = label + "Button";
        clone.AddComponent<MenuButtonClone>(); // so FindTemplate skips it next time

        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent(); // drop the template's wiring
        button.onClick.AddListener(() => onClick?.Invoke());

        foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true)) text.text = label;
        foreach (var legacy in clone.GetComponentsInChildren<Text>(true)) legacy.text = label;

        // A layout group will place it for us; without one, stack manually.
        var parent = template.transform.parent;
        if (parent == null || parent.GetComponent<LayoutGroup>() == null)
        {
            var templateRect = (RectTransform)template.transform;
            var rect = (RectTransform)clone.transform;
            rect.anchoredPosition = templateRect.anchoredPosition
                - new Vector2(0f, (templateRect.sizeDelta.y + 14f) * (stackIndex + 1));
        }

        return button;
    }

    /// The lowest labelled button that isn't one of ours, so clones land at the
    /// bottom of the menu rather than in the middle of it.
    private static Button FindTemplate(Transform owner)
    {
        Button best = null;
        float lowest = float.MaxValue;

        foreach (var candidate in UnityEngine.Object.FindObjectsByType<Button>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (owner != null && candidate.transform.IsChildOf(owner)) continue;
            if (candidate.GetComponent<MenuButtonClone>() != null) continue; // one of ours
            if (candidate.GetComponentInChildren<TMP_Text>(true) == null
                && candidate.GetComponentInChildren<Text>(true) == null) continue;

            float y = candidate.transform.position.y;
            if (y < lowest)
            {
                lowest = y;
                best = candidate;
            }
        }

        return best;
    }
}
