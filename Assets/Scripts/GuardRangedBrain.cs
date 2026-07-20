using UnityEngine;

/// Ranged-guard counterpart to GuardBrain: same Idle/Chase/Attack shape,
/// reusing GuardPerception/GuardMotor unchanged, but it keeps its distance
/// and shoots instead of closing to melee range. Kept as a separate script
/// (rather than edited into GuardBrain) since GuardBrain is owned by Jacob.
[RequireComponent(typeof(GuardPerception), typeof(GuardMotor))]
public class GuardRangedBrain : MonoBehaviour
{
    private enum State { Idle, Chase, Attack }

    [SerializeField] private float attackEnterRange = 5f;
    [SerializeField] private float attackExitRange = 6.5f;
    [SerializeField] private float retreatRange = 2.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private GameObject bulletPrefab;

    private State state = State.Idle;
    private GuardPerception perception;
    private GuardMotor motor;
    private Transform target;
    private float nextAttackTime;

    private void Awake()
    {
        perception = GetComponent<GuardPerception>();
        motor = GetComponent<GuardMotor>();
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
            case State.Chase: TickChase(); break;
            case State.Attack: TickAttack(); break;
            case State.Idle: break;
        }
    }

    private void TickChase()
    {
        if (target == null) { HandleTargetLost(); return; }

        SeekOrRetreat();

        if (Vector2.Distance(transform.position, target.position) <= attackEnterRange)
            TransitionTo(State.Attack);
    }

    private void TickAttack()
    {
        if (target == null) { HandleTargetLost(); return; }

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance >= attackExitRange)
        {
            TransitionTo(State.Chase);
            return;
        }

        SeekOrRetreat();

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            Fire();
        }
    }

    private void SeekOrRetreat()
    {
        float distance = Vector2.Distance(transform.position, target.position);

        if (distance < retreatRange)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)target.position).normalized;
            motor.Seek((Vector2)transform.position + away * 2f);
        }
        else if (distance > attackEnterRange)
        {
            motor.Seek(target.position);
        }
        else
        {
            motor.Stop();
        }
    }

    private void Fire()
    {
        if (bulletPrefab == null || target == null) return;

        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        GameObject go = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        if (go.TryGetComponent<Bullet>(out var bullet))
            bullet.Init(dir, attackDamage, false);
    }

    private void TransitionTo(State next)
    {
        if (state == next) return;
        state = next;
    }
}
