using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LiveCamera : NetworkBehaviour
{
    private GameObject GameManagerPrefab;
    private GameObject EventWallManagerPrefab;
    private GameObject MonsterPrefab;
    public GameObject lastHit;
    private LayerMask eventLayer;
    public Vector3 collision = Vector3.zero;
    private GameObject EventWallPrefab;
    private float eventWalltimer = 0;
    private const float eventWallInterval = 3;
    private float monsterTimer = 0;
    private const float monsterInterval = 0.5f;

    private float monsterPointsAvailable = 100;

    // Start is called before the first frame update
    void Start()
    {
        eventLayer = LayerMask.NameToLayer("EventRayCollider");

        GameManagerPrefab = GameObject.FindGameObjectWithTag("GameManager");
    }

    // Update is called once per frame
    void Update()
    {
        if (tag == "InHand")
        {
            var ray = new Ray(transform.position, transform.right);
            Debug.DrawRay(transform.position, transform.right, Color.red, 20);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 20))
            {
                if (hit.transform.gameObject.tag == "RecordEventObject")
                {
                    eventWalltimer += Time.deltaTime;
                    if(eventWalltimer >= eventWallInterval)
                    {
                        EventWallPrefab = hit.transform.gameObject;
                        AddPointsByRayServerRpc();
                        eventWalltimer = 0;
                    }
                    Debug.Log(hit.transform.gameObject);
                }

                if (hit.transform.gameObject.tag == "RecordEnemyObject")
                {
                    Debug.Log("FILMING MONSTER!");
                    monsterTimer += Time.deltaTime;
                    if (monsterTimer >= monsterInterval)
                    {
                        MonsterPrefab = hit.transform.gameObject;
                        AddPointsByMonsterServerRpc();
                        monsterTimer = 0;
                    }
                    Debug.Log(hit.transform.gameObject);
                }
            }
        }
        else return;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPointsByMonsterServerRpc()
    {

        if(monsterPointsAvailable > 0)
        {
            GameManagerPrefab.GetComponent<GameRunner>().n_daypoints.Value += monsterPointsAvailable;
            monsterPointsAvailable = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPointsByRayServerRpc()
    {
        var eventWall = EventWallPrefab.GetComponent<EventWallManager>();

        if (eventWall.pointsAvailable > 0)
        {
            GameManagerPrefab.GetComponent<GameRunner>().n_daypoints.Value += eventWall.pointsAvailable;
            eventWall.pointsAvailable = 0;
            Debug.Log("adding " + eventWall.pointsPerTick + "points!");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(collision, 0.2f);
    }
}
