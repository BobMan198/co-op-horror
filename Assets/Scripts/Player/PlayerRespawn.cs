using System.Collections;
using System.Collections.Generic;
using Dissonance;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.VisualScripting;

public class PlayerRespawn : NetworkBehaviour
{

    private PlayerMovement pm;
    private LayerMask playerlayer = 8;
    private GameRunner gameRunner;
    public Vector3 spawnPoint;
    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
        gameRunner = FindObjectOfType<GameRunner>();
    }

    private void Update()   
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (sceneName == "HQ")
        {
            if (gameRunner.alivePlayers.Count < gameRunner.playersLoadedIn.Count)
            {
                RespawnPlayersServerRpc();
            }
        }
        else
        {
            return;
        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void RespawnPlayersServerRpc()
    {
        var dissonance = FindObjectOfType<DissonanceComms>();
        var voiceChat = dissonance.FindPlayer(dissonance.LocalPlayerName);

        gameObject.tag = "Player";
        gameObject.layer = playerlayer;
        dissonance.IsMuted = false;
        pm.playerHealth = 100;
        pm.fadeBlack.gameObject.SetActive(false);
        pm.audioListener.enabled = true;
        pm.playerItemHolder.SetActive(true);
        RespawnPlayersClientRpc();
    }

    [ClientRpc]
    private void RespawnPlayersClientRpc()
    {
        var dissonance = FindObjectOfType<DissonanceComms>();
        var voiceChat = dissonance.FindPlayer(dissonance.LocalPlayerName);

        gameObject.tag = "Player";
        gameObject.layer = playerlayer;
        dissonance.IsMuted = false;
        pm.enabled = true;
        pm.playerHealth = 100;
        pm.playerCamera.gameObject.SetActive(true);
        pm.playerCamera.enabled = true;
        pm.fadeBlack.gameObject.SetActive(false);
        if(pm.spectatorCamera != null)
        {
            pm.spectatorCamera.gameObject.SetActive(false);
            pm.spectatorCamera.transform.SetParent(transform);
        }
        pm.controller.enabled = true;
        pm.audioListener.enabled = true;
        pm.playerStaminaUI.SetActive(true);
        pm.playerItemHolder.SetActive(true);
        var spawnpointZ = Random.Range(-6, 11);
        Vector3 spawnpoint = new Vector3(spawnPoint.x, spawnPoint.y, spawnpointZ);
        pm.transform.position = spawnpoint;
    }
}
