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

    private float ToDecibel(float volume) =>
        volume > 0f ? Mathf.Log10(volume) * 20f : -80f;

    // 마스터 볼륨
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.MasterVolume, ToDecibel(volume));
    }

    // 효과음 볼륨
    public void SetSfxVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.SFXVolume, ToDecibel(volume));
    }

    // 배경음악 볼륨
    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat(ConstValues.BGMVolume, ToDecibel(volume));
    }
}
