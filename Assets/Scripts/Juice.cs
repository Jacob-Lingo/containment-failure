using System.Collections;
using UnityEngine;

/// Screen shake and hitstop, the two "feel" primitives every combat hook wants
/// to reach for. Static so call sites stay one-liners (Juice.Kill(), etc.) with
/// no serialized reference to wire up; the coroutine host is a hidden
/// GameObject created on demand and kept across scene loads.
///
/// Everything here runs on unscaled time and cooperates with GameState: a
/// hitstop can't start while the game is already frozen (pause menu / level-up
/// card), and it never clears someone else's freeze.
public static class Juice
{
    /// Read by CameraFollow after it positions itself — shake has to be applied
    /// last or the follow would overwrite it every LateUpdate.
    public static Vector3 CameraOffset { get; private set; }

    private const float ShakeDuration = 0.18f;
    private const float MaxHitstop = 0.12f;

    private static Runner runner;
    private static float shakeStrength;
    private static float shakeTimer;
    private static bool hitstopActive;

    /// Player landed a killing blow — the big one: brief freeze then a shake.
    public static void Kill() { Hitstop(0.06f); Shake(0.25f); }

    /// Player got hit — shake only. No hitstop, since freezing on damage taken
    /// makes a game feel unresponsive exactly when the player wants to react.
    public static void PlayerHurt() => Shake(0.35f);

    /// Something big went off (explosion, boss, beam).
    public static void Boom() { Hitstop(0.08f); Shake(0.5f); }

    public static void Shake(float strength)
    {
        // A weaker shake shouldn't cut a stronger one short.
        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeTimer = Mathf.Max(shakeTimer, ShakeDuration);
        EnsureRunner();
    }

    public static void Hitstop(float seconds)
    {
        if (hitstopActive || GameState.IsFrozen) return;

        EnsureRunner();
        runner.StartCoroutine(HitstopRoutine(Mathf.Min(seconds, MaxHitstop)));
    }

    private static IEnumerator HitstopRoutine(float seconds)
    {
        hitstopActive = true;
        float previous = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(seconds);

        // Only hand time back if nothing else took ownership of the freeze
        // while we were stopped (pause menu opened mid-hitstop).
        if (!GameState.IsFrozen) Time.timeScale = previous;
        hitstopActive = false;
    }

    private static void EnsureRunner()
    {
        if (runner != null) return;

        var go = new GameObject("~Juice") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    /// Statics survive play sessions when Domain Reload is off (Unity 6
    /// default), so a run stopped mid-shake would leak a stale offset and a
    /// destroyed runner into the next Play. Same pattern as RunStats.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        runner = null;
        CameraOffset = Vector3.zero;
        shakeStrength = 0f;
        shakeTimer = 0f;
        hitstopActive = false;
    }

    private class Runner : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (shakeTimer <= 0f)
            {
                CameraOffset = Vector3.zero;
                return;
            }

            shakeTimer -= Time.unscaledDeltaTime;

            // Decay to zero over the shake's life so it settles instead of
            // cutting out mid-displacement.
            float falloff = Mathf.Clamp01(shakeTimer / ShakeDuration);
            float amount = shakeStrength * falloff * falloff;

            CameraOffset = new Vector3(
                Random.Range(-amount, amount),
                Random.Range(-amount, amount),
                0f);

            if (shakeTimer <= 0f) shakeStrength = 0f;
        }
    }
}
