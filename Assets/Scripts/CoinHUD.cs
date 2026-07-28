using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Bottom-left coin counter showing the banked MetaProgression total, so the
/// shop balance is visible while you're earning it. Icon is Unity's built-in
/// "Knob" UI sprite tinted gold — a stock round sprite that ships with the
/// editor, so there's no art asset to import or license.
///
/// Injected into the gameplay scene on load like ShopUI, rather than added to
/// Master.unity (integration-only).
public class CoinHUD : MonoBehaviour
{
    private const string GameSceneName = "Master";
    private const float PunchDuration = 0.2f;

    private static readonly Color Gold = new Color(1f, 0.84f, 0.25f);

    private TMP_Text label;
    private RectTransform group;
    private int shownCoins = -1;
    private float punchTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameSceneName) return;
        if (FindFirstObjectByType<CoinHUD>() != null) return;

        new GameObject("CoinHUD").AddComponent<CoinHUD>();
    }

    private void Start()
    {
        var font = UiFont.Resolve();
        if (font == null)
        {
            Debug.LogWarning("CoinHUD: no TMP font available, skipping the counter.");
            return;
        }

        var canvasGO = new GameObject("CoinHUDCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400; // under the shop (900) and the wipe (1000)

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var groupGO = new GameObject("Counter");
        groupGO.transform.SetParent(canvasGO.transform, false);
        group = groupGO.AddComponent<RectTransform>();
        group.anchorMin = group.anchorMax = group.pivot = new Vector2(0f, 0f);
        group.anchoredPosition = new Vector2(40f, 40f);
        group.sizeDelta = new Vector2(260f, 64f);

        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(group, false);
        var icon = iconGO.AddComponent<Image>();
        icon.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        icon.color = Gold;
        icon.raycastTarget = false;
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0f, 0.5f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        icon.rectTransform.sizeDelta = new Vector2(52f, 52f);

        var labelGO = new GameObject("Amount");
        labelGO.transform.SetParent(group, false);
        label = labelGO.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 40f;
        label.color = Gold;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0f, 0.5f);
        label.rectTransform.anchoredPosition = new Vector2(66f, 0f);
        label.rectTransform.sizeDelta = new Vector2(200f, 60f);
    }

    private void Update()
    {
        if (label == null) return;

        int coins = MetaProgression.Coins;
        if (coins != shownCoins)
        {
            shownCoins = coins;
            label.text = coins.ToString();
            punchTimer = PunchDuration;
        }

        // Scale punch on change, so picking a coin up registers in the corner
        // of the eye. Unscaled so it still plays during a hitstop.
        if (punchTimer > 0f)
        {
            punchTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(punchTimer / PunchDuration);
            group.localScale = Vector3.one * (1f + 0.25f * t * t);
        }
        else if (group.localScale != Vector3.one)
        {
            group.localScale = Vector3.one;
        }
    }
}
