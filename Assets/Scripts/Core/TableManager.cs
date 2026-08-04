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
public class ArenaData
{
    public string id;
    public int round;
    public string monster;
    public int posIdx;
    public float range;
}
[Serializable]
public class ArenaDataList
{
    public List<ArenaData> Arena;
}

[Serializable]
public class AttackData
{
    public string id;
    public string effectType;
    public float effectTime;
    public string deBuff;
    public string deBuffPercent;
    public string deBuffTime;
    public bool ignoreSuperArmor;
    public bool ignoreImmortal;
    public bool respawnAttack;
    public bool destroyProjectile;
    public bool continuous;
    public float continuousDelay;
    public bool duplicate;
    public string directionType;
    public int coefficient;
    public int criticalChance;
    public int stagger;
    public int gainResource;
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
public class BuffData
{
    public string id;
    public string buffType;
    public string buffPos;
}
[Serializable]
public class BuffDataList
{
    public List<BuffData> Buff;
}

[Serializable]
public class DialogueData
{
    public string id;
    public string speaker;
    public int talk;
    public bool isEnd;
    public string choiceGroupId;
    public string speechFrame;
    public string poseCharacter;
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
    public string checkKey;
    public string checkKeyValue;
}
[Serializable]
public class DialogueChoiceDataList
{
    public List<DialogueChoiceData> DialogueChoice;
}

[Serializable]
public class GrenadeData
{
    public string id;
    public string minForce;
    public string maxForce;
    public float timer;
    public bool spinGrenade;
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
public class ItemData
{
    public string id;
    public int name;
    public int explain;
    public string rank;
    public string type;
}
[Serializable]
public class ItemDataList
{
    public List<ItemData> Item;
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
public class NpcData
{
    public string id;
    public int talk;
    public string firstDialog;
    public string startDialog;
    public string dialogKey;
    public string questItemId;
    public string questItemCount;
    public string questClearChoice;
}
[Serializable]
public class NpcDataList
{
    public List<NpcData> Npc;
}

[Serializable]
public class PassiveData
{
    public string id;
    public int valueResource;
    public string resourceStat;
    public int resourceValue;
    public string resourceUnit;
    public int getBuffResource;
    public float buffTime;
    public string buffId;
    public int buffValue;
    public string buffUnit;
    public int penaltyValue;
    public int passiveName;
    public int passiveExplain;
}
[Serializable]
public class PassiveDataList
{
    public List<PassiveData> Passive;
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
    public string passive;
    public int resource;
    public float jumpHeight;
}
[Serializable]
public class PlayerDataList
{
    public List<PlayerData> Player;
}

[Serializable]
public class RelicData
{
    public string id;
    public string stat;
    public string value;
    public string specialValue;
}
[Serializable]
public class RelicDataList
{
    public List<RelicData> Relic;
}

[Serializable]
public class RoomsData
{
    public string id;
    public string productIdx;
    public string npc;
    public string customObject;
    public string skill;
    public string treasureBox;
    public int attributePoint;
    public string relic;
    public int potion;
    public string item;
    public string namedMonster;
    public string bgSprite;
    public bool bgDeco;
    public string bgm;
    public string place;
}
[Serializable]
public class RoomsDataList
{
    public List<RoomsData> Rooms;
}

[Serializable]
public class SkillData
{
    public string id;
    public string type;
    public string caster;
    public string coolTime;
    public string buffName;
    public string buffValue;
    public float buffTime;
    public int buffCount;
    public float skillSpeed;
    public string skillArmor;
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
    public string targetObject;
    public int cost;
    public string passiveId;
    public string addObjectId;
    public string objectId;
    public int objectCount;
    public string upgradeId;
    public string upgradeValue;
    public string buffId;
    public string deBuffId;
    public float buffTime;
    public int buffValue;
    public int talk;
    public int explainTalk;
    public bool firstLock;
}
[Serializable]
public class SkillAttributeDataList
{
    public List<SkillAttributeData> SkillAttribute;
}

[Serializable]
public class SpawnedObjectData
{
    public string id;
    public bool xFlip;
    public bool yFlip;
    public bool zFlip;
    public bool tracePos;
    public bool timeScale;
    public string basicPosition;
    public string flipPosition;
    public string basicAngle;
    public string flipAngle;
    public string laserAngle;
    public string flipLaserAngle;
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
public class StoreItemData
{
    public string id;
    public string storeId;
    public int cost;
}
[Serializable]
public class StoreItemDataList
{
    public List<StoreItemData> StoreItem;
}

[Serializable]
public class TalkData
{
    public int idx;
    public string kr;
    public string en;
    public string ja;
    public string cn;
    public string tw;
    public string es;
    public string ru;
    public string pt;
    
}
[Serializable]
public class TalkDataList
{
    public List<TalkData> Talk;
}

public class TableManager : SingletonMono<TableManager>
{
    public AnimationsDataList animationsTable;
    public ArenaDataList arenaTable;
    public AttackDataList attackTable;
    public BuffDataList buffTable;
    public DialogueDataList dialogueTable;
    public DialogueChoiceDataList dialogueChoiceTable;
    public GrenadeDataList grenadeTable;
    public ItemDataList itemTable;
    public MissileDataList missileTable;
    public MonsterDataList monsterTable;
    public NpcDataList npcTable;
    public PassiveDataList passiveTable;
    public PlayerDataList playerTable;
    public RelicDataList relicTable;
    public RoomsDataList roomsTable;
    public SkillDataList skillTable;
    public SkillAttributeDataList skillAttributeTable;
    public SpawnedObjectDataList spawnedObjectTable;
    public StoreItemDataList storeItemTable;
    public TalkDataList talkTable;
    
