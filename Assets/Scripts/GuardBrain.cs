using UnityEngine;

[RequireComponent(typeof(GuardPerception), typeof(GuardMotor))]
public class GuardBrain : MonoBehaviour
{
    private enum State { Idle, Chase, Attack }

    [SerializeField] private float attackEnterRange = 1.2f;
    [SerializeField] private float attackExitRange = 1.8f;   // hysteresis
    [SerializeField] private float attackCooldown = 1.0f;

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
        motor.Seek(target.position);

        if (Vector2.Distance(transform.position, target.position) <= attackEnterRange)
            TransitionTo(State.Attack);
    }

    private void TickAttack()
    {
        motor.Stop();

        if (Vector2.Distance(transform.position, target.position) >= attackExitRange)
        {
            TransitionTo(State.Chase);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            Debug.Log($"{name} attacks!");   // damage hook lands when player health exists
        }
    }

    private void TransitionTo(State next)
    {
        if (state == next) return;
        Debug.Log($"{name}: {state} -> {next}");
        state = next;
    }
    
    public void SetTarget(Transform target)
    {
        perception.SetTarget(target);
    }
}