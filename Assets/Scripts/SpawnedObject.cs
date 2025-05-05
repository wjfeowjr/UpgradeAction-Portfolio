using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class SpawnObjectInfo
{
    public string id;
    public bool xFlip;
    public bool yFlip;
    public bool zFlip;
    public bool tracePos;
    public Vector3 basicAngle;
    public Vector3 flipAngle;
    public float objectTime;
    public List<string> soundList = new List<string>();
    public Vector2 cameraShake;
}

public class SpawnedObject : MonoBehaviour
{
    [SerializeField] private SpawnObjectInfo spawnObjectInfo;

    private BoxCollider2D boxCollider2D;
    private Vector2 defaultBoxColOffset;
    private Vector2 reverseBoxColOffset;
    
    private CircleCollider2D circleCollider2D;
    private Vector2 defaultCircleColOffset;
    private Vector2 reverseCircleColOffset;
    
    private Vector3 defaultScale;
    private Vector3 defaultAngle;
    
    private float dir;
    private float leftObjectTime;

    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        if (boxCollider2D)
        {
            defaultBoxColOffset = boxCollider2D.offset;
            reverseBoxColOffset = new Vector2(-boxCollider2D.offset.x, boxCollider2D.offset.y);
        }
        
        circleCollider2D = GetComponent<CircleCollider2D>();
        if (circleCollider2D)
        {
            defaultCircleColOffset = circleCollider2D.offset;
            reverseCircleColOffset = new Vector2(-circleCollider2D.offset.x, circleCollider2D.offset.y);
        }

        defaultScale = transform.localScale;
        defaultAngle = transform.eulerAngles;
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
        
        var basicAngle = objectData.basicAngle.Split(',');
        spawnObjectInfo.basicAngle = new Vector3(float.Parse(basicAngle[0]), float.Parse(basicAngle[1]), float.Parse(basicAngle[2]));
        
        var flipAngle = objectData.flipAngle.Split(',');
        spawnObjectInfo.flipAngle = new Vector3(float.Parse(flipAngle[0]), float.Parse(flipAngle[1]), float.Parse(flipAngle[2]));
        
        spawnObjectInfo.objectTime = objectData.objectTime;

        var soundArray = objectData.sound.Split(',');
        foreach (var sound in soundArray)
            spawnObjectInfo.soundList.Add(sound);
        
        var cameraShakeArray = objectData.cameraShake.Split(';');
        spawnObjectInfo.cameraShake = new Vector2(float.Parse(cameraShakeArray[0]), float.Parse(cameraShakeArray[1]));

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

        if (spawnObjectInfo.flipAngle != Vector3.zero)
        {
            Transform firstChildTransform = transform.GetChild(0);

            if (dir > 0)
            {
                firstChildTransform.eulerAngles = spawnObjectInfo.basicAngle;
                if (boxCollider2D)
                    boxCollider2D.offset = defaultBoxColOffset;
                if (circleCollider2D)
                    circleCollider2D.offset = defaultCircleColOffset;
            }
            else
            {
                firstChildTransform.eulerAngles = spawnObjectInfo.flipAngle;
                if (boxCollider2D)
                    boxCollider2D.offset = reverseBoxColOffset;
                if (circleCollider2D)
                    circleCollider2D.offset = reverseCircleColOffset;
            }
        }

        transform.localScale = new Vector3(xScale, yScale, zScale);
        transform.eulerAngles = defaultAngle;

        foreach (var sound  in spawnObjectInfo.soundList)
            SoundManager.Instance.PlaySound(sound);
        
        if(spawnObjectInfo.cameraShake != Vector2.zero)
            GameManager.Instance.CameraShake(spawnObjectInfo.cameraShake.x, spawnObjectInfo.cameraShake.y);
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
