using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MonsterSpawn : NetworkBehaviour
{
    private GameRunner gameRunner;
    public NetworkVariable<bool> n_monsterSpawned = new NetworkVariable<bool>();

    private void Awake()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();
        var monster = FindAnyObjectByType<EnemyMovement>();

        Destroy(monster);

        StartCoroutine(waitToSpawnMonster());
    }
    private IEnumerator waitToSpawnMonster()
    {
        if(n_monsterSpawned.Value == false)
        {
            yield return new WaitForSeconds(5);
            SpawnMonster();
            StopCoroutine(waitToSpawnMonster());
        }
    }
    private void SpawnMonster()
    {
        gameRunner.HandleMonsterSpawnServerRpc();
        n_monsterSpawned.Value = true;
    }
}
