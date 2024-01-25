using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class MimicPlayers : MonoBehaviour
{
    public static string dataSubFolder = "recordedClips";
    public static string fileNamePrefix = "remoteAudio-";

    //[SerializeField]
    //public NetworkVariable<float> mimicPlayerTimer = new NetworkVariable<float>();

    public float mimicPlayerTimer = 0;
    private const float mimicPlayerInterval = 60;

    public AudioClip voiceLine;
    private bool canSeePlayer;
    private void Update()
    {
        HandleLowPass();
        HandleMimic();
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

    //[ServerRpc(RequireOwnership = false)]
    private void HandleMimic()
    {
        mimicPlayerTimer += Time.deltaTime;

        if (mimicPlayerTimer >= mimicPlayerInterval)
        {
            LoadAndPlayRandomAudio();
            mimicPlayerTimer = 0;
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

        try
        {
            string[] matchingFilePaths = Directory.GetFiles(folder, $"{fileNamePrefix}*");

            int index = UnityEngine.Random.Range(0, matchingFilePaths.Length);
            toReturn = matchingFilePaths[index];
        }catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
        
        return toReturn;
    }

    public string GetAudioFolder()
    {
        return Path.Combine(Application.persistentDataPath, dataSubFolder);
    }
}
