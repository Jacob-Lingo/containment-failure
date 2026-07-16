using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// The 3-card level-up panel from the GDD: pauses the game (Time.timeScale),
/// shows up to 3 choices with title/description, and reports back which
/// index was clicked. Card content is set at runtime by EvolutionSystem
/// since which 3 skills are on offer is randomized each level-up.
public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardTitles;
    [SerializeField] private TMP_Text[] cardDescriptions;

    private Action<int> onChosen;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show(string[] titles, string[] descriptions, Action<int> chosen)
    {
        onChosen = chosen;

        for (int i = 0; i < cardButtons.Length; i++)
        {
            bool active = i < titles.Length && titles[i] != null;
            cardButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            cardTitles[i].text = titles[i];
            cardDescriptions[i].text = descriptions[i];

            int index = i;
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => Choose(index));
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void Choose(int index)
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        onChosen?.Invoke(index);
    }
}
