using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class CockroachManager : NetworkBehaviour
{
    public NetworkVariable<float> cockroachsStressed = new NetworkVariable<float>();
    public MonsterSpawn monsterSpawn;
    public GameObject cockroachColonyPrefab;
    public List<Transform> cockroachSpawners;
    public GameObject dungeonfloorInstance;
    void Update()
    {
        HandleKingRoachSpawn();
    }

    private void HandleKingRoachSpawn()
    {
        if(cockroachsStressed.Value >= 30 && !monsterSpawn.roachKingReadyToSpawn.Value)
        {
            ReadyToSpawnServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnRoachColonyServerRpc()
    {
        int countToSpawn = GameRunner.RandomSeed.Next(0, cockroachSpawners.Count);

        for (int i = 0; i < countToSpawn; i++)
        {

            GameObject cockroachInstance = Instantiate(cockroachColonyPrefab, dungeonfloorInstance.transform);
            cockroachInstance.transform.position = cockroachSpawners[i].transform.position;
            var bugs = cockroachInstance.GetComponentsInChildren<NetworkObject>();
            foreach(var roach in bugs)
            {
                roach.Spawn();
                roach.transform.position = cockroachSpawners[i].transform.position;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyToSpawnServerRpc()
    {
        monsterSpawn.roachKingReadyToSpawn.Value = true;

        if (monsterSpawn.n_roachMonsterSpawned.Value == false  )
        {
            monsterSpawn.SpawnRoachMonsterServerRpc(GameObject.FindGameObjectWithTag("RoachKingSpawner").transform.position);
        }
    }

}
