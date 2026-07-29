using System.Collections.Generic;
using UnityEngine;

/// Summoner tier. Never attacks: it only ever runs from the player and
/// periodically raises a wave of 1-HP skeletons that do the fighting.
///
/// Spawned by SpawnDirector onto a GuardRanged.prefab instance whose
/// GuardRangedBrain has been destroyed — it reuses that prefab purely for the
/// GuardPerception/GuardMotor/GuardHealth stack, same "re-tune at runtime, no
/// new prefab asset" rule the military/heavy/boss tiers follow.
[RequireComponent(typeof(GuardPerception), typeof(GuardMotor))]
public class NecromancerBrain : MonoBehaviour
{
    [SerializeField] private float summonInterval = 2f;
    [SerializeField] private int minionsPerWave = 6;

    [Tooltip("Live minions this necromancer may have at once. 6 every 2s is unbounded without it and the floor drowns.")]
    [SerializeField] private int maxLiveMinions = 18;

    [SerializeField] private float summonRadius = 1.4f;
    [SerializeField] private float minionScale = 0.75f;
    [SerializeField] private float minionSpeed = 3.6f;
    [SerializeField] private float minionAttackCooldown = 1f;

    [Tooltip("Below this the necromancer runs. It has no attack, so unlike GuardRangedBrain there is no standoff band to hold.")]
    [SerializeField] private float fleeRange = 8f;

    private static readonly Color SummonColor = new Color(0.55f, 0.35f, 0.9f);

    private GuardPerception perception;
    private GuardMotor motor;
    private Transform target;

    private GameObject minionPrefab;
    private SpawnDirector director;
    private float nextSummonTime;

    private readonly List<GameObject> minions = new List<GameObject>();

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

    /// Called by SpawnDirector right after AddComponent. The minion prefab is
    /// SpawnDirector's own guardPrefab, and the director is handed over so
    /// summoned minions land in activeGuards — otherwise they'd survive
    /// ResetForNewRun and leak across a restart.
    public void Configure(GameObject minionPrefab, Transform player, SpawnDirector director)
    {
        this.minionPrefab = minionPrefab;
        this.director = director;
        nextSummonTime = Time.time + summonInterval;
        perception.SetTarget(player);
    }

    private void HandleTargetSpotted(Transform t) => target = t;

    private void HandleTargetLost()
    {
        target = null;
        motor.Stop();
    }

    private void Update()
    {
        if (target == null) return;

        Flee();

        if (Time.time >= nextSummonTime)
        {
            nextSummonTime = Time.time + summonInterval;
            SummonWave();
        }
    }

    /// GuardRangedBrain.SeekOrRetreat's retreat branch with the other two
    /// branches deleted: with no attack of its own there is never a reason to
    /// hold position or close in.
    private void Flee()
    {
        Vector2 away = (Vector2)transform.position - (Vector2)target.position;

        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle.normalized;
        else if (away.magnitude > fleeRange) { motor.Stop(); return; }

        motor.Seek((Vector2)transform.position + away.normalized * 3f);
    }

    private void SummonWave()
    {
        if (minionPrefab == null) return;

        minions.RemoveAll(m => m == null);

        int room = maxLiveMinions - minions.Count;
        if (room <= 0) return;

        int count = Mathf.Min(minionsPerWave, room);

        HitFlashFx.Spawn(transform.position, SummonColor, 0.9f);

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Vector3 pos = transform.position
                + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * summonRadius;

            minions.Add(SpawnMinion(pos));
        }
    }

    private GameObject SpawnMinion(Vector3 position)
    {
        GameObject minion = Instantiate(minionPrefab, position, Quaternion.identity);
        minion.transform.localScale = Vector3.one * minionScale;
        Bestiary.Apply(minion, Bestiary.Skeleton);
        HitFlashFx.Spawn(position, SummonColor, 0.4f);

        if (minion.TryGetComponent<GuardHealth>(out var minionHealth))
        {
            minionHealth.SetBaseMaxHealth(1);
            // Deliberately no ScaleForFloor: "dies to one hit of anything" is
            // the whole point of the tier, and a floor multiplier would take
            // that away by floor 3.
            minionHealth.SuppressDrops();
        }

        if (minion.TryGetComponent<GuardBrain>(out var brain))
        {
            // Flat 1 damage, no ScaleDamageForFloor — six of these every two
            // seconds is already the threat; scaling it as well is a wipe.
            brain.SetAttackProfile(1, minionAttackCooldown);
            brain.SetTarget(target);
        }

        if (minion.TryGetComponent<GuardMotor>(out var minionMotor))
            minionMotor.SetMaxSpeed(minionSpeed);

        if (director != null) director.RegisterGuard(minion);

        return minion;
    }

    // No death cleanup needed: the component dies with the necromancer, so
    // Update stops and no further waves are summoned. Minions already raised
    // are left alive on purpose — killing the summoner cuts off
    // reinforcements, it isn't a screen-clear.
}
