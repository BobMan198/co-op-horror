using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevatorStart : MonoBehaviour
{
   public GameRunner gameRunner;
    public DoorController doorController;

    private DungeonCreator dungeonCreator;

    public NetworkVariable<float> n_playersInElevator = new NetworkVariable<float>();
    public NetworkVariable<float> n_startTimer = new NetworkVariable<float>();
    public NetworkVariable<bool> ElevatorStarted = new NetworkVariable<bool>();

    private const float startTimerInterval = 5;
    void Start()
    {
        gameRunner = FindObjectOfType<GameRunner>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        AddPlayerInElevatorServerRpc();
    }

    private void OnTriggerExit(Collider collision)
    {
        RemovePlayerInElevatorServerRpc();
    }

    private void Update()
    {
        if(n_playersInElevator.Value == gameRunner.alivePlayers.Count && !ElevatorStarted.Value)
        {

            n_startTimer.Value += Time.deltaTime;

            if(n_startTimer.Value >= startTimerInterval)
            {
                StartElevatorServerRpc();
                ElevatorStarted.Value = true;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerInElevatorServerRpc()
    {
        n_playersInElevator.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemovePlayerInElevatorServerRpc()
    {
        n_playersInElevator.Value--;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartElevatorServerRpc()
    {

        doorController.CloseElevatorDoors();

        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        NetworkManager.Singleton.SceneManager.LoadScene("TestScene", LoadSceneMode.Additive);
    }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        var scene = SceneManager.GetSceneByName("Outside");

        gameRunner.n_inGame.Value = true;

        NetworkManager.Singleton.SceneManager.UnloadScene(scene);
        //gameManager.GenerateRoomSeedServerRpc();

        doorController.OpenElevatorDoors();

        if (gameRunner.n_inGame.Value == false)
        {
            gameRunner.n_inGame.Value = true;
            Debug.Log("Value isnt being set");
        }
    }
}
