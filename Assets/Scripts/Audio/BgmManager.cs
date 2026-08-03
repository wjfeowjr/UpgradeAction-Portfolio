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

    // BGM 요청 버전. 새 요청(PlayBgm/Stop/DelayStop)이 들어오면 증가하며,
    // 진행 중이던 이전 요청은 await 지점마다 이 값을 비교해 스스로 중단한다
    private int playRequestId;

    public async void PlayBgm(string uniqueId, bool immediately = false)
    {
        if (!myAudioSource)
            return;

        int requestId = ++playRequestId;

        if (!string.IsNullOrWhiteSpace(uniqueId))
        {
            // 이미 같은 BGM을 재생 중이면 볼륨만 복구
            // (다른 BGM으로 전환하다 되돌아온 경우 페이드로 볼륨이 줄어 있을 수 있음)
            if (currentBgm == uniqueId && myAudioSource.isPlaying)
            {
                myAudioSource.volume = firstVolume;
                return;
            }

            // 같은 곡이 멈춰있으면 이어서 재생
            if (myAudioSource.resource == bgmDic[uniqueId] && !myAudioSource.isPlaying)
            {
                if (!immediately)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                    if (requestId != playRequestId)
                        return;
                }

                myAudioSource.volume = firstVolume;
                myAudioSource.Play();
                currentBgm = uniqueId;
                return;
            }
        }

        // 서서히 음악 줄어들게 하기
        if (!immediately && !await FadeVolume(0.05f, requestId))
            return;

        myAudioSource.Stop();

        if (string.IsNullOrWhiteSpace(uniqueId))
        {
            myAudioSource.volume = firstVolume;
            return;
        }

        if (!immediately)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            if (requestId != playRequestId)
                return;
        }

        myAudioSource.volume = firstVolume;
        myAudioSource.resource = bgmDic[uniqueId];

        myAudioSource.Play();
        currentBgm = uniqueId;
    }

    // 페이드 도중 새 요청이 들어오면 중단하고 false 반환
    private async UniTask<bool> FadeVolume(float value, int requestId)
    {
        while (myAudioSource.volume > 0)
        {
            myAudioSource.volume -= value;
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

            if (requestId != playRequestId)
                return false;
        }

        myAudioSource.volume = 0;
        return true;
    }

    public void Play()
    {
        myAudioSource.Play();
    }
    
    public void Stop()
    {
        // 진행 중이던 BGM 전환 요청도 함께 무효화
        playRequestId++;
        myAudioSource.Stop();
        currentBgm = default;
    }

    public async void DelayStop(float value)
    {
        int requestId = ++playRequestId;
        if (!await FadeVolume(value, requestId))
            return;

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
