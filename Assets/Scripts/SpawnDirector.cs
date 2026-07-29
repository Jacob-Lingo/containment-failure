// This program was written with AI assistance

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SpawnDirector : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ProceduralLevelGenerator proceduralLevelGenerator;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject guardPrefab; // Drag Guard prefab here in the inspector
    [SerializeField] private GameObject rangedGuardPrefab; // Optional: drag GuardRanged prefab here to mix in ranged guards
    [SerializeField, Range(0f, 1f)] private float rangedGuardChance = 0.3f;
    [SerializeField] private Transform playerTransform; // Drag the Player here so the Spawner knows the target
    [SerializeField] private float spawnInterval = 0.6f; // Seconds between spawns
    [SerializeField] private int guardsPerSpawnTick = 3; // How many guards try to spawn each interval — this is what makes it read as a swarm rather than a trickle
    [SerializeField] private int initialBurstCount = 10; // Guards spawned immediately on Start so the player isn't waiting alone for the first wave
<<<<<<< HEAD
    private static readonly Color BaseRangedTint = new Color(0.35f, 0.55f, 0.85f); // distinguishes plain pistol guards from melee guards even before military tiers exist
=======
    [SerializeField] private Transform[] spawnPoints; // Array of spawn locations around your arena
>>>>>>> origin/mattbranch

    [Header("Prototype Constraints")]
    [SerializeField] private int maxGuardCount = 45; // Maximum number of guards allowed on screen at once
    [SerializeField] private float minDistanceFromOtherGuards = 1.2f; // Prevent spawning right on top of each other; small so the ring can actually pack a swarm

    [Header("Player Distance Constraints")]
    [SerializeField] private float minDistanceFromPlayer = 4.0f; // Don't spawn right on top of the player
    [SerializeField] private float maxDistanceFromPlayer = 12.0f; // Don't spawn so far away that they are useless

    [Header("Game State Timer")]
    [SerializeField] private float gameDuration = 60.0f; // 60-second prototype round

    [Header("Military Tier (re-tuned GuardRanged, floor 5+)")]
    [SerializeField] private int militaryStartFloor = 5;
    [SerializeField, Range(0f, 1f)] private float militaryUpgradeChance = 0.35f; // chance a rolled ranged spawn becomes military instead
    [SerializeField] private int militaryAkHealth = 4;
    [SerializeField] private int militaryAkDamage = 2;
    [SerializeField] private float militaryAkCooldown = 0.6f;
    [SerializeField] private int militaryBazookaHealth = 6;
    [SerializeField] private int militaryBazookaDamage = 3;
    [SerializeField] private float militaryBazookaCooldown = 2.5f;
    [SerializeField] private float militaryBazookaBulletScale = 1.8f;
    [SerializeField] private float militaryBazookaSpeed = 1.5f;

    [Header("Beast Tiers (re-tuned Guard.prefab, no new prefabs)")]
    [SerializeField, Range(0f, 1f)] private float wolfChance = 0.18f;   // fast, fragile
    [SerializeField] private int wolfHealth = 2;
    [SerializeField] private float wolfSpeed = 5.2f;
    [SerializeField, Range(0f, 1f)] private float ratChance = 0.15f;    // trivial filler
    [SerializeField] private int ratHealth = 1;
    [SerializeField] private float ratSpeed = 4.2f;
    [SerializeField, Range(0f, 1f)] private float entChance = 0.08f;    // slow wall of wood
    [SerializeField] private int entHealth = 14;
    [SerializeField] private float entSpeed = 0.9f;
    [SerializeField] private int entDamage = 3;
    [SerializeField] private float entScale = 1.5f;

    [Header("Endless Depth")]
    [Tooltip("Endless only: a Warden appears every this many floors, so the boss is reachable there at all.")]
    [SerializeField] private int bossEveryNFloors = 5;
    [Tooltip("Floor from which ordinary spawns can come up Elite.")]
    [SerializeField] private int eliteStartFloor = 4;
    [SerializeField, Range(0f, 0.2f)] private float eliteChancePerFloor = 0.04f;
    [SerializeField, Range(0f, 1f)] private float eliteMaxChance = 0.45f;
    [SerializeField] private float eliteHealthMultiplier = 2.2f;
    [SerializeField] private float eliteDamageMultiplier = 1.6f;
    [SerializeField] private float eliteScale = 1.3f;
    private static readonly Color EliteTint = new Color(1f, 0.86f, 0.55f);
    private int lastBossFloor = -1;

    [Header("Cosmetic Variants")]
    [SerializeField, Range(0f, 1f)] private float skeletonChance = 0.3f; // chance a plain melee spawn looks like a skeleton rather than a knight

    [Header("Heavy Melee Tier (re-tuned GuardBrain)")]
    [SerializeField] private int heavyMeleeStartFloor = 3;
    [SerializeField, Range(0f, 1f)] private float heavyMeleeChance = 0.25f; // chance a rolled melee spawn becomes Heavy instead of a plain guard
    [SerializeField] private int heavyMeleeHealth = 6;
    [SerializeField] private int heavyMeleeDamage = 2;
    [SerializeField] private float heavyMeleeCooldown = 1.4f;
    [SerializeField] private float heavyMeleeSpeed = 1.8f;
    [SerializeField] private float heavyMeleeScale = 1.25f;

    [Header("Necromancer Tier (re-tuned GuardRanged, summons skeletons, never attacks)")]
    [SerializeField] private int necromancerStartFloor = 2;
    [SerializeField, Range(0f, 1f)] private float necromancerChance = 0.1f; // rolled before the melee/ranged split, so it's a share of all spawns
    [SerializeField] private int necromancerHealth = 8;
    [SerializeField] private float necromancerSpeed = 3.6f; // faster than a plain guard: it has to actually get away
    [SerializeField] private float necromancerScale = 1.15f;

    [Header("Leaping Brute Tier (re-tuned Guard + GuardLeap gap-closer)")]
    [SerializeField] private int leaperStartFloor = 2;
    [SerializeField, Range(0f, 1f)] private float leaperChance = 0.15f; // chance a rolled plain melee spawn becomes a Brute
    [SerializeField] private int leaperHealth = 5;
    [SerializeField] private int leaperSlamDamage = 3;
    [SerializeField] private float leaperLeapCooldown = 4f;
    [SerializeField] private float leaperSpeed = 2.4f; // slow on the ground; the leap is how it reaches you
    [SerializeField] private float leaperScale = 1.15f;

    [Header("Coins (yellow, scattered map-wide, not tied to kills)")]
    [SerializeField] private int coinPoolSize = 7;
    [SerializeField] private float coinRefillInterval = 45f;

    [Header("Exp Orbs (green, scattered map-wide — same orb guards drop)")]
    [SerializeField] private int expOrbPoolSize = 14;
    [SerializeField] private float expOrbRefillInterval = 20f;
    private readonly List<GameObject> activeExpOrbs = new List<GameObject>();
    private float expOrbTimer;
    [SerializeField] private float expPickupScatterRadius = 10f; // spread around each spawn point (or spawner position if none set)
    private readonly List<GameObject> activeCoins = new List<GameObject>();
    private float coinTimer;

    [Header("Boss (floor 10, spawned once, re-tuned GuardRanged)")]
    [SerializeField] private int bossBaseHealth = 40;
    [SerializeField] private float bossMaxSpeed = 1.2f;
    [SerializeField] private float bossScale = 1.4f;
    [SerializeField] private int bossAttackDamage = 4;
    [SerializeField] private float bossAttackCooldown = 1.8f;
    [SerializeField] private float bossBulletScale = 2.2f;

    // Trackers
    private float spawnTimer;
    private float gameTimer;
    private bool isGameActive = true;

    // List to keep track of currently alive guards
    private List<GameObject> activeGuards = new List<GameObject>();

    public float CurrentTime => gameTimer;
    public bool IsGameActive => isGameActive;

    private float baseSpawnInterval;
    private int baseMaxGuardCount;

    void Start()
    {
        baseSpawnInterval = spawnInterval;
        baseMaxGuardCount = maxGuardCount;
<<<<<<< HEAD
        gameTimer = gameDuration;
=======
        RescaleForFloor();

        spawnTimer = spawnInterval;
        coinTimer = coinRefillInterval;
        expOrbTimer = expOrbRefillInterval;
>>>>>>> origin/mattbranch

        if (playerTransform == null)
        {
            // Fail-safe: Try to automatically find the player if not set in inspector
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
<<<<<<< HEAD
=======

        for (int i = 0; i < initialBurstCount && activeGuards.Count < maxGuardCount; i++)
        {
            SpawnGuard();
        }

        RefillCoins();
        RefillExpOrbs();
>>>>>>> origin/mattbranch
    }

    /// Called by FloorExitDoor when the floor advances without a scene
    /// reload — recomputes pacing from the original inspector values so
    /// repeated calls don't compound.
    public void RescaleForFloor()
    {
        // Fresh time budget for the new floor (GDD: reach the exit before
        // the floor's containment systems activate).
        gameTimer = gameDuration;

        // Floor of 0.15s, not 0.75s. The old clamp sat *above* the scene's
        // 0.6s base interval, so floor 1 spawned slower than the Inspector
        // said and no later floor could ever spawn faster than 0.75s — the
        // difficulty multiplier had no effect on spawn rate at all.
        spawnInterval = Mathf.Max(0.15f, baseSpawnInterval / FloorManager.DifficultyMultiplier);
        maxGuardCount = Mathf.RoundToInt(baseMaxGuardCount * FloorManager.DifficultyMultiplier);
    }

    /// GDD lose condition #2: the floor timer expired before the player
    /// reached the exit ("The gas has filled the floor"). Mirrors the death
    /// path in FloorRunWatcher: reset run-scoped statics, then load the
    /// lose screen.
    private void Lose()
    {
        isGameActive = false;

        GameManager.LastLevelIndex = SceneManager.GetActiveScene().buildIndex;

        var evolution = FindFirstObjectByType<EvolutionSystem>();
        RunSummary.Capture("THE DARK TOOK YOU", evolution != null ? evolution.GetSummary() : "Claws only");

        FloorManager.ResetRun();
        RunStats.ResetRun();
        BossState.Reset();

        SceneManager.LoadScene("LoseScreen");
        Debug.Log("The torches gutter out. The dungeon takes you back.");
    }

    /// In-place "Restart Run" hook (PauseMenu) — clears every guard currently
    /// alive and resets pacing back to floor 1, no scene reload required.
    public void ResetForNewRun()
    {
        foreach (var guard in activeGuards)
        {
            if (guard != null) Destroy(guard);
        }
        activeGuards.Clear();

        isGameActive = true;
        lastBossFloor = -1;
        gameTimer = gameDuration;
        RescaleForFloor();
        spawnTimer = spawnInterval;
        coinTimer = coinRefillInterval;
        expOrbTimer = expOrbRefillInterval;

        for (int i = 0; i < initialBurstCount && activeGuards.Count < maxGuardCount; i++)
        {
            SpawnGuard();
        }

        foreach (var pickup in activeCoins)
        {
            if (pickup != null) Destroy(pickup);
        }
        activeCoins.Clear();
        RefillCoins();

        foreach (var orb in activeExpOrbs)
        {
            if (orb != null) Destroy(orb);
        }
        activeExpOrbs.Clear();
        RefillExpOrbs();
    }

    void Update()
    {
        if (!isGameActive) return;

        // Clean up dead guards from the tracking list
        activeGuards.RemoveAll(guard => guard == null);

        // GDD lose condition #2: each floor has its own time budget (refilled
        // by RescaleForFloor); if it expires before the player escapes the
        // floor, the gas gets them. Ticks on the boss floor too.
        gameTimer -= Time.deltaTime;
        if (gameTimer <= 0)
        {
            gameTimer = 0;
            Lose();
            return;
        }

        // The exp-orb pool tops itself up regardless of floor (including the
        // floor-10 boss room), independent of the kill-drop chance in GuardHealth.
        coinTimer -= Time.deltaTime;
        if (coinTimer <= 0f)
        {
            coinTimer = coinRefillInterval;
            RefillCoins();
        }

        expOrbTimer -= Time.deltaTime;
        if (expOrbTimer <= 0f)
        {
            expOrbTimer = expOrbRefillInterval;
            RefillExpOrbs();
        }

        // Floor 10 is a boss room: regular spawning stops (leftover guards
        // from floor 9 just play out) and exactly one Tank appears once.
        if (FloorManager.IsFinalFloor)
        {
            if (!BossState.Spawned && playerTransform != null) SpawnBoss();
            return;
        }

        // Endless has no final floor, so the story-mode boss branch never runs
        // and the Warden would be unreachable there. Bring him back on a
        // cadence instead — it gives depth a milestone to dread, and it's the
        // only way the bestiary can be completed in Endless.
        if (GameMode.IsEndless && bossEveryNFloors > 0
            && FloorManager.CurrentFloor % bossEveryNFloors == 0
            && lastBossFloor != FloorManager.CurrentFloor
            && playerTransform != null)
        {
            lastBossFloor = FloorManager.CurrentFloor;
            SpawnBoss();
        }

        // Handle Spawn Pacing
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            for (int i = 0; i < guardsPerSpawnTick && activeGuards.Count < maxGuardCount; i++)
            {
                SpawnGuard();
            }
            spawnTimer = spawnInterval;
        }
    }

    /// Tops the yellow coin pool back up to coinPoolSize, placing
    /// new ones at fresh random positions — "20 scattered around the map,
