using UnityEngine;
using UnityEngine.UI;

/// World-space health bar that floats above a guard's head. Guards rotate to
/// face their target (GuardMotor.UpdateFacing), which would otherwise tip the
/// bar sideways since it's a child of the rotating body — resetting world
/// rotation every frame keeps it level regardless of parent orientation.
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    public void SetMaxHealth(int maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
    }

    public void SetHealth(int health)
    {
        healthSlider.value = health;
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
