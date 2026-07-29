using UnityEngine;

/// Resets run-scoped static state (FloorManager, RunStats) once death is
/// observed. Polls PlayerHealth.IsDead rather than just health, since health
/// may be 0 for a frame before PlayerHealth has initialized.
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

        if (playerHealth.IsDead)
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