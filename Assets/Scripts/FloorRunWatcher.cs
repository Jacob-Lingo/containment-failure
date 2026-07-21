using UnityEngine;

/// Resets run-scoped static state (FloorManager, RunStats) once death is
/// observed. Polls PlayerHealth.Health rather than an event since
/// PlayerHealth (owned by Noah) doesn't expose a death event to hook.
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

        if (playerHealth.Health <= 0)
        {
            resetQueued = true;
            FloorManager.ResetRun();
            RunStats.ResetRun();
            BossState.Reset();
        }
    }
}
