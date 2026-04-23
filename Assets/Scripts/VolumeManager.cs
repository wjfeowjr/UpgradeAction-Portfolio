using System;
using UnityEngine;
using UnityEngine.Audio;

public class VolumeManager : Singleton<VolumeManager>
{
    public AudioMixer audioMixer;

    private void Start()
    {
        SetMasterVolume(GameManager.Instance.masterVolume);
        SetSfxVolume(GameManager.Instance.sfxVolume);
        SetBGMVolume(GameManager.Instance.bgmVolume);
    }

    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.F1))
    //     {
    //         SetMasterVolume(GameManager.Instance.masterVolume);
    //     }
    //     if (Input.GetKeyDown(KeyCode.F2))
    //     {
    //         SetMasterVolume(1.0f);
    //     }
    // }

    // 마스터 볼륨
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.MasterVolume, Mathf.Log10(volume) * 20);
    }
    
    // 효과음 볼륨
    public void SetSfxVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.SFXVolume, Mathf.Log10(volume) * 20);
    }

    // 배경음악 볼륨
    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.BGMVolume, Mathf.Log10(volume) * 20);
    }
}
