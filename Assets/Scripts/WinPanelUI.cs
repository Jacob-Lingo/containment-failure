using UnityEngine;

/// In-scene "you escaped" overlay shown on reaching the final floor —
/// deliberately doesn't change scenes or stop the game, so mobs keep
/// spawning underneath it and testing can continue uninterrupted. Continue
/// just hides the panel again.
public class WinPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Continue()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
