using UnityEngine;
using TMPro;

public class FloorHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text floorText;

    private void Update()
    {
        if (floorText == null) return;

        string floorLine = GameMode.IsEndless
            ? $"Depth {FloorManager.CurrentFloor}   (best {GameMode.BestEndlessFloor})"
            : $"Depth {FloorManager.CurrentFloor} / {FloorManager.TotalFloors}";

        if (FloorManager.IsFinalFloor)
        {
            if (!BossState.Defeated)
                floorText.text = floorLine + "\nSlay the Warden!";
            else if (MetaProgression.HasIronKey)
                floorText.text = floorLine + "\nTHE GATE IS OPEN — flee!";
            else
                // Otherwise the player stands at an unresponsive door with no
                // idea why. Name the missing item and where to get it.
                floorText.text = floorLine + "\nThe gate is locked — you need the Iron Key from the Shop.";
        }
        else
        {
            floorText.text = RunStats.FloorKills >= FloorManager.KillQuota
                ? floorLine + "\nTHE GATE IS OPEN — reach the stairs!"
                : floorLine + $"\nSlain {RunStats.FloorKills} / {FloorManager.KillQuota}";
        }
    }
}
