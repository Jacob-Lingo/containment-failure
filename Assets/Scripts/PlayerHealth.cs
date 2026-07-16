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

    private void Start()
    {
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