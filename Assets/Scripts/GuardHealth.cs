using System.Collections;
using UnityEngine;

/// Lets guards take damage from the player's attacks. Implements IDamageable
/// so any attacker can deal damage generically; the richer TakeDamage(amount,
/// source) overload is used by PlayerCombat/Bullet so kills are attributed to
/// the attack type that landed them (RunStats -> EvolutionSystem).
public class GuardHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private EnemyHealthBar healthBar;

    private static readonly Color FlashColor = Color.white;
    private const float FlashDuration = 0.08f;

    public int Health { get; private set; }

    private SpriteRenderer sr;
    private Color baseColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        Health = maxHealth;
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    /// Called by SpawnDirector right after tinting a military/boss variant —
    /// without this, FlashHit()'s revert would snap back to the original
    /// untinted color cached in Awake, erasing the tint after the first hit.
    public void SetBaseColor(Color color)
    {
        baseColor = color;
        if (sr != null) sr.color = color;
    }

    /// Called by SpawnDirector for military/boss tiers, before ScaleForFloor,
    /// to give the tier its own base HP that the floor multiplier then scales
    /// proportionally — distinct from ScaleForFloor's multiply-in-place.
    public void SetBaseMaxHealth(int hp)
    {
        maxHealth = Mathf.Max(1, hp);
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

        DamageNumber.Spawn(transform.position, amount);

        if (sr != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashHit());
        }

        if (Health <= 0)
        {
            RunStats.RegisterKill(source);
            if (Random.value < 0.15f) ExpOrb.Spawn(transform.position);
            Destroy(gameObject);
        }
    }

    private IEnumerator FlashHit()
    {
        sr.color = FlashColor;
        yield return new WaitForSeconds(FlashDuration);
        sr.color = baseColor;
    }
}
