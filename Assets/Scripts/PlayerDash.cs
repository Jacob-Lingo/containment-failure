using UnityEngine;

/// Dash ability, unlocked via the evolution "Dash" card — Left Shift bursts
/// the player forward for a short window once unlocked. Reads its own input
/// and drives the Rigidbody2D directly so it never has to touch
/// PlayerController.cs; runs after it (DefaultExecutionOrder) so the dash
/// velocity isn't immediately stomped by PlayerController's own FixedUpdate.
///
/// Two mutually exclusive evolutions change what the dash does (see
/// EvolutionSystem): Phase Dash grants i-frames + a translucent sprite;
/// Lunge Dash sweeps the dash path, knocking back and damaging guards while
/// dealing the player a small amount of capped self-damage in return.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Phase Dash")]
    [SerializeField] private float phaseDashAlpha = 0.45f;

    [Header("Lunge Dash")]
    [SerializeField] private float lungeRadius = 1f;
    [SerializeField] private int lungeDamage = 1;
    [SerializeField] private int lungeSelfDamagePerHit = 1;
    [SerializeField] private float lungeKnockbackForce = 8f;
    [SerializeField] private float lungeKnockbackDuration = 0.25f;
    [SerializeField] private LayerMask guardLayer = ~0;

    private Rigidbody2D rb;
    private EvolutionSystem evolution;
    private PlayerHealth playerHealth;
    private PlayerCombat combat;
    private SpriteRenderer sr;
    private Color baseSpriteColor;

    private Vector2 lastMoveDirection = Vector2.right;
    private Vector2 dashDirection;
    private float dashEndTime;
    private float nextDashTime;
    private float lastDashCooldownUsed = 1f;

    public bool IsDashing { get; private set; }

    /// 0 = ready, 1 = just used. Read by AbilityBarUI for the Shift slot.
    public float DashCooldownFraction => Mathf.Clamp01((nextDashTime - Time.time) / Mathf.Max(0.0001f, lastDashCooldownUsed));

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        evolution = GetComponent<EvolutionSystem>();
        playerHealth = GetComponent<PlayerHealth>();
        combat = GetComponent<PlayerCombat>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseSpriteColor = sr.color;
    }

    private void Update()
    {
        if (GameState.IsFrozen) return; // no dash input while paused/choosing

        // Berserk suspends all manual control, including dashing.
        if (combat != null && combat.IsBerserking) return;

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(x, y);
        if (input.sqrMagnitude > 0.0001f)
            lastMoveDirection = input.normalized;

        bool unlocked = evolution == null || evolution.UnlockedDash;

        if (unlocked && Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime)
        {
            IsDashing = true;
            dashDirection = lastMoveDirection;
            lastVenomPosition = rb.position - dashDirection * VenomSpacing;
            dashEndTime = Time.time + dashDuration;

            float cooldownReduction = evolution != null ? evolution.DashCooldownReduction : 0f;
            // Wisp line dashes far more often; that speed IS its defence.
            float formScale = evolution != null ? evolution.Form.DashCooldownScale : 1f;
            lastDashCooldownUsed = dashCooldown * (1f - Mathf.Clamp01(cooldownReduction)) * formScale;
            nextDashTime = Time.time + lastDashCooldownUsed;

            // Shade line gets i-frames from the form itself, not the card.
            if (evolution != null && evolution.Form.DashInvuln > 0f)
                formInvulnUntil = Time.time + evolution.Form.DashInvuln;

            if (evolution != null && evolution.DashPhase) StartPhase();
            if (evolution != null && evolution.DashLunge) PerformLunge();
        }

        if (IsDashing && Time.time >= dashEndTime)
        {
            IsDashing = false;
            EndPhase();
        }

        // Form i-frames outlast the dash itself, so they're released on their
        // own clock rather than by EndPhase.
        if (formInvulnUntil > 0f && Time.time >= formInvulnUntil)
        {
            formInvulnUntil = 0f;
            if (playerHealth != null && (evolution == null || !evolution.DashPhase || !IsDashing))
                playerHealth.Invulnerable = false;
        }
        else if (formInvulnUntil > 0f && playerHealth != null)
        {
            playerHealth.Invulnerable = true;
        }
    }

    private void FixedUpdate()
    {
        if (!IsDashing) return;
        rb.linearVelocity = dashDirection * dashSpeed;

        DropVenom();
    }

    /// The venom line's whole identity: dashing paints the ground behind you.
    /// Dropped on a distance interval rather than a time one, so the trail is
    /// evenly spaced whatever the form's speed multiplier does to the dash.
    private void DropVenom()
    {
        if (evolution == null) return;

        var form = evolution.Form;
        if (form.VenomDuration <= 0f) return;

        if (Vector2.Distance(rb.position, lastVenomPosition) < VenomSpacing) return;
        lastVenomPosition = rb.position;

        HazardZone.Spawn(rb.position, VenomRadius, form.VenomDuration, 1,
                         new Color(0.4f, 0.9f, 0.2f, 0.55f),
                         form.VenomSlows ? 0.45f : 1f,
                         form.VenomSpreads);
    }

    private const float VenomSpacing = 0.7f;
    private const float VenomRadius = 1.1f;
    private Vector2 lastVenomPosition;
    private float formInvulnUntil;

    private void StartPhase()
    {
        if (playerHealth != null) playerHealth.Invulnerable = true;
        if (sr != null) sr.color = new Color(baseSpriteColor.r, baseSpriteColor.g, baseSpriteColor.b, phaseDashAlpha);
    }

    private void EndPhase()
    {
        if (playerHealth != null) playerHealth.Invulnerable = false;
        if (sr != null) sr.color = baseSpriteColor;
    }

    private void PerformLunge()
    {
        float travelDistance = dashSpeed * dashDuration;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, lungeRadius, dashDirection, travelDistance, guardLayer);

        int startingHealth = playerHealth != null ? playerHealth.Health : 0;
        int selfDamageCap = startingHealth / 2;
        int selfDamageTotal = 0;

        foreach (var hit in hits)
        {
            if (hit.collider == null || !hit.collider.TryGetComponent<GuardHealth>(out var guard)) continue;

            guard.TakeDamage(lungeDamage, AttackType.Melee);

            if (hit.collider.TryGetComponent<GuardMotor>(out var motor))
            {
                Vector2 away = ((Vector2)hit.collider.transform.position - (Vector2)transform.position).normalized;
                motor.ApplyKnockback(away * lungeKnockbackForce, lungeKnockbackDuration);
            }

            selfDamageTotal += lungeSelfDamagePerHit;
        }

        if (selfDamageTotal > 0 && playerHealth != null)
        {
            int applied = Mathf.Min(selfDamageTotal, selfDamageCap);
            if (applied > 0) playerHealth.TakeDamage(applied);
        }
    }
}
