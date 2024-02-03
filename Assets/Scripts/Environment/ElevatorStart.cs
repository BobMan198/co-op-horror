using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevatorStart : NetworkBehaviour
{
    public GameRunner gameRunner;
    public DoorController doorController;

    private DungeonCreator dungeonCreator;

    public NetworkVariable<float> n_playersInElevator = new NetworkVariable<float>();
    public NetworkVariable<float> n_startTimer = new NetworkVariable<float>();
    public NetworkVariable<bool> ElevatorStarted = new NetworkVariable<bool>();

    private const float startTimerInterval = 5;
    public static Vector3 elevatorPosition;

    void Start()
    {
        gameRunner = FindObjectOfType<GameRunner>();
        elevatorPosition = transform.position;
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
        //if(n_playersInElevator.Value == gameRunner.alivePlayers.Count && !ElevatorStarted.Value)
        //{
        //    n_startTimer.Value += Time.deltaTime;

        //    if(n_startTimer.Value >= startTimerInterval)
        //    {
        //        StartElevatorServerRpc();
        //        n_startTimer.Value = 0;
        //        ElevatorStarted.Value = true;
        //    }
        //}
        //else
        //{
        //    n_startTimer.Value = 0;
        //}
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

        if (gameRunner.n_inGame.Value)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Outside", LoadSceneMode.Additive);
        }
        else
        {
            NetworkManager.Singleton.SceneManager.LoadScene("TestScene", LoadSceneMode.Additive);
        }
    }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;

        gameRunner.n_inGame.Value = sceneName == "TestScene";

        UnityEngine.SceneManagement.Scene sceneToUnload;
        if (gameRunner.n_inGame.Value)
        {
            sceneToUnload = SceneManager.GetSceneByName("Outside");
        }
        else
        {
            sceneToUnload = SceneManager.GetSceneByName("TestScene");
        }

        NetworkManager.Singleton.SceneManager.UnloadScene(sceneToUnload);

        doorController.OpenElevatorDoors(true);
    }
}
