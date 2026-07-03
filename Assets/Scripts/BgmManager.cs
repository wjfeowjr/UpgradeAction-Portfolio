using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class BgmManager : Singleton<BgmManager>
{
    private AudioSource myAudioSource;
    private readonly Dictionary<string, AudioClip> bgmDic = new Dictionary<string, AudioClip>();
    public AudioClip[] bgmArray;
    public string currentBgm;
    public float firstVolume;

    protected override void Awake()
    {
        base.Awake();
        myAudioSource = GetComponent<AudioSource>();
        firstVolume = myAudioSource.volume;
        
        foreach (var bgm in bgmArray)
        {
            string soundName = bgm.name;
            if (bgmDic.ContainsKey(soundName))
            {
                Debug.LogError(soundName);
                continue;
            }
            bgmDic.Add(soundName, bgm);
        }
    }

    public async void PlayBgm(string uniqueId, bool immediately = false)
    {
        if (!string.IsNullOrWhiteSpace(uniqueId))
        {
            if (!myAudioSource)
                return;

            if (myAudioSource.resource == bgmDic[uniqueId])
            {
                if (!myAudioSource.isPlaying)
                {
                    if(!immediately)
                        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                    
                    myAudioSource.Play();
                    currentBgm = uniqueId;
                    return;
                }
            }

            if (currentBgm == uniqueId)
                return;
        }
        
        // 서서히 음악 줄어들게 하기
        if(!immediately)
            await FadeVolume(0.05f);
        
        myAudioSource.Stop();

        if (string.IsNullOrWhiteSpace(uniqueId))
        {
            myAudioSource.volume = firstVolume;
            return;
        }
        
        if(!immediately)
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        
        myAudioSource.volume = firstVolume;
        myAudioSource.resource = bgmDic[uniqueId];
        
        myAudioSource.Play();
        currentBgm = uniqueId;
    }

    private async UniTask FadeVolume(float value)
    {
        while (myAudioSource.volume > 0)
        {
            myAudioSource.volume -= value;
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
        }

        myAudioSource.volume = 0;
    }

    public void Play()
    {
        myAudioSource.Play();
    }
    
    public void Stop()
    {
        myAudioSource.Stop();
        currentBgm = default;
    }

    public async void DelayStop(float value)
    {
        await FadeVolume(value);
        myAudioSource.Stop();
    }

    public bool IsPlaying()
    {
        return myAudioSource.isPlaying;
    }

    public void ReplayBgm()
    {
        Stop();
        Play();
    }
}
