using UnityEngine;

[RequireComponent(typeof(GuardPerception), typeof(GuardMotor))]
public class GuardBrain : MonoBehaviour
{
    private enum State { Idle, Chase, Attack }

    [SerializeField] private float attackEnterRange = 1.2f;
    [SerializeField] private float attackExitRange = 1.8f;   // hysteresis
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private int attackDamage = 1;

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

    /// Called by SpawnDirector at instantiation; forwards to perception,
    /// which drives all state transitions via TargetSpotted/TargetLost.
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
            case State.Chase:  TickChase();  break;
            case State.Attack: TickAttack(); break;
            case State.Idle:   break;        // future: patrol lives here
        }
    }

    private void TickChase()
    {
        // Guard against the target being destroyed mid-chase. Perception's
        // null early-return never fires TargetLost in that case, so the
        // brain must detect it here or throw on target.position next line.
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

            Sfx.PlayRandom("guard_baton_hit", 3, target.position);
            HitFlashFx.Spawn(target.position, new Color(1f, 1f, 1f, 0.85f), 0.35f);

            // No-op until a component implementing IDamageable exists on the
            // player, so this commits safely ahead of Noah's health system.
            if (target.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(attackDamage);
        }
    }

    private void TransitionTo(State next)
    {
        if (state == next) return;
#if UNITY_EDITOR
        Debug.Log($"{name}: {state} -> {next}");
#endif
        state = next;
    }
}
