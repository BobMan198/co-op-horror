using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemEvents : NetworkBehaviour
{
    public PickupItem itemInHand;

    [SerializeField]
    private GameObject cameraItem;
    private void Update()
    {
        if(itemInHand != null)
        {
            if(itemInHand.ItemName == "Camera")
            {
                if (Input.GetMouseButton(1))
                {
                    ZoomCameraServerRpc();
                }
                else
                {
                    UnZoomCameraServerRpc();
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ZoomCameraServerRpc()
    {
        ZoomCameraClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnZoomCameraServerRpc()
    {
        UnZoomCameraClientRpc();
    }

    [ClientRpc]
    private void ZoomCameraClientRpc()
    {
        cameraItem = FindObjectOfType<LiveCamera>().gameObject;
        var camera = cameraItem.GetComponentInChildren<Camera>();

        float zoomTimer = +Time.deltaTime;
        float zoom = zoomTimer * 45;
        if (camera.focalLength >= 120)
        {
            return;
        }
        else
        {
            camera.focalLength += zoom;
        }
    }

    [ClientRpc]
    private void UnZoomCameraClientRpc()
    {
        cameraItem = FindObjectOfType<LiveCamera>().gameObject;
        var camera = cameraItem.GetComponentInChildren<Camera>();

        float zoomTimer = -Time.deltaTime;
        float zoom = zoomTimer * 45;
        if (camera.focalLength <= 50)
        {
            return;
        }
        else
        {
            camera.focalLength += zoom;
        }
    }
}