    public void Init()
    {
        animationsTable = LoadDataFromJson<AnimationsDataList>(ConstValues.Animations);
        arenaTable = LoadDataFromJson<ArenaDataList>(ConstValues.Arena);
        attackTable = LoadDataFromJson<AttackDataList>(ConstValues.Attack);
        buffTable = LoadDataFromJson<BuffDataList>(ConstValues.Buff);
        dialogueTable = LoadDataFromJson<DialogueDataList>(ConstValues.Dialogue);
        dialogueChoiceTable = LoadDataFromJson<DialogueChoiceDataList>(ConstValues.DialogueChoice);
        grenadeTable = LoadDataFromJson<GrenadeDataList>(ConstValues.Grenade);
        itemTable = LoadDataFromJson<ItemDataList>(ConstValues.Item);
        missileTable = LoadDataFromJson<MissileDataList>(ConstValues.Missile);
        monsterTable = LoadDataFromJson<MonsterDataList>(ConstValues.Monster);
        npcTable = LoadDataFromJson<NpcDataList>(ConstValues.Npc);
        passiveTable = LoadDataFromJson<PassiveDataList>(ConstValues.Passive);
        playerTable = LoadDataFromJson<PlayerDataList>(ConstValues.Player);
        relicTable = LoadDataFromJson<RelicDataList>(ConstValues.Relic);
        roomsTable = LoadDataFromJson<RoomsDataList>(ConstValues.Rooms);
        skillTable = LoadDataFromJson<SkillDataList>(ConstValues.Skill);
        skillAttributeTable = LoadDataFromJson<SkillAttributeDataList>(ConstValues.SkillAttribute);
        spawnedObjectTable = LoadDataFromJson<SpawnedObjectDataList>(ConstValues.SpawnedObject);
        storeItemTable = LoadDataFromJson<StoreItemDataList>(ConstValues.StoreItem);
        talkTable = LoadDataFromJson<TalkDataList>(ConstValues.Talk);

        BuildIndexes();
        GameLog.Info($"{name} 초기화 완료");
    }

    private T LoadDataFromJson<T>(string fileName)
    {
        var jsonText = Resources.Load<TextAsset>($"JsonFolder/{fileName}");
        if (jsonText == null)
        {
            Debug.LogError($"JSON 파일을 찾을 수 없다: {fileName}");
            return default;
        }

        var data = JsonUtility.FromJson<T>(jsonText.text);
        return data;
    }

    #region id 조회 인덱스
    // 테이블은 로드 후 변하지 않으므로 id -> 데이터 사전을 한 번만 만들어 둔다.
    //
    // 이전에는 조회할 때마다 Find(x => x.id == id) 로 리스트 전체를 훑었다.
    // SpawnedObject 는 439개인데 이펙트가 생성될 때마다, Attack 은 152개인데
    // 공격 판정이 만들어질 때마다 호출되므로 전투 중 반복 비용이 컸다.
    // 람다가 지역 변수를 캡처해 호출마다 클로저까지 할당하고 있었다.
    //
    // 원본 List 는 그대로 둔다. 인스펙터 표시와 순회가 필요한 곳이 있고,
    // 사전은 조회 전용으로만 쓴다.

