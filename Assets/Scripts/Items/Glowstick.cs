using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Glowstick : NetworkBehaviour
{
    public GameObject redGlowstick;
    public GameObject greenGlowstick;
    public GameObject blueGlowstick;

    private void Start()
    {
        if(!IsOwner)
        {
            this.enabled = false;
        }
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha8))
        {
            HandleRedGlowStickServerRpc();
        }

        if (Input.GetKeyUp(KeyCode.Alpha9))
        {
            HandleGreenGlowStickServerRpc();
        }

        if (Input.GetKeyUp(KeyCode.Alpha0))
        {
            HandleBlueGlowStickServerRpc();
        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleRedGlowStickServerRpc()
    {
            var redGlow = Instantiate(redGlowstick, new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z), new Quaternion());
            redGlow.GetComponent<NetworkObject>().Spawn();
            redGlow.GetComponent<Rigidbody>().AddForce(transform.forward * 130);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleGreenGlowStickServerRpc()
    {
        var greenGlow = Instantiate(greenGlowstick, new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z), new Quaternion());
        greenGlow.GetComponent<NetworkObject>().Spawn();
        greenGlow.GetComponent<Rigidbody>().AddForce(transform.forward * 130);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleBlueGlowStickServerRpc()
    {
        var blueGlow = Instantiate(blueGlowstick, new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z), new Quaternion());
        blueGlow.GetComponent<NetworkObject>().Spawn();
        blueGlow.GetComponent<Rigidbody>().AddForce(transform.forward * 130);
    }
}
