using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MovePlayersOnLeave : NetworkBehaviour
{

    private GameRunner gameRunner;

    [SerializeField]
    private GameObject playerDeadZone;

    private void Awake()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();

        //playerDeadZone = GameObject.FindGameObjectWithTag("PlayerDeadZone");
    }

    private void Update()
    {
        HandleDeadZoneClientRpc();
    }

    [ClientRpc]
    private void HandleDeadZoneClientRpc()
    {
        var players = gameRunner.playersLoadedIn;

        foreach (var player in players)
        {
            if (player.position.y <= playerDeadZone.transform.position.y)
            {
                SetPositionClientRpc();
                Debug.Log("Player below map moving to correct " + player.name);
            }
        }
    }

    [ClientRpc]

    public void SetPositionClientRpc()
    {   
        var players = gameRunner.playersLoadedIn;

        foreach (var player in players)
        {
            if (gameRunner.n_inGame.Value)
            {
                player.position = GameRunner.PlayerSpawn.position;
            }
            else
            {
                Vector3 randomSpawnPosition = new Vector3(12.5f, 1.15f, Random.Range(-6, 11));
                player.position = randomSpawnPosition;

                Debug.Log(player.position);
            }
        }
    }
}