<<<<<<< HEAD
    // regenerating every 30s," not tied to enemy deaths or player position.
    private void RefillExpPickups()
=======
    /// regenerating every 30s," not tied to enemy deaths or player position.
    private void RefillCoins()
>>>>>>> origin/mattbranch
    {
        activeCoins.RemoveAll(pickup => pickup == null);

        while (activeCoins.Count < coinPoolSize)
        {
            activeCoins.Add(Coin.Spawn(RandomMapPosition()));
        }
    }

    /// Same idea as the coin pool but for the green exp orbs — deliberately
    /// the identical orb guards drop on death, so "green = progress, gold =
    /// currency" stays true whether you earned it from a kill or found it.
    private void RefillExpOrbs()
    {
        activeExpOrbs.RemoveAll(orb => orb == null);

        while (activeExpOrbs.Count < expOrbPoolSize)
        {
            activeExpOrbs.Add(ExpOrb.Spawn(RandomMapPosition(), persistent: true));
        }
    }

    private Vector3 RandomMapPosition()
    {
        var spawnPoints = proceduralLevelGenerator.SpawnPoints;
        Vector3 basePos = (spawnPoints != null && spawnPoints.Count > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Count)]
            : transform.position;

        Vector2 offset = Random.insideUnitCircle * expPickupScatterRadius;
        return basePos + (Vector3)offset;
    }

    /// An enemy tier scheduled for a floor the player never reaches may as
    /// well not exist. Story mode stops spawning on the final (boss) floor, so
    /// the last floor that spawns anything is TotalFloors-1; Endless has no
    /// cap and uses the configured floor as-is.
    private static int TierFloor(int configured)
    {
        if (GameMode.IsEndless) return configured;
        return Mathf.Min(configured, Mathf.Max(1, FloorManager.TotalFloors - 1));
    }

    private void SpawnGuard()
    {
        if (guardPrefab == null || playerTransform == null) return;

        if (!TryFindSpawnSpot(out Vector3 chosenSpawnPos)) return;

        // Floor 1 is a pure melee introduction; every other tier unlocks at its
        // configured floor, CLAMPED so it can't be scheduled past the last
        // floor that actually spawns anything. The tiers were tuned for a
        // 10-floor run and the game is now 3 floors with the last one being a
        // boss room, so heavyMeleeStartFloor=3 meant Knight Captains literally
        // never spawned, and the caster tier was one floor from never.
        int floor = FloorManager.CurrentFloor;

        // Rolled first and exclusive: a necromancer is a support unit, so it
        // must not also be an archer or a military variant.
        bool spawnNecromancer = rangedGuardPrefab != null
            && floor >= TierFloor(necromancerStartFloor)
            && Random.value < necromancerChance;

        bool spawnRanged = !spawnNecromancer
            && rangedGuardPrefab != null
            && floor >= TierFloor(2)
            && Random.value < rangedGuardChance;

        bool spawnMilitary = spawnRanged
            && floor >= TierFloor(militaryStartFloor)
            && Random.value < militaryUpgradeChance;

        bool bazooka = spawnMilitary && Random.value < 0.5f;

        // Heavy is the melee-side counterpart to the ranged military tier:
        // tougher, harder-hitting, slower — same base GuardBrain, re-tuned
        // via SetAttackProfile instead of a separate prefab.
        bool spawnHeavyMelee = !spawnRanged && !spawnNecromancer
            && floor >= TierFloor(heavyMeleeStartFloor)
            && Random.value < heavyMeleeChance;

        // Melee-side alternative to Heavy: same base Guard, but with a
        // telegraphed leap bolted on (GuardLeap) instead of bigger numbers.
        bool spawnLeaper = !spawnRanged && !spawnNecromancer && !spawnHeavyMelee
            && floor >= TierFloor(leaperStartFloor)
            && Random.value < leaperChance;

        GameObject prefabToSpawn = (spawnRanged || spawnNecromancer) ? rangedGuardPrefab : guardPrefab;
        GameObject newGuard = Instantiate(prefabToSpawn, chosenSpawnPos, Quaternion.identity);
        activeGuards.Add(newGuard);

        // Beasts are melee-only variants of the same GuardBrain, distinguished
        // by stats rather than behaviour: the wolf is a fast glass cannon, the
        // rat is filler that dies instantly, the ent is a slow wall. Rolled
        // before the cosmetic skin so they keep their own recognisable faces.
        bool spawnWolf = !spawnRanged && !spawnNecromancer && !spawnHeavyMelee && !spawnLeaper && Random.value < wolfChance;
        bool spawnRat = !spawnRanged && !spawnNecromancer && !spawnHeavyMelee && !spawnLeaper && !spawnWolf && Random.value < ratChance;
        bool spawnEnt = !spawnRanged && !spawnNecromancer && !spawnHeavyMelee && !spawnLeaper && !spawnWolf && !spawnRat
                        && floor >= TierFloor(2) && Random.value < entChance;

        // Base look for the tier. The stronger variants below overwrite this,
        // so this is the "plain" bowman / footknight, plus a free reskin-only
        // variant: some melee spawns come up as risen skeletons instead of
        // living knights. Pure cosmetics — same stats, more visual variety.
        if (spawnWolf) Bestiary.Apply(newGuard, Bestiary.Wolf);
        else if (spawnRat) Bestiary.Apply(newGuard, Bestiary.Rat);
        else if (spawnEnt) Bestiary.Apply(newGuard, Bestiary.Ent);
        else Bestiary.Apply(newGuard, spawnRanged ? Bestiary.RandomRangedSkin() : Bestiary.RandomMeleeSkin());

        if (newGuard.TryGetComponent<GuardHealth>(out var guardHealth))
        {
            if (spawnMilitary) guardHealth.SetBaseMaxHealth(bazooka ? militaryBazookaHealth : militaryAkHealth);
            else if (spawnHeavyMelee) guardHealth.SetBaseMaxHealth(heavyMeleeHealth);
            else if (spawnWolf) guardHealth.SetBaseMaxHealth(wolfHealth);
            else if (spawnRat) guardHealth.SetBaseMaxHealth(ratHealth);
            else if (spawnEnt) guardHealth.SetBaseMaxHealth(entHealth);
            else if (spawnNecromancer) guardHealth.SetBaseMaxHealth(necromancerHealth);
            else if (spawnLeaper) guardHealth.SetBaseMaxHealth(leaperHealth);
            guardHealth.ScaleForFloor(FloorManager.DifficultyMultiplier);
        }

        if (spawnNecromancer) SetUpNecromancer(newGuard);

        if (newGuard.TryGetComponent<GuardBrain>(out var guardBrain))
        {
            guardBrain.SetTarget(playerTransform);
            if (spawnWolf && newGuard.TryGetComponent<GuardMotor>(out var wolfMotor)) wolfMotor.SetMaxSpeed(wolfSpeed);
            if (spawnRat && newGuard.TryGetComponent<GuardMotor>(out var ratMotor)) ratMotor.SetMaxSpeed(ratSpeed);
            if (spawnEnt)
            {
                guardBrain.SetAttackProfile(entDamage, 2.2f);
                if (newGuard.TryGetComponent<GuardMotor>(out var entMotor)) entMotor.SetMaxSpeed(entSpeed);
                newGuard.transform.localScale = Vector3.one * entScale;
            }

            if (!spawnHeavyMelee) guardBrain.ScaleDamageForFloor(FloorManager.DifficultyMultiplier);

            if (spawnHeavyMelee)
            {
                guardBrain.SetAttackProfile(heavyMeleeDamage, heavyMeleeCooldown);
                guardBrain.ScaleDamageForFloor(FloorManager.DifficultyMultiplier);
                if (newGuard.TryGetComponent<GuardMotor>(out var meleeMotor)) meleeMotor.SetMaxSpeed(heavyMeleeSpeed);
                newGuard.transform.localScale = Vector3.one * heavyMeleeScale;
                Bestiary.Apply(newGuard, Bestiary.KnightCaptain);
            }
            else if (spawnLeaper)
            {
                // GuardBrain is left intact — the Brute still chases and still
                // swings in melee; GuardLeap only adds the gap-closer.
                var leap = newGuard.AddComponent<GuardLeap>();
                leap.SetSlamProfile(leaperSlamDamage, leaperLeapCooldown);
                leap.ScaleDamageForFloor(FloorManager.DifficultyMultiplier);
                leap.Configure(playerTransform);

                if (newGuard.TryGetComponent<GuardMotor>(out var leaperMotor)) leaperMotor.SetMaxSpeed(leaperSpeed);
                newGuard.transform.localScale = Vector3.one * leaperScale;
                Bestiary.Apply(newGuard, Bestiary.Brigand);
            }
        }

        // Skipped entirely for necromancers: SetUpNecromancer has already
        // Destroy()ed the GuardRangedBrain, but Unity defers that to end of
        // frame so TryGetComponent would still hand it back and re-arm it.
        if (!spawnNecromancer && newGuard.TryGetComponent<GuardRangedBrain>(out var rangedBrain))
        {
            rangedBrain.SetTarget(playerTransform);

            if (bazooka)
            {
                rangedBrain.SetAttackProfile(militaryBazookaDamage, militaryBazookaCooldown);
                rangedBrain.ScaleDamageForFloor(FloorManager.DifficultyMultiplier);
                rangedBrain.SetBulletProfile(militaryBazookaBulletScale, true);
                if (newGuard.TryGetComponent<GuardMotor>(out var motor)) motor.SetMaxSpeed(militaryBazookaSpeed);
                Bestiary.Apply(newGuard, Bestiary.RandomCasterSkin());
            }
            else if (spawnMilitary)
            {
                rangedBrain.SetAttackProfile(militaryAkDamage, militaryAkCooldown);
                rangedBrain.ScaleDamageForFloor(FloorManager.DifficultyMultiplier);
                Bestiary.Apply(newGuard, Bestiary.Archer);
            }
            else
            {
                rangedBrain.ScaleDamageForFloor(FloorManager.DifficultyMultiplier);
            }
        }

        // Last, deliberately: Elite multiplies whatever health, damage and
        // scale this spawn ended up with, so it stacks on top of its tier
        // rather than being overwritten by it.
        TryMakeElite(newGuard);
    }

    /// Turns a freshly spawned GuardRanged into a Necromancer: its shooting
    /// brain is removed outright (this tier has no attack of its own) and
    /// replaced with NecromancerBrain, which flees and raises skeletons.
    private void SetUpNecromancer(GameObject necromancer)
    {
        if (necromancer.TryGetComponent<GuardRangedBrain>(out var shooter)) Destroy(shooter);

        if (necromancer.TryGetComponent<GuardMotor>(out var motor)) motor.SetMaxSpeed(necromancerSpeed);
        necromancer.transform.localScale = Vector3.one * necromancerScale;
        Bestiary.Apply(necromancer, Bestiary.Cultist);

        // No ScaleDamageForFloor: the necromancer never deals damage. Its
        // minions are flat 1 damage by design (see NecromancerBrain).
        necromancer.AddComponent<NecromancerBrain>().Configure(guardPrefab, playerTransform, this);
    }

    /// Lets a spawned creature enrol something it created (NecromancerBrain's
    /// minions) in the same tracking list, so maxGuardCount sees them and
    /// ResetForNewRun clears them instead of leaking them across a restart.
    public void RegisterGuard(GameObject guard)
    {
        if (guard != null) activeGuards.Add(guard);
    }

    /// Past eliteStartFloor an ordinary spawn can come up Elite: markedly
    /// tougher, hits harder, visibly bigger and gold-tinted so it's readable on
    /// sight. This is what stops deep Endless being the same fight with bigger
    /// numbers — the *composition* keeps shifting, not just the stats.
    private void TryMakeElite(GameObject guard)
    {
        int floor = FloorManager.CurrentFloor;
        if (floor < eliteStartFloor) return;

        float chance = Mathf.Min(eliteMaxChance, eliteChancePerFloor * (floor - eliteStartFloor + 1));
        if (Random.value >= chance) return;

        if (guard.TryGetComponent<GuardHealth>(out var health))
        {
            health.SetBaseMaxHealth(Mathf.Max(1, Mathf.RoundToInt(health.Health * eliteHealthMultiplier)));
            // Tint through GuardHealth so the hit flash reverts to gold, not white.
            health.SetBaseColor(EliteTint);
        }

        if (guard.TryGetComponent<GuardBrain>(out var melee)) melee.ScaleDamageForFloor(eliteDamageMultiplier);
        if (guard.TryGetComponent<GuardRangedBrain>(out var ranged)) ranged.ScaleDamageForFloor(eliteDamageMultiplier);

        guard.transform.localScale *= eliteScale;
    }

    private void SpawnBoss()
    {
        if (rangedGuardPrefab == null) return;
        if (!TryFindSpawnSpot(out Vector3 spawnPos)) spawnPos = transform.position;

        GameObject boss = Instantiate(rangedGuardPrefab, spawnPos, Quaternion.identity);
        activeGuards.Add(boss);
        boss.transform.localScale = Vector3.one * bossScale;
        boss.AddComponent<BossMarker>();

        if (boss.TryGetComponent<GuardHealth>(out var health))
        {
            health.SetBaseMaxHealth(bossBaseHealth);
            health.ScaleForFloor(FloorManager.DifficultyMultiplier);
        }

        if (boss.TryGetComponent<GuardMotor>(out var motor))
            motor.SetMaxSpeed(bossMaxSpeed);

        if (boss.TryGetComponent<GuardRangedBrain>(out var brain))
        {
            brain.SetAttackProfile(bossAttackDamage, bossAttackCooldown);
            brain.SetBulletProfile(bossBulletScale, true);
            brain.SetTarget(playerTransform);
        }

        Bestiary.Apply(boss, Bestiary.Warden);
        BossState.MarkSpawned();
    }

    /// Picks a spot on a ring around the *player*, not around the fixed
    /// spawnPoints. The old version offset up to 3 units from one of four
    /// static points and then required the result to also be within
    /// maxDistanceFromPlayer, which had two failure modes: walk away from the
    /// spawn points and spawning stopped completely, and the four 3-unit
    /// circles saturated at roughly twenty guards (minDistanceFromOtherGuards
    /// spacing) so maxGuardCount was unreachable. A ring around the player
    /// always has room and always follows them.
    private bool TryFindSpawnSpot(out Vector3 chosenSpawnPos)
    {
        chosenSpawnPos = Vector3.zero;
        if (playerTransform == null) return false;

<<<<<<< HEAD
        var spawnPoints = proceduralLevelGenerator.SpawnPoints;
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            // Fallback to spawner's own position if no points are available
            if (IsValidSpawnSpot(transform.position))
            {
                chosenSpawnPos = transform.position;
                return true;
            }
            return false;
        }

        // Shuffle spawn points to keep spawns randomized
        List<Vector3> basePositions = new List<Vector3>(spawnPoints);
        for (int i = 0; i < basePositions.Count; i++)
        {
            Vector3 temp = basePositions[i];
            int randomIndex = Random.Range(i, basePositions.Count);
            basePositions[i] = basePositions[randomIndex];
            basePositions[randomIndex] = temp;
        }

        // Try to find a valid spot around our base positions
        foreach (Vector3 basePos in basePositions)
        {
            // Attempt to find a randomized offset position around the base spawn location
            for (int attempts = 0; attempts < 10; attempts++)
            {
                // Generate a random position offset within a 3-unit radius of the spawn spot
                Vector2 randomOffset = Random.insideUnitCircle * 3.0f;
                Vector3 potentialPos = basePos + new Vector3(randomOffset.x, randomOffset.y, 0);

                if (IsValidSpawnSpot(potentialPos))
                {
                    chosenSpawnPos = potentialPos;
                    return true;
                }
=======
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
            Vector3 candidate = playerTransform.position
                + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

            if (IsValidSpawnSpot(candidate))
            {
                chosenSpawnPos = candidate;
                return true;
>>>>>>> origin/mattbranch
            }
        }

        return false;
    }

    /// Only rule left is "don't stack two guards on the same pixel". The old
    /// "must be within maxDistanceFromOtherGuards of an existing guard" clamp
    /// was removed: it forced every spawn to clump onto the existing pack, and
    /// blocked spawning outright whenever the pack drifted away from the
    /// player.
    private bool IsValidSpawnSpot(Vector3 position)
    {
<<<<<<< HEAD
        // 1. Check distance parameters relative to the Player
        if (playerTransform == null) return false;
        float distanceToPlayer = Vector2.Distance(position, playerTransform.position);
        if (distanceToPlayer < minDistanceFromPlayer || distanceToPlayer > maxDistanceFromPlayer)
        {
            return false;
        }

        // 2. Check distance parameters relative to other existing guards
        bool withinRangeOfAtLeastOneGuard = activeGuards.Count == 0; // First guard spawns free of guard clamps

=======
>>>>>>> origin/mattbranch
        foreach (GameObject guard in activeGuards)
        {
            if (guard == null) continue;
            if (Vector2.Distance(position, guard.transform.position) < minDistanceFromOtherGuards)
                return false;
        }

        return true;
    }
}