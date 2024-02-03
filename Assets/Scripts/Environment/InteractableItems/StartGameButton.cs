using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : InteractableItem
{
    public override void Interact()
    {
        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        var outsideStatus = NetworkManager.Singleton.SceneManager.LoadScene("Outside", LoadSceneMode.Single);
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
    }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        NetworkManager.Singleton.SceneManager.LoadScene("Elevator", LoadSceneMode.Additive);
    }
}
