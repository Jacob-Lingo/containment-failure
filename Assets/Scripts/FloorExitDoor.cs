using UnityEngine;

/// The exit door: touching it advances FloorManager and re-scales the
/// existing SpawnDirector in place (harder guards next time), or — on floor
/// 10 — shows the in-scene win panel. Deliberately never changes scenes:
/// floor progression and winning both stay on the same screen the player
/// was already testing in.
public class FloorExitDoor : MonoBehaviour
{
    private float nextUseTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || Time.time < nextUseTime) return;
        nextUseTime = Time.time + 1f;

        if (FloorManager.IsFinalFloor)
        {
            var winPanel = FindFirstObjectByType<WinPanelUI>();
            if (winPanel != null) winPanel.Show();
            return;
        }

        FloorManager.AdvanceFloor();

        var spawner = FindFirstObjectByType<SpawnDirector>();
        if (spawner != null) spawner.RescaleForFloor();
    }
}
