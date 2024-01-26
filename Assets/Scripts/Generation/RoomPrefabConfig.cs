using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPrefabConfig : MonoBehaviour
{
    public Vector3 minSize;
    public Vector3 maxSize;

    public Transform monsterSpawnLocation;
    public int maxToSpawn;

    [SerializeField]
    private bool showBounds;

    private void OnDrawGizmos()
    {
        if (!showBounds)
        {
            return;
        }

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(transform.position, minSize);
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawCube(transform.position, maxSize);
    }
}
