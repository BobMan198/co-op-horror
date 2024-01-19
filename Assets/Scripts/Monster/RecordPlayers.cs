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

    DissonanceComms comms;
    VoicePlayerState localPlayer;
    WaveFormat waveFormat;
    private List<string> savedFiles;

    private DateTime audioStartTime;
    private void Awake()
    {
        comms = FindObjectOfType<DissonanceComms>();
        localPlayer = comms.FindPlayer(comms.LocalPlayerName);
        savedFiles = new List<string>();
    }

    public override void Update()
    {
        base.Update();
    }

    private void Start()
    {
        comms.SubscribeToRecordedAudio(this);
    }

    protected override void ProcessAudio(ArraySegment<float> data)
    {
        var dateSpan = DateTime.Now - audioStartTime;
        if(dateSpan.Seconds > 5) 
        {
            SetupNextStream();
        }

        // filter out empty mic input
        if (fileWriter != null)
        {
            fileWriter.WriteSamples(data);
        }
    }

    protected override void ResetAudioStream(WaveFormat waveFormat)
    {
        this.waveFormat = waveFormat;
        SetupNextStream();
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

        // C:/<user>/appData/Local/<game-name>/test.wav
        string filePath = Application.persistentDataPath;
        filePath = Path.Combine(filePath, $"test-{fileCount}.wav");
        savedFiles.Add( filePath );
        fileWriter = new AudioFileWriter(filePath, waveFormat);
        audioStartTime = DateTime.Now;
    }

    private void OnDestroy()
    {
        fileWriter.Flush();
        fileWriter.Dispose();
        fileWriter = null;
        comms.UnsubscribeFromRecordedAudio(this);
        DeleteAudioFiles();
    }

    public void DeleteAudioFiles()
    {
        foreach (var file in savedFiles)
        {
            File.Delete(file);
        }
    }

    public void UseSoundFile()
    {
        // play the audio clip

        // delete the file
    }
}
