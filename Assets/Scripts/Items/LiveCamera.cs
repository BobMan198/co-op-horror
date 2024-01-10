using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LiveCamera : MonoBehaviour
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
                        AddPointsByRayClientRpc(EventWallPrefab);
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
                        AddPointsByMonsterClientRpc(MonsterPrefab);
                        monsterTimer = 0;
                    }
                    Debug.Log(hit.transform.gameObject);
                }
            }
        }
        else return;
    }

    [ClientRpc]
    public void AddPointsByMonsterClientRpc(GameObject monsterPrefab)
    {
        GameManagerPrefab = GameObject.FindGameObjectWithTag("GameManager");

        if(monsterPointsAvailable > 0)
        {
            GameManagerPrefab.GetComponent<GameRunner>().dayPoints += monsterPointsAvailable;
            monsterPointsAvailable = 0;
            GameManagerPrefab.GetComponent<GameRunner>().HandlePointsClientRpc();
        }
    }

    [ClientRpc]
    public void AddPointsByRayClientRpc(GameObject eventWallPrefab)
    {
        GameManagerPrefab = GameObject.FindGameObjectWithTag("GameManager");

        var eventWall = eventWallPrefab.GetComponent<EventWallManager>();

        if (eventWall.pointsAvailable > 0)
        {
            GameManagerPrefab.GetComponent<GameRunner>().dayPoints += eventWall.pointsAvailable;
            eventWall.pointsAvailable = 0;
            GameManagerPrefab.GetComponent<GameRunner>().HandlePointsClientRpc();
            Debug.Log("adding " + eventWall.pointsPerTick + "points!");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(collision, 0.2f);
    }
}
