using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

public class ElevatorController : NetworkBehaviour
{
    public GameRunner gameRunner;
    public DoorController doorController;
    public Light elevatorSceneLight;
    public AudioSource elevatorAudioSource;

    public ElevatorOpenKeypad elevatorOpenKeypad;
    public ElevatorCloseKeypad elevatorCloseKeypad;

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
        if(collision.GetComponent<PlayerMovement>())
        {
            AddPlayerInElevatorServerRpc();
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.GetComponent<PlayerMovement>())
        {
            RemovePlayerInElevatorServerRpc();
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
    public void TryStartElevatorServerRpc()
    {
        if(n_playersInElevator.Value >= gameRunner.alivePlayers.Count && !ElevatorStarted.Value)
        {
            ElevatorStarted.Value = true;
            StartCoroutine(StartElevatorSequence());
        }
    }

    [ClientRpc]
    private void TryStartLightingClientRpc()
    {
        StartCoroutine(MatchLighting());
    }

    private IEnumerator StartElevatorSequence()
    {
        doorController.CloseElevatorDoors();
        yield return new WaitForSeconds(2);
        elevatorAudioSource.Play();
        StartElevatorServerRpc();
        TryStartLightingClientRpc();
    }

    private IEnumerator MatchLighting()
    {
        float targetIntensity = gameRunner.n_inGame.Value ? 0.3f : 0f;
        float currentIntensity = gameRunner.n_inGame.Value ? 0f : 0.3f;
        float fadeTime = 3;
        float fadeTimer = 0;

        while (targetIntensity != elevatorSceneLight.intensity)
        {
            fadeTimer += Time.deltaTime;
            elevatorSceneLight.intensity = Mathf.Lerp(currentIntensity, targetIntensity, fadeTimer / fadeTime);
            yield return new WaitForEndOfFrame();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartElevatorServerRpc()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;

        foreach (var player in gameRunner.playersLoadedIn)
        {
            var playeritem = player.GetComponentInChildren<PickupItem>();
            if (playeritem != null)
            {
                playeritem.transform.parent = null;
                DontDestroyOnLoad(playeritem.gameObject);
                DeTagItemClientRpc(playeritem.gameObject);
                playeritem.gameObject.tag = "Item";
            }
        }

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
        UnloadSceneServerRpc(sceneName);
        StartCoroutine(WaitToOpenDoors());
    }

    [ServerRpc]
    private void UnloadSceneServerRpc(string sceneName)
    {
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

        UnloadSceneClientRpc(sceneName);
    }

    [ClientRpc]
    private void UnloadSceneClientRpc(string sceneName)
    {
        Scene sceneToUnload;

        if (gameRunner.n_inGame.Value)
        {
            sceneToUnload = SceneManager.GetSceneByName("Outside");
        }
        else
        {
            sceneToUnload = SceneManager.GetSceneByName("TestScene");
        }

        SceneManager.UnloadSceneAsync(sceneToUnload);
    }

    [ClientRpc]
    private void DeTagItemClientRpc(NetworkObjectReference playeritem)
    {
        if (!playeritem.TryGet(out NetworkObject networkObject))
        {
            Debug.Log("Cant get player item");
        }
        else
        {
            foreach (var player in gameRunner.playersLoadedIn)
            {
                var itempickup = player.GetComponent<ItemPickup>();
                if (itempickup.hasItem)
                {
                    itempickup.hasItem = false;
                }
            }

            networkObject.tag = "Item";
            var pickedUpObjectRigidbody = networkObject.GetComponent<Rigidbody>();
            pickedUpObjectRigidbody.isKinematic = false;
            pickedUpObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            networkObject.GetComponent<NetworkTransform>().InLocalSpace = false;
        }
    }

    private IEnumerator WaitToOpenDoors()
    {
        yield return new WaitForSeconds(7);

        doorController.OpenElevatorDoors(true);
        elevatorOpenKeypad.openDoorSource.Play();

        ElevatorStarted.Value = false;
    }
}
