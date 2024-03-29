﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.GridLayoutGroup;
using Random = UnityEngine.Random;

namespace Dissonance.Integrations.Unity_NFGO.Demo
{
    public class NfgoPlayerSpawner
        : NetworkBehaviour
    {
        public GameObject PlayerPrefab;
        private void OnEnable()
        {
            StartCoroutine(SpawnCo());
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

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;

        }

        public void SpawnPlayers(ulong id)
        {
            GameObject player = Instantiate(PlayerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);
        }

        private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            var nm = NetworkManager.Singleton;
            if (NetworkManager.Singleton.IsHost && sceneName == "GameLobby")
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
                    player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);

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
}
      
