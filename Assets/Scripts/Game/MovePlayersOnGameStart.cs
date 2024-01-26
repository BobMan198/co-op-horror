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
        playerDeadZone = GameObject.FindGameObjectWithTag("PlayerDeadZone");
    }
}