    private Dictionary<string, AttackData> attackById;
    private Dictionary<string, BuffData> buffById;
    private Dictionary<string, DialogueData> dialogueById;
    private Dictionary<string, MissileData> missileById;
    private Dictionary<string, MonsterData> monsterById;
    private Dictionary<string, PlayerData> playerById;
    private Dictionary<string, RoomsData> roomsById;
    private Dictionary<string, SkillData> skillById;
    private Dictionary<string, SpawnedObjectData> spawnedObjectById;
    private Dictionary<string, StoreItemData> storeItemById;

    private void BuildIndexes()
    {
        attackById        = BuildIndex(attackTable?.Attack,               x => x.id, nameof(AttackData));
        buffById          = BuildIndex(buffTable?.Buff,                   x => x.id, nameof(BuffData));
        dialogueById      = BuildIndex(dialogueTable?.Dialogue,           x => x.id, nameof(DialogueData));
        missileById       = BuildIndex(missileTable?.Missile,             x => x.id, nameof(MissileData));
        monsterById       = BuildIndex(monsterTable?.Monster,             x => x.id, nameof(MonsterData));
        playerById        = BuildIndex(playerTable?.Player,               x => x.id, nameof(PlayerData));
        roomsById         = BuildIndex(roomsTable?.Rooms,                 x => x.id, nameof(RoomsData));
        skillById         = BuildIndex(skillTable?.Skill,                 x => x.id, nameof(SkillData));
        spawnedObjectById = BuildIndex(spawnedObjectTable?.SpawnedObject, x => x.id, nameof(SpawnedObjectData));
        storeItemById     = BuildIndex(storeItemTable?.StoreItem,         x => x.id, nameof(StoreItemData));
    }

    /// <summary>
    /// id 가 겹치면 먼저 나온 것을 쓴다. 기존 Find 동작과 동일하다.
    /// 중복은 시트 실수이므로 경고를 남긴다.
    /// </summary>
    private static Dictionary<string, T> BuildIndex<T>(List<T> list, Func<T, string> keySelector, string typeName)
    {
        var dic = new Dictionary<string, T>();
        if (list == null)
            return dic;

        foreach (var item in list)
        {
            var key = keySelector(item);
            if (string.IsNullOrEmpty(key))
                continue;

            if (dic.ContainsKey(key))
            {
                Debug.LogWarning($"[Table] {typeName} 에 중복된 id 가 있습니다: {key}");
                continue;
            }
            dic.Add(key, item);
        }
        return dic;
    }

    private static T Get<T>(Dictionary<string, T> dic, string id, string typeName) where T : class
    {
        if (dic == null || string.IsNullOrEmpty(id))
            return null;

        return dic.TryGetValue(id, out var data) ? data : null;
    }

    public AttackData GetAttack(string id) => Get(attackById, id, nameof(AttackData));
    public BuffData GetBuff(string id) => Get(buffById, id, nameof(BuffData));
    public DialogueData GetDialogue(string id) => Get(dialogueById, id, nameof(DialogueData));
    public MissileData GetMissile(string id) => Get(missileById, id, nameof(MissileData));
    public MonsterData GetMonster(string id) => Get(monsterById, id, nameof(MonsterData));
    public PlayerData GetPlayer(string id) => Get(playerById, id, nameof(PlayerData));
    public RoomsData GetRoom(string id) => Get(roomsById, id, nameof(RoomsData));
    public SkillData GetSkill(string id) => Get(skillById, id, nameof(SkillData));
    public SpawnedObjectData GetSpawnedObject(string id) => Get(spawnedObjectById, id, nameof(SpawnedObjectData));
    public StoreItemData GetStoreItem(string id) => Get(storeItemById, id, nameof(StoreItemData));

    // 인덱스 검증용
    public int IndexedCount(string typeName) => typeName switch
    {
        nameof(AttackData) => attackById?.Count ?? 0,
        nameof(SpawnedObjectData) => spawnedObjectById?.Count ?? 0,
        nameof(MissileData) => missileById?.Count ?? 0,
        _ => 0,
    };
    #endregion
}
