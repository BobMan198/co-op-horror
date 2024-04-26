using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Flashlight : NetworkBehaviour
{
    public Light flashLight;
    public AudioSource flashSource;
    public AudioClip clickOn;
    public AudioClip clickOff;
    public AudioClip flickOn;
    public AudioClip flickOff;
    public PlayerMovement playerMovement;

    [SerializeField]
    private NetworkVariable<bool> LightState = new NetworkVariable<bool>();
    private const bool initialState = false;

    [ServerRpc(RequireOwnership = false)]
    public void ToggleServerRpc()
    {
        // this will cause a replication over the network
        // and ultimately invoke `OnValueChanged` on receivers
        LightState.Value = !LightState.Value;
    }

    public override void OnNetworkSpawn()
    {
        LightState.OnValueChanged += OnLightStateChanged;
        if (IsServer)
        {
            LightState.Value = initialState;
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }
    }

    public override void OnNetworkDespawn()
    {
        LightState.OnValueChanged -= OnLightStateChanged;
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        flashLight.enabled = LightState.Value;
    }

    private void OnLightStateChanged(bool previous, bool current)
    {
        flashLight.enabled = current;

        if(current)
        {
            flashSource.PlayOneShot(clickOn);
        }
        else
        {
            flashSource.PlayOneShot(clickOff);
        }
    }

    [ClientRpc]

    public void FlashFlickerOnClientRpc()
    {
        flashLight.enabled = true;
    }

    [ClientRpc]
    public void FlashFlickerOffClientRpc()
    {
        flashLight.enabled = false;
        flashSource.PlayOneShot(flickOff, 0.12f);
    }
}
