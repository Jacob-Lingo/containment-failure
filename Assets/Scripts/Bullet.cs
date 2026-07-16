using UnityEngine;

/// Shared projectile for both the player's ranged attack and ranged guards.
/// isPlayerBullet decides who it can hurt, since guards aren't tagged
/// distinctly from each other — cheaper and more robust than layer/tag
/// juggling for a two-faction game.
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f;

    private int damage;
    private bool isPlayerBullet;
    private Rigidbody2D rb;
    private Vector2 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = Resources.Load<Sprite>(isPlayerBullet ? "Combat/bullet_player" : "Combat/bullet_guard");
    }

    public void Init(Vector2 dir, int dmg, bool playerOwned)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        damage = dmg;
        isPlayerBullet = playerOwned;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = Resources.Load<Sprite>(isPlayerBullet ? "Combat/bullet_player" : "Combat/bullet_guard");

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerBullet)
        {
            if (other.CompareTag("Player")) return;

            if (other.TryGetComponent<GuardHealth>(out var guard))
            {
                guard.TakeDamage(damage, AttackType.Ranged);
                Destroy(gameObject);
            }
        }
        else
        {
            if (other.TryGetComponent<GuardHealth>(out _)) return; // ignore other guards

            if (other.TryGetComponent<PlayerHealth>(out var player))
            {
                player.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
