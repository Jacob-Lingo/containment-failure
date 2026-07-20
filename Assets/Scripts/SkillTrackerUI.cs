using UnityEngine;
using TMPro;

/// Shows the run's acquired skills as a simple list — the "skill slots" the
/// player has filled so far. Polls EvolutionSystem since skills change
/// infrequently (only on level-up) and this avoids needing an event hookup.
public class SkillTrackerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text skillListText;

    private EvolutionSystem evolution;

    private void Start()
    {
        evolution = FindFirstObjectByType<EvolutionSystem>();
    }

    private void Update()
    {
        if (evolution == null || skillListText == null) return;
        skillListText.text = evolution.GetSummary();
    }
}
