using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class AnimationsData
{
    public string id;
    public string caster;
    public string bodyType;
    public bool canFlip;
    public bool canMove;
    public string landingAnim;
    public string finishAnim;
}
//[Serializable]
public class AnimationsDataList
{
    public List<AnimationsData> Animations;
}

[Serializable]
public class AttackData
{
    public string id;
    public string effectType;
    public float effectTime;
    public string directionType;
    public int coefficient;
    public float knockBack;
    public string upperPower;
    public bool flipScale;
    public bool tracePos;
    public float colliderTime;
    public float objectTime;
    public string hitEffectId;
    public string sound;
}
//[Serializable]
public class AttackDataList
{
    public List<AttackData> Attack;
}

[Serializable]
public class MissileData
{
    public string id;
    public float speed;
    public bool piercingBullet;
    public float limitLength;
    public string hitTag;
    public string spawnObject;
}
[Serializable]
public class MissileDataList
{
    public List<MissileData> Missile;
}

public class TableManager : SingletonMono<TableManager>
{
    public AnimationsDataList animations;
    public AttackDataList attack;
    public MissileDataList missile;

    public void Init()
    {
        animations = LoadDataFromJson<AnimationsDataList>(ConstValues.Animations, ConstValues.Animations);
        attack = LoadDataFromJson<AttackDataList>(ConstValues.Attack, ConstValues.Attack);
        missile = LoadDataFromJson<MissileDataList>(ConstValues.Missile, ConstValues.Missile);
        Debug.Log($"{name} 초기화 완료");
    }
    
    private T LoadDataFromJson<T>(string fileName, string headerName)
    {
        var jsonText = Resources.Load<TextAsset>($"JsonFolder/{fileName}");
        if (jsonText == null)
        {
            Debug.LogError($"JSON 파일을 찾을 수 없다: {fileName}");
            return default;
        }

        // JSON이 배열 형식일 경우, { "headerName": [...] } 로 감싸기
        string wrappedJson = $"{{\"{headerName}\": {jsonText.text}}}";
        //Debug.Log($"Wrapped JSON: {wrappedJson}");
        var data = JsonUtility.FromJson<T>(wrappedJson);
        return data;
    }
}
