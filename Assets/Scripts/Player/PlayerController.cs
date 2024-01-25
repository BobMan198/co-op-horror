using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Dissonance.Integrations.Unity_NFGO;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    public PlayerUI playerUI;
    public ItemPickup itemPickup;

    private void Start()
    {
    }

    void Awake()
    {
    }

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer) 
        {
            GameRunner.LocalPlayer = this;
        }
    }
}