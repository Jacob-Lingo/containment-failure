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

    [Header("Prefabs")]
    [SerializeField] private GameObject hazardPrefab;

    [Header("Spawning & Hazards")]
    [SerializeField, Range(0f, 1f)] private float hazardDensity = 0.05f;
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
                int pillarX = Random.Range(room.Bounds.x + 2, room.Bounds.xMax - 2);
                int pillarY = Random.Range(room.Bounds.y + 2, room.Bounds.yMax - 2);

                if (_map[pillarX, pillarY] == 0)
                {
                    _map[pillarX, pillarY] = 1;
                    if (Random.value > 0.5f && pillarX + 1 < width - 1 && pillarY + 1 < height - 1)
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
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                if (_map[x, y] == 0)
                {
                    floorTilemap.SetTile(tilePos, floorTile);
                }
                else
                {
                    bool isOuterWall = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    wallTilemap.SetTile(tilePos, isOuterWall ? outerWallTile : GetWallTileForPosition(x, y));
                }
            }
        }

        // Instantiate Hazards
        foreach (var pos in _spawnPoints)
        {
            if (Vector3.Distance(pos, PlayerStartPosition) <= playerStartSafeRadius) continue;
            if (Random.value < hazardDensity)
            {
                if (hazardPrefab != null)
                    Instantiate(hazardPrefab, pos, Quaternion.identity, transform);
            }
        }
    }

    private TileBase GetWallTileForPosition(int x, int y)
    {
        bool hasFloorNorth = (y + 1 < height && _map[x, y + 1] == 0);
        bool hasFloorSouth = (y - 1 >= 0 && _map[x, y - 1] == 0);
        bool hasFloorEast = (x + 1 < width && _map[x + 1, y] == 0);
        bool hasFloorWest = (x - 1 >= 0 && _map[x - 1, y] == 0);

        // Corners
        if (hasFloorSouth && hasFloorEast) return wallCornerTLTile;
        if (hasFloorSouth && hasFloorWest) return wallCornerTRTile;
        if (hasFloorNorth && hasFloorEast) return wallCornerBLTile;
        if (hasFloorNorth && hasFloorWest) return wallCornerBRTile;

        // Edges
        if (hasFloorSouth) return wallTopTile;
        if (hasFloorNorth) return wallBottomTile;
        if (hasFloorEast) return wallLeftTile;
        if (hasFloorWest) return wallRightTile;

        // If no adjacent floor, it's a solid wall
        return wallFillTile;
    }
}