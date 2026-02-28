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
    public bool timeScale;
    public Vector3 basicPosition;
    public Vector3 flipPosition;
    public Vector3 basicAngle;
    public Vector3 flipAngle;
    public float objectTime;
    public List<string> soundList = new List<string>();
    public float soundVolume;
    public Vector2 cameraShake;
    public float shakeTime;
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
        spawnObjectInfo.timeScale = objectData.timeScale;
        
        var basicPosition = objectData.basicPosition.Split(',');
        spawnObjectInfo.basicPosition = new Vector3(float.Parse(basicPosition[0]), float.Parse(basicPosition[1]), float.Parse(basicPosition[2]));
        
        var flipPosition = objectData.flipPosition.Split(',');
        spawnObjectInfo.flipPosition = new Vector3(float.Parse(flipPosition[0]), float.Parse(flipPosition[1]), float.Parse(flipPosition[2]));
        
        var basicAngle = objectData.basicAngle.Split(',');
        spawnObjectInfo.basicAngle = new Vector3(float.Parse(basicAngle[0]), float.Parse(basicAngle[1]), float.Parse(basicAngle[2]));
        
        var flipAngle = objectData.flipAngle.Split(',');
        spawnObjectInfo.flipAngle = new Vector3(float.Parse(flipAngle[0]), float.Parse(flipAngle[1]), float.Parse(flipAngle[2]));
        
        spawnObjectInfo.objectTime = objectData.objectTime;

        var soundArray = objectData.sound.Split(',');
        foreach (var sound in soundArray)
        {
            if (!string.IsNullOrWhiteSpace(sound))
            {
                spawnObjectInfo.soundList.Add(sound);
            }
        }

        spawnObjectInfo.soundVolume = objectData.soundVolume;
        
        var cameraShakeArray = objectData.cameraShake.Split(';');
        spawnObjectInfo.cameraShake = new Vector2(float.Parse(cameraShakeArray[0]), float.Parse(cameraShakeArray[1]));
        
        spawnObjectInfo.shakeTime = objectData.shakeTime;
        dir = dirX;
    }
    
    public void EnableSetting(bool ignoreSoundCondition = false)
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

                if (spawnObjectInfo.basicPosition != Vector3.zero)
                    firstChildTransform.localPosition = spawnObjectInfo.basicPosition;
            }
            else
            {
                firstChildTransform.eulerAngles = spawnObjectInfo.flipAngle;
                if (boxCollider2D) 
                    boxCollider2D.offset = reverseBoxColOffset;
                if (circleCollider2D)
                    circleCollider2D.offset = reverseCircleColOffset;
                
                if (spawnObjectInfo.flipPosition != Vector3.zero)
                    firstChildTransform.localPosition = spawnObjectInfo.flipPosition;
            }
        }

        transform.localScale = new Vector3(xScale, yScale, zScale);
        transform.eulerAngles = defaultAngle;

        foreach (var sound  in spawnObjectInfo.soundList)
            SoundManager.Instance.PlaySound(sound, ignoreSoundCondition);
        
        if(spawnObjectInfo.cameraShake != Vector2.zero)
            GameManager.Instance.CameraShake(spawnObjectInfo.cameraShake.x, spawnObjectInfo.cameraShake.y, spawnObjectInfo.shakeTime);
    }
    
    // 크기를 변경하는 특성은 여기서 관리
    public void AttributeCheck()
    {
        var upgradeList = GameManager.Instance.PlayerSkill.GetAttributeUpgrade(spawnObjectInfo.id);
        foreach (var upgrade in upgradeList)
        {
            switch (upgrade.upgradeId)
            {
                // 피해 증가
                case ConstValues.SizeUp:
                    var percentage = 1 + upgrade.upgradeValue * 0.01f;
                    var scale = transform.localScale;
                    transform.localScale = scale * percentage;
                    break;
            }
        }
    }
     
    private void ObjectTimer()
    {
        if (spawnObjectInfo == null)
        {
            Debug.Log(name);
            return;
        }
        
        if (spawnObjectInfo.objectTime == 0)
            return;
        
        if(spawnObjectInfo.timeScale)
            leftObjectTime += Time.deltaTime;
        else
            leftObjectTime += Time.unscaledDeltaTime;

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
