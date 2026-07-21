using UnityEngine;

/// Attached to the Tank boss instance alongside its untouched GuardHealth —
/// no logic beyond reporting death, so boss-defeat detection needs zero
/// changes to GuardHealth's existing decoupled IDamageable death path.
public class BossMarker : MonoBehaviour
{
    private GuardHealth guardHealth;

    private void Awake() => guardHealth = GetComponent<GuardHealth>();

    // OnDestroy also fires on scene teardown (reload/restart), not just a
    // real kill — only report a defeat if the boss's health actually hit 0,
    // otherwise a scene reload while the boss is still alive would wrongly
    // unlock the exit door on the next attempt.
    private void OnDestroy()
    {
        if (guardHealth == null || guardHealth.Health <= 0)
            BossState.MarkDefeated();
    }
}
