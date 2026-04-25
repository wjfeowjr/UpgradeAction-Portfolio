using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PortalObject : InteractionController
{
    [SerializeField] private Transform playerPos;
    [SerializeField] private GameObject minimapObject;
    [SerializeField] private AudioSource myAudioSource;
    [SerializeField] private string targetRoom;

    private float volume;

    public Transform PlayerPos => playerPos;
    public Vector2 ColSize => GetComponent<BoxCollider2D>().size;
    public GameObject MinimapObject => minimapObject;
    public string TargetRoom => targetRoom;

    private void Awake()
    {
        volume = myAudioSource.volume;
    }

    private void OnEnable()
    {
        myAudioSource.volume = volume;
    }

    public void SetPortalAction(Action action)
    {
        SetInteractionAction(action, 30018, GameManager.Instance.upKey);
    }
    
    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }

    public void SoundActive(bool active)
    {
        if (active)
            myAudioSource.volume = volume;
        else
            myAudioSource.volume = 0;
    }
}
