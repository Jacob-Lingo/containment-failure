using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Death")]
    [SerializeField] private string loseScene = "LoseScreen";
    [SerializeField] private float loseDelay = 1f;

    public int Health { get; private set; }

    public bool Invulnerable { get; set; }

    [Header("Thorns retaliation")]
    [SerializeField] private float thornsRadius = 2.5f;
    [SerializeField] private LayerMask guardLayer = ~0;

    private HealthBar healthBar;
    private EvolutionSystem evolution;
    private bool isDead;
    private int startingMaxHealth;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Color baseColor;
    private Coroutine flashRoutine;

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
        if (healthBar != null) healthBar.SetHealth(Health);
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0) return;
        maxHealth += amount;
        Health = Mathf.Min(maxHealth, Health + amount);
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(Health);
        }
    }

    public void ResetToStartingHealth()
    {
        maxHealth = startingMaxHealth;
        Health = maxHealth;
        isDead = false;
        if (TryGetComponent<PlayerController>(out var controller)) controller.enabled = true;
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = true;
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.enabled = true;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(Health);
        }
    }

    private void Start()
    {
        startingMaxHealth = maxHealth;
        Health = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (sr != null) baseColor = sr.color;
        evolution = GetComponent<EvolutionSystem>();
        healthBar = FindFirstObjectByType<HealthBar>();
        if (healthBar != null) healthBar.SetMaxHealth(maxHealth);
        else Debug.LogWarning("No HealthBar was found in the scene.");
    }

    public void TakeDamage(int amount, Vector2? knockbackDirection = null)
    {
        if (isDead || amount <= 0 || Invulnerable) return;

        if (evolution != null && evolution.ArmorFlat > 0)
            amount = Mathf.Max(1, amount - evolution.ArmorFlat);

        Health -= amount;
        Health = Mathf.Clamp(Health, 0, maxHealth);

        if (healthBar != null) healthBar.SetHealth(Health);

        Debug.Log("Player Health: " + Health);

        Sfx.PlayRandom("player_hurt", 2, transform.position);
        HitFlashFx.Spawn(transform.position, new Color(1f, 0.2f, 0.2f, 0.85f), 0.4f);

        if (sr != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashHit());
        }

        if (knockbackDirection.HasValue && rb != null)
        {
            StartCoroutine(ApplyKnockback(knockbackDirection.Value));
        }

        if (Health <= 0)
        {
            if (evolution != null && evolution.TryConsumeSecondWind())
            {
                Health = 1;
                if (healthBar != null) healthBar.SetHealth(Health);
                HitFlashFx.Spawn(transform.position, new Color(0.3f, 1f, 1f, 0.9f), 0.6f);
                StartCoroutine(SecondWindGrace());
            }
            else
            {
                isDead = true;
                StartCoroutine(Die());
            }
        }
    }

    public void TakeDamage(int amount, AttackType attackType)
    {
        TakeDamage(amount);
        if (attackType == AttackType.Melee)
        {
            TriggerThorns();
        }
    }

    private IEnumerator ApplyKnockback(Vector2 direction)
    {
        if (rb == null) yield break;
        
        float timer = 0;
        while (timer < knockbackDuration)
        {
            rb.AddForce(direction.normalized * knockbackForce, ForceMode2D.Impulse);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator SecondWindGrace()
    {
        Invulnerable = true;
        yield return new WaitForSeconds(1.5f);
        Invulnerable = false;
    }

    private void TriggerThorns()
    {
        if (evolution == null || evolution.ThornsDamage <= 0) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, thornsRadius, guardLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<GuardHealth>(out var guard))
                guard.TakeDamage(evolution.ThornsDamage, AttackType.Melee);
        }
    }

    private IEnumerator FlashHit()
    {
        float endTime = Time.time + 1f;
        while (Time.time < endTime)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (sr == null) yield break;
            sr.color = baseColor;
            yield return new WaitForSeconds(0.1f);
        }
        if (sr != null)
        {
            sr.color = baseColor;
        }
    }

    private IEnumerator Die()
    {
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;
        foreach (Collider2D playerCollider in GetComponentsInChildren<Collider2D>()) playerCollider.enabled = false;
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.enabled = false;
        GameManager.LastLevelIndex = SceneManager.GetActiveScene().buildIndex;
        yield return new WaitForSeconds(loseDelay);
        SceneTransition.LoadScene(loseScene);
    }
}