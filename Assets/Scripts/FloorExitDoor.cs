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
            // Beating the Warden isn't enough — the vault gate needs the Iron
            // Key, bought in the shop with gold banked across runs. This is the
            // deliberate "you must come back better equipped" gate.
            if (BossState.Defeated && MetaProgression.HasIronKey)
                SceneTransition.LoadScene(escapeSceneName);
            return;
        }

        // Quota met -> this door is open; walking into it is the escape.
        if (RunStats.FloorKills >= FloorManager.KillQuota)
        {
            // Advance behind the wipe: the rescale and the next floor's
            // spawns land while the screen is covered, so the change reads as
            // "descended a floor" rather than as enemies popping in.
            SceneTransition.Interstitial($"DEPTH {FloorManager.CurrentFloor + 1}", () =>
            {
                FloorManager.AdvanceFloor();
                RunStats.ResetFloorKills();

                var spawner = FindFirstObjectByType<SpawnDirector>();
                if (spawner != null) spawner.RescaleForFloor();
            });
        }
    }
}
