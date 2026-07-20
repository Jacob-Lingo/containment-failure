using UnityEngine;
using UnityEngine.UI;

public class ExpBar : MonoBehaviour
{
    [SerializeField] private Slider expSlider;

    public void SetMaxExp(int maxExp)
    {
        expSlider.maxValue = maxExp;
        expSlider.value = 0;
    }

    public void SetExp(int exp)
    {
        expSlider.value = exp;
    }
}
