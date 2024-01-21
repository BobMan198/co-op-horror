using Dissonance.Audio;
using Dissonance.Audio.Playback;
using JetBrains.Annotations;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class RecordRemotePlayers : MonoBehaviour, IAudioOutputSubscriber
{
    ConcurrentQueue<AudioData> audioQueue;
    AudioFileWriter fileWriter;
    int fileCount = 0;
    private List<string> savedFiles;
    private DateTime audioStartTime;
    private bool hasWrittenToFile;

    public static string dataSubFolder = "recordedClips";
    public static string fileNamePrefix = "remoteAudio-";

    public void OnAudioPlayback(ArraySegment<float> data, bool complete)
    {
        audioQueue.Enqueue(new AudioData(data, complete));
    }

    void Start()
    {
        savedFiles = new List<string>();
        audioQueue = new ConcurrentQueue<AudioData>();
        SetupNextStream();
        hasWrittenToFile = false;
    }

    void Update()
    {
        while (!audioQueue.IsEmpty)
        {
            bool didComplete = audioQueue.TryDequeue(out AudioData audioData);

            if (didComplete)
            {
                if (!hasWrittenToFile)
                {
                    audioStartTime = DateTime.Now;
                }
                fileWriter.WriteSamples(audioData.data);
                hasWrittenToFile = true;
            }
            else
            {
                Debug.LogError("Could not dequeue data");
            }
        }

        if (hasWrittenToFile)
        {
            var dateSpan = DateTime.Now - audioStartTime;
            if (dateSpan.Seconds > 5)
            {
                SetupNextStream();
                hasWrittenToFile = false;
            }
        }
    }

    private void SetupNextStream()
    {
        if (fileWriter != null)
        {
            fileWriter.Flush();
            fileWriter.Dispose();
            fileWriter = null;

            fileCount++;
        }

        // example path:     C:/<user>/appData/Local/<game-name>/recordedClips/remoteAudio-1.wav
        string filePath = Path.Combine(GetAudioFolder(), $"{fileNamePrefix}{fileCount}.wav");

        while (File.Exists(filePath))
        {
            fileCount++;
            filePath = Path.Combine(GetAudioFolder(), $"{fileNamePrefix}{fileCount}.wav");
        }

        savedFiles.Add(filePath);
        fileWriter = new AudioFileWriter(filePath, new WaveFormat(48000, 1));
    }
    private void OnDestroy()
    {
        if (fileWriter != null)
        {
            fileWriter.Flush();
            fileWriter.Dispose();
            fileWriter = null;
        }
        //DeleteAudioFiles();
    }

    public void DeleteAudioFiles()
    {
        foreach (var file in savedFiles)
        {
            File.Delete(file);
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
    
    public void LoadAndPlayRandomAudio()
    {
        string audioFilePath = GetRandomFilePath();
        StartCoroutine(LoadWavFile(audioFilePath, PlayAudioClip));
    }

    public void PlayAudioClip(AudioClip clip)
    {
        audioSource.Play(clip);
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
                try
                {
                    File.Delete(path);
                }
                catch(Exception e)
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
}


struct AudioData
{
    public ArraySegment<float> data;
    public bool complete;

    public AudioData(ArraySegment<float> data, bool complete)
    {
        this.data = data;
        this.complete = complete;
    }
}