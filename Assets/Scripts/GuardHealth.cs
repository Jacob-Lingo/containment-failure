using UnityEngine;

/// Lets guards take damage from the player's attacks. Implements IDamageable
/// so any attacker can deal damage generically; the richer TakeDamage(amount,
/// source) overload is used by PlayerCombat/Bullet so kills are attributed to
/// the attack type that landed them (RunStats -> EvolutionSystem).
public class GuardHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private EnemyHealthBar healthBar;

    public int Health { get; private set; }

    private void Awake()
    {
        Health = maxHealth;
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);
    }

    /// Called by SpawnDirector right after Instantiate to make later floors'
    /// guards tougher, per FloorManager.DifficultyMultiplier.
    public void ScaleForFloor(float multiplier)
    {
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));
        Health = maxHealth;

        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, AttackType.Melee);
    }

    public void TakeDamage(int amount, AttackType source)
    {
        if (amount <= 0 || Health <= 0) return;

        Health = Mathf.Max(0, Health - amount);

        if (healthBar != null)
            healthBar.SetHealth(Health);

        if (Health <= 0)
        {
            RunStats.RegisterKill(source);
            Destroy(gameObject);
        }
    }
}
