using System;
using UnityEngine;
using UnityEngine.Audio;

public class VolumeManager : Singleton<VolumeManager>
{
    public AudioMixer audioMixer;

    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.F1))
    //     {
    //         SetMasterVolume(0.0001f);
    //     }
    //     if (Input.GetKeyDown(KeyCode.F2))
    //     {
    //         SetMasterVolume(1);
    //     }
    // }

    // 마스터 볼륨
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.MasterVolume, Mathf.Log10(volume) * 20);
    }
    
    // 효과음 볼륨
    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.SFXVolume, Mathf.Log10(volume) * 20);
    }

    // 배경음악 볼륨
    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.BGMVolume, Mathf.Log10(volume) * 20);
    }
}
