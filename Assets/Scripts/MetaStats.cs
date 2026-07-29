using UnityEngine;

/// Lifetime player statistics, kept across runs alongside MetaProgression.
///
/// Three things are recorded, and the interesting one is the pair:
///   * `Seen`   — how many times a card has been *offered*
///   * `Picked` — how many times it was taken when offered
/// Pick rate (Picked/Seen) is the only number here that says something the
/// player doesn't already know: it's their revealed preference, not a raw
/// total, so a card offered twice and taken twice reads as 100% rather than
/// being buried under a card they've seen forty times.
///
/// `Used` counts actual activations, which is a different question again — a
/// card can be picked often and then barely cast.
///
/// PlayerPrefs-backed for the same reason as MetaProgression: this is a few
/// dozen ints and needs no schema.
public static class MetaStats
{
    private const string SeenKey = "stats.seen.";
    private const string PickedKey = "stats.picked.";
    private const string UsedKey = "stats.used.";
    private const string FormKey = "stats.form.";
    private const string EnemyKey = "stats.enemy.";

    public static int Seen(SkillId id) => PlayerPrefs.GetInt(SeenKey + id, 0);
    public static int Picked(SkillId id) => PlayerPrefs.GetInt(PickedKey + id, 0);
    public static int Used(SkillId id) => PlayerPrefs.GetInt(UsedKey + id, 0);

    /// 0-1. Zero when never offered, so callers can show "-" instead of a
    /// misleading 0%.
    public static float PickRate(SkillId id)
    {
        int seen = Seen(id);
        return seen <= 0 ? 0f : Mathf.Clamp01(Picked(id) / (float)seen);
    }

    public static void RecordOffered(SkillId id) => Bump(SeenKey + id);
    public static void RecordPicked(SkillId id) => Bump(PickedKey + id);
    public static void RecordUsed(SkillId id) => Bump(UsedKey + id);

    public static bool HasUnlockedForm(string form) => PlayerPrefs.GetInt(FormKey + form, 0) > 0;

    public static int EnemiesSlain(string enemy) => PlayerPrefs.GetInt(EnemyKey + enemy, 0);
    public static bool HasSeenEnemy(string enemy) => EnemiesSlain(enemy) > 0;

    /// Bumped when a creature spawns, so the bestiary fills in as you meet
    /// things rather than only when you manage to kill them.
    public static void RecordEnemy(string enemy) => Bump(EnemyKey + enemy);

    public static void RecordForm(string form)
    {
        if (HasUnlockedForm(form)) return;
        PlayerPrefs.SetInt(FormKey + form, 1);
        PlayerPrefs.Save();
    }

    /// Writes are frequent (every card offer), so batch the flush: PlayerPrefs
    /// only actually needs saving when the session might end, and Unity writes
    /// on quit anyway. Saved here on picks/forms only, which are rare.
    private static void Bump(string key) => PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);

    public static void ResetAll()
    {
        foreach (SkillId id in System.Enum.GetValues(typeof(SkillId)))
        {
            PlayerPrefs.DeleteKey(SeenKey + id);
            PlayerPrefs.DeleteKey(PickedKey + id);
            PlayerPrefs.DeleteKey(UsedKey + id);
        }

        foreach (string form in MonsterForm.AllForms)
            PlayerPrefs.DeleteKey(FormKey + form);

        foreach (string enemy in Bestiary.AllEnemies)
            PlayerPrefs.DeleteKey(EnemyKey + enemy);

        PlayerPrefs.Save();
    }
}
