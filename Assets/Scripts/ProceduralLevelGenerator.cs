using UnityEngine;
using UnityEngine.Tilemaps;
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

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Tile Assets")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase exitTile;

    [Header("Directional Wall Tiles")]
    [SerializeField] private TileBase wallTopTile;
    [SerializeField] private TileBase wallBottomTile;
    [SerializeField] private TileBase wallLeftTile;
    [SerializeField] private TileBase wallRightTile;
    [SerializeField] private TileBase wallCornerTLTile;
    [SerializeField] private TileBase wallCornerTRTile;
    [SerializeField] private TileBase wallCornerBLTile;
    [SerializeField] private TileBase wallCornerBRTile;
    [SerializeField] private TileBase wallFillTile;
    [SerializeField] private TileBase outerWallTile; // Keep for outer boundary

    [Header("Exit Gate Tiles")]
    [SerializeField] private TileBase exitGateLeftTile;
    [SerializeField] private TileBase exitGateMiddleTile;
    [SerializeField] private TileBase exitGateRightTile;

    [Header("Prefabs")]
    [SerializeField] private GameObject hazardPrefab;
    [SerializeField] private GameObject[] decoPrefabs;
    [SerializeField] private GameObject floorDoorPrefab;

    [Header("Spawning & Hazards")]
    [SerializeField, Range(0f, 1f)] private float hazardDensity = 0.05f;
    [SerializeField, Range(0f, 0.2f)] private float decoDensity = 0.05f;
    [SerializeField] private float playerStartSafeRadius = 5f;

    private int[,] _map;
    private readonly List<Vector3> _spawnPoints = new List<Vector3>();
    private readonly List<Room> _rooms = new List<Room>();

    public List<Vector3> SpawnPoints => _spawnPoints;
    public Vector3 PlayerStartPosition { get; private set; }

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
                _map[x, y] = 1;
            }
        }

        CreateBspRooms();
        CreateCorridors();
        CreateCoverPillars();

        if (_rooms.Count > 0)
        {
            var startCenter = _rooms[0].Center;
            PlayerStartPosition = new Vector3(startCenter.x, startCenter.y, 0);
        }
        else
        {
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
            if (child.GetComponent<Tilemap>() == null)
            {
                Destroy(child.gameObject);
            }
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
                if (room.Bounds.width < 5 || room.Bounds.height < 5) continue;

                int pillarX = Random.Range(room.Bounds.x + 2, room.Bounds.xMax - 3);
                int pillarY = Random.Range(room.Bounds.y + 2, room.Bounds.yMax - 3);

                if (pillarX + 1 < width && pillarY + 1 < height &&
                    _map[pillarX, pillarY] == 0 &&
                    _map[pillarX + 1, pillarY] == 0 &&
                    _map[pillarX, pillarY + 1] == 0 &&
                    _map[pillarX + 1, pillarY + 1] == 0)
                {
                    _map[pillarX, pillarY] = 1;
                    _map[pillarX + 1, pillarY] = 1;
                    _map[pillarX, pillarY + 1] = 1;
                    _map[pillarX + 1, pillarY + 1] = 1;
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
                _map[x, y] = 0;
            }
        }
    }

    private void CarveCorridor(Room roomA, Room roomB)
    {
        Vector2Int centerA = roomA.Center;
        Vector2Int centerB = roomB.Center;

        int startX = Mathf.Min(centerA.x, centerB.x);
        int endX = Mathf.Max(centerA.x, centerB.x);
        int startY = Mathf.Min(centerA.y, centerB.y);
        int endY = Mathf.Max(centerA.y, centerB.y);

        if (Random.value > 0.5f)
        {
            CarveHorizontalCorridor(startX, endX, centerA.y);
            CarveVerticalCorridor(startY, endY, centerB.x);
        }
        else
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
        if (floorTilemap == null || wallTilemap == null)
        {
            Debug.LogError("Tilemaps are not assigned in the inspector!");
            return;
        }

        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_map[x, y] == 1)
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), GetWallTileForPosition(x, y));
                }
            }
        }

        // Instantiate Hazards and Decor
        foreach (var pos in _spawnPoints)
        {
            if (Vector3.Distance(pos, PlayerStartPosition) <= playerStartSafeRadius) continue;

            // Instantiate Hazards
            if (Random.value < hazardDensity)
            {
                if (hazardPrefab != null)
                    Instantiate(hazardPrefab, pos, Quaternion.identity, transform);
            }

            // Instantiate Decor
            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);

            bool isAdjacentToWall = (x > 0 && _map[x - 1, y] == 1) ||
                                    (x < width - 1 && _map[x + 1, y] == 1) ||
                                    (y > 0 && _map[x, y - 1] == 1) ||
                                    (y < height - 1 && _map[x, y + 1] == 1);

            if (isAdjacentToWall && Random.value < decoDensity)
            {
                if (decoPrefabs != null && decoPrefabs.Length > 0)
                {
                    GameObject prefab = decoPrefabs[Random.Range(0, decoPrefabs.Length)];
                    Instantiate(prefab, pos, Quaternion.identity, transform);
                }
            }
        }

        // Generate Exit
        if (_rooms.Count > 0)
        {
            Vector2Int exitCoord = _rooms.Last().Center;
            Vector3Int exitPos = new Vector3Int(exitCoord.x, exitCoord.y, 0);

            // Paint the 5-tile wide exit gate
            wallTilemap.SetTile(exitPos + Vector3Int.left * 2, exitGateLeftTile);
            wallTilemap.SetTile(exitPos + Vector3Int.left, exitGateMiddleTile);
            wallTilemap.SetTile(exitPos, exitGateMiddleTile);
            wallTilemap.SetTile(exitPos + Vector3Int.right, exitGateMiddleTile);
            wallTilemap.SetTile(exitPos + Vector3Int.right * 2, exitGateRightTile);

            if (floorDoorPrefab != null)
            {
                GameObject door = Instantiate(floorDoorPrefab, exitPos, Quaternion.identity, transform);
                var doorScript = door.GetComponent<FloorExitDoor>();
                if (doorScript != null)
                {
                    var gatePositions = new List<Vector3Int>
                    {
                        exitPos + Vector3Int.left,
                        exitPos,
                        exitPos + Vector3Int.right
                    };
                    doorScript.Initialize(wallTilemap, gatePositions);
                }

                var collider = door.GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    collider.size = new Vector2(3, 1);
                }
            }
        }
    }

    private TileBase GetWallTileForPosition(int x, int y)
    {
        bool N = (y + 1 < height && _map[x, y + 1] == 0);
        bool S = (y - 1 >= 0 && _map[x, y - 1] == 0);
        bool E = (x + 1 < width && _map[x + 1, y] == 0);
        bool W = (x - 1 >= 0 && _map[x - 1, y] == 0);

        int adjacentFloors = (N ? 1 : 0) + (S ? 1 : 0) + (E ? 1 : 0) + (W ? 1 : 0);

        if ((N && S) || (E && W) || adjacentFloors >= 3)
        {
            return wallFillTile;
        }

        // Inward Corners
        if (S && E) return wallCornerTLTile;
        if (S && W) return wallCornerTRTile;
        if (N && E) return wallCornerBLTile;
        if (N && W) return wallCornerBRTile;

        // Straight Walls
        if (S) return wallTopTile;
        if (N) return wallBottomTile;
        if (E) return wallLeftTile;
        if (W) return wallRightTile;

        return wallFillTile;
    }
}
