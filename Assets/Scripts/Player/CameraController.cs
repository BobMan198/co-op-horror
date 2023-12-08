using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class CameraController : NetworkBehaviour
{
    public GameObject cameraHolder;
    public AudioListener audioListener;
    public Camera playerCam;
    public Vector3 offset;

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            audioListener.enabled = true;
            playerCam.enabled = true;
        }else
        {
            playerCam.enabled = false;
        }
        cameraHolder.SetActive(true);
    }

    public void Update()
    {
        if(SceneManager.GetActiveScene().name == "Game")
        {
            cameraHolder.transform.position = transform.position + offset;
        }
    }
}
