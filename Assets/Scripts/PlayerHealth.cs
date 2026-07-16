using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    [Header("Death")]
    [SerializeField] private string loseScene = "LoseScreen";
    [SerializeField] private float loseDelay = 1f;

    public int Health { get; private set; }

    private HealthBar healthBar;
    private bool isDead;
    private int startingMaxHealth;

    /// Additive hook for the evolution system's "Vitality" passive — raises the
    /// cap and heals by the same amount so picking it always feels like a gain.
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

    /// Additive hook for an in-place "Restart Run" (PauseMenu) — undoes any
    /// Vitality stacking and re-enables everything Die() turned off, without
    /// requiring a scene reload.
    public void ResetToStartingHealth()
    {
        maxHealth = startingMaxHealth;
        Health = maxHealth;
        isDead = false;

        if (TryGetComponent<PlayerController>(out var controller))
            controller.enabled = true;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = true;

        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
            sprite.enabled = true;

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

        healthBar = FindFirstObjectByType<HealthBar>();

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }
        else
        {
            Debug.LogWarning("No HealthBar was found in the scene.");
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        Health -= amount;
        Health = Mathf.Clamp(Health, 0, maxHealth);

        if (healthBar != null)
        {
            healthBar.SetHealth(Health);
        }

        Debug.Log("Player Health: " + Health);

        if (Health <= 0)
        {
            isDead = true;
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        PlayerController controller = GetComponent<PlayerController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        foreach (Collider2D playerCollider in GetComponentsInChildren<Collider2D>())
        {
            playerCollider.enabled = false;
        }

        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.enabled = false;
        }

        // Save the level the player died on.
        GameManager.LastLevelIndex =
            SceneManager.GetActiveScene().buildIndex;

        // Wait before opening the lose screen.
        yield return new WaitForSeconds(loseDelay);

        SceneManager.LoadScene(loseScene);
    }
}