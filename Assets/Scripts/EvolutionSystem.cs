using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum SkillId
{
    UnlockRanged,
    UnlockScream,
    UnlockDash,
    BiggerClaw,
    DoubleProjectile,
    RadiusScream,
    Vitality,
    Swift
}

/// Drives the "evolution track" from the GDD: kills feed a level counter,
/// and each level-up offers 3 random skill cards (LevelUpUI), pausing the
/// game until the player picks one. The run starts with melee claw only —
/// ranged, scream, and dash are all unlocked through cards rather than
/// available from the start, and their respective upgrade cards (Double
/// Projectile, Radius Scream) only enter the pool once unlocked. Falls back
/// to auto-granting the first option if no LevelUpUI exists in the scene,
/// so progression never stalls during a headless/dev-scene test.
public class EvolutionSystem : MonoBehaviour
{
    [SerializeField] private int killsPerLevel = 5;

    public bool UnlockedRanged { get; private set; }
    public bool UnlockedScream { get; private set; }
    public bool UnlockedDash { get; private set; }
    public bool BiggerClaw { get; private set; }
    public bool DoubleProjectile { get; private set; }
    public bool RadiusScream { get; private set; }

    private struct Skill
    {
        public SkillId Id;
        public string Title;
        public string Description;
        public int MaxStacks;
    }

    private static readonly Skill[] AllSkills =
    {
        new Skill { Id = SkillId.UnlockRanged, Title = "Ranged Attack", Description = "Unlocks a projectile attack (right click).", MaxStacks = 1 },
        new Skill { Id = SkillId.UnlockScream, Title = "Scream", Description = "Unlocks a shockwave attack (space).", MaxStacks = 1 },
        new Skill { Id = SkillId.UnlockDash, Title = "Dash", Description = "Unlocks a quick burst of speed (left shift).", MaxStacks = 1 },
        new Skill { Id = SkillId.BiggerClaw, Title = "Bigger Claw", Description = "Melee range and damage +50%.", MaxStacks = 1 },
        new Skill { Id = SkillId.DoubleProjectile, Title = "Double Projectile", Description = "Ranged attack fires two extra bullets.", MaxStacks = 1 },
        new Skill { Id = SkillId.RadiusScream, Title = "Radius Scream", Description = "Scream hits all around you, not just in front.", MaxStacks = 1 },
        new Skill { Id = SkillId.Vitality, Title = "Vitality", Description = "+2 max health.", MaxStacks = 5 },
        new Skill { Id = SkillId.Swift, Title = "Swift", Description = "+15% move speed.", MaxStacks = 5 },
    };

    private readonly Dictionary<SkillId, int> stacks = new Dictionary<SkillId, int>();

    private int level;
    private int vitalityStacks;
    private int swiftStacks;
    private bool choicePending;

    private ExpBar expBar;
    private LevelUpUI levelUpUI;
    private PlayerHealth playerHealth;
    private PlayerMoveSpeedBoost moveSpeedBoost;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        moveSpeedBoost = GetComponent<PlayerMoveSpeedBoost>();
    }

    private void Start()
    {
        expBar = FindFirstObjectByType<ExpBar>();
        levelUpUI = FindFirstObjectByType<LevelUpUI>();

        if (expBar != null)
            expBar.SetMaxExp(killsPerLevel);
        else
            Debug.LogWarning("No ExpBar was found in the scene.");
    }

    private void Update()
    {
        if (!choicePending)
        {
            int targetLevel = RunStats.TotalKills / killsPerLevel;
            if (level < targetLevel)
            {
                level++;
                OfferChoice();
            }
        }

        if (expBar != null)
            expBar.SetExp(RunStats.TotalKills % killsPerLevel);
    }

    private bool IsAvailable(SkillId id)
    {
        // Upgrade cards for an ability only show up once that ability is unlocked.
        if (id == SkillId.DoubleProjectile && !UnlockedRanged) return false;
        if (id == SkillId.RadiusScream && !UnlockedScream) return false;
        return true;
    }

    private void OfferChoice()
    {
        var pool = new List<Skill>();
        foreach (var s in AllSkills)
        {
            if (!IsAvailable(s.Id)) continue;
            int taken = stacks.TryGetValue(s.Id, out int n) ? n : 0;
            if (taken < s.MaxStacks) pool.Add(s);
        }

        if (pool.Count == 0) return; // everything maxed out

        var choices = new List<Skill>();
        while (choices.Count < 3 && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            choices.Add(pool[i]);
            pool.RemoveAt(i);
        }

        if (levelUpUI != null)
        {
            choicePending = true;
            Time.timeScale = 0f;

            var titles = new string[3];
            var descriptions = new string[3];
            for (int i = 0; i < choices.Count; i++)
            {
                titles[i] = choices[i].Title;
                descriptions[i] = choices[i].Description;
            }

            levelUpUI.Show(titles, descriptions, index => ChooseSkill(choices[index].Id));
        }
        else
        {
            ChooseSkill(choices[0].Id);
        }
    }

    private void ChooseSkill(SkillId id)
    {
        stacks[id] = (stacks.TryGetValue(id, out int n) ? n : 0) + 1;

        switch (id)
        {
            case SkillId.UnlockRanged: UnlockedRanged = true; break;
            case SkillId.UnlockScream: UnlockedScream = true; break;
            case SkillId.UnlockDash: UnlockedDash = true; break;
            case SkillId.BiggerClaw: BiggerClaw = true; break;
            case SkillId.DoubleProjectile: DoubleProjectile = true; break;
            case SkillId.RadiusScream: RadiusScream = true; break;
            case SkillId.Vitality:
                vitalityStacks++;
                if (playerHealth != null) playerHealth.IncreaseMaxHealth(2);
                break;
            case SkillId.Swift:
                swiftStacks++;
                if (moveSpeedBoost != null) moveSpeedBoost.SpeedMultiplier = 1f + 0.15f * swiftStacks;
                break;
        }

        if (choicePending)
        {
            Time.timeScale = 1f;
            choicePending = false;
        }
    }

    /// In-place "Restart Run" hook (PauseMenu) — clears every unlock/stack
    /// and level so the next run starts from claw-only again, no scene
    /// reload required.
    public void ResetProgress()
    {
        UnlockedRanged = false;
        UnlockedScream = false;
        UnlockedDash = false;
        BiggerClaw = false;
        DoubleProjectile = false;
        RadiusScream = false;

        stacks.Clear();
        level = 0;
        vitalityStacks = 0;
        swiftStacks = 0;
        choicePending = false;
        Time.timeScale = 1f;

        if (moveSpeedBoost != null) moveSpeedBoost.SpeedMultiplier = 1f;
        if (expBar != null) expBar.SetMaxExp(killsPerLevel);
    }

    /// Human-readable list of acquired skills, for the HUD's skill tracker.
    public string GetSummary()
    {
        var sb = new StringBuilder();
        if (UnlockedRanged) sb.AppendLine("Ranged Attack");
        if (UnlockedScream) sb.AppendLine("Scream");
        if (UnlockedDash) sb.AppendLine("Dash");
        if (BiggerClaw) sb.AppendLine("Bigger Claw");
        if (DoubleProjectile) sb.AppendLine("Double Projectile");
        if (RadiusScream) sb.AppendLine("Radius Scream");
        if (vitalityStacks > 0) sb.AppendLine($"Vitality x{vitalityStacks}");
        if (swiftStacks > 0) sb.AppendLine($"Swift x{swiftStacks}");

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "Claw only";
    }
}
