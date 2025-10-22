using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SoundManager : Singleton<SoundManager>
{
    private AudioSource myAudioSource;
    private readonly Dictionary<string, AudioClip> soundDic = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();
    private readonly float minInterval = 0.05f; // 최소 0.05초 간격으로 제한
    
    [SerializeField] private List<AudioClip> soundList = new List<AudioClip>();

    protected override void Awake()
    {
        base.Awake();
        myAudioSource = GetComponent<AudioSource>();
        
        foreach (var sound in soundList)
        {
            string soundName = sound.name;
            if (soundDic.ContainsKey(soundName))
            {
                Debug.LogError(soundName);
                continue;
            }
            soundDic.Add(soundName, sound);
        }
    }

    public void PlaySound(string uniqueId, float volumeScale = 0.8f)
    {
        if (!myAudioSource || uniqueId == ConstValues.None || !soundDic.ContainsKey(uniqueId))
            return;
        
        float now = Time.time;
        
        if (lastPlayTime.TryGetValue(uniqueId, out float t) && now - t < minInterval)
            return;
        
        lastPlayTime[uniqueId] = now;
        myAudioSource.PlayOneShot(soundDic[uniqueId], volumeScale);
    }
    
    public void PlaySoundNotCondition(string uniqueId, float volumeScale = 0.8f)
    {
        if (!myAudioSource || uniqueId == ConstValues.None || !soundDic.ContainsKey(uniqueId))
            return;
        
        myAudioSource.PlayOneShot(soundDic[uniqueId], volumeScale);
    }

    public List<AudioClip> GetSoundList()
    {
        return soundList;
    }
}
