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
        if (!myAudioSource || uniqueId == ConstValues.None)
            return;

        if (myAudioSource.resource == bgmDic[uniqueId])
            return;

        if (currentBgm == uniqueId)
            return;
        
        // 서서히 음악 줄어들게 하기
        if(!immediately)
            await FadeVolume();
        
        myAudioSource.Stop();
        
        if(!immediately)
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        
        myAudioSource.volume = firstVolume;
        myAudioSource.resource = bgmDic[uniqueId];
        
        myAudioSource.Play();
        currentBgm = uniqueId;
    }

    private async UniTask FadeVolume()
    {
        for (int i = 0; i < 5; i++)
        {
            myAudioSource.volume -= 0.05f;
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
        }
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
