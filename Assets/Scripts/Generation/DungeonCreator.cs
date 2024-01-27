using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.VisualScripting;
using DolbyIO.Comms;
using System.Linq;

public class DungeonCreator : NetworkBehaviour
{
    public static DungeonCreator Instance;
    public MonsterSpawn NetworkedMonsterSpawner;
    public NavMeshSurface navMeshSurface;
    public List<RoomPrefabConfig> roomPrefabs;
    public GameObject playerSpawnerPrefab;

    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
    public int maxIterations;
    public int corridorWidth;
    public Material floorMaterial;
    public Material wallMaterial;
    public GameRunner gameRunner;
        
    [Range(0.0f, 0.3f)]
    public float roomBottomCornerModifier;
    [Range(0.7f, 1f)]
    public float roomTopCornerModifier;
    [Range(0f, 2f)]
    public int roomOffset;
    public GameObject wallPrefab;
    List<Vector3Int> possibleDoorVerticalPosition;
    List<Vector3Int> possibleDoorHorizontalPosition;
    List<Vector3Int> possibleWallVerticalPosition;
    List<Vector3Int> possibleWallHorizontalPosition;

    private GameObject wallParent;
    private List<Node> rooms;
    private Dictionary<RoomPrefabConfig, int> spawnMapping;

    [SerializeField]
    private bool isDebugging;
    private List<Node> debugRooms = new List<Node>()
    {
        new RoomNode(new Vector2Int(0,0), new Vector2Int(10,10), null, 0),
        new RoomNode(new Vector2Int(10,4), new Vector2Int(15,6), null, 0),
    };

    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public void CreateDungeon()
    {
        if(NetworkManager.Singleton != null)
        {
            NetworkedMonsterSpawner.DestroyMonsterServerRpc();
        }

        DestroyAllChildren();

        DungeonGenerator generator = new DungeonGenerator(dungeonWidth, dungeonLength);
        rooms = generator.CalculateDungeon(maxIterations,
            roomWidthMin,
            roomLengthMin,
            roomBottomCornerModifier,
            roomTopCornerModifier,
            roomOffset,
            corridorWidth);

        wallParent = new GameObject("WallParent");
        wallParent.transform.parent = transform;

        possibleDoorVerticalPosition = new List<Vector3Int>();
        possibleDoorHorizontalPosition = new List<Vector3Int>();
        possibleWallVerticalPosition = new List<Vector3Int>();
        possibleWallHorizontalPosition = new List<Vector3Int>();

        spawnMapping = new Dictionary<RoomPrefabConfig, int>();

        GenerateRoomObjects();
    }

    private void GenerateRoomObjects()
    {
        var roomsList = isDebugging ? debugRooms : rooms;

        for (int i = 0; i < roomsList.Count; i++)
        {
            GameObject floor = CreateFloor(roomsList[i].BottomLeftAreaCorner, roomsList[i].TopRightAreaCorner);
            SetWallPositions(floor);
            SpawnRandomRoom(floor);
        }

        List<WallSection> wallSections = ConvertToSections(possibleWallHorizontalPosition, false);
        wallSections.AddRange(ConvertToSections(possibleWallVerticalPosition, true));

        foreach (var wallSection in wallSections)
        {
            CreateWall(wallSection);
        }

        StartCoroutine(BuildNavMesh());
    }

    private List<WallSection> ConvertToSections(List<Vector3Int> wallPositions, bool isVertical)
    {
        List<WallSection> sections = new List<WallSection>();

        if(wallPositions.Count == 0)
        {
            return sections;
        }

        Vector3Int sectionStart = wallPositions[0];
        int currentIndex = 1;
        List<Vector3Int> sectionPositions = new List<Vector3Int>() { wallPositions[0] };

        while (currentIndex < wallPositions.Count)
        {
            Vector3Int currentPosition = wallPositions[currentIndex];
            Vector3Int lastPosition = wallPositions[currentIndex-1];
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
                section.size = isVertical ? new Vector3(1, 12, sectionPositions.Count)  : new Vector3(sectionPositions.Count, 12, 1);
                sections.Add(section);

                sectionStart = wallPositions[currentIndex];
                sectionPositions = new List<Vector3Int>() { sectionStart};
            }


            if(currentIndex == wallPositions.Count - 1)
            {
                WallSection section = new WallSection();
                Vector3 start = sectionPositions[0];
                Vector3 end = sectionPositions[sectionPositions.Count - 1];
                section.position = (start + end) / 2;
                section.size = isVertical ? new Vector3(1, 12, sectionPositions.Count) : new Vector3(sectionPositions.Count, 12, 1);
                sections.Add(section);
            }
            currentIndex++;
        }

