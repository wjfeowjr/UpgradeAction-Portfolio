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
    public bool timeScale;
    public string basicAngle;
    public string flipAngle;
    public float objectTime;
    public string sound;
    public float soundVolume;
    public string cameraShake;
    public float shakeTime;
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
    public bool ignoreSuperArmor;
    public bool ignoreImmortal;
    public bool continuous;
    public bool duplicate;
    public string directionType;
    public int coefficient;
    public int stagger;
    public float knockBack;
    public string upperPower;
    public int customDir;
    public float colliderTime;
    public string hitShake;
    public float shakeTime;
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
    public string type;
    public float speed;
    public float limitLength;
    public string hitTag;
    public string spawnObject;
    public bool hitSpawn;
    public bool afterImage;
}
[Serializable]
public class MissileDataList
{
    public List<MissileData> Missile;
}

[Serializable]
public class GrenadeData
{
    public string id;
    public string minForce;
    public string maxForce;
    public bool dirObject;
    public string hitTag;
    public string spawnObject;
}
[Serializable]
public class GrenadeDataList
{
    public List<GrenadeData> Grenade;
}

[Serializable]
public class PlayerData
{
    // 공통 데이터
    public string id;
    public int talk;
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
    public int talk;
    public string bodyType;
    public int hp;
    public int power;
    public int defence;
    public float moveSpeed;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
    public int gold;
    public float weight;
    public int stagger;
    public float staggerTime;
    
    // 독립 데이터
    public bool standMotion;
    public float appearDelay;
    public float firstCoolTime;
    public float globalCoolTime;
    public string attackRange;
    public string agroRange;
    public string jumpRange;
    public string dropRange;
    public string coolTime;
    public string priority;
    public string pageHp;
    public string pagePattern;
    public float traceLength;
    public bool hovering;
    public string hoveringHeight;
    public float hoveringSpeed;
    public float customPatrol;
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
    public string caster;
    public string coolTime;
    public int talk;
    public int explainTalk;
}
[Serializable]
public class SkillDataList
{
    public List<SkillData> Skill;
}

[Serializable]
public class SkillAttributeData
{
    public string id;
    public string skill;
    public int level;
    public int cost;
    public int talk;
    public int explainTalk;
}
[Serializable]
public class SkillAttributeDataList
{
    public List<SkillAttributeData> SkillAttribute;
}

[Serializable]
public class RoomsData
{
    public string id;
    public int productCount;
    public string productIdx;
    public string skill;
    public string treasureBox;
    public string namedMonster;
    public string npcAppearProductIdx;
    public string bgSprite;
    public bool bgDeco;
    public string bgm;
}
[Serializable]
public class RoomsDataList
{
    public List<RoomsData> Rooms;
}

[Serializable]
public class NpcData
{
    public string id;
    public int talk;
    public string startDialog;
    public string dialogKey;
}
[Serializable]
public class NpcDataList
{
    public List<NpcData> Npc;
}

[Serializable]
public class DialogueData
{
    public string id;
    public bool isSpeaker;
    public int talk;
    public bool isEnd;
    public string choiceGroupId;
    public string speechFrame;
    public string speechPose;
    public string sound;
    public string cameraShake;
    public float shakeTime;
    public string checkKey;
    public bool checkKeyValue;
    public string endEvent;
    public string reward;
}
[Serializable]
public class DialogueDataList
{
    public List<DialogueData> Dialogue;
}

[Serializable]
public class DialogueChoiceData
{
    public string id;
    public string npc;
    public int talk;
}
[Serializable]
public class DialogueChoiceDataList
{
    public List<DialogueChoiceData> DialogueChoice;
}

[Serializable]
public class ProductDialogueData
{
    public string id;
    public int talk;
}
[Serializable]
public class ProductDialogueDataList
{
    public List<ProductDialogueData> ProductDialogue;
}

[Serializable]
public class TalkData
{
    public int idx;
    public string kr;
}
[Serializable]
public class TalkDataList
{
    public List<TalkData> Talk;
}

public class TableManager : SingletonMono<TableManager>
{
    public SpawnedObjectDataList spawnedObjectTable;
    public AnimationsDataList animationsTable;
    public AttackDataList attackTable;
    public MissileDataList missileTable;
    public GrenadeDataList grenadeTable;
    public PlayerDataList playerTable;
    public MonsterDataList monsterTable;
    public SkillDataList skillTable;
    public SkillAttributeDataList skillAttributeTable;
    public RoomsDataList roomsTable;
    public NpcDataList npcTable;
    public DialogueDataList dialogueTable;
    public DialogueChoiceDataList dialogueChoiceTable;
    public ProductDialogueDataList productDialogueTable;
    public TalkDataList talkTable;
    
    public void Init()
    {
        spawnedObjectTable = LoadDataFromJson<SpawnedObjectDataList>(ConstValues.SpawnedObject);
        animationsTable = LoadDataFromJson<AnimationsDataList>(ConstValues.Animations);
        attackTable = LoadDataFromJson<AttackDataList>(ConstValues.Attack);
        missileTable = LoadDataFromJson<MissileDataList>(ConstValues.Missile);
        grenadeTable = LoadDataFromJson<GrenadeDataList>(ConstValues.Grenade);
        playerTable = LoadDataFromJson<PlayerDataList>(ConstValues.Player);
        monsterTable = LoadDataFromJson<MonsterDataList>(ConstValues.Monster);
        skillTable = LoadDataFromJson<SkillDataList>(ConstValues.Skill);
        skillAttributeTable = LoadDataFromJson<SkillAttributeDataList>(ConstValues.SkillAttribute);
        roomsTable = LoadDataFromJson<RoomsDataList>(ConstValues.Rooms);
        npcTable = LoadDataFromJson<NpcDataList>(ConstValues.Npc);
        dialogueTable = LoadDataFromJson<DialogueDataList>(ConstValues.Dialogue);
        dialogueChoiceTable = LoadDataFromJson<DialogueChoiceDataList>(ConstValues.DialogueChoice);
        productDialogueTable = LoadDataFromJson<ProductDialogueDataList>(ConstValues.ProductDialogue);
        talkTable = LoadDataFromJson<TalkDataList>(ConstValues.Talk);
        
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
