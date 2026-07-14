using UnityEngine;

public class FallbackSpawner : MonoBehaviour
{
    [SerializeField] private GuardBrain guardPrefab;   // prefab asset, NOT a scene object
    [SerializeField] private Transform player;          // scene reference lives HERE, on the spawner
    [SerializeField] private Transform[] spawnPoints;   // empty GameObjects at arena edges
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform spawnedParent;   // your --- SPAWNED --- anchor

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            Spawn();
        }
    }

    private void Spawn()
    {
        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        var guard = Instantiate(guardPrefab, point.position, Quaternion.identity, spawnedParent);
        guard.SetTarget(player);
    }
}
