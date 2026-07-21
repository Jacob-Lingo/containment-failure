using UnityEngine;
using TMPro;

public class FloorHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text floorText;

    private void Update()
    {
        if (floorText == null) return;

        floorText.text = FloorManager.IsFinalFloor
            ? $"Floor {FloorManager.CurrentFloor} / {FloorManager.TotalFloors}\nDefeat the Tank!"
            : $"Floor {FloorManager.CurrentFloor} / {FloorManager.TotalFloors}\nKills {RunStats.FloorKills} / {FloorManager.KillQuota}";
    }
}
