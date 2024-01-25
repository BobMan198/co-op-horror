using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.VisualScripting;

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
    [Range(0.0f, 0.3f)]
    public float roomBottomCornerModifier;
    [Range(0.7f, 1f)]
    public float roomTopCornerModifier;
    [Range(0f, 2f)]
    public int roomOffset;
    public GameObject wallVertical, wallHorizontal;
    List<Vector3Int> possibleDoorVerticalPosition;
    List<Vector3Int> possibleDoorHorizontalPosition;
    List<Vector3Int> possibleWallVerticalPosition;
    List<Vector3Int> possibleWallHorizontalPosition;

    private GameRunner gameRunner;
    void Start()
    {
        Instance = this;
        //CreateDungeon();
        DontDestroyOnLoad(this);

    }


    public void CreateDungeon()
    {
        playerSpawnerSpawned = false;
        cafeteriaSpawned = false;
        shadowMonsterSpawned = false;
        pillarAndFountainSpawned = false;
        DestroyAllChildren();
        DungeonGenerator generator = new DungeonGenerator(dungeonWidth, dungeonLength);
        var listOfRooms = generator.CalculateDungeon(maxIterations,
            roomWidthMin,
            roomLengthMin,
            roomBottomCornerModifier,
            roomTopCornerModifier,
            roomOffset,
            corridorWidth);

        GameObject wallParent = new GameObject("WallParent");
        wallParent.transform.parent = transform;
        possibleDoorVerticalPosition = new List<Vector3Int>();
        possibleDoorHorizontalPosition = new List<Vector3Int>();
        possibleWallVerticalPosition = new List<Vector3Int>();
        possibleWallHorizontalPosition = new List<Vector3Int>();

        for (int i = 0; i < listOfRooms.Count; i++)
        {
            CreateMesh(listOfRooms[i].BottomLeftAreaCorner, listOfRooms[i].TopRightAreaCorner);
        }
        CreateWalls(wallParent);
    }

    private void CreateWalls(GameObject wallParent)
    {
        foreach(var wallPosition in possibleWallHorizontalPosition)
        {
            CreateWall(wallParent, wallPosition, wallHorizontal);
        }
        foreach(var wallPosition in possibleWallVerticalPosition)
        {
            CreateWall(wallParent, wallPosition, wallVertical);
        }
    }

    private void CreateWall(GameObject wallParent, Vector3Int wallPosition, GameObject wallPrefab)
    {
        var wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity, wallParent.transform);

        wall.GetComponent<MeshRenderer>().material = wallMaterial;
        wall.AddComponent<NavMeshSurface>();
        wall.GetComponent<NavMeshSurface>().defaultArea = 1;
        wall.AddComponent<NavMeshModifier>();
        wall.layer = 11;
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

        if (floorSize.x >= 40 && floorSize.z >= 40 && !cafeteriaSpawned)
        {
            CreateCafeteria(dungeonFloor, dungeonFloor.GetComponent<MeshCollider>().bounds.center, cafeteriaPrefab);
            cafeteriaCount++;

            if(cafeteriaCount >= 2)
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

        for (int row = (int)bottomLeftV.x; row < (int)bottomRightV.x; row++)
        {
            var wallPosition = new Vector3(row, 0, bottomLeftV.z);
            AddWallPositionToList(wallPosition, possibleWallHorizontalPosition, possibleDoorHorizontalPosition);
        }

        for(int row = (int)topLeftV.x; row < (int)topRightCorner.x; row++)
        {
            var wallPosition = new Vector3(row, 0, topRightV.z);
            AddWallPositionToList(wallPosition, possibleWallHorizontalPosition, possibleDoorHorizontalPosition);
        }
        for(int col = (int)bottomLeftV.z; col < (int)topLeftV.z; col++)
        {
            var wallPosition = new Vector3(bottomLeftV.x, 0, col);
            AddWallPositionToList(wallPosition, possibleWallVerticalPosition, possibleDoorVerticalPosition);
        }
        for (int col = (int)bottomRightV.z; col < (int)topRightV.z; col++)
        {
            var wallPosition = new Vector3(bottomRightV.x, 0, col);
            AddWallPositionToList(wallPosition, possibleWallVerticalPosition, possibleDoorVerticalPosition);
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
            navMeshSurface.BuildNavMesh();
        }
    }
}