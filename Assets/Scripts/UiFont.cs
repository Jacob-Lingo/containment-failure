using UnityEngine;
using TMPro;

/// Finds a usable TMP font for UI that's built at runtime rather than authored
/// in a scene (SceneTransition's floor card, RunSummary's recap). Prefers the
/// project default; failing that it borrows whatever font the current scene's
/// own labels use, so the runtime text matches the hand-authored HUD.
public static class UiFont
{
    public static TMP_FontAsset Resolve()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        var existing = Object.FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        return existing != null ? existing.font : null;
    }
}
