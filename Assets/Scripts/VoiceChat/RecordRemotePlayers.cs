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
    public static string dataSubFolder = "recordedClips";
    public static string fileNamePrefix = "remoteAudio-";

    ConcurrentQueue<AudioData> audioQueue;
    AudioFileWriter fileWriter;
    int fileCount = 0;
    private List<string> savedFiles;
    private DateTime audioStartTime;
    private bool hasWrittenToFile;
    private GameObject monsterGameObject;
    private GameRunner gameRunner;

    public void OnAudioPlayback(ArraySegment<float> data, bool complete)
    {
        audioQueue.Enqueue(new AudioData(data, complete));
    }

    void Start()
    {
        gameRunner = FindAnyObjectByType<GameRunner>();
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
        DeleteAudioFiles();
    }

    public void DeleteAudioFiles()
    {
        foreach (var file in savedFiles)
        {
            File.Delete(file);
        }
    }
    public void GetMonsterGameObject()
    {
        // TODO: get from MonsterSpawn
    }

    public string GetAudioFolder()
    {
        return Path.Combine(Application.persistentDataPath, dataSubFolder);
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