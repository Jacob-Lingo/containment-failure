// This program was written with AI assistance

using UnityEngine;
using System.Collections.Generic;

public class SpawnDirector : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject guardPrefab; // Drag Guard prefab here in the inspector
    [SerializeField] private Transform playerTransform; // Drag the Player here so the Spawner knows the target
    [SerializeField] private float spawnInterval = 3.0f; // Seconds between spawns
    [SerializeField] private Transform[] spawnPoints; // Array of spawn locations around your arena

    [Header("Prototype Constraints")]
    [SerializeField] private int maxGuardCount = 15; // Maximum number of guards allowed on screen at once
    [SerializeField] private float minDistanceFromOtherGuards = 2.5f; // Prevent spawning right on top of each other
    [SerializeField] private float maxDistanceFromOtherGuards = 15.0f; // Keep guards relatively grouped/clamped to each other
    
    [Header("Player Distance Constraints")]
    [SerializeField] private float minDistanceFromPlayer = 4.0f; // Don't spawn right on top of the player
    [SerializeField] private float maxDistanceFromPlayer = 12.0f; // Don't spawn so far away that they are useless

    [Header("Game State Timer")]
    [SerializeField] private float gameDuration = 60.0f; // 60-second prototype round

    // Trackers
    private float spawnTimer;
    private float gameTimer;
    private bool isGameActive = true;
    
    // List to keep track of currently alive guards
    private List<GameObject> activeGuards = new List<GameObject>();

    public float CurrentTime => gameTimer;
    public bool IsGameActive => isGameActive;

    void Start()
    {
        gameTimer = gameDuration;
        spawnTimer = spawnInterval;

        if (playerTransform == null)
        {
            // Fail-safe: Try to automatically find the player if not set in inspector
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        // Clean up dead guards from the tracking list
        activeGuards.RemoveAll(guard => guard == null);

        // 1. Handle Game Timer
        gameTimer -= Time.deltaTime;
        if (gameTimer <= 0)
        {
            gameTimer = 0;
            EndGame(true);
        }

        // 2. Handle Spawn Pacing
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            if (activeGuards.Count < maxGuardCount)
            {
                SpawnGuard();
            }
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnGuard()
    {
        if (guardPrefab == null || playerTransform == null) return;

        Vector3 chosenSpawnPos = Vector3.zero;
        bool foundValidSpot = false;

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
                    foundValidSpot = true;
                    break;
                }
            }

            if (foundValidSpot) break;
        }

        // If all evaluated zones are invalid, skip spawning this tick to preserve distance rules
        if (!foundValidSpot) return;

        // Instantiate
        GameObject newGuard = Instantiate(guardPrefab, chosenSpawnPos, Quaternion.identity);
        activeGuards.Add(newGuard);

        GuardBrain guardBrain = newGuard.GetComponent<GuardBrain>();
        if (guardBrain != null)
        {
            guardBrain.SetTarget(playerTransform);
        }
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

    private void EndGame(bool playerWon)
    {
        isGameActive = false;
        if (playerWon)
        {
            Debug.Log("Time ran out! Containment Failure successful! A.D.A.M. escapes!");
        }
    }
}