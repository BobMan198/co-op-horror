using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

public class ElevatorController : NetworkBehaviour
{
    public GameRunner gameRunner;
    public DoorController doorController;

    public NetworkVariable<float> n_playersInElevator = new NetworkVariable<float>();
    public NetworkVariable<bool> ElevatorStarted = new NetworkVariable<bool>();

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

    public void TryStartElevator()
    {
        if(n_playersInElevator.Value == gameRunner.alivePlayers.Count && !ElevatorStarted.Value)
        {
            ElevatorStarted.Value = true;
            StartElevatorServerRpc();
        }
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

        Scene sceneToUnload;
        if (gameRunner.n_inGame.Value)
        {
            sceneToUnload = SceneManager.GetSceneByName("Outside");
        }
        else
        {
            sceneToUnload = SceneManager.GetSceneByName("TestScene");
        }

        NetworkManager.Singleton.SceneManager.UnloadScene(sceneToUnload);

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);

        doorController.OpenElevatorDoors(true);

        ElevatorStarted.Value = false;
    }
}
