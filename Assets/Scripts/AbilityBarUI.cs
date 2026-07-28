using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Bottom-of-screen ability bar: perfect-square slots for M1 (melee or
/// ranged, whichever PlayerCombat currently maps M1 to), Scream (Space),
/// Dash (Shift), Beam (R), Cyclone (E), and Berserk (F). Builds its whole UI
/// at runtime — no scene wiring, no art assets beyond the existing Combat
/// icons — following the same build-your-own-Canvas approach
/// SceneTransition.cs already established. Re-created fresh each time
/// PlayerCombat.Awake() runs (i.e. every scene load), so it never holds a
/// stale reference to a destroyed player.
public class AbilityBarUI : MonoBehaviour
{
    private const int SlotSize = 64;
    private const float LockedAlpha = 0.3f;
    private static readonly Color CooldownColor = new Color(0.8f, 0.1f, 0.1f, 0.75f);

    private static AbilityBarUI instance;

    private class Slot
    {
        public Image icon;
        public Image fill;
        public TextMeshProUGUI keyText;
        public TextMeshProUGUI nameText;
        public string keybind;
        public Color tint;
        public Func<bool> isUnlocked;
        public Func<float> cooldownFraction;
        public Func<string> abilityName;
    }

    private PlayerCombat combat;
    private PlayerDash dash;
    private EvolutionSystem evolution;
    private Sprite clawSprite, gunSprite;

    private Image m1Icon, m1Fill;
    private TextMeshProUGUI m1NameText;
    private readonly List<Slot> slots = new List<Slot>();

    public static void EnsureInstance(PlayerCombat playerCombat, PlayerDash playerDash)
    {
        if (instance != null) Destroy(instance.gameObject);

        var go = new GameObject("AbilityBarUI");
        instance = go.AddComponent<AbilityBarUI>();
        instance.combat = playerCombat;
        instance.dash = playerDash;
        instance.evolution = playerCombat != null ? playerCombat.GetComponent<EvolutionSystem>() : null;
        instance.clawSprite = Resources.Load<Sprite>("Combat/claw_slash");
        instance.gunSprite = Resources.Load<Sprite>("Combat/gun_icon");
        instance.BuildUI();
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("AbilityBarCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasGO.AddComponent<CanvasScaler>();

        var row = new GameObject("Row");
        row.transform.SetParent(canvasGO.transform, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 24f);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Without this, the row's own RectTransform keeps its default size
        // instead of shrinking/growing to fit its children, so anchoring its
        // pivot at screen-center only centers an undersized box — the actual
        // content overflows to one side instead of the whole group being centered.
        var fitter = row.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildSlot(row.transform, clawSprite, "M1", null, out m1Icon, out m1Fill, out _, out m1NameText);

        // Dash keeps a dedicated key and its own pip; it isn't slottable.
        slots.Add(MakeSlot(row.transform, "SHIFT", new Color(0.2f, 0.8f, 1f),
            () => dash != null && evolution != null && evolution.UnlockedDash,
            () => dash != null ? dash.DashCooldownFraction : 0f,
            () => evolution != null && evolution.DashLunge ? "Lunge Dash" : evolution != null && evolution.DashPhase ? "Phase Dash" : "Dash"));

        // Four bindable slots, keys 1-4. Each reads whatever the player has
        // bound rather than a fixed ability, so the bar follows the loadout.
        for (int i = 0; i < AbilitySlots.Count; i++)
        {
            int index = i;
            slots.Add(MakeSlot(row.transform, AbilitySlots.KeyLabel(index), SlotTint(index),
                () => evolution != null && evolution.Slots.At(index).HasValue,
                () => CooldownOf(evolution != null ? evolution.Slots.At(index) : null),
                () => NameOf(index)));
        }
    }

    private static Color SlotTint(int index)
    {
        switch (index)
        {
            case 0: return new Color(0.6f, 0.2f, 1f);
            case 1: return new Color(1f, 0.75f, 0.2f);
            case 2: return new Color(0.3f, 0.9f, 0.4f);
            default: return new Color(0.9f, 0.2f, 0.2f);
        }
    }

    /// Slot label. Empower shows its banked charges, since its cooldown pip
    /// says when it can be recast — not how many empowered swings are left,
    /// which is the number the player actually needs mid-fight.
    private string NameOf(int index)
    {
        if (evolution == null) return "";

        string name = evolution.Slots.NameAt(index) ?? "";
        if (combat != null && evolution.Slots.At(index) == SkillId.Empower && combat.EmpowerRemaining > 0)
            name += " x" + combat.EmpowerRemaining;

        return name;
    }

    private float CooldownOf(SkillId? id)
    {
        if (combat == null || !id.HasValue) return 0f;

        switch (id.Value)
        {
            case SkillId.UnlockScream: return combat.ScreamCooldownFraction;
            case SkillId.UnlockBeam: return combat.BeamCooldownFraction;
            case SkillId.UnlockCyclone: return combat.CycloneCooldownFraction;
            case SkillId.UnlockBerserk: return combat.BerserkCooldownFraction;
            case SkillId.GroundSlam: return combat.SlamCooldownFraction;
            case SkillId.Meteor: return combat.MeteorCooldownFraction;
            case SkillId.WebTrap: return combat.WebCooldownFraction;
            case SkillId.FrostNova: return combat.NovaCooldownFraction;
            case SkillId.Blink: return combat.BlinkCooldownFraction;
            case SkillId.LeapSmash: return combat.LeapCooldownFraction;
            case SkillId.ChainLightning: return combat.ChainCooldownFraction;
            case SkillId.VenomBurst: return combat.VenomBurstCooldownFraction;
            case SkillId.Warp: return combat.WarpCooldownFraction;
            case SkillId.Bulwark: return combat.BulwarkCooldownFraction;
            case SkillId.Terrify: return combat.TerrifyCooldownFraction;
            case SkillId.Maelstrom: return combat.MaelstromCooldownFraction;
            case SkillId.FlameWheel: return combat.FlameWheelCooldownFraction;
            case SkillId.Starfall: return combat.StarfallCooldownFraction;
            case SkillId.BroodCall: return combat.BroodCooldownFraction;
            case SkillId.Overcharge: return combat.OverchargeCooldownFraction;
            case SkillId.Ward: return combat.WardCooldownFraction;
            case SkillId.Empower: return combat.EmpowerCooldownFraction;
            default: return 0f;
        }
    }

    private Slot MakeSlot(Transform parent, string keybind, Color tint, Func<bool> isUnlocked, Func<float> cooldownFraction, Func<string> abilityName)
    {
        BuildSlot(parent, null, keybind, tint, out Image icon, out Image fill, out TextMeshProUGUI keyText, out TextMeshProUGUI nameText);
        return new Slot
        {
            icon = icon,
            fill = fill,
            keyText = keyText,
            nameText = nameText,
            keybind = keybind,
            tint = tint,
            isUnlocked = isUnlocked,
            cooldownFraction = cooldownFraction,
            abilityName = abilityName,
        };
    }

    private void BuildSlot(Transform parent, Sprite iconSprite, string keybind, Color? tint, out Image icon, out Image fill, out TextMeshProUGUI keyText, out TextMeshProUGUI nameText)
    {
        var slot = new GameObject(keybind + "Slot");
        slot.transform.SetParent(parent, false);
        var slotLayout = slot.AddComponent<VerticalLayoutGroup>();
        slotLayout.childAlignment = TextAnchor.UpperCenter;
        slotLayout.spacing = 2f;
        slotLayout.childForceExpandWidth = false;
        slotLayout.childForceExpandHeight = false;
        slotLayout.childControlWidth = false;
        slotLayout.childControlHeight = false;

        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(slot.transform, false);
        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(SlotSize, SlotSize); // perfect square
        icon = iconGO.AddComponent<Image>();
        icon.sprite = iconSprite != null ? iconSprite : GetSolidSprite();
        icon.color = tint ?? Color.white;

        var fillGO = new GameObject("CooldownFill");
        fillGO.transform.SetParent(iconGO.transform, false);
        var fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill = fillGO.AddComponent<Image>();
        fill.sprite = GetSolidSprite();
        fill.color = CooldownColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;

        var keyGO = new GameObject("Keybind");
        keyGO.transform.SetParent(slot.transform, false);
        var keyRect = keyGO.AddComponent<RectTransform>();
        keyRect.sizeDelta = new Vector2(SlotSize, 18f);
        keyText = keyGO.AddComponent<TextMeshProUGUI>();
        keyText.text = keybind;
        keyText.fontSize = 14f;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = Color.white;

        var nameGO = new GameObject("AbilityName");
        nameGO.transform.SetParent(slot.transform, false);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(SlotSize + 20f, 16f);
        nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = "";
        nameText.fontSize = 11f;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.85f, 0.85f, 0.85f);
    }

    private static Sprite solidSprite;
    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null) return solidSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        solidSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return solidSprite;
    }

    private void Update()
    {
        if (combat == null) return;

        m1Icon.sprite = combat.RangedIsPrimary ? gunSprite : clawSprite;
        m1Fill.fillAmount = combat.M1CooldownFraction;
        m1NameText.text = combat.RangedIsPrimary ? "Ranged Shot" : "Claw";

        foreach (var slot in slots)
        {
            bool unlocked = slot.isUnlocked();
            slot.icon.color = new Color(slot.tint.r, slot.tint.g, slot.tint.b, unlocked ? 1f : LockedAlpha);
            slot.fill.fillAmount = unlocked ? slot.cooldownFraction() : 0f;
            slot.keyText.text = unlocked ? slot.keybind : "";
            slot.nameText.text = unlocked ? slot.abilityName() : "";
        }
    }
}
