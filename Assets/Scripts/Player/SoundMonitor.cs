using System;
using System.Collections;
using System.Collections.Generic;
using Dissonance;
using Dissonance.Audio.Playback;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Windows.Speech;
using System.Linq;

public class SoundMonitor : NetworkBehaviour
{
    public NetworkVariable<float> playerMovementLoudness = new NetworkVariable<float>();
    private float tempPlayerMovementLoudness;
    public NetworkVariable<float> voiceLoudness = new NetworkVariable<float>();
    private float tempVoiceLoudness;
    public NetworkVariable<float> overallLoudness = new NetworkVariable<float>();
    private DissonanceComms _dissonanceComms;
    private VoicePlayerState _local;
    private PlayerMovement playerMovement;

    private KeywordRecognizer keyworkRecognizer;
    private Dictionary<string, Action> actions = new Dictionary<string, Action>();

    private KeywordRecognizer keyworkTrishaRecognizer;
    private Dictionary<string, Action> trishaActions = new Dictionary<string, Action>();

    private KeywordRecognizer keyworkTrishaShopRecognizer;
    private Dictionary<string, Action> trishaShopActions = new Dictionary<string, Action>();

    [SerializeField]
    private float voiceLoudnessMultiplier = 4;
    [SerializeField]
    private float sprintMultiplier = 1.5f;
    [SerializeField]
    private float walkMultiplier = 0.6f;
    [SerializeField]
    private bool debugMode = false;

    private void Awake()
    {
        _dissonanceComms = FindObjectOfType<DissonanceComms>();
        _local = _dissonanceComms.FindPlayer(_dissonanceComms.LocalPlayerName);
        playerMovement = GetComponent<PlayerMovement>();
        GameRunner.soundMonitors.Add(this);

        trishaActions.Add("trisha", TrishaActivate);
        keyworkTrishaRecognizer = new KeywordRecognizer(trishaActions.Keys.ToArray());
        keyworkTrishaRecognizer.OnPhraseRecognized += TrishaRecognizedSpeech;
        keyworkTrishaRecognizer.Start();

        trishaShopActions.Add("add glowstick to cart", GlowStick);
        trishaShopActions.Add("get me a glowstick", GlowStick);
        trishaShopActions.Add("buy glowstick", GlowStick);
        trishaShopActions.Add("buy me glowstick", GlowStick);
        trishaShopActions.Add("buy me a glowstick", GlowStick);
        trishaShopActions.Add("glowstick", GlowStick);
        keyworkTrishaShopRecognizer = new KeywordRecognizer(trishaShopActions.Keys.ToArray());
    }

    private void Update()
    {
        if(_local.IsSpeaking)
        {
            tempVoiceLoudness = _local.Amplitude * voiceLoudnessMultiplier;
            UpdateLocalVoiceAmpServerRpc();
        }

        if(playerMovement.isSprinting)
        {
            tempPlayerMovementLoudness = sprintMultiplier;
        }
        else if(playerMovement.playerIsWalking && !playerMovement.isSprinting)
        {
            tempPlayerMovementLoudness = walkMultiplier;
        }
        else if(!playerMovement.playerIsWalking && !playerMovement.isSprinting)
        {
            tempPlayerMovementLoudness = 0f;
        }

        UpdateLocalMovementAmpServerRpc();


        if (debugMode)
        {
            DebugVolume();
        }
    }

    private void DebugVolume()
    {
        if (overallLoudness.Value > 0.05f)
        {
            Debug.Log("Low Volume");
        }

        if (overallLoudness.Value > 1.5f)
        {
            Debug.Log("Medium Volume");
        }

        if (overallLoudness.Value > 2.8f)
        {
            Debug.Log("Loud Volume");
        }

        if (overallLoudness.Value > 3.8)
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
        var loudness = playerMovementLoudness.Value + voiceLoudness.Value;
        overallLoudness.Value = loudness;
    }

    public void TrishaActivate()
    {
        Debug.Log("trisha activated!");
        keyworkTrishaShopRecognizer.OnPhraseRecognized += TrishaShopRecognizedSpeech;
        keyworkTrishaShopRecognizer.Start();
    }

    private void TrishaRecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.Log(speech.text);
        trishaActions[speech.text].Invoke();
    }

    private void TrishaShopRecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.Log(speech.text);
        trishaShopActions[speech.text].Invoke();
    }

    private void GlowStick()
    {
        keyworkTrishaShopRecognizer.OnPhraseRecognized -= TrishaShopRecognizedSpeech;
        keyworkTrishaShopRecognizer.Stop();

        Debug.Log("Buying Glowstick");
    }
}
