using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Dissonance;
using Dissonance.Audio;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;

public class RecordPlayers : BaseMicrophoneSubscriber
{
    AudioFileWriter fileWriter;
    int fileCount = 0;

    private void Start()
    {
        var files = Directory.EnumerateFiles(Application.persistentDataPath);

        foreach (var file in files)
        {
            File.Delete(file);
        }
        FindObjectOfType<DissonanceComms>().SubscribeToRecordedAudio(this);
    }

    protected override void ProcessAudio(ArraySegment<float> data)
    { 
        if(fileWriter != null)
        {
            fileWriter.WriteSamples(data);
        }
    }

    protected override void ResetAudioStream(WaveFormat waveFormat)
    {
            if(fileWriter != null)
        {
            fileWriter.Flush();
            fileWriter.Dispose();
            fileWriter = null;

            fileCount++;
        }

        // C:/<user>/appData/Local/<game-name>/test.wav
        string filePath = Application.persistentDataPath;
        filePath = Path.Combine(filePath, $"test-{fileCount}.wav");
        fileWriter = new AudioFileWriter(filePath, waveFormat);
    }

    private void OnDestroy()
    {
        fileWriter.Flush();
        fileWriter.Dispose();
        fileWriter = null;
    }
}
