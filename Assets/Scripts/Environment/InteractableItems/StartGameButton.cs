using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : InteractableItem
{
    public override void Interact()
    {
        StartGameRayServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameRayServerRpc()
    {
        //var monsterspawn = FindAnyObjectByType<MonsterSpawn>();

        Debug.Log("Loading Game Scene");
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        NetworkManager.Singleton.SceneManager.LoadScene("Outside", LoadSceneMode.Single);
    }
}
