using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicInput : MonoBehaviour
{
    public float soundVolume;

    private string _device;
    private AudioClip _clipRecord;
    private readonly int _sampleWindow = 256;

    private void Start()
    {
        InitializeMic();
    }

    void Update()
    {
        soundVolume = LevelMax();
    }

    private void InitializeMic()
    {
        _device ??= Microphone.devices[0];
        _clipRecord = Microphone.Start(_device, true, 999, 44100);
        print(_device);
    }

    private void StopMic()
    {
        Microphone.End(_device);
    }

    private float LevelMax()
    {
        float maxLevel = 0;
        float[] waveData = new float[_sampleWindow];
        int micPosition = Microphone.GetPosition(_device) - _sampleWindow + 1;
        if (micPosition < 0) return 0;
    
        _clipRecord.GetData(waveData, micPosition);

        // Calculate the max level within the sample window
        foreach (var level in waveData)
        {
            float wavePeak = Mathf.Abs(level);
            if (wavePeak > maxLevel)
            {
                maxLevel = wavePeak;
            }
        }
    
        return maxLevel;
    }
}
