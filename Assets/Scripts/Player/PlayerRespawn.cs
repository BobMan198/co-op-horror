using System.Collections;
using System.Collections.Generic;
using Dissonance;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerRespawn : NetworkBehaviour
{

    private PlayerMovement pm;
    private LayerMask playerlayer = 8;
    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()   
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (sceneName == "HQ")
        {
            if (gameObject.CompareTag("DeadPlayer") && IsLocalPlayer)
            {
                var dissonance = FindObjectOfType<DissonanceComms>();

                gameObject.tag = "Player";
                gameObject.layer = playerlayer;
                dissonance.IsMuted = false;
                pm.enabled = true;
                pm.playerHealth = 100;
                pm.playerCamera.gameObject.SetActive(true);
                pm.playerCamera.enabled = true;
                pm.fadeBlack.gameObject.SetActive(false);
                pm.spectatorCamera.gameObject.SetActive(false);
                pm.spectatorCamera.transform.SetParent(transform);
                pm.controller.enabled = true;
                pm.audioListener.enabled = true;
                pm.playerStaminaUI.SetActive(true);
                pm.playerItemHolder.SetActive(true);

            }

            if(pm.enabled == false)
            {
                pm.enabled = true;
            }

        }
        else
        {
            return;
        }

    }
}
