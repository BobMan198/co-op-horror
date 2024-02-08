using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterType
{
    CockroachMonster,
    ShadowMonster
}


public class RoomPrefabConfig : MonoBehaviour
{
    public Vector3 minSize;
    public Vector3 maxSize;

    public Transform shadowMonsterSpawnLocation;
    public Transform cockroachMonsterSpawnLocation;
    public MonsterType monsterType;
    public List<Transform> cockroachSpawnLocations;

    public List<MoveableRoomPiece> roomPieces;

    public Transform playerSpawnLocation;

    public int maxToSpawn;

    [SerializeField]
    private bool showBounds;

    private void Start()
    {
        if(playerSpawnLocation != null)
        {
            GameRunner.PlayerSpawn = transform;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showBounds)
        {
            return;
        }

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Vector3 realSize = maxSize;
        if(maxSize.x == 0)
        {
            realSize.x = 500;
        }

        if (maxSize.z == 0)
        {
            realSize.z = 500;
        }

        Gizmos.DrawCube(transform.position, minSize);
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawCube(transform.position, realSize);
    }
}

[Serializable]
public class MoveableRoomPiece
{
    public GameObject prefab;
    public SpawnStyle spawnStyle;

    public enum SpawnStyle {
        Corner,
        AttachedToWall,
        Chandelier
    }
}