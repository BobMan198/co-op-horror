using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using Random = UnityEngine.Random;
using Dissonance;
using static UnityEngine.UI.GridLayoutGroup;
using static UnityEditor.PlayerSettings;

public class PlayerSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject PlayerPrefab;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        //NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    private void OnEnable()
    {
        //StartCoroutine(SpawnCo());
        //NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }


    private IEnumerator SpawnCo()
    {
        var nm = NetworkManager.Singleton;
        if (!nm.IsServer)
            yield break;

        // Wait until Dissonance is created
        DissonanceComms comms = null;
        while (ReferenceEquals(comms, null))
        {
            comms = FindObjectOfType<DissonanceComms>();
            yield return null;
        }

    }

    private void Update()
    {

    }



    private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        var nm = NetworkManager.Singleton;
        if (nm.IsHost && sceneName == "GameLobby")
        {
            foreach (ulong id in clientsCompleted)
            {
                Debug.Log("Spawning player...");
                //GameObject player = Instantiate(Player);
                //player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);

                //nm.OnClientConnectedCallback += Spawn;
                //Spawn(id);


                var pos = new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15));
                var player = Instantiate(PlayerPrefab, pos, Quaternion.identity);
                var net = player.GetComponent<NetworkObject>();
                net.SpawnAsPlayerObject(id, true);
                //player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);

            }
        }
    }

    private void Spawn(ulong owner)
    {
        var pos = new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15));
        var player = Instantiate(PlayerPrefab, pos, Quaternion.identity);
        var net = player.GetComponent<NetworkObject>();
        net.SpawnAsPlayerObject(owner, true);
    }
}
