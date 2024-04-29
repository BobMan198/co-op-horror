using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MonsterSpawn : NetworkBehaviour
{
    private GameRunner gameRunner;
    public NetworkVariable<bool> n_monsterSpawned = new NetworkVariable<bool>();
    public NetworkVariable<bool> n_bansheeSpawned = new NetworkVariable<bool>();
    public NetworkVariable<bool> n_roachMonsterSpawned = new NetworkVariable<bool>();
    public NetworkVariable<bool> roachKingReadyToSpawn = new NetworkVariable<bool>();
    public GameObject monsterPrefab;
    private GameObject monsterInstance;
    public GameObject roachKingPrefab;
    private GameObject roachKingInstance;
    public GameObject bansheePrefab;
    private GameObject bansheeInstance;

    private float roachSpawnCounter;

    private void Awake()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();

        DontDestroyOnLoad(this.gameObject);
    }

    private void Update()
    {
        //if (n_roachMonsterSpawned.Value == false && roachKingReadyToSpawn.Value && roachSpawnCounter == 0 && gameRunner.n_inGame.Value)
        //{
        //    SpawnRoachMonsterServerRpc(GameObject.FindGameObjectWithTag("RoachKingSpawner").transform.position);
        //    roachSpawnCounter++;
        //}
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnMonsterServerRpc(Vector3 monsterSpawnPosition)
    {
        if(IsServer && n_monsterSpawned.Value == false)
        {
            monsterInstance = Instantiate(monsterPrefab, monsterSpawnPosition, Quaternion.identity);
            monsterInstance.GetComponent<NetworkObject>().Spawn();
            //monsterInstance.transform.position = monsterSpawnPosition;
            monsterInstance.transform.position = monsterSpawnPosition;
            n_monsterSpawned.Value = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnBansheeServerRpc(Vector3 monsterSpawnPosition)
    {
        if (IsServer && n_monsterSpawned.Value == false)
        {
            bansheeInstance = Instantiate(bansheePrefab, monsterSpawnPosition, Quaternion.identity);
            bansheeInstance.GetComponent<NetworkObject>().Spawn();
            //monsterInstance.transform.position = monsterSpawnPosition;
            bansheeInstance.transform.position = monsterSpawnPosition;
            n_bansheeSpawned.Value = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnRoachMonsterServerRpc(Vector3 monsterSpawnPosition)
    {
        roachKingInstance = Instantiate(roachKingPrefab, monsterSpawnPosition, Quaternion.identity);
        roachKingInstance.GetComponent<NetworkObject>().Spawn();
        n_roachMonsterSpawned.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyMonsterServerRpc()
    {
        monsterInstance.GetComponent<NetworkObject>().Despawn();
        Destroy(monsterInstance.gameObject);
        bansheeInstance.GetComponent<NetworkObject>().Despawn();
        Destroy(bansheeInstance.gameObject);
        n_monsterSpawned.Value = false;
        n_bansheeSpawned.Value = false;
        n_roachMonsterSpawned.Value = false;
        roachKingReadyToSpawn.Value = false;
        roachSpawnCounter = 0;

        if(roachKingInstance != null)
        {
            roachKingInstance.GetComponent<NetworkObject>().Despawn();
            Destroy(roachKingInstance.gameObject);
        }
    }
}