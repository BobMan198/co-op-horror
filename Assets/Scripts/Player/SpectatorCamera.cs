using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    public List<Transform> playersToSpectate;
    public Transform currentPlayer;
    private GameObject deathUI;
    public int currentPlayerIndex = 0;

    private GameRunner gameRunner;
    void Start()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();
        playersToSpectate = gameRunner.playersLoadedIn;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            deathUI = GameObject.FindGameObjectWithTag("DeathUI");
            currentPlayerIndex++;

            if (currentPlayerIndex >= playersToSpectate.Count)
            {
                currentPlayerIndex = 0;
            }

            

            currentPlayer = playersToSpectate[currentPlayerIndex];

            // parent myself to that player so i follow them
            transform.parent = currentPlayer.transform;

            // set my position relative to that player
            //transform.localPosition = Vector3.zero;

            transform.localPosition = new Vector3(0, 1.5f, 0);

            transform.localRotation = Quaternion.identity;

            deathUI.SetActive(false);
        }
    }
}
