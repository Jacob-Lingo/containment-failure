using UnityEngine;

/// The exit door — the player's escape route on every floor, per the GDD's
/// fight-to-grow vs. escape-in-time loop. On floors 1..N-1 it unlocks once
/// the floor's kill quota is met (FloorManager.KillQuota) and touching it
/// advances the floor (which also refills the floor timer). On the final
/// floor it unlocks only when the Tank is dead (BossState.Defeated) and
/// loads the escape/win scene via SceneTransition. Locked = silent no-op.
public class FloorExitDoor : MonoBehaviour
{
    [SerializeField] private string escapeSceneName = "Dev_FloorWin";

    private float nextUseTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || Time.time < nextUseTime) return;
        nextUseTime = Time.time + 1f;

        if (FloorManager.IsFinalFloor)
        {
            if (BossState.Defeated)
                SceneTransition.LoadScene(escapeSceneName);
            return;
        }

        // Quota met -> this door is open; walking into it is the escape.
        if (RunStats.FloorKills >= FloorManager.KillQuota)
        {
            FloorManager.AdvanceFloor();
            RunStats.ResetFloorKills();

            var spawner = FindFirstObjectByType<SpawnDirector>();
            if (spawner != null) spawner.RescaleForFloor();
        }
    }
}
