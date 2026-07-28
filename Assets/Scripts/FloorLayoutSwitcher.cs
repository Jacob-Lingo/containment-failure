using UnityEngine;

public class FloorLayoutSwitcher : MonoBehaviour
{
    [SerializeField] private ProceduralLevelGenerator proceduralLevelGenerator;
    [SerializeField] private SpawnDirector spawnDirector;
    [SerializeField] private Transform playerTransform;

    private int lastAppliedFloor = -1;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    private void Update()
    {
        if (FloorManager.CurrentFloor == lastAppliedFloor) return;
        lastAppliedFloor = FloorManager.CurrentFloor;

        if (proceduralLevelGenerator != null)
        {
            proceduralLevelGenerator.GenerateLevel();

            // Move player to the designated start position
            if (playerTransform != null)
            {
                playerTransform.position = proceduralLevelGenerator.PlayerStartPosition;
            }
        }

        if (spawnDirector != null)
        {
            spawnDirector.ResetForNewRun();
        }
    }
}