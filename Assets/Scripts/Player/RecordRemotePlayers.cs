using Dissonance.Audio;
using Dissonance.Audio.Playback;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class RecordRemotePlayers : MonoBehaviour, IAudioOutputSubscriber
{
    ConcurrentQueue<AudioData> audioQueue;
    AudioFileWriter fileWriter;
    int fileCount = 0;
    private List<string> savedFiles;

    public void OnAudioPlayback(ArraySegment<float> data, bool complete)
    {
        audioQueue.Enqueue(new AudioData(data, complete));
    }

    void Start()
    {
        audioQueue = new ConcurrentQueue<AudioData>();
        SetupNextStream();
    }

    void Update()
    {
        while(!audioQueue.IsEmpty)
        {
            bool didComplete = audioQueue.TryDequeue(out AudioData audioData);

            if(!didComplete)
            {
                Debug.LogError("Could not dequeue data");
            }
            else
            {
                fileWriter.WriteSamples(audioData.data);
                if (audioData.complete)
                {
                    SetupNextStream();
                }
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

        // example path:     C:/<user>/appData/Local/<game-name>/remoteAudio-1.wav
        string filePath = Application.persistentDataPath;
        filePath = Path.Combine(filePath, $"remoteAudio-{fileCount}.wav");
        savedFiles.Add(filePath);
        fileWriter = new AudioFileWriter(filePath, new WaveFormat(48000, 1));
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