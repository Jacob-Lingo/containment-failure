using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private int damage = 10;

    // Use OnTriggerEnter2D for 2D physics
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Try to get the IDamageable component and deal damage
            other.GetComponent<IDamageable>()?.TakeDamage(damage);
        }
    }
}