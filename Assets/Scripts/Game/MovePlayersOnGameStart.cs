using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MovePlayersOnGameStart : NetworkBehaviour
{

    private GameRunner gameRunner;

    [SerializeField]
    private GameObject playerDeadZone;

    private void Awake()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();

        //SetPositionClientRpc();

        playerDeadZone = GameObject.FindGameObjectWithTag("PlayerDeadZone");
    }

    [ClientRpc]

    public void SetPositionClientRpc()
    {
        var players = gameRunner.playersLoadedIn;

        foreach (var player in players)
        {
            Vector3 randomSpawnPosition = new Vector3(transform.position.x + (Random.Range(-5, 5)), transform.position.y + 2, transform.position.x + (Random.Range(-5, 5)));
            player.position = randomSpawnPosition;

            Debug.Log(player.position);
        }
    }
}