        return sections;
    }

    private void CreateWall(WallSection section)
    {
        var wall = Instantiate(wallPrefab, section.position, Quaternion.identity, wallParent.transform);
        wall.transform.localScale = section.size;

        wall.GetComponent<MeshRenderer>().material = wallMaterial;
        var surface = wall.AddComponent<NavMeshSurface>();
        surface.defaultArea = 1;
        wall.AddComponent<NavMeshModifier>();
        wall.layer = 11;
    }

    private IEnumerator BuildNavMesh()
    {
        yield return new WaitForSeconds(5);
        navMeshSurface.BuildNavMesh();
    }

    class WallSection
    {
        public Vector3 position;
        public Vector3 size;
    }

    public Vector3 GetAveragePosition(List<Vector3Int> positions)
    {
        Vector3 average = Vector3.zero;

        foreach(var pos in positions)
        {
            average += pos;
        }

        return average / positions.Count;
    }
    private GameObject CreateFloor(Vector2 bottomLeftCorner, Vector2 topRightCorner)
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

        GameObject dungeonFloor = new GameObject("Mesh" + bottomLeftCorner, typeof(MeshFilter), typeof(MeshRenderer));
        dungeonFloor.transform.position = Vector3.zero;
        dungeonFloor.transform.localScale = Vector3.one;
        dungeonFloor.GetComponent<MeshFilter>().mesh = mesh;
        dungeonFloor.GetComponent<MeshRenderer>().material = floorMaterial;
        dungeonFloor.AddComponent<MeshCollider>();
        dungeonFloor.AddComponent<NavMeshSurface>();
        dungeonFloor.transform.parent = transform;

        return dungeonFloor;
    }

    private void SpawnRandomRoom(GameObject dungeonFloor)
    {
        MeshRenderer floorRenderer = dungeonFloor.GetComponent<MeshRenderer>();
        Vector3 floorSize = floorRenderer.bounds.size;


        List<RoomPrefabConfig> viablePrefabs = new List<RoomPrefabConfig>();

        foreach(var roomPrefab in roomPrefabs)
        {
            bool hasSpawned = spawnMapping.TryGetValue(roomPrefab, out int roomSpawnCount);

            bool canSpawnMore = true;
            if (hasSpawned)
            {
                canSpawnMore = roomSpawnCount < roomPrefab.maxToSpawn;
            }

            if(floorSize.x > roomPrefab.minSize.x &&
                floorSize.z > roomPrefab.minSize.z &&
                floorSize.x < roomPrefab.maxSize.z &&
                floorSize.z < roomPrefab.maxSize.z &&
                canSpawnMore
            )
            {
                viablePrefabs.Add(roomPrefab);
            }
        }

        if(viablePrefabs.Count == 0)
        {
            return;
        }

        int randomIndex = GameRunner.RandomSeed.Next(0, viablePrefabs.Count);
        RoomPrefabConfig configToSpawn = viablePrefabs[randomIndex];

        var prefabInstance = Instantiate(configToSpawn, floorRenderer.bounds.center, Quaternion.identity);
        prefabInstance.transform.parent = transform;

        if(prefabInstance.shadowMonsterSpawnLocation != null && gameRunner != null)
        {
            if (NetworkedMonsterSpawner.n_monsterSpawned.Value == false)
            {
                NetworkedMonsterSpawner.SpawnMonsterServerRpc(prefabInstance.shadowMonsterSpawnLocation.transform.position);
            }
        }

        if (prefabInstance.playerSpawnLocation != null)
        {
            Instantiate(playerSpawnerPrefab, prefabInstance.playerSpawnLocation.transform.position, Quaternion.identity, dungeonFloor.transform);
        }

        if (spawnMapping.TryGetValue(configToSpawn, out int spawnCount))
        {
            spawnMapping[configToSpawn] = spawnCount + 1;
        }
        else
        {
            spawnMapping[configToSpawn] = 1;
        }
    }

    private void SetWallPositions(GameObject dungeonFloor)
    {
        MeshRenderer floorRenderer = dungeonFloor.GetComponent<MeshRenderer>();


        for (int row = (int)floorRenderer.bounds.min.x; row <= floorRenderer.bounds.max.x; row++)
        {
            var bottomWallPosition = new Vector3(row, 0, floorRenderer.bounds.min.z);
            AddWallPositionToList(bottomWallPosition, possibleWallHorizontalPosition, possibleDoorHorizontalPosition);

            var topWallPosition = new Vector3(row, 0, floorRenderer.bounds.max.z);
            AddWallPositionToList(topWallPosition, possibleWallHorizontalPosition, possibleDoorHorizontalPosition);

        }
        for (int col = (int)floorRenderer.bounds.min.z; col <= floorRenderer.bounds.max.z; col++)
        {
            var leftWallPosition = new Vector3(floorRenderer.bounds.min.x, 0, col);
            AddWallPositionToList(leftWallPosition, possibleWallVerticalPosition, possibleDoorVerticalPosition);

            var rightWallPosition = new Vector3(floorRenderer.bounds.max.x, 0, col);
            AddWallPositionToList(rightWallPosition, possibleWallVerticalPosition, possibleDoorVerticalPosition);
        }
    }
    
    private void AddWallPositionToList(Vector3 wallPosition, List<Vector3Int> wallList, List<Vector3Int> doorList)
    {
        Vector3Int point = Vector3Int.CeilToInt(wallPosition);
        if (wallList.Contains(point))
        {
            doorList.Add(point);
            wallList.Remove(point);
        }
        else
        {
            wallList.Add(point);
        }
    }

    private void DestroyAllChildren()
    {
        while(transform.childCount != 0)
        {
            foreach(Transform item in transform)
            {
                DestroyImmediate(item.gameObject);
            }
        }
    }
}
