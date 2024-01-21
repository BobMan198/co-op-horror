using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class MimicPlayers : NetworkBehaviour
{
    public static string dataSubFolder = "recordedClips";
    public static string fileNamePrefix = "remoteAudio-";

    [SerializeField]
    public NetworkVariable<float> mimicPlayerTimer = new NetworkVariable<float>();
    private const float mimicPlayerInterval = 60;

    public AudioClip voiceLine;
    private bool canSeePlayer;
    private void Update()
    {
        HandleLowPass();
        HandleMimicServerRpc();
    }

    private void HandleLowPass()
    {
        var parent = GetComponentInParent<EnemyMovement>().gameObject;
        var enemyLOS = parent.GetComponentInChildren<EnemyLineOfSightChecker>();
        var insight = enemyLOS.InSight;

        if (insight)
        {
            GetComponent<AudioLowPassFilter>().cutoffFrequency = (10472);
        }
        else
        {
            GetComponent<AudioLowPassFilter>().cutoffFrequency = (1000);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleMimicServerRpc()
    {
        mimicPlayerTimer.Value += Time.deltaTime;

        if (mimicPlayerTimer.Value >= mimicPlayerInterval)
        {
            LoadAndPlayRandomAudio();
            mimicPlayerTimer.Value = 0;
        }
    }
    public void PlayAudioClip(AudioClip voiceClip)
    {
        AudioSource mimicPlayers = GetComponent<AudioSource>();

        voiceClip = voiceLine;
        mimicPlayers.clip = voiceClip;
        mimicPlayers.PlayOneShot(voiceClip);
    }

    public void LoadAndPlayRandomAudio()
    {
        string audioFilePath = GetRandomFilePath();
        StartCoroutine(LoadWavFile(audioFilePath, PlayAudioClip));
    }

    public IEnumerator LoadWavFile(string path, Action<AudioClip> callback)
    {
        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
        try
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                voiceLine = audioClip;
                try
                {
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
                callback(audioClip);
            }
        }
        finally
        {
            www.Dispose();
        }
    }

    public string GetRandomFilePath()
    {
        string toReturn = string.Empty;

        string folder = GetAudioFolder();

        string[] matchingFilePaths = Directory.GetFiles(folder, $"{fileNamePrefix}*");

        int index = UnityEngine.Random.Range(0, matchingFilePaths.Length);
        toReturn = matchingFilePaths[index];
        return toReturn;
    }

    public string GetAudioFolder()
    {
        return Path.Combine(Application.persistentDataPath, dataSubFolder);
    }
}
