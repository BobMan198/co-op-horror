using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    public List<GameObject> playersToSpectate;
    public GameObject currentPlayer;
    public int currentPlayerIndex = 0;
    void Start()
    {
        
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            playersToSpectate = GameObject.FindGameObjectsWithTag("Player").ToList();
            currentPlayerIndex++;

            if (currentPlayerIndex >= playersToSpectate.Count)
            {
                currentPlayerIndex = 0;
            }

            currentPlayer = playersToSpectate[currentPlayerIndex];

            // parent myself to that player so i follow them
            transform.parent = currentPlayer.transform;

            // set my position relative to that player
            transform.localPosition = Vector3.zero;

            transform.localRotation = Quaternion.identity;
        }
    }
}
