using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BgmManager : Singleton<BgmManager>
{
    private AudioSource myAudioSource;
    private readonly Dictionary<string, AudioClip> bgmDic = new Dictionary<string, AudioClip>();
    public AudioClip[] bgmArray;
    public string currentBgm;

    protected override void Awake()
    {
        base.Awake();
        myAudioSource = GetComponent<AudioSource>();
        
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
 
    public void PlayBgm(string uniqueId)
    {
        if (!myAudioSource || uniqueId == ConstValues.None)
            return;

        if (myAudioSource.resource == bgmDic[uniqueId])
            return;

        if (currentBgm == uniqueId)
            return;
        
        myAudioSource.Stop();
        myAudioSource.resource = bgmDic[uniqueId];
        myAudioSource.Play();
        currentBgm = uniqueId;
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
