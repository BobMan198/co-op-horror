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
            GameObject colonyInstance = Instantiate(cockroachColonyPrefab, cockroachSpawners[i].transform.position, cockroachSpawners[i].transform.rotation, dungeonfloorInstance.transform);
            //colonyInstance.GetComponent<NetworkObject>().Spawn();
            colonyInstance.transform.position = cockroachSpawners[i].transform.position;
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

    [ServerRpc]
    public void HandleEncounterGainServerRpc()
    {
        cockroachsStressed.Value++;
    }

    public void DEBUG_SetReady()
    {
        cockroachsStressed.Value = 30;
        monsterSpawn.roachKingReadyToSpawn.Value = false;
    }
}
