using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using Random = UnityEngine.Random;
using Dissonance;
using System.Linq;
using Unity.VisualScripting;

public class PlayerSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject PlayerPrefab;
    public List<ulong> playersConnected;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        //playersConnected = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        SpawnAllPlayers();
        //NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
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

    public void SpawnAllPlayers()
    {
        if (IsHost)
        {
            foreach (ulong id in playersConnected)
            {
                var pos = new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15));
                var player = Instantiate(PlayerPrefab, pos, Quaternion.identity);
                var net = player.GetComponent<NetworkObject>();
                //net.SpawnAsPlayerObject(id, true);
                net.SpawnWithOwnership(id, true);
            }
        }
        //else
        //{
        //    SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        //}
    }

    [ServerRpc]

    public void SpawnPlayerServerRpc(ulong id2)
    {
        var pos2 = new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15));
        var player2 = Instantiate(PlayerPrefab, pos2, Quaternion.identity);
        var net2 = player2.GetComponent<NetworkObject>();
        net2.SpawnWithOwnership(id2, true);
    }


    private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        StartCoroutine(test(sceneName, clientsCompleted));
    }

    IEnumerator test(string sceneName, List<ulong> clientsCompleted)
    {
        yield return new WaitForSeconds(1);
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
