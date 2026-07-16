using UnityEngine;
using TMPro;

public class FloorHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text floorText;

    private void Update()
    {
        if (floorText != null)
            floorText.text = $"Floor {FloorManager.CurrentFloor} / {FloorManager.TotalFloors}";
    }
}
