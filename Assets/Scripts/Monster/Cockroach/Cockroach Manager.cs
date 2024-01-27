using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CockroachManager : NetworkBehaviour
{
    public NetworkVariable<float> cockroachsStressed = new NetworkVariable<float>();
    public MonsterSpawn monsterSpawn;
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
    private void ReadyToSpawnServerRpc()
    {
        monsterSpawn.roachKingReadyToSpawn.Value = true;

        if (monsterSpawn.n_roachMonsterSpawned.Value == false  )
        {
            monsterSpawn.SpawnRoachMonsterServerRpc(GameObject.FindGameObjectWithTag("RoachKingSpawner").transform.position);
        }
    }

}
