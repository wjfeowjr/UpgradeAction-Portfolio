using System;
using UnityEngine;

[Serializable]
public class SpawnObjectInfo
{
    public string id;
    public bool xFlip;
    public bool yFlip;
    public bool zFlip;
    public bool tracePos;
    public float objectTime;
    public string sound;
}

public class SpawnedObject : MonoBehaviour
{
    [SerializeField] private SpawnObjectInfo spawnObjectInfo;
    private Vector3 defaultScale;
    
    private float dir;
    private float leftObjectTime;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void Update()
    {
        ObjectTimer();
    }

    public void SetupData(SpawnedObjectData objectData, float dirX)
    {
        spawnObjectInfo = new SpawnObjectInfo();
        spawnObjectInfo.id = objectData.id;
        spawnObjectInfo.xFlip = objectData.xFlip;
        spawnObjectInfo.yFlip = objectData.yFlip;
        spawnObjectInfo.zFlip = objectData.zFlip;
        spawnObjectInfo.tracePos = objectData.tracePos;
        spawnObjectInfo.objectTime = objectData.objectTime;
        spawnObjectInfo.sound = objectData.sound;
        dir = dirX;
    }
    
    public void EnableSetting()
    {
        leftObjectTime = 0;
        float xScale = defaultScale.x;
        float yScale = defaultScale.y;
        float zScale = defaultScale.z;

        if (spawnObjectInfo.xFlip && dir < 0)
            xScale = -defaultScale.x;
        
        if (spawnObjectInfo.yFlip && dir < 0)
            yScale = -defaultScale.y;
        
        if (spawnObjectInfo.zFlip && dir < 0)
            zScale = -defaultScale.z;

        transform.localScale = new Vector3(xScale, yScale, zScale);
        SoundManager.Instance.PlaySound(spawnObjectInfo.sound);
    }
    
    private void ObjectTimer()
    {
        if (spawnObjectInfo.objectTime == 0)
            return;
        
        leftObjectTime += Time.deltaTime;

        if (leftObjectTime >= spawnObjectInfo.objectTime)
            gameObject.SetActive(false);
    }
    
    public float GetObjectTime()
    {
        return spawnObjectInfo.objectTime;
    }
    
    public bool GetTrace()
    {
        return spawnObjectInfo.tracePos;
    }
}
