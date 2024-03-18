using System.Collections;
using System.Collections.Generic;
using Dissonance;
using Dissonance.Audio.Playback;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SoundMonitor : NetworkBehaviour
{
    public NetworkVariable<float> playerMovementLoudness = new NetworkVariable<float>();
    private float tempPlayerMovementLoudness;
    public NetworkVariable<float> voiceLoudness = new NetworkVariable<float>();
    private float tempVoiceLoudness;
    public NetworkVariable<float> overallLoudness = new NetworkVariable<float>();
    private DissonanceComms _dissonanceComms;
    private VoicePlayerState _local;
    private PlayerMovement pM;

    private void Awake()
    {
        _dissonanceComms = FindObjectOfType<DissonanceComms>();
        _local = _dissonanceComms.FindPlayer(_dissonanceComms.LocalPlayerName);
        pM = GetComponent<PlayerMovement>();
        GameRunner.soundMonitors.Add(this);
    }

    private void Update()
    {
        if(_local.IsSpeaking)
        {
            tempVoiceLoudness = _local.Amplitude;
            tempVoiceLoudness *= 4f;
            UpdateLocalVoiceAmpServerRpc();
        }

        if(pM.isSprinting)
        {
            tempPlayerMovementLoudness = 1.5f;
            UpdateLocalMovementAmpServerRpc();
        }

        if(pM.playerIsWalking && !pM.isSprinting)
        {
            tempPlayerMovementLoudness = 0.6f;
            UpdateLocalMovementAmpServerRpc();
        }

        if(!pM.playerIsWalking && !pM.isSprinting)
        {
            tempPlayerMovementLoudness = 0f;
            UpdateLocalMovementAmpServerRpc();
        }

        if(overallLoudness.Value > 0.05f)
        {
            Debug.Log("Low Volume");
        }

        if(overallLoudness.Value > 1.5f)
        {
            Debug.Log("Medium Volume");
        }

        if (overallLoudness.Value > 2.8f)
        {
            Debug.Log("Loud Volume");
        }

        if(overallLoudness.Value > 3.8)
        {
            Debug.Log("Danger Volume");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateLocalVoiceAmpServerRpc()
    {
        voiceLoudness.Value = tempVoiceLoudness;
        UpdateOverallAmpServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateLocalMovementAmpServerRpc()
    {
        playerMovementLoudness.Value = tempPlayerMovementLoudness;
        UpdateOverallAmpServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateOverallAmpServerRpc()
    {
        var loudness = playerMovementLoudness.Value += voiceLoudness.Value;
        overallLoudness.Value = loudness;
        loudness = 0;
    }
}
