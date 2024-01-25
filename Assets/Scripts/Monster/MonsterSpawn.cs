using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MonsterSpawn : NetworkBehaviour
{
    private GameRunner gameRunner;
    public NetworkVariable<bool> n_monsterSpawned = new NetworkVariable<bool>();
    public GameObject monsterPrefab;

    private void Awake()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();

        DontDestroyOnLoad(this.gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnMonsterServerRpc(Vector3 monsterSpawnPosition)
    {
        GameObject monster = Instantiate(monsterPrefab, monsterSpawnPosition, Quaternion.identity);
        monster.GetComponent<NetworkObject>().Spawn();
        n_monsterSpawned.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyMonsterServerRpc()
    {
        var monster = FindAnyObjectByType<EnemyMovement>();

        if (n_monsterSpawned.Value)
        {
            Destroy(monster.gameObject);
            n_monsterSpawned.Value = false;
        }
    }
}
