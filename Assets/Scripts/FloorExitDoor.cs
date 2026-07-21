using UnityEngine;

/// The exit door: on the final floor, loads the escape/win scene via
/// SceneTransition — but only once the Tank boss is dead (BossState.Defeated),
/// otherwise it's a locked no-op. Floors 1-9 no longer use the door at all —
/// SpawnDirector auto-advances the floor once its kill quota is met
/// (FloorManager.KillQuota), so touching the door before floor 10 does nothing.
public class FloorExitDoor : MonoBehaviour
{
    [SerializeField] private string escapeSceneName = "Dev_FloorWin";

    private float nextUseTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || Time.time < nextUseTime) return;
        nextUseTime = Time.time + 1f;

        if (FloorManager.IsFinalFloor && BossState.Defeated)
            SceneTransition.LoadScene(escapeSceneName);
    }
}
