using UnityEngine;

/// Resets run-scoped static state (FloorManager, RunStats) once death is
/// observed. Polls PlayerHealth.IsDead rather than just health, since health
/// may be 0 for a frame before PlayerHealth has initialized.
public class FloorRunWatcher : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private bool resetQueued;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (resetQueued || playerHealth == null) return;

        if (playerHealth.IsDead)
        {
            resetQueued = true;
            FloorManager.ResetRun();
            RunStats.ResetRun();
            BossState.Reset();
        }
    }
}