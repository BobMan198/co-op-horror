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
    [Header("Required references")]
    public MonsterSpawn NetworkedMonsterSpawner;
    public NavMeshSurface navMeshSurface;
    public GameRunner gameRunner;
    public CockroachManager roachManager;
    public RoomInstance roomPrefab;


    [Header("Generation Settings")]
    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
    public int maxIterations;
    public int corridorWidth;
    [Range(0.0f, 0.3f)]
    public float roomBottomCornerModifier;
    [Range(0.7f, 1f)]
    public float roomTopCornerModifier;
    [Range(0f, 2f)]
    public int roomOffset;

    [Header("Runtime global variables")]
    public GameObject generatedDungeonParent;
    public List<RoomInstance> generatedRooms;
    private List<Node> rooms;
    public static Dictionary<RoomPrefabConfig, int> SpawnedRoomCount;
    public static Transform PlayerSpawnRoom;

    [Header("Debug only")]
    [SerializeField]
    private bool isDebugging;
    public List<DebugRoom> debugRooms;

    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public void CreateDungeon()
    {
        DestroyGeneratedDungeon();

        if (isDebugging)
        {
            rooms = new List<Node>();
            rooms.AddRange(debugRooms.Select(r => new RoomNode(r.bottomLeft, r.topRight, null, 0)).ToList());
        }
        else
        {
            DungeonGenerator generator = new DungeonGenerator(dungeonWidth, dungeonLength);
            rooms = generator.CalculateDungeon(maxIterations,
                roomWidthMin,
                roomLengthMin,
                roomBottomCornerModifier,
                roomTopCornerModifier,
                roomOffset,
                corridorWidth);
        }

        generatedDungeonParent = new GameObject("Dungeon container");

        generatedRooms = new List<RoomInstance>();
        SpawnedRoomCount = new Dictionary<RoomPrefabConfig, int>();

        GenerateRooms();
    }

    private void GenerateRooms()
    {
        // TODO: guarantee player room spawns

        // Generate floors
        int counter = 1;
        foreach(var room in rooms)
        {
            RoomInstance roomInstance = Instantiate(roomPrefab, generatedDungeonParent.transform);
            roomInstance.Setup(NetworkedMonsterSpawner, roachManager, gameRunner);
            roomInstance.CreateFloor(room.BottomLeftAreaCorner, room.TopRightAreaCorner);
            roomInstance.gameObject.name = $"Dungeon Room {counter++}";
            generatedRooms.Add(roomInstance);
        }

        // Use floors to find neighbors
        foreach(var roomInstance in generatedRooms)
        {
            PopulateNeighbors(roomInstance);
        }

        // Generate walls, using neighbors to create doors
        foreach(var roomInstance in generatedRooms)
        {
            roomInstance.SetWallPositions();
        }

        // Generate walls, entities, and props
        foreach(var roomInstance in generatedRooms)
        {
            roomInstance.GenerateWalls();
            roomInstance.Populate();
        }

        StartCoroutine(BuildNavMesh());
    }

    private void PopulateNeighbors(RoomInstance roomInstance)
    {
        List<RoomInstance> neighbors = new List<RoomInstance>();

        Bounds myRoomBounds = roomInstance.floor.meshRenderer.bounds;

        foreach (var potentialNeighbor in generatedRooms)
        {
            if(roomInstance.floor.meshRenderer.bounds == potentialNeighbor.floor.meshRenderer.bounds)
            {
                continue;
            }

            Bounds neighborBounds = potentialNeighbor.floor.meshRenderer.bounds;

            // one of us contains the other in the opposite axis
            bool shareZAxis = (neighborBounds.max.z < myRoomBounds.max.z &&
                                    neighborBounds.min.z > myRoomBounds.min.z) ||
                                    (neighborBounds.max.z > myRoomBounds.max.z &&
                                    neighborBounds.min.z < myRoomBounds.min.z);

            bool shareXAxis = (neighborBounds.max.x < myRoomBounds.max.x &&
                                    neighborBounds.min.x > myRoomBounds.min.x) ||
                                    (neighborBounds.max.x > myRoomBounds.max.x &&
                                    neighborBounds.min.x < myRoomBounds.min.x);

            // we share an axis
             bool isLeftNeighbor = neighborBounds.max.x == myRoomBounds.min.x && shareZAxis;
            bool isRightNeighbor = neighborBounds.min.x == myRoomBounds.max.x && shareZAxis;
            bool isTopNeighbor = neighborBounds.min.z == myRoomBounds.max.z && shareXAxis;
            bool isBottomNeighbor = neighborBounds.max.z == myRoomBounds.min.z && shareXAxis;


            if (isLeftNeighbor || isRightNeighbor || isTopNeighbor || isBottomNeighbor)
            {
                neighbors.Add(potentialNeighbor);
            }
        }

        roomInstance.neighbors = neighbors;
    }

    
    private IEnumerator BuildNavMesh()
    {
        yield return new WaitForSeconds(3);
        navMeshSurface.BuildNavMesh();
    }

    public void DestroyGeneratedDungeon()
    {
        if(generatedDungeonParent == null)
        {
            return;
        }

        foreach(Transform item in generatedDungeonParent.transform)
        {
            DestroyImmediate(item.gameObject);
        }

        DestroyImmediate(generatedDungeonParent);
    }

    public void CheckDungeon()
    {
        Debug.Log("checking dungeon");
    }
}

[Serializable]
public class DebugRoom
{
    public Vector2Int bottomLeft;
    public Vector2Int topRight;
}

