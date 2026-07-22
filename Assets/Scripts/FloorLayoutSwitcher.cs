using UnityEngine;

/// Enables exactly one obstacle-layout parent per floor, so the three
/// "floors" read as distinct spaces without a scene reload (floor advance
/// is in-place by design — see FloorExitDoor). Level design workflow:
/// build each layout as a child of a "FloorLayouts" object in Master
/// (Layout_Floor1, Layout_Floor2, Layout_Floor3), drag them into the array
/// in order, and this keeps the active one in sync with FloorManager.
/// Polls like FloorHUD does — floor changes are rare, and polling avoids
/// event wiring across scripts with different owners.
public class FloorLayoutSwitcher : MonoBehaviour
{
    [Tooltip("Index 0 = floor 1. Missing entries are skipped safely.")]
    [SerializeField] private GameObject[] floorLayouts;

    private int lastAppliedFloor = -1;

    private void Update()
    {
        if (FloorManager.CurrentFloor == lastAppliedFloor) return;
        lastAppliedFloor = FloorManager.CurrentFloor;

        for (int i = 0; i < floorLayouts.Length; i++)
        {
            if (floorLayouts[i] != null)
                floorLayouts[i].SetActive(i == lastAppliedFloor - 1);
        }
    }
}
