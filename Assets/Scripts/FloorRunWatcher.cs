using UnityEngine;

/// Resets run-scoped static state (FloorManager, RunStats) once death is
/// observed. Polls PlayerHealth.Health rather than an event since
/// PlayerHealth (owned by Noah) doesn't expose a death event to hook.
public class FloorRunWatcher : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private EvolutionSystem evolution;
    private bool resetQueued;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        evolution = GetComponent<EvolutionSystem>();
    }

    private void Update()
    {
        if (resetQueued || playerHealth == null) return;

        if (playerHealth.Health <= 0)
        {
            resetQueued = true;

            // Before the resets below wipe them — this is the only point where
            // the dead run's floor/kills/build are all still intact.
            RunSummary.Capture("SLAIN", evolution != null ? evolution.GetSummary() : "Claws only");

            FloorManager.ResetRun();
            RunStats.ResetRun();
            BossState.Reset();
        }
    }
}
