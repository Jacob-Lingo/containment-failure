using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Grid & Room Settings")]
    [SerializeField] private int width = 50;
    [SerializeField] private int height = 50;
    [SerializeField] private int minRoomSize = 8;
    [SerializeField] private int maxRooms = 5;
    [SerializeField] private int corridorWidth = 2;

    [Header("Prefabs")]
    [SerializeField] private GameObject outerWallPrefab;
    [SerializeField] private GameObject innerWallPrefab;
    [SerializeField] private GameObject hazardPrefab;

    [Header("Spawning & Hazards")]
    [SerializeField, Range(0f, 1f)] private float hazardDensity = 0.05f;
    [SerializeField] private float playerStartSafeRadius = 5f;

    private int[,] _map;
    private readonly List<Vector3> _spawnPoints = new List<Vector3>();
    private readonly List<Room> _rooms = new List<Room>();

    public List<Vector3> SpawnPoints => _spawnPoints;
    public Vector3 PlayerStartPosition { get; private set; }

    /// <summary>
    /// Represents a rectangular room in the level.
    /// </summary>
    private class Room
    {
        public readonly RectInt Bounds;
        public Vector2Int Center => new Vector2Int(Bounds.x + Bounds.width / 2, Bounds.y + Bounds.height / 2);

        public Room(int x, int y, int width, int height)
        {
            Bounds = new RectInt(x, y, width, height);
        }
    }

    public void GenerateLevel()
    {
        ClearLevel();

        // Pre-move player to origin to avoid any frame-1 collisions from a previous level layout.
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = Vector3.zero;
        }

        _map = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _map[x, y] = 1; // Initialize entire map with inner walls
            }
        }

        CreateBspRooms();
        CreateCorridors();
        CreateCoverPillars();

        // Set player start position and move the player there *before* instantiating walls.
        if (_rooms.Count > 0)
        {
            var startCenter = _rooms[0].Center;
            PlayerStartPosition = new Vector3(startCenter.x, startCenter.y, 0);
        }
        else
        {
            // Fallback if no rooms were generated
            PlayerStartPosition = new Vector3(width / 2f, height / 2f, 0);
            Debug.LogWarning("No rooms generated. Placing player at map center.");
        }

        if (player != null)
        {
            player.transform.position = PlayerStartPosition;
        }

        PopulateSpawnPoints();
        InstantiateLevel();
    }

    private void ClearLevel()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _spawnPoints.Clear();
        _rooms.Clear();
    }

    private void CreateBspRooms()
    {
        var partitions = new Queue<RectInt>();
        partitions.Enqueue(new RectInt(1, 1, width - 2, height - 2));

        while (partitions.Count > 0 && _rooms.Count < maxRooms)
        {
            var partition = partitions.Dequeue();
            if (partition.width < minRoomSize * 2 && partition.height < minRoomSize * 2)
            {
                CreateRoomInPartition(partition);
                continue;
            }

            bool splitHorizontal = (partition.width < partition.height) ? false : (partition.height < partition.width) ? true : Random.value > 0.5f;

            if (splitHorizontal)
            {
                if (partition.height < minRoomSize * 2)
                {
                    CreateRoomInPartition(partition);
                    continue;
                }
                int split = Random.Range(minRoomSize, partition.height - minRoomSize);
                partitions.Enqueue(new RectInt(partition.x, partition.y, partition.width, split));
                partitions.Enqueue(new RectInt(partition.x, partition.y + split, partition.width, partition.height - split));
            }
            else
            {
                if (partition.width < minRoomSize * 2)
                {
                    CreateRoomInPartition(partition);
                    continue;
                }
                int split = Random.Range(minRoomSize, partition.width - minRoomSize);
                partitions.Enqueue(new RectInt(partition.x, partition.y, split, partition.height));
                partitions.Enqueue(new RectInt(partition.x + split, partition.y, partition.width - split, partition.height));
            }
        }

        // Process any remaining partitions into rooms
        foreach (var partition in partitions)
        {
            CreateRoomInPartition(partition);
        }
    }

    private void CreateRoomInPartition(RectInt partition)
    {
        int roomWidth = Random.Range(minRoomSize, partition.width - 1);
        int roomHeight = Random.Range(minRoomSize, partition.height - 1);
        int roomX = partition.x + Random.Range(1, partition.width - roomWidth);
        int roomY = partition.y + Random.Range(1, partition.height - roomHeight);

        var newRoom = new Room(roomX, roomY, roomWidth, roomHeight);
        _rooms.Add(newRoom);
        CarveRoom(newRoom);
    }

    private void CreateCorridors()
    {
        for (int i = 0; i < _rooms.Count - 1; i++)
        {
            CarveCorridor(_rooms[i], _rooms[i + 1]);
        }
    }

    private void CreateCoverPillars()
    {
        foreach (var room in _rooms)
        {
            int pillarCount = Random.Range(1, 4);
            for (int i = 0; i < pillarCount; i++)
            {
                int pillarX = Random.Range(room.Bounds.x + 2, room.Bounds.xMax - 2);
                int pillarY = Random.Range(room.Bounds.y + 2, room.Bounds.yMax - 2);

                // Ensure pillar is not blocking a path (simple check)
                if (_map[pillarX, pillarY] == 0)
                {
                    _map[pillarX, pillarY] = 1;
                    // Optional: 2x2 pillars
                    if (Random.value > 0.5f && pillarX + 1 < width -1 && pillarY + 1 < height -1)
                    {
                        _map[pillarX + 1, pillarY] = 1;
                        _map[pillarX, pillarY + 1] = 1;
                        _map[pillarX + 1, pillarY + 1] = 1;
                    }
                }
            }
        }
    }

    private void CarveRoom(Room room)
    {
        for (int x = room.Bounds.x; x < room.Bounds.xMax; x++)
        {
            for (int y = room.Bounds.y; y < room.Bounds.yMax; y++)
            {
                _map[x, y] = 0; // 0 represents a floor tile
            }
        }
    }

    private void CarveCorridor(Room roomA, Room roomB)
    {
        Vector2Int centerA = roomA.Center;
        Vector2Int centerB = roomB.Center;

        // L-shaped corridor
        int startX = Mathf.Min(centerA.x, centerB.x);
        int endX = Mathf.Max(centerA.x, centerB.x);
        int startY = Mathf.Min(centerA.y, centerB.y);
        int endY = Mathf.Max(centerA.y, centerB.y);

        if (Random.value > 0.5f) // Horizontal then Vertical
        {
            CarveHorizontalCorridor(startX, endX, centerA.y);
            CarveVerticalCorridor(startY, endY, centerB.x);
        }
        else // Vertical then Horizontal
        {
            CarveVerticalCorridor(startY, endY, centerA.x);
            CarveHorizontalCorridor(startX, endX, centerB.y);
        }
    }

    private void CarveHorizontalCorridor(int startX, int endX, int y)
    {
        for (int x = startX; x <= endX; x++)
        {
            for (int i = 0; i < corridorWidth; i++)
            {
                if (y + i < height - 1) _map[x, y + i] = 0;
            }
        }
    }

    private void CarveVerticalCorridor(int startY, int endY, int x)
    {
        for (int y = startY; y <= endY; y++)
        {
            for (int i = 0; i < corridorWidth; i++)
            {
                if (x + i < width - 1) _map[x + i, y] = 0;
            }
        }
    }

    private void PopulateSpawnPoints()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_map[x, y] == 0)
                {
                    _spawnPoints.Add(new Vector3(x, y, 0));
                }
            }
        }
    }

    private void InstantiateLevel()
    {
        // Instantiate Walls
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isOuterWall = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                if (isOuterWall)
                {
                    if(outerWallPrefab != null)
                        Instantiate(outerWallPrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
                }
                else if (_map[x, y] == 1)
                {
                    if(innerWallPrefab != null)
                        Instantiate(innerWallPrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
                }
            }
        }

        // Instantiate Hazards
        foreach (var pos in _spawnPoints)
        {
            if (Vector3.Distance(pos, PlayerStartPosition) <= playerStartSafeRadius) continue;
            if (Random.value < hazardDensity)
            {
                if(hazardPrefab != null)
                    Instantiate(hazardPrefab, pos, Quaternion.identity, transform);
            }
        }
    }
}