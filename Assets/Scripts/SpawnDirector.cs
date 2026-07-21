// This program was written with AI assistance

using UnityEngine;
using System.Collections.Generic;

public class SpawnDirector : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject guardPrefab; // Drag Guard prefab here in the inspector
    [SerializeField] private GameObject rangedGuardPrefab; // Optional: drag GuardRanged prefab here to mix in ranged guards
    [SerializeField, Range(0f, 1f)] private float rangedGuardChance = 0.3f;
    [SerializeField] private Transform playerTransform; // Drag the Player here so the Spawner knows the target
    [SerializeField] private float spawnInterval = 0.6f; // Seconds between spawns
    [SerializeField] private int guardsPerSpawnTick = 3; // How many guards try to spawn each interval — this is what makes it read as a swarm rather than a trickle
    [SerializeField] private int initialBurstCount = 10; // Guards spawned immediately on Start so the player isn't waiting alone for the first wave
    [SerializeField] private Transform[] spawnPoints; // Array of spawn locations around your arena
    private static readonly Color BaseRangedTint = new Color(0.35f, 0.55f, 0.85f); // distinguishes plain pistol guards from melee guards even before military tiers exist

    [Header("Prototype Constraints")]
    [SerializeField] private int maxGuardCount = 45; // Maximum number of guards allowed on screen at once
    [SerializeField] private float minDistanceFromOtherGuards = 2.5f; // Prevent spawning right on top of each other
    [SerializeField] private float maxDistanceFromOtherGuards = 15.0f; // Keep guards relatively grouped/clamped to each other

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
    private static readonly Color MilitaryAkTint = new Color(0.25f, 0.35f, 0.15f);
    [SerializeField] private int militaryBazookaHealth = 6;
    [SerializeField] private int militaryBazookaDamage = 3;
    [SerializeField] private float militaryBazookaCooldown = 2.5f;
    [SerializeField] private float militaryBazookaBulletScale = 1.8f;
    [SerializeField] private float militaryBazookaSpeed = 1.5f;
    private static readonly Color MilitaryBazookaTint = new Color(0.3f, 0.3f, 0.3f);

    [Header("Persistent Exp Orbs (yellow, scattered map-wide, not tied to kills)")]
    [SerializeField] private int expPickupPoolSize = 20;
    [SerializeField] private float expPickupRefillInterval = 30f;
    [SerializeField] private float expPickupScatterRadius = 10f; // spread around each spawn point (or spawner position if none set)
    private readonly List<GameObject> activeExpPickups = new List<GameObject>();
    private float expPickupTimer;

    [Header("Boss (floor 10, spawned once, re-tuned GuardRanged)")]
    [SerializeField] private int bossBaseHealth = 40;
    [SerializeField] private float bossMaxSpeed = 1.2f;
    [SerializeField] private float bossScale = 1.4f;
    [SerializeField] private int bossAttackDamage = 4;
    [SerializeField] private float bossAttackCooldown = 1.8f;
    [SerializeField] private float bossBulletScale = 2.2f;
    private static readonly Color BossTint = new Color(0.5f, 0.1f, 0.1f);

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
        gameTimer = gameDuration;

        baseSpawnInterval = spawnInterval;
        baseMaxGuardCount = maxGuardCount;
        RescaleForFloor();

        spawnTimer = spawnInterval;
        expPickupTimer = expPickupRefillInterval;

        if (playerTransform == null)
        {
            // Fail-safe: Try to automatically find the player if not set in inspector
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        for (int i = 0; i < initialBurstCount && activeGuards.Count < maxGuardCount; i++)
        {
            SpawnGuard();
        }

        RefillExpPickups();
    }

    /// Called by FloorExitDoor when the floor advances without a scene
    /// reload — recomputes pacing from the original inspector values so
    /// repeated calls don't compound.
    public void RescaleForFloor()
    {
        spawnInterval = Mathf.Max(0.75f, baseSpawnInterval / FloorManager.DifficultyMultiplier);
        maxGuardCount = Mathf.RoundToInt(baseMaxGuardCount * FloorManager.DifficultyMultiplier);
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
        gameTimer = gameDuration;
        RescaleForFloor();
        spawnTimer = spawnInterval;
        expPickupTimer = expPickupRefillInterval;

        for (int i = 0; i < initialBurstCount && activeGuards.Count < maxGuardCount; i++)
        {
            SpawnGuard();
        }

        foreach (var pickup in activeExpPickups)
        {
            if (pickup != null) Destroy(pickup);
        }
        activeExpPickups.Clear();
        RefillExpPickups();
    }

    void Update()
    {
        if (!isGameActive) return;

        // Clean up dead guards from the tracking list
        activeGuards.RemoveAll(guard => guard == null);

        // Legacy 60s round timer is intentionally not gating anything anymore —
        // winning is now driven by FloorManager.IsFinalFloor via FloorExitDoor.
        gameTimer -= Time.deltaTime;
        if (gameTimer < 0) gameTimer = 0;

        // The exp-orb pool tops itself up regardless of floor (including the
        // floor-10 boss room), independent of the kill-drop chance in GuardHealth.
        expPickupTimer -= Time.deltaTime;
        if (expPickupTimer <= 0f)
        {
            expPickupTimer = expPickupRefillInterval;
            RefillExpPickups();
        }

        // Floor 10 is a boss room: regular spawning stops (leftover guards
        // from floor 9 just play out) and exactly one Tank appears once.
        if (FloorManager.IsFinalFloor)
        {
            if (!BossState.Spawned && playerTransform != null) SpawnBoss();
            return;
        }

        // Hitting the floor's kill quota auto-advances — no exit door needed
        // for floors 1-9 (FloorExitDoor is a no-op until floor 10).
        if (RunStats.FloorKills >= FloorManager.KillQuota)
        {
            FloorManager.AdvanceFloor();
            RunStats.ResetFloorKills();
            RescaleForFloor();
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

    /// Tops the yellow ExpPickup pool back up to expPickupPoolSize, placing
    /// new ones at fresh random positions — "20 scattered around the map,
    /// regenerating every 30s," not tied to enemy deaths or player position.
    private void RefillExpPickups()
    {
        activeExpPickups.RemoveAll(pickup => pickup == null);

        while (activeExpPickups.Count < expPickupPoolSize)
        {
            activeExpPickups.Add(ExpPickup.Spawn(RandomMapPosition()));
        }
    }

    private Vector3 RandomMapPosition()
    {
        Vector3 basePos = (spawnPoints != null && spawnPoints.Length > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Length)].position
            : transform.position;

        Vector2 offset = Random.insideUnitCircle * expPickupScatterRadius;
        return basePos + (Vector3)offset;
    }

    private void SpawnGuard()
    {
        if (guardPrefab == null || playerTransform == null) return;

        if (!TryFindSpawnSpot(out Vector3 chosenSpawnPos)) return;

        // Ranged guards only start showing up from floor 2 onward, so floor 1
        // is a pure melee introduction before guns enter the mix. From floor
        // militaryStartFloor, a rolled ranged spawn has a further chance to
        // upgrade into a tougher military variant (AK or Bazooka).
        bool spawnRanged = rangedGuardPrefab != null && FloorManager.CurrentFloor >= 2 && Random.value < rangedGuardChance;
        bool spawnMilitary = spawnRanged && FloorManager.CurrentFloor >= militaryStartFloor && Random.value < militaryUpgradeChance;
        bool bazooka = spawnMilitary && Random.value < 0.5f;

        GameObject prefabToSpawn = spawnRanged ? rangedGuardPrefab : guardPrefab;
        GameObject newGuard = Instantiate(prefabToSpawn, chosenSpawnPos, Quaternion.identity);
        activeGuards.Add(newGuard);

        if (newGuard.TryGetComponent<GuardHealth>(out var guardHealth))
        {
            if (spawnMilitary) guardHealth.SetBaseMaxHealth(bazooka ? militaryBazookaHealth : militaryAkHealth);
            guardHealth.ScaleForFloor(FloorManager.DifficultyMultiplier);
        }

        if (newGuard.TryGetComponent<GuardBrain>(out var guardBrain))
        {
            guardBrain.SetTarget(playerTransform);
        }

        if (newGuard.TryGetComponent<GuardRangedBrain>(out var rangedBrain))
        {
            rangedBrain.SetTarget(playerTransform);

            if (bazooka)
            {
                rangedBrain.SetAttackProfile(militaryBazookaDamage, militaryBazookaCooldown);
                rangedBrain.SetBulletProfile(militaryBazookaBulletScale, true);
                if (newGuard.TryGetComponent<GuardMotor>(out var motor)) motor.SetMaxSpeed(militaryBazookaSpeed);
                Tint(newGuard, MilitaryBazookaTint);
            }
            else if (spawnMilitary)
            {
                rangedBrain.SetAttackProfile(militaryAkDamage, militaryAkCooldown);
                Tint(newGuard, MilitaryAkTint);
            }
            else
            {
                // Plain pistol guard (not military) — tinted so it reads as a
                // distinct type from melee guards even on floors before
                // military variants start appearing.
                Tint(newGuard, BaseRangedTint);
            }
        }
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

        Tint(boss, BossTint);
        BossState.MarkSpawned();
    }

    // Tints the body via GuardHealth.SetBaseColor (not a raw SpriteRenderer
    // write) so its hit-flash reverts to the tint instead of snapping back
    // to the original untinted color baked in at Awake.
    private static void Tint(GameObject guard, Color color)
    {
        if (guard.TryGetComponent<GuardHealth>(out var health))
            health.SetBaseColor(color);
        else if (guard.TryGetComponent<SpriteRenderer>(out var bodySr))
            bodySr.color = color;

        var weaponIcon = guard.GetComponentInChildren<WeaponIcon>();
        if (weaponIcon != null && weaponIcon.TryGetComponent<SpriteRenderer>(out var iconSr))
            iconSr.color = color;
    }

    private bool TryFindSpawnSpot(out Vector3 chosenSpawnPos)
    {
        chosenSpawnPos = Vector3.zero;

        // Collect all potential base origins to evaluate (either spawn points or spawner position)
        List<Vector3> basePositions = new List<Vector3>();
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (Transform pt in spawnPoints)
            {
                if (pt != null) basePositions.Add(pt.position);
            }
        }
        else
        {
            basePositions.Add(transform.position);
        }

        // Shuffle base positions to keep spawns randomized
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
            }
        }

        // If all evaluated zones are invalid, the caller skips spawning this tick to preserve distance rules
        return false;
    }

    private bool IsValidSpawnSpot(Vector3 position)
    {
        // 1. Check distance parameters relative to the Player
        float distanceToPlayer = Vector2.Distance(position, playerTransform.position);
        if (distanceToPlayer < minDistanceFromPlayer || distanceToPlayer > maxDistanceFromPlayer)
        {
            return false;
        }

        // 2. Check distance parameters relative to other existing guards
        bool withinRangeOfAtLeastOneGuard = activeGuards.Count == 0; // First guard spawns free of guard clamps

        foreach (GameObject guard in activeGuards)
        {
            if (guard != null)
            {
                float distanceToGuard = Vector2.Distance(position, guard.transform.position);

                // Rule: Must not be too close to any guard
                if (distanceToGuard < minDistanceFromOtherGuards)
                {
                    return false;
                }

                // Rule: Track if we are within the maximum allowed distance to at least one guard
                if (distanceToGuard <= maxDistanceFromOtherGuards)
                {
                    withinRangeOfAtLeastOneGuard = true;
                }
            }
        }

        return withinRangeOfAtLeastOneGuard;
    }
}