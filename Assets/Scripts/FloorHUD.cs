using UnityEngine;
using TMPro;

public class FloorHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text floorText;

    private void Update()
    {
        if (floorText == null) return;

        string floorLine = $"Floor {FloorManager.CurrentFloor} / {FloorManager.TotalFloors}";

        if (FloorManager.IsFinalFloor)
            floorText.text = BossState.Defeated
                ? floorLine + "\nEXIT OPEN — escape!"
                : floorLine + "\nDefeat the Tank!";
        else
            floorText.text = RunStats.FloorKills >= FloorManager.KillQuota
                ? floorLine + "\nEXIT OPEN — get to the door!"
                : floorLine + $"\nKills {RunStats.FloorKills} / {FloorManager.KillQuota}";
    }
}
