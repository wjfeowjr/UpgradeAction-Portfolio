using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class SpawnedObjectData
{
    public string id;
    public bool xFlip;
    public bool yFlip;
    public bool zFlip;
    public bool tracePos;
    public float objectTime;
    public string sound;
}
[Serializable]
public class SpawnedObjectDataList
{
    public List<SpawnedObjectData> SpawnedObject;
}

[Serializable]
public class AnimationsData
{
    public string id;
    public string caster;
    public string bodyType;
    public bool canFlip;
    public bool canMove;
    public float moveRatio;
}
[Serializable]
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
    public float colliderTime;
    public string hitEffectId;
}
[Serializable]
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

[Serializable]
public class PlayerData
{
    // 공통 데이터
    public string id;
    public int name;
    public string bodyType;
    public int hp;
    public int power;
    public int defence;
    public float moveSpeed;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
    public float weight;
    public int stagger;
    public float staggerTime;
    
    // 독립 데이터
    public int passiveComment;
    public string passive;
    public float jumpForce;
    public float jumpHeight;
    public int jumpAttackCount;
    public float jumpAttackForce;
}
[Serializable]
public class PlayerDataList
{
    public List<PlayerData> Player;
}

[Serializable]
public class MonsterData
{
    // 공통 데이터
    public string id;
    public int name;
    public string bodyType;
    public int hp;
    public int power;
    public int defence;
    public float moveSpeed;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
    public float weight;
    public int stagger;
    public float staggerTime;
    
    // 독립 데이터
    public bool standMotion;
    public float appearDelay;
    public float firstCoolTime;
    public float globalCoolTime;
    public string attackRange;
    public string coolTime;
    public string priority;
    public string pageHp;
    public string pagePattern;
    public float traceLength;
    public bool hovering;
    public string hoveringHeight;
    public float hoveringSpeed;
    public string appearShake;
    public string appearEffect;
    public string dyingMiniEffect;
    public string dyingEffect;
}
[Serializable]
public class MonsterDataList
{
    public List<MonsterData> Monster;
}

[Serializable]
public class SkillData
{
    public string id;
    public string type;
    public float coolTime;
    public string icon;
}
[Serializable]
public class SkillDataList
{
    public List<SkillData> Skill;
}

public class TableManager : SingletonMono<TableManager>
{
    public SpawnedObjectDataList spawnedObjectTable;
    public AnimationsDataList animationsTable;
    public AttackDataList attackTable;
    public MissileDataList missileTable;
    public PlayerDataList playerTable;
    public MonsterDataList monsterTable;
    public SkillDataList skillTable;

    public void Init()
    {
        spawnedObjectTable = LoadDataFromJson<SpawnedObjectDataList>(ConstValues.SpawnedObject);
        animationsTable = LoadDataFromJson<AnimationsDataList>(ConstValues.Animations);
        attackTable = LoadDataFromJson<AttackDataList>(ConstValues.Attack);
        missileTable = LoadDataFromJson<MissileDataList>(ConstValues.Missile);
        playerTable = LoadDataFromJson<PlayerDataList>(ConstValues.Player);
        monsterTable = LoadDataFromJson<MonsterDataList>(ConstValues.Monster);
        skillTable = LoadDataFromJson<SkillDataList>(ConstValues.Skill);
        
        Debug.Log($"{name} 초기화 완료");
    }
    
    private T LoadDataFromJson<T>(string fileName)
    {
        var jsonText = Resources.Load<TextAsset>($"JsonFolder/{fileName}");
        if (jsonText == null)
        {
            Debug.LogError($"JSON 파일을 찾을 수 없다: {fileName}");
            return default;
        }
        
        //Debug.Log(jsonText.text);
        var data = JsonUtility.FromJson<T>(jsonText.text);
        return data;
    }
}
