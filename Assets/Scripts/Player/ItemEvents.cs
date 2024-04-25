using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEvents : MonoBehaviour
{
    public PickupItem itemInHand;

    [SerializeField]
    private GameObject cameraItem;

    private void Start()
    {

    }
    private void Update()
    {
        if(itemInHand != null)
        {
            if(itemInHand.ItemName == "Camera")
            {
                cameraItem = GameObject.FindObjectOfType<LiveCamera>().gameObject;
                var camera = cameraItem.GetComponentInChildren<Camera>();

                if (Input.GetMouseButton(1))
                {
                    float zoomTimer =+ Time.deltaTime;
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
                else
                {
                    float zoomTimer =- Time.deltaTime;
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
        }
    }
}
