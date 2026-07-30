using UnityEngine;

[RequireComponent(typeof(GuardPerception), typeof(GuardMotor))]
public class GuardBrain : MonoBehaviour
{
    private enum State { Idle, Chase, Attack }

    [Header("Attack")]
    [SerializeField] private float attackEnterRange = 1.2f;
    [SerializeField] private float attackExitRange = 1.8f;   // hysteresis
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private float attackOffset = 1.0f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.12f;

    private State state = State.Idle;
    private GuardPerception perception;
    private GuardMotor motor;
    private Animator animator;
    private Transform target;
    private float nextAttackTime;

    private void Awake()
    {
        perception = GetComponent<GuardPerception>();
        motor = GetComponent<GuardMotor>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        perception.TargetSpotted += HandleTargetSpotted;
        perception.TargetLost += HandleTargetLost;
    }

    private void OnDisable()
    {
        perception.TargetSpotted -= HandleTargetSpotted;
        perception.TargetLost -= HandleTargetLost;
    }

    public void SetTarget(Transform target)
    {
        perception.SetTarget(target);
    }

    public void SetAttackProfile(int damage, float cooldown)
    {
        attackDamage = damage;
        attackCooldown = cooldown;
    }

    private void HandleTargetSpotted(Transform t)
    {
        target = t;
        TransitionTo(State.Chase);
    }

    private void HandleTargetLost()
    {
        target = null;
        motor.Stop();
        TransitionTo(State.Idle);
    }

    private void Update()
    {
        switch (state)
        {
            case State.Chase:  TickChase();  break;
            case State.Attack: TickAttack(); break;
            case State.Idle:   break;
        }
    }

    private void TickChase()
    {
        if (target == null) { HandleTargetLost(); return; }

        motor.Seek(target.position);

        if (Vector2.Distance(transform.position, target.position) <= attackEnterRange)
            TransitionTo(State.Attack);
    }

    private void TickAttack()
    {
        if (target == null) { HandleTargetLost(); return; }

        motor.Stop();

        if (Vector2.Distance(transform.position, target.position) >= attackExitRange)
        {
            TransitionTo(State.Chase);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            
            animator.SetTrigger("Attack");
            
            Vector2 attackDir = (target.position - transform.position).normalized;

            if (slashPrefab != null)
            {
                Vector3 spawnPos = transform.position + (Vector3)(attackDir * attackOffset);
                float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
                GameObject slashInstance = Instantiate(slashPrefab, spawnPos, Quaternion.Euler(0, 0, angle));

                slashInstance.transform.localScale *= 0.8f;

                var sr = slashInstance.GetComponent<SpriteRenderer>() ?? slashInstance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.red;
                    sr.sortingOrder = 20;
                }
            }
 
            Sfx.PlayRandom("guard_baton_hit", 3, target.position);
            HitFlashFx.Spawn(target.position, new Color(1f, 1f, 1f, 0.85f), 0.35f);

            if (target.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                playerHealth.TakeDamage(attackDamage, AttackType.Melee);

                Vector2 pushDir = (target.position - transform.position).normalized;
                if (target.TryGetComponent<PlayerController>(out var playerController))
                {
                    playerController.ApplyKnockback(pushDir * knockbackForce, knockbackDuration);
                }
                else if (target.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }

    private void TransitionTo(State next)
    {
        if (state == next) return;
        state = next;
    }
}