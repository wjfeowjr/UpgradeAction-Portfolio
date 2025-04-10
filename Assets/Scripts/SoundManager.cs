using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SoundManager : Singleton<SoundManager>
{
    private AudioSource myAudioSource;
    private readonly Dictionary<string, AudioClip> soundDic = new Dictionary<string, AudioClip>();
    public AudioClip[] sounds;

    protected override void Awake()
    {
        base.Awake();
        myAudioSource = GetComponent<AudioSource>();
        
        foreach (var sound in sounds)
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

    public void PlaySound(string uniqueId)
    {
        if (!myAudioSource || uniqueId == ConstValues.None)
            return;
        
        myAudioSource.PlayOneShot(soundDic[uniqueId], 1.0f);
    }
}
