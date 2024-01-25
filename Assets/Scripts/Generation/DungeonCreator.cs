using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.VisualScripting;
using DolbyIO.Comms;
using System.Linq;
using Random = UnityEngine.Random;

public class DungeonCreator : NetworkBehaviour
{
    public static DungeonCreator Instance;
    public MonsterSpawn NetworkedMonsterSpawner;
    public NavMeshSurface navMeshSurface;

    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
    public int maxIterations;
    public int corridorWidth;
    public Material floorMaterial;
    public Material wallMaterial;
    public GameObject cafeteriaPrefab;
    public GameObject playerSpawnerPrefab;
    public GameObject pillarAndFountainPrefab;
    public GameObject shadowMonsterSpawnerPrefab;
    private bool playerSpawnerSpawned = false;
    private bool pillarAndFountainSpawned = false;
    private bool cafeteriaSpawned = false;
    private bool shadowMonsterSpawned = false;
    private float pillarAndFountainCount = 0;
    private float cafeteriaCount = 0;

    public NetworkVariable<int> GameSeed = new NetworkVariable<int>();
        
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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            //GameSeed.Value = Random.Range(0, 1000);
            GameSeed.Value = 1;
        }
    }

    public void CreateDungeon()
    {
        Random.InitState(GameSeed.Value);

        NetworkedMonsterSpawner.DestroyMonsterServerRpc();
        playerSpawnerSpawned = false;
        cafeteriaSpawned = false;
        shadowMonsterSpawned = false;
        pillarAndFountainSpawned = false;
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

        GenerateRoomObjects();
    }

    private void GenerateRoomObjects()
    {
        int iterationCount = 0;

        var roomsList = isDebugging ? debugRooms : rooms;

        for (int i = 0; i < roomsList.Count; i++)
        {
            CreateMesh(roomsList[i].BottomLeftAreaCorner, roomsList[i].TopRightAreaCorner);
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
    private void CreateMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner)
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

        Vector3 floorSize;
        GameObject dungeonFloor = new GameObject("Mesh" + bottomLeftCorner, typeof(MeshFilter), typeof(MeshRenderer));

        dungeonFloor.transform.position = Vector3.zero;
        dungeonFloor.transform.localScale = Vector3.one;
        dungeonFloor.GetComponent<MeshFilter>().mesh = mesh;
        dungeonFloor.GetComponent<MeshRenderer>().material = floorMaterial;
        dungeonFloor.AddComponent<MeshCollider>();
        dungeonFloor.AddComponent<NavMeshSurface>();
        dungeonFloor.transform.parent = transform;

        floorSize = dungeonFloor.GetComponent<MeshRenderer>().bounds.size;

        if (floorSize.x >= 48 && floorSize.z >= 48 && !cafeteriaSpawned)
        {
            CreateCafeteria(dungeonFloor, dungeonFloor.GetComponent<MeshCollider>().bounds.center, cafeteriaPrefab);
            cafeteriaCount++;

            if (cafeteriaCount >= 2)
            {
                cafeteriaSpawned = true;
            }
        }

        if (floorSize.x >= 20 && floorSize.z >= 20 && !playerSpawnerSpawned)
        {
            CreatePlayerSpawner(dungeonFloor, dungeonFloor.GetComponent<MeshCollider>().bounds.center, playerSpawnerPrefab);
            playerSpawnerSpawned = true;
        }

        if (floorSize.x >= 20 && floorSize.z >= 20 && floorSize.x <= 38 && floorSize.z <= 38 && !pillarAndFountainSpawned)
        {
            CreatePillarAndFountain(dungeonFloor, dungeonFloor.GetComponent<MeshCollider>().bounds.center, pillarAndFountainPrefab);
            pillarAndFountainCount++;

            if (pillarAndFountainCount >= 2)
            {
                pillarAndFountainSpawned = true;
            }
        }

        if (floorSize.x >= 20 && floorSize.z >= 20 && !shadowMonsterSpawned && !dungeonFloor.GetComponentInChildren<MovePlayersOnGameStart>())
        {
            CreateShadowMonsterSpawner(dungeonFloor, dungeonFloor.GetComponent<MeshCollider>().bounds.center, shadowMonsterSpawnerPrefab);
            shadowMonsterSpawned = true;
        }

        for (int row = (int)bottomLeftV.x; row <= bottomRightV.x; row++)
        {
            var wallPosition = new Vector3(row, 0, bottomLeftV.z);
            AddWallPositionToList(wallPosition, possibleWallHorizontalPosition, possibleDoorHorizontalPosition);
        }
        for (int row = (int)topLeftV.x; row <= topRightCorner.x; row++)
        {
            var wallPosition = new Vector3(row, 0, topRightV.z);
            AddWallPositionToList(wallPosition, possibleWallHorizontalPosition, possibleDoorHorizontalPosition);
        }
        for (int col = (int)bottomLeftV.z; col <= topLeftV.z; col++)
        {
            var wallPosition = new Vector3(bottomLeftV.x, 0, col);
            AddWallPositionToList(wallPosition, possibleWallVerticalPosition, possibleDoorVerticalPosition);
        }
        for (int col = (int)bottomRightV.z; col <= topRightV.z; col++)
        {
            var wallPosition = new Vector3(bottomRightV.x, 0, col);
            AddWallPositionToList(wallPosition, possibleWallVerticalPosition, possibleDoorVerticalPosition);
        }
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

    private void CreateCafeteria(GameObject cafeteriaParent, Vector3 cafeteriaPosition, GameObject cafeteriaPrefab)
    {
        var cafeteria = Instantiate(cafeteriaPrefab, cafeteriaPosition, Quaternion.identity, cafeteriaParent.transform);
        cafeteria.transform.rotation = Quaternion.Euler(-90, 0, 0);
        cafeteria.transform.position = new Vector3(cafeteriaPosition.x - 101, 32, cafeteriaPosition.z);
    }

    private void CreatePlayerSpawner(GameObject dungeonFloor, Vector3 playerSpawnerPosition, GameObject playerSpawnerPrefab)
    {
        var playerSpawner = Instantiate(playerSpawnerPrefab, playerSpawnerPosition, Quaternion.identity, dungeonFloor.transform);
        playerSpawner.transform.rotation = Quaternion.Euler(0, 0, 0);
        playerSpawner.transform.position = new Vector3(playerSpawnerPosition.x, 2, playerSpawnerPosition.z);
    }

    private void CreatePillarAndFountain(GameObject dungeonFloor, Vector3 pillarAndFountainPosition, GameObject pillarAndFountainPrefab)
    {
        var playerSpawner = Instantiate(pillarAndFountainPrefab, pillarAndFountainPosition, Quaternion.identity, dungeonFloor.transform);
        playerSpawner.transform.rotation = Quaternion.Euler(0, 0, 0);
        playerSpawner.transform.position = new Vector3(pillarAndFountainPosition.x, -0.5f, pillarAndFountainPosition.z);
    }
    private void CreateShadowMonsterSpawner(GameObject dungeonFloor, Vector3 shadowMonsterSpawnerPosition, GameObject shadowMonsterSpawnerPrefab)
    {
        shadowMonsterSpawnerPosition = new Vector3(shadowMonsterSpawnerPosition.x + 5, 2, shadowMonsterSpawnerPosition.z);
        var shadowMonsterSpawner = Instantiate(shadowMonsterSpawnerPrefab, shadowMonsterSpawnerPosition, Quaternion.Euler(0, 0, 0), dungeonFloor.transform);
        if(NetworkedMonsterSpawner.n_monsterSpawned.Value == false)
        {
            NetworkedMonsterSpawner.SpawnMonsterServerRpc(shadowMonsterSpawner.transform.position);
        }
    }
}
