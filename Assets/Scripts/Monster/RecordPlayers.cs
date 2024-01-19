using System;
using System.Collections;
using System.Collections.Generic;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;

public class RecordPlayers : MonoBehaviour, IMicrophoneCapture
{
    public bool IsRecording => throw new NotImplementedException();

    public string Device => throw new NotImplementedException();

    public TimeSpan Latency => throw new NotImplementedException();

    public WaveFormat StartCapture([CanBeNull] string name)
    {
        throw new NotImplementedException();
    }

    public void StopCapture()
    {
        throw new NotImplementedException();
    }

    public void Subscribe([NotNull] IMicrophoneSubscriber listener)
    {
        throw new NotImplementedException();
    }

    public bool Unsubscribe([NotNull] IMicrophoneSubscriber listener)
    {
        throw new NotImplementedException();
    }

    public bool UpdateSubscribers()
    {
        throw new NotImplementedException();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
