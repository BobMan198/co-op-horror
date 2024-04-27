using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : InteractableItem
{
    private GameRunner gameRunner;
    private void Start()
    {
        gameRunner = FindObjectOfType<GameRunner>();
    }
    public override void Interact()
    {
        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        foreach(var player in gameRunner.playersLoadedIn)
        {
            var playeritem = player.GetComponentInChildren<PickupItem>();
            if(playeritem != null)
            {
                playeritem.transform.parent = null;
                DontDestroyOnLoad(playeritem.gameObject);
                DeTagItemClientRpc(playeritem.gameObject);
                playeritem.gameObject.tag = "Item";
            }
        }
        var outsideStatus = NetworkManager.Singleton.SceneManager.LoadScene("Outside", LoadSceneMode.Single);
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
    }

    [ClientRpc]
    private void DeTagItemClientRpc(NetworkObjectReference playeritem)
    {
       if(!playeritem.TryGet(out NetworkObject networkObject))
        {
            Debug.Log("Cant get player item");
        }
       else
        {
            foreach(var player in gameRunner.playersLoadedIn)
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

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Scene outside = SceneManager.GetSceneByName("Outside");
        SceneManager.SetActiveScene(outside);
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        NetworkManager.Singleton.SceneManager.LoadScene("Elevator", LoadSceneMode.Additive);
    }
}
