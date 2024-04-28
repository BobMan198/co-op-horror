using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomInstance : MonoBehaviour
{
    [Header("Set In Editor")]   
    public FloorInstance floorPrefab;
    public GameObject wallPrefab;
    public List<RoomPrefabConfig> roomPrefabs;
    public GameObject playerSpawnerPrefab;
    public LayerMask navMeshLayerMask;
    public NavMeshSurface floorNavSurface;

    public Material floorMaterial;
    public Material wallMaterial;

    [Header("Set At Runtime")]
    public FloorInstance floor;

    public List<Vector3Int> horizontalWallPositions;
    public List<Vector3Int> verticalWallPositions;
    public List<WallInstanceInfo> walls;
    public List<WallInstanceInfo> unusedWalls;
    private GameObject wallParent;

    public GameObject ceiling;

    public RoomPrefabConfig roomPrefabConfig;
    public bool isRotated;
    private GameObject configParent;

    public List<RoomInstance> neighbors;

    private MonsterSpawn NetworkedMonsterSpawner;
    private CockroachManager roachManager;
    private GameRunner gameRunner;

    private bool isLargeRoom = false;
    private readonly float largeRoomWallHeightMultiplier = 3;

    private readonly Vector2 largeRoomMinSize = new Vector2(40,40);
    private float wallHeight = 6;
    private float wallThickness = 1;

    public void Setup(MonsterSpawn monsterSpawner, CockroachManager roachManager, GameRunner gameRunner)
    {
        floor = Instantiate(floorPrefab, transform);
        wallParent = new GameObject("Walls");
        wallParent.transform.parent = transform;
        configParent = new GameObject("Room pieces");
        configParent.transform.parent = transform;

        NetworkedMonsterSpawner = monsterSpawner;
        this.roachManager = roachManager;
        this.gameRunner = gameRunner;

        walls = new List<WallInstanceInfo>();
        unusedWalls = new List<WallInstanceInfo>();
    }
    public void CreateFloor(Vector2 bottomLeftCorner, Vector2 topRightCorner)
    {
        Vector3 bottomLeftV = new Vector3(bottomLeftCorner.x, 0, bottomLeftCorner.y);
        Vector3 bottomRightV = new Vector3(topRightCorner.x, 0, bottomLeftCorner.y);
        Vector3 topLeftV = new Vector3(bottomLeftCorner.x, 0, topRightCorner.y);
        Vector3 topRightV = new Vector3(topRightCorner.x, 0, topRightCorner.y);

        Vector3[] vertices = new Vector3[]
        {
            topLeftV,
            topRightV,
            bottomLeftV,
            bottomRightV
        };

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        int[] triangles = new int[]
        {
            0,
            1,
            2,
            2,
            1,
            3
        };
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        floor.meshFilter.mesh = mesh;
        floor.meshRenderer.material = floorMaterial;
        floor.AddComponent<MeshCollider>();
        //floor.AddComponent<NavMeshSurface>();
        //floor.GetComponent<NavMeshSurface>().layerMask = navMeshLayerMask;
    }

    public void SetWallPositions()
    {
        MeshRenderer floorRenderer = floor.meshRenderer;

        // the for loops should stay separate instead of doing the top and bottom wall in one for loop.
        // this prevents having to reorder the list when creating wall sections
        for (int row = (int)floorRenderer.bounds.min.x; row <= floorRenderer.bounds.max.x; row++)
        {
            var bottomWallPosition = new Vector3(row, 0, floorRenderer.bounds.min.z);
            AddHorizontalWallPosition(bottomWallPosition);
        }
        for (int row = (int)floorRenderer.bounds.min.x; row <= floorRenderer.bounds.max.x; row++)
        {
            var topWallPosition = new Vector3(row, 0, floorRenderer.bounds.max.z);
            AddHorizontalWallPosition(topWallPosition);
        }
        for (int col = (int)floorRenderer.bounds.min.z; col <= floorRenderer.bounds.max.z; col++)
        {
            var leftWallPosition = new Vector3(floorRenderer.bounds.min.x, 0, col);
            AddVerticalWallPosition(leftWallPosition);
        }
        for (int col = (int)floorRenderer.bounds.min.z; col <= floorRenderer.bounds.max.z; col++)
        {
            var rightWallPosition = new Vector3(floorRenderer.bounds.max.x, 0, col);
            AddVerticalWallPosition(rightWallPosition);
        }


        if(floorRenderer.bounds.size.x > largeRoomMinSize.x && floorRenderer.bounds.size.z > largeRoomMinSize.y){
            isLargeRoom = true;
        }
    }

    public void AddHorizontalWallPosition(Vector3 position)
    {
        Vector3Int point = Vector3Int.CeilToInt(position);

        RoomInstance neighborWithDuplicate = neighbors.FirstOrDefault(n => n.horizontalWallPositions.Contains(point));
        if (neighborWithDuplicate != null)
        {
            neighborWithDuplicate.horizontalWallPositions.Remove(point);
        }
        else
        {
            horizontalWallPositions.Add(point);
        }
    }

    public void AddVerticalWallPosition(Vector3 position)
    {
        Vector3Int point = Vector3Int.CeilToInt(position);

        RoomInstance neighborWithDuplicate = neighbors.FirstOrDefault(n => n.verticalWallPositions.Contains(point));
        if (neighborWithDuplicate != null)
        {
            neighborWithDuplicate.verticalWallPositions.Remove(point);
        }
        else
        {
            verticalWallPositions.Add(point);
        }
    }

    public void GenerateWalls()
    {
        List<WallSection> wallSections = ConvertToSections(horizontalWallPositions, false);

        foreach (var wallSection in wallSections)
        {
            CreateWall(wallSection);
        }

        wallSections = ConvertToSections(verticalWallPositions, true);

        foreach (var wallSection in wallSections)
        {
            CreateWall(wallSection);
        }

        SetWallInfo();
    }

    private List<WallSection> ConvertToSections(List<Vector3Int> wallPositions, bool isVertical)
    {
        List<WallSection> sections = new List<WallSection>();

        if (wallPositions.Count == 0)
        {
            return sections;
        }

        Vector3Int sectionStart = wallPositions[0];
        int currentIndex = 1;
        List<Vector3Int> sectionPositions = new List<Vector3Int>() { wallPositions[0] };

        while (currentIndex < wallPositions.Count)
        {
            Vector3Int currentPosition = wallPositions[currentIndex];
            Vector3Int lastPosition = wallPositions[currentIndex - 1];
            // check if we haven't moved to a new horizontal section
            // and if we aren't skipping y values due to a door
            int nonDirectionalOffset = isVertical ? Mathf.Abs(currentPosition.x - sectionStart.x) : Mathf.Abs(currentPosition.z - sectionStart.z);
            int offset = isVertical ? Mathf.Abs(currentPosition.z - lastPosition.z) : Mathf.Abs(currentPosition.x - lastPosition.x);
            if (nonDirectionalOffset == 0 && offset == 1)
            {
                sectionPositions.Add(currentPosition);
            }
            else
            {
                WallSection section = new WallSection();
                Vector3 start = sectionPositions[0];
                Vector3 end = sectionPositions[sectionPositions.Count - 1];
                section.position = (start + end) / 2;
                section.size = isVertical ? new Vector3(wallThickness, wallHeight, sectionPositions.Count) : new Vector3(sectionPositions.Count, wallHeight, wallThickness);
                sections.Add(section);

                sectionStart = wallPositions[currentIndex];
                sectionPositions = new List<Vector3Int>() { sectionStart };
            }


            if (currentIndex == wallPositions.Count - 1)
            {
                WallSection section = new WallSection();
                Vector3 start = sectionPositions[0];
                Vector3 end = sectionPositions[sectionPositions.Count - 1];
                section.position = (start + end) / 2;
                section.size = isVertical ? new Vector3(wallThickness, wallHeight, sectionPositions.Count) : new Vector3(sectionPositions.Count, wallHeight, wallThickness);
                sections.Add(section);
            }
            currentIndex++;
        }

        return sections;
    }

    private void CreateWall(WallSection section)
    {
        var wallGameobject = Instantiate(wallPrefab, section.position, Quaternion.identity, wallParent.transform);

        var wall = wallGameobject.AddComponent<WallInstanceInfo>();

        if(isLargeRoom){
            section.size.y = wallHeight * largeRoomWallHeightMultiplier;
            wall.transform.localScale = new Vector3(0,section.size.y, 0);
        }
        Vector3 position = section.position;
        position.y = transform.position.y + (wall.transform.localScale.y / 2);
        wall.transform.position = position;
        wall.transform.localScale = section.size;

        var meshRenderer = wall.GetComponent<MeshRenderer>();
        meshRenderer.material = wallMaterial;

        float width = wall.transform.localScale.x > wall.transform.localScale.z ? wall.transform.localScale.x : wall.transform.localScale.z;
        meshRenderer.material.mainTextureScale = new Vector2(width, wall.transform.localScale.y);
        var surface = wall.GetComponent<NavMeshSurface>();
        surface.defaultArea = 1;
        //wall.AddComponent<NavMeshModifier>();
        wallGameobject.layer = 11;

        walls.Add(wall);
        unusedWalls.Add(wall);
    }

    public void Populate()
    {
        var selectedConfig = SelectConfig();

        if (roomPrefabConfig == null)
        {
            return;
        }

        SpawnRoomEntities();
        SpawnRoomPieces();

        if (DungeonCreator.SpawnedRoomCount.TryGetValue(selectedConfig, out int spawnCount))
        {
            DungeonCreator.SpawnedRoomCount[selectedConfig] = spawnCount + 1;
        }
        else
        {
            DungeonCreator.SpawnedRoomCount[selectedConfig] = 1;
        }
    }

    private RoomPrefabConfig SelectConfig()
    {
        Dictionary<RoomPrefabConfig, bool> viablePrefabs = GetViableRooms(floor.meshRenderer.bounds.size);

        if (viablePrefabs.Count == 0)
        {
            return null;
        }

        int randomIndex = GameRunner.RandomSeed.Next(0, viablePrefabs.Count);

        var config = viablePrefabs.ElementAt(randomIndex).Key;
        isRotated = viablePrefabs.ElementAt(randomIndex).Value;

        roomPrefabConfig = Instantiate(config, floor.meshRenderer.bounds.center, Quaternion.identity, configParent.transform);
        if (isRotated)
        {
            roomPrefabConfig.transform.localRotation = Quaternion.Euler(0, 90, 0);
        }

        return config;
    }

    private Dictionary<RoomPrefabConfig, bool> GetViableRooms(Vector3 floorSize)
    {
        Dictionary<RoomPrefabConfig, bool> viablePrefabs = new Dictionary<RoomPrefabConfig, bool>();

        foreach (var roomPrefab in roomPrefabs)
        {
            bool hasSpawned = DungeonCreator.SpawnedRoomCount.TryGetValue(roomPrefab, out int roomSpawnCount);

            bool canSpawnMore = true;
            if (hasSpawned)
            {
                canSpawnMore = roomSpawnCount < roomPrefab.maxToSpawn;
            }
            // if the x and z are swapped, does it fit, if so, spawn this prefab rotated 90 degrees in the y

            if (floorSize.x >= roomPrefab.minSize.x &&
                floorSize.z >= roomPrefab.minSize.z &&
                (floorSize.x <= roomPrefab.maxSize.x || roomPrefab.maxSize.x == 0) &&
                (floorSize.z <= roomPrefab.maxSize.z || roomPrefab.maxSize.z == 0) &&
                canSpawnMore
            )
            {
                viablePrefabs.Add(roomPrefab, false);
            }
            else
            {
                if (floorSize.x >= roomPrefab.minSize.z &&
                floorSize.z >= roomPrefab.minSize.x &&
                (floorSize.x <= roomPrefab.maxSize.z || roomPrefab.maxSize.z == 0) &&
                (floorSize.z <= roomPrefab.maxSize.x || roomPrefab.maxSize.x == 0) &&
                canSpawnMore)
                {
                    viablePrefabs.Add(roomPrefab, true);
                }
            }
        }

        return viablePrefabs;
    }

    private void SpawnRoomEntities()
    {
        if (roomPrefabConfig.playerSpawnLocation != null && DungeonCreator.PlayerSpawnRoom == null)
        {
            var playerSpawnRoomInstance = Instantiate(playerSpawnerPrefab, roomPrefabConfig.playerSpawnLocation.transform.position, Quaternion.identity, transform);
            DungeonCreator.PlayerSpawnRoom = playerSpawnRoomInstance.transform;
        }
        // Don't spawn monsters if gameRunner is null, as we are in the test scene and they will error out
        if (gameRunner == null)
        {
            return;
        }

        if (roomPrefabConfig.shadowMonsterSpawnLocation != null && NetworkedMonsterSpawner.n_monsterSpawned.Value == false)
        {
            StartCoroutine(SpawnMonster());
            //roomPrefabConfig.shadowMonsterSpawnLocation.SetParent(null);
            //StartCoroutine(SpawnMonster(roomPrefabConfig.shadowMonsterSpawnLocation.transform.position));
            //StartCoroutine(SpawnMonster(roomPrefabConfig.transform.position));
        }

        if (roomPrefabConfig.cockroachSpawnLocations != null && roomPrefabConfig.cockroachSpawnLocations.Count > 0)
        {
            roachManager.dungeonfloorInstance = floor.gameObject;
            roachManager.cockroachSpawners = roomPrefabConfig.cockroachSpawnLocations;
            roachManager.SpawnRoachColonyServerRpc();
        }
    }

    private void SpawnRoomPieces()
    {
        if (roomPrefabConfig.roomPieces == null)
        {
            return;
        }

        foreach (var roomPiece in roomPrefabConfig.roomPieces)
        {
            if (roomPiece.spawnStyle == MoveableRoomPiece.SpawnStyle.AttachedToWall)
            {
                var pieceInstance = Instantiate(roomPiece.prefab, Vector3.zero, Quaternion.identity, configParent.transform);
                var wallInfo = FindWallSpawn(pieceInstance);

                if(wallInfo != null)
                {
                    pieceInstance.transform.position = wallInfo.MeshRenderer.bounds.center;

                    if (wallInfo.wallside == WallSide.left)
                    {
                        pieceInstance.transform.rotation = Quaternion.Euler(new Vector3(0, -180, 0));
                    }

                    if (wallInfo.wallside == WallSide.right)
                    {
                        pieceInstance.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    }

                    if (wallInfo.wallside == WallSide.top)
                    {
                        pieceInstance.transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));
                    }

                    if (wallInfo.wallside == WallSide.bottom)
                    {
                        pieceInstance.transform.rotation = Quaternion.Euler(new Vector3(0, -270, 0));
                    }
                }
            }
        }
    }

    private WallInstanceInfo FindWallSpawn(GameObject pieceToSpawn)
    {
        BoxCollider boxCollider = pieceToSpawn.GetComponent<BoxCollider>();
        Bounds bounds = boxCollider.bounds;

        foreach (var wall in unusedWalls.ToList())
        {
            MeshRenderer wallMeshRenderer = wall.GetComponent<MeshRenderer>();

            bool useXWidth = wallMeshRenderer.bounds.size.x > wallMeshRenderer.bounds.size.z;

            bool isValid = false;
            
            // this method assumes the prefab is set up to always have 0 degrees of rotation when it is on the right wall.
            // so, when checking if we fit horizontally (meaning we are aligning on the top or bottom), we need to check the z axis of the prefab,
            // as that is the long side of the prefab

            if (useXWidth)
            {
                if (wallMeshRenderer.bounds.size.x - (2 * wallThickness) > bounds.size.z &&
                    wallMeshRenderer.bounds.size.y > bounds.size.y
                )
                {
                    isValid = true;
                }
            }
            else
            {
                if (wallMeshRenderer.bounds.size.z - (2 * wallThickness) > bounds.size.z &&
                    wallMeshRenderer.bounds.size.y > bounds.size.y
                )
                {
                    isValid = true;
                }
            }

            if (isValid)
            {
                unusedWalls.Remove(wall);
                return wall;
            }
        }

        return null;
    }

    private void SetWallInfo()
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach(var wall in walls)
        {
            var wallPos = wall.transform.position;

            if(wallPos.x < min.x)
            {
                min.x = wallPos.x;
            }

            if(wallPos.x > max.x)
            {
                max.x = wallPos.x;
            }

            if (wallPos.z < min.z)
            {
                min.z = wallPos.z;
            }

            if (wallPos.z > max.z)
            {
                max.z = wallPos.z;
            }
        }

        foreach(var wall in walls)
        {
            if(wall.transform.position.x == min.x)
            {
                wall.wallside = WallSide.left;
            }

            if (wall.transform.position.x == max.x)
            {
                wall.wallside = WallSide.right;
            }

            if (wall.transform.position.z == min.z)
            {
                wall.wallside = WallSide.bottom;
            }

            if (wall.transform.position.z == max.z)
            {
                wall.wallside = WallSide.top;
            }
        }
    }

    class WallInfo
    {
        public Vector3 centerPosition;
        public bool isRotated;
    }


    IEnumerator SpawnMonster()
    {
        yield return new WaitForSeconds(5);
        var position = GameObject.FindGameObjectWithTag("ShadowMonsterSpawn").transform.position;
        NetworkedMonsterSpawner.SpawnMonsterServerRpc(position);
    }
}

public class WallSection
{
    public Vector3 position;
    public Vector3 size;
}
