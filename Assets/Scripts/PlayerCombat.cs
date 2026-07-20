using UnityEngine;

/// Player attacks: left click claws in melee range, right click fires a
/// bullet (ammo-gated, refilled by melee kills per the GDD's ammo economy),
/// space lets out a scream AOE. Reads EvolutionSystem for passive upgrades
/// but never assumes it's present, so this still works standalone.
public class PlayerCombat : MonoBehaviour
{
    [Header("Melee (Claw)")]
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private float meleeArc = 90f;
    [SerializeField] private int meleeDamage = 2;
    [SerializeField] private float meleeCooldown = 0.4f;

    [Header("Ranged (Bullet)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int rangedDamage = 1;
    [SerializeField] private float rangedCooldown = 0.5f;
    [SerializeField] private int maxAmmo = 3;

    [Header("Scream")]
    [SerializeField] private float screamRange = 3f;
    [SerializeField] private float screamArc = 100f;
    [SerializeField] private int screamDamage = 1;
    [SerializeField] private float screamCooldown = 3f;

    [SerializeField] private LayerMask guardLayer = ~0;

    private EvolutionSystem evolution;
    private int currentAmmo;
    private float nextMeleeTime, nextRangedTime, nextScreamTime;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    private void Awake()
    {
        evolution = GetComponent<EvolutionSystem>();
        currentAmmo = maxAmmo;
    }

    /// In-place "Restart Run" hook (PauseMenu) — refills ammo and clears
    /// cooldowns left over from the previous run.
    public void ResetCombatState()
    {
        currentAmmo = maxAmmo;
        nextMeleeTime = 0f;
        nextRangedTime = 0f;
        nextScreamTime = 0f;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryMelee();
        if (Input.GetMouseButtonDown(1)) TryRanged();
        if (Input.GetKeyDown(KeyCode.Space)) TryScream();
    }

    private Vector2 AimDirection()
    {
        if (Camera.main == null) return Vector2.right;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    private void TryMelee()
    {
        if (Time.time < nextMeleeTime) return;
        nextMeleeTime = Time.time + meleeCooldown;
        RunStats.RegisterAttackUsed(AttackType.Melee);

        bool biggerClaw = evolution != null && evolution.BiggerClaw;
        float range = biggerClaw ? meleeRange * 1.5f : meleeRange;
        int damage = biggerClaw ? meleeDamage * 2 : meleeDamage;

        Vector2 aim = AimDirection();
        SpawnClawEffect(aim, range);

        bool killedOne = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, guardLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;

            Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            if (Vector2.Angle(aim, toTarget) > meleeArc * 0.5f) continue;

            guard.TakeDamage(damage, AttackType.Melee);
            if (guard.Health <= 0) killedOne = true;
        }

        if (killedOne) RefillAmmo(1);
    }

    private void SpawnClawEffect(Vector2 aim, float range)
    {
        GameObject fx = new GameObject("ClawFX");
        fx.transform.position = (Vector2)transform.position + aim * (range * 0.6f);
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        fx.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Combat/claw_slash");
        sr.sortingOrder = 11;
        fx.transform.localScale = Vector3.one * 0.6f;

        Destroy(fx, 0.15f);
    }

    private void TryRanged()
    {
        if (evolution == null || !evolution.UnlockedRanged) return;
        if (Time.time < nextRangedTime || currentAmmo <= 0 || bulletPrefab == null) return;
        nextRangedTime = Time.time + rangedCooldown;
        currentAmmo--;
        RunStats.RegisterAttackUsed(AttackType.Ranged);

        Vector2 aim = AimDirection();
        bool doubleShot = evolution != null && evolution.DoubleProjectile;

        SpawnBullet(aim);
        if (doubleShot)
        {
            const float spread = 12f;
            SpawnBullet(Quaternion.Euler(0, 0, spread) * aim);
            SpawnBullet(Quaternion.Euler(0, 0, -spread) * aim);
        }
    }

    private void SpawnBullet(Vector2 dir)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        GameObject go = Instantiate(bulletPrefab, origin, Quaternion.identity);
        if (go.TryGetComponent<Bullet>(out var bullet))
            bullet.Init(dir, rangedDamage, true);
    }

    private void RefillAmmo(int amount)
    {
        currentAmmo = Mathf.Min(maxAmmo, currentAmmo + amount);
    }

    private void TryScream()
    {
        if (evolution == null || !evolution.UnlockedScream) return;
        if (Time.time < nextScreamTime) return;
        nextScreamTime = Time.time + screamCooldown;
        RunStats.RegisterAttackUsed(AttackType.Scream);

        bool radial = evolution != null && evolution.RadiusScream;
        float range = radial ? screamRange * 1.3f : screamRange;

        Vector2 aim = AimDirection();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, guardLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<GuardHealth>(out var guard)) continue;

            if (!radial)
            {
                Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
                if (Vector2.Angle(aim, toTarget) > screamArc * 0.5f) continue;
            }

            guard.TakeDamage(screamDamage, AttackType.Scream);
        }
    }
}
