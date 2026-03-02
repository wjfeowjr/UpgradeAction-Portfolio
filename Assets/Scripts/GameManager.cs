using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.UI;

public static class SaveSystem
{
    // 원하는대로 폴더명 바꾸면 됨 (ex: "Save", "Saves", "Profile" 등)
    private const string SaveFolderName = "Save";

    /// <summary>
    /// 예: Windows 기준
    /// C:\Users\{User}\AppData\LocalLow\{CompanyName}\{ProductName}\Saves
    /// </summary>
    public static string SaveDirectory
    {
        get
        {
            // persistentDataPath = LocalLow\CompanyName\ProductName
            return Path.Combine(Application.persistentDataPath, SaveFolderName);
        }
    }

    /// <summary>
    /// 파일명에 .json이 없으면 자동으로 붙임
    /// </summary>
    public static string GetSavePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("fileName is null/empty");

        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            fileName += ".json";

        return Path.Combine(SaveDirectory, fileName);
    }

    public static void Save<T>(string fileName, T data)
    {
        try
        {
            Directory.CreateDirectory(SaveDirectory);

            // JsonUtility는 클래스/구조체의 public 필드 또는 [SerializeField] 필드만 직렬화함
            string json = JsonUtility.ToJson(data, prettyPrint: true);

            string path = GetSavePath(fileName);

            // 한글 등 깨짐 방지: UTF8
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

#if UNITY_EDITOR
            Debug.Log($"[SaveSystem] Saved");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed ({fileName}): {e}");
        }
    }

    public static bool TryLoad<T>(string fileName, out T data)
    {
        data = default;

        try
        {
            string path = GetSavePath(fileName);
            if (!File.Exists(path))
                return false;

            string json = File.ReadAllText(path, Encoding.UTF8);
            data = JsonUtility.FromJson<T>(json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed ({fileName}): {e}");
            return false;
        }
    }

    public static bool Exists(string fileName)
    {
        string path = GetSavePath(fileName);
        return File.Exists(path);
    }

    public static void Delete(string fileName)
    {
        try
        {
            string path = GetSavePath(fileName);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Delete failed ({fileName}): {e}");
        }
    }
}

public static class KeyBinding
{
    // 저장할 때
    public static void SaveKey(string prefKey, KeyCode key)
    {
        PlayerPrefs.SetInt(prefKey, (int)key);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 키도 지정 가능)
    public static KeyCode LoadKey(string prefKey, KeyCode defaultKey)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            Debug.Log($"저장된 키값 {PlayerPrefs.GetInt(prefKey)}을 불러왔습니다");
            return (KeyCode)PlayerPrefs.GetInt(prefKey);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"최초 키 설정: {prefKey}를 {defaultKey}로 저장");
            SaveKey(prefKey, defaultKey);
            return defaultKey;
        }
    }
}

[Serializable]
public class Skill
{
    public string skillId;
    public List<string> attributeList = new List<string>();
}
[Serializable]
public class SkillSetting
{
    public int attributePoint;
    public List<Skill>skillList;
}
[Serializable]
public class SkillAttributeInfo
{
    public string id;
    public string skill;
    public string targetObject;
    public int cost;
    public List<string> passiveId = new List<string>();
    
    public string addObjectId;
    public string objectId;
    public int objectCount;
    
    public List<string> upgradeId = new List<string>();
    public List<int> upgradeValue = new List<int>();
    public string buffId;
    public float buffTime;
    public int buffValue;
    public int talk;
    public int explainTalk;
}

[Serializable]
public class SkillAttributeAddObjectInfo
{
    public string addObjectId;
    public string objectId;
    public int objectCount;
}

[Serializable]
public class SkillAttributeUpgradeInfo
{
    public string upgradeId;
    public int upgradeValue;
}
[Serializable]
public class SkillAttributeBuffInfo
{
    public string buffId;
    public float buffTime;
    public int buffValue;
}

[Serializable]
public class SkillCollection
{
    public int totalAttributePoint;
    public SkillSetting berserkerSkillSetting;
    public SkillSetting gunnerSkillSetting;

    public void PlusAttributePoint(int point)
    {
        totalAttributePoint += point;
        berserkerSkillSetting.attributePoint += point;
        gunnerSkillSetting.attributePoint += point;
    }
    
    public bool IsHaveSkill(string skillId)
    {
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (berserkerSkill != null)
            return true;

        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
            return true;

        return false;
    }
    public List<string> GetSkillAttribute(string id)
    {
        var berserkerSkillList = berserkerSkillSetting.skillList.Find(x => x.skillId == id).attributeList;
        if (berserkerSkillList != null)
            return berserkerSkillList;
        
        var gunnerSkillList = gunnerSkillSetting.skillList.Find(x => x.skillId == id).attributeList;
        if (gunnerSkillList != null)
            return gunnerSkillList;

        Debug.Log("검색되는 특성 없음");
        return null;
    }
    public bool IsHaveAttribute(string skillId, string attributeId)
    {
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (berserkerSkill != null)
            return berserkerSkill.attributeList.Contains(attributeId);
        
        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
            return gunnerSkill.attributeList.Contains(attributeId);
        
        Debug.Log("해당 특성 자체가 없음");
        return false;
    }
    public async void BuyAttribute(string skillId, string attributeId, Vector3 effectPos)
    {
        var attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.id == attributeId);
        
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (berserkerSkill != null)
        {
            var targetAttribute = berserkerSkill.attributeList.Contains(attributeId);
            if (!targetAttribute)
            {
                if (berserkerSkillSetting.attributePoint < attributeData[0].cost)
                {
                    SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
                    await GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30202));
                }
                else
                {
                    berserkerSkill.attributeList.Add(attributeId);
                    berserkerSkillSetting.attributePoint -= attributeData[0].cost;
                    // 올리는 연출 넣기
                    GameManager.Instance.SpawnHighestObject(ConstValues.AttributeUpEffect, effectPos);
                }
            }
        }
        
        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
        {
            var targetAttribute = gunnerSkill.attributeList.Contains(attributeId);
            
            if (!targetAttribute)
            {
                if (gunnerSkillSetting.attributePoint < attributeData[0].cost)
                {
                    SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
                    await GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30202));
                }
                else
                {
                    gunnerSkill.attributeList.Add(attributeId);
                    gunnerSkillSetting.attributePoint -= attributeData[0].cost;
                    // 올리는 연출 넣기
                    GameManager.Instance.SpawnHighestObject(ConstValues.AttributeUpEffect, effectPos);
                }
            }
        }
    }
    
    public async void SellAttribute(string skillId, string attributeId, Vector3 effectPos)
    {
        var attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.id == attributeId);
        
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (berserkerSkill != null)
        {
            var targetAttribute = berserkerSkill.attributeList.Contains(attributeId);

            if (targetAttribute)
            {
                var attributeList = attributeData.FindAll(x => x.skill == skillId);
                var attribute = attributeList.Find(x => x.id == attributeId);
                berserkerSkillSetting.attributePoint += attribute.cost;
                berserkerSkill.attributeList.Remove(attributeId);
                // 내리는 연출 넣기
                GameManager.Instance.SpawnHighestObject(ConstValues.AttributeDownEffect, effectPos);
            }
        }
        
        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
        {
            var targetAttribute = gunnerSkill.attributeList.Contains(attributeId);
            
            if (targetAttribute)
            {
                var attributeList = attributeData.FindAll(x => x.skill == skillId);
                var attribute = attributeList.Find(x => x.id == attributeId);
                gunnerSkillSetting.attributePoint += attribute.cost;
                gunnerSkill.attributeList.Remove(attributeId);
                // 내리는 연출 넣기
                GameManager.Instance.SpawnHighestObject(ConstValues.AttributeDownEffect, effectPos);
            }
        }
    }

    public async void ResetAttribute(string character)
    {
        switch (character)
        {
            case ConstValues.Berserker:
                foreach (var skillList in berserkerSkillSetting.skillList)
                    skillList.attributeList.Clear();
                
                berserkerSkillSetting.attributePoint = GameManager.Instance.PlayerSkill.totalAttributePoint;
                break;
            
            case ConstValues.Gunner:
                foreach (var skillList in gunnerSkillSetting.skillList)
                    skillList.attributeList.Clear();
                
                gunnerSkillSetting.attributePoint = GameManager.Instance.PlayerSkill.totalAttributePoint;
                break;
        }
        await GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30205));
    }
    
    // 해당 스킬의 패시브 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<string> GetAttributePassive(string id)
    {
        List<string> passiveList = new List<string>();
        string[] idSplit = id.Split('_');
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인
        var attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.targetObject == id);
        
        // 정확히 id가 일치하는 오브젝트만 효과를 적용받음
        if (attributeData.Count > 0)
        {
            foreach (var attribute in attributeData)
            {
                if (attribute.passiveId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                foreach (var passive in attribute.passiveId)
                {
                    passiveList.Add(passive);
                }
            }
        }
        // 파생기도 효과를 적용받음
        attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
        foreach (var attribute in attributeData)
        {
            if (attribute.passiveId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                continue;
                
            foreach (var passive in attribute.passiveId)
            {
                passiveList.Add(passive);
            }
        }
        return passiveList;
    }
    // 해당 스킬의 추가 생성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeAddObjectInfo> GetAttributeAddObject(string id)
    {
        var addObjectList = new List<SkillAttributeAddObjectInfo>();
        string[] idSplit = id.Split('_');
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인
        var attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.targetObject == id);
        
        // 정확히 id가 일치하는 오브젝트만 효과를 적용받음
        if (attributeData.Count > 0)
        {
            foreach (var attribute in attributeData)
            {
                if (string.IsNullOrWhiteSpace(attribute.addObjectId) || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                var addObjectInfo = new SkillAttributeAddObjectInfo
                {
                    addObjectId = attribute.addObjectId,
                    objectId = attribute.objectId,
                    objectCount = attribute.objectCount,
                };
                addObjectList.Add(addObjectInfo);
            }
        }
        // 파생기도 효과를 적용받음
        attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
        foreach (var attribute in attributeData)
        {
            if (string.IsNullOrWhiteSpace(attribute.addObjectId) || !IsHaveAttribute(skillId, attribute.id))
                continue;
                
            var addObjectInfo = new SkillAttributeAddObjectInfo
            {
                addObjectId = attribute.addObjectId,
                objectId = attribute.objectId,
                objectCount = attribute.objectCount,
            };
            addObjectList.Add(addObjectInfo);
        }
        return addObjectList;
    }
    // 해당 스킬의 수치 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeUpgradeInfo> GetAttributeUpgrade(string id)
    {
        var upgradeList = new List<SkillAttributeUpgradeInfo>();
        string[] idSplit = id.Split('_');
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인
        var attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.targetObject == id);
        
        // 정확히 id가 일치하는 오브젝트만 효과를 적용받음
        if (attributeData.Count > 0)
        {
            foreach (var attribute in attributeData)
            {
                if (attribute.upgradeId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                for (int i = 0; i < attribute.upgradeId.Count; i++)
                {
                    var upgradeInfo = new SkillAttributeUpgradeInfo
                    {
                        upgradeId = attribute.upgradeId[i],
                        upgradeValue = attribute.upgradeValue[i]
                    };
                    upgradeList.Add(upgradeInfo);
                }
            }
        }
        // 파생기도 해당 효과를 적용받음
        attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
        foreach (var attribute in attributeData)
        {
            if (attribute.upgradeId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                continue;
                
            for (int i = 0; i < attribute.upgradeId.Count; i++)
            {
                var upgradeInfo = new SkillAttributeUpgradeInfo
                {
                    upgradeId = attribute.upgradeId[i],
                    upgradeValue = attribute.upgradeValue[i]
                };
                upgradeList.Add(upgradeInfo);
            }
        }
        return upgradeList;
    }
    // 해당 스킬의 버프 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeBuffInfo> GetAttributeBuff(string id)
    {
        var buffList = new List<SkillAttributeBuffInfo>();
        string[] idSplit = id.Split('_');
        if (idSplit.Length > 1)
        {
            // 파생기도 해당 효과를 적용받음
            string skillId = $"{idSplit[0]}_{idSplit[1]}";
            var attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.skill == skillId);
            foreach (var attribute in attributeData)
            {
                if (string.IsNullOrWhiteSpace(attribute.buffId) || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                var buffInfo = new SkillAttributeBuffInfo
                {
                    buffId = attribute.buffId,
                    buffTime = attribute.buffTime,
                    buffValue = attribute.buffValue,
                };
                buffList.Add(buffInfo);
            }
        }
        return buffList;
    }
}

[Serializable]
public class SkillKey
{
    public string skillId;
    public KeyCode keyCode;
}
[Serializable]
public class SkillKeyCollection
{
    public List<SkillKey> berserkerSkillKeyList;
    public List<SkillKey> gunnerSkillKeyList;
}
[Serializable]
public class SettingSkill
{
    public string skillId;
    public KeyCode keyCode;
    public PlayerSkill playerSkill;
}

[Serializable]
// 연출 이벤트
public class RoomProduct
{
    public int idx;
    public int count;
    public bool isFinish;
}

[Serializable]
// 이벤트를 처리해야하는 Npc
public class EventNpc
{
    public string id;
    public bool isActive;
}

[Serializable]
// 숏컷
public class ShortCut
{
    public string id;
    public string type;
    public bool isOpened;
}

[Serializable]
// 스킬 및 패시브
public class SkillAndPassive
{
    public string id;
    public bool alreadyGet;
}

[Serializable]
// 보물상자
public class TreasureBox
{
    public string id;
    public int count;
    public bool alreadyGet;
}

[Serializable]
// 아이템(보물상자)
public class Item
{
    public string id;
    public int count;
    public bool alreadyGet;
}

[Serializable]
// 엘리베이터
public class ElevatorData
{
    public string id;
    public int idx;
}

[Serializable]
// 잠긴 문
public class LockDoorData
{
    public string id;
    public bool isOpen;
}

[Serializable]
public class RoomInfo
{
    public string roomId;

    public string visitedFrameCells;                                 // 방문한 구역 테두리
    public string visitedInCells;                                    // 방문한 구역 내부
    public List<string> visitedShortcutCells = new List<string>();   // 방문한 숏컷
    public bool savePointCheck;                                      // 세이브 포인트
    public bool portalCheck;                                         // 포탈

    public List<RoomProduct> roomProduct = new List<RoomProduct>();
    public List<EventNpc> eventNpc = new List<EventNpc>();
    public List<ShortCut> shortCut = new List<ShortCut>();
    public List<SkillAndPassive> skillAndPassive = new List<SkillAndPassive>();
    public List<TreasureBox> treasureBox = new List<TreasureBox>();
    public List<Item> item = new List<Item>();
    public List<ElevatorData> elevators = new List<ElevatorData>();
    public List<LockDoorData> lockDoors = new List<LockDoorData>();
}

[Serializable]
public class ItemInfo
{
    public string id;
    public int count;
}

[Serializable]
public class NpcInfo
{
    public string id;
    public DialogKey dialogKey = new DialogKey();
}

[Serializable]
public class DialogKey
{
    public string id;
    public bool isUse;
}

public enum eUIType
{
    None,
    
    // UI
    UI_Interface,
    UI_Episode,
    UI_BossMessage,
    UI_StageClear,
    
    // 팝업
    Popup_GameOver,
    Popup_Guide,
    Popup_Minimap,
    Popup_Warning,
    Popup_Attribute,
    Popup_Select,
}

[Serializable]
public class SaveData
{
    // 재화
    public int gold;
    
    // 세이브 포인트
    public string savePoint;

    public bool firstGetSkill;
    public bool firstGetAttribute;
    
    public string firstPlayer;
    public string secondPlayer;

    public int npcSunGetSkill;

    public List<ItemInfo> itemList = new List<ItemInfo>();
    public SkillCollection playerSkill;
    public SkillKeyCollection playerSkillKey;
    public List<Vector2> miniMapCheckers = new List<Vector2>();
    public List<NpcInfo> npcInfoList = new List<NpcInfo>();
    public List<RoomInfo> roomInfoList = new List<RoomInfo>();
}

public class GameManager : Singleton<GameManager>
{
    public Material defaultMaterial;
    public Material hitMaterial;
    
    public KeyCode escKey;
    public KeyCode tabKey;
    public KeyCode spaceKey;
    public KeyCode attributeKey;
    public KeyCode markKey;
    public KeyCode confirmKey;
    
    public KeyCode leftMoveKey;
    public KeyCode rightMoveKey;

    public KeyCode attackKey;
    public KeyCode jumpKey;
    public KeyCode downKey;

    public KeyCode changeCharacterKey;
    public KeyCode dashKey;
    public KeyCode skillKey1;
    public KeyCode skillKey2;
    public KeyCode skillKey3;
    public KeyCode skillKey4;
    //public KeyCode skillKey5;
    //public KeyCode skillKey6;
    //public KeyCode skillKey7;
    //public KeyCode skillKey8;

    public KeyCode interactionKey;
    public KeyCode optionKey;

    [SerializeField] private SpriteAtlas uiAtlas;
    [SerializeField] private SpriteAtlas bgAtlas;
    private Sprite[] cloneSprites;
    private Dictionary<string, Sprite> atlasDic = new Dictionary<string, Sprite>();

    [SerializeField] private Player curPlayer;
    [SerializeField] private Transform objectPool;
    [SerializeField] private Transform uiObjectPool;
    [SerializeField] private Transform uiPool;
    [SerializeField] private Transform popupPool;
    [SerializeField] private Transform highestPool;

    [SerializeField] private Player[] players;
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>();
    [SerializeField] private List<GameObject> objectList = new List<GameObject>();
    [SerializeField] private List<Monster> monsterList = new List<Monster>();

    private UI_Interface uiInterface;
    private Popup_Warning popupWarning;
    
    // 세이브 넘버
    private string saveFileName;
    private int saveIdx;
    
    [SerializeField] private bool controlStart;
    private bool bossProduct;
    private bool timeProduct;
    private int comboCount;

    [SerializeField] private SaveData saveData;
    
    // 등록된 스킬 및 키 세팅 목록
    private SettingSkill changeSkill;
    
    // 복제체 데이터들
    public List<SkillAttributeInfo> skillAttributeList = new List<SkillAttributeInfo>();

    // 매니저들
    public TableManager tableManager;

    // 카메라
    private FollowCamera mainCamera;
    [SerializeField] private Transform miniMapCamera;
    [SerializeField] private Canvas uiObjectCanvas;

    private CancellationTokenSource productCancellation;
    private CancellationTokenSource fadeCancellation;
    private CancellationTokenSource waitCancellation;

    // 프로퍼티
    public Player CurPlayer
    {
        get => curPlayer;
        set => curPlayer = value;
    }

    public bool ControlStart 
    {
        get => controlStart;
        set => controlStart = value;
    }

    public bool BossProduct
    {
        get => bossProduct;
        set => bossProduct = value;
    }

    public bool TimeProduct
    {
        get => timeProduct;
        set => timeProduct = value;
    }

    public int ComboCount
    {
        get => comboCount;
        set => comboCount = value;
    }

    public int Gold
    {
        get => saveData.gold;
        set => saveData.gold = value;
    }

    public string SavePoint
    {
        get => saveData.savePoint;
        set => saveData.savePoint = value;
    }

    public bool FirstGetSkill
    {
        get => saveData.firstGetSkill;
        set => saveData.firstGetSkill = value;
    }

    public bool FirstGetAttribute
    {
        get => saveData.firstGetAttribute;
        set => saveData.firstGetAttribute = value;
    }
    
    public string FirstPlayer
    {
        get => saveData.firstPlayer;
        set => saveData.firstPlayer = value;
    }
    
    public string SecondPlayer
    {
        get => saveData.secondPlayer;
        set => saveData.secondPlayer = value;
    }
    
    public int NpcSunGetSkill
    {
        get => saveData.npcSunGetSkill;
        set => saveData.npcSunGetSkill = value;
    }

    public List<Vector2> MiniMapCheckers
    {
        get => saveData.miniMapCheckers;
        set => saveData.miniMapCheckers = value;
    }

    public List<ItemInfo> ItemList => saveData.itemList;

    public SkillCollection PlayerSkill => saveData.playerSkill;

    public SkillKeyCollection PlayerSkillKey => saveData.playerSkillKey;

    public List<RoomInfo> RoomInfoList => saveData.roomInfoList;
    public List<NpcInfo> NpcInfoInfoList => saveData.npcInfoList;

    public SettingSkill ChangeSkill => changeSkill;
    
    public Transform ObjectPool => objectPool;

    public List<Monster> MonsterList
    {
        get => monsterList;
        set => monsterList = value;
    }

    public FollowCamera MainCamera
    {
        get => mainCamera;
        set => mainCamera = value;
    }

    public Transform MiniMapCamera
    {
        get => miniMapCamera;
        set => miniMapCamera = value;
    }

    public CancellationTokenSource ProductCancellation => productCancellation;
    public CancellationTokenSource FadeCancellation => fadeCancellation;
    public CancellationTokenSource WaitCancellation => waitCancellation;
    
    protected override void Awake()
    {
        base.Awake();
        //QualitySettings.vSyncCount = 0;
        
        saveFileName = $"{ConstValues.User}_{saveIdx}";
#if UNITY_EDITOR
        saveFileName = $"{ConstValues.User}_{saveIdx}_Editor";
#endif
        
        Application.targetFrameRate = 60;
        InitManager();
        SetCopyData();
        GameStart();
        InitAtlas(uiAtlas);
        InitAtlas(bgAtlas);
        InitPlayer();
        InitChangeSkill();
        SetPrefabActive(false);
        FirstCashing();
    }

    private void OnDestroy()
    {
        SetPrefabActive(true);
    }
    
    public void SaveGame()
    {
        // json화
        SaveSystem.Save(saveFileName, saveData);
    }

    public void LoadGame()
    {
        // json화
        if(SaveSystem.TryLoad(saveFileName, out SaveData loadData))
            saveData = loadData;
    }
    
    public void DeleteData()
    {
        // json화
        SaveSystem.Delete(saveFileName);
    }

    public void FirstStart()
    {
        DeleteData();
        DefaultDataSetting();
        DefaultSkillSetting();
        DefaultMapSetting();
        DefaultNpcSetting();
        SetPlayerOrder(ConstValues.Berserker, default);
        SaveGame();
    }

    private void GameStart()
    {
        DefaultSkillKeySetting();
        
        if (SaveSystem.Exists(saveFileName))
        {
            LoadGame();
        }
        else
        {
            FirstStart(); 
        }
    }

    private void SetPrefabActive(bool active)
    {
        foreach (var prefab in prefabList)
            prefab.SetActive(active);

        string text = active ? "활성화" : "비활성화";
        Debug.Log($"{prefabList}개의 프리팹 {text}완료");
    }

    public List<GameObject> GetPrefabList()
    {
        return prefabList;
    }

    public string GetTalk(int idx)
    {
        return TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).kr;
    }
    public string GetItemTalk(string id)
    {
        int itemName = TableManager.Instance.itemTable.Item.Find(x => x.id == id).name;
        return TableManager.Instance.talkTable.Talk.Find(x => x.idx == itemName).kr;
    }
    
    public string GetKeyCode(KeyCode keycode)
    {
        return keycode switch
        {
            KeyCode.LeftArrow => "←",
            KeyCode.RightArrow => "→",
            KeyCode.UpArrow => "↑",
            KeyCode.DownArrow => "↓",
            KeyCode.Escape => "Esc",
            KeyCode.Return => "Enter",
            KeyCode.LeftShift => "Shift",
            _ => keycode.ToString()
        };
    }

    public void GoScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void DefaultDataSetting()
    {
        saveData.gold = 0;
        saveData.itemList.Clear();
    }

    private void DefaultSkillSetting()
    {
        FirstGetSkill = false;
        FirstGetAttribute = false;
        
        PlayerSkill.totalAttributePoint = 0;

        SkillSetting berserkerSkillSetting = new SkillSetting();
        berserkerSkillSetting.attributePoint = 0;
        berserkerSkillSetting.skillList = new List<Skill>();
        
        SkillSetting gunnerSkillSetting = new SkillSetting();
        gunnerSkillSetting.attributePoint = 0;
        gunnerSkillSetting.skillList = new List<Skill>();
        
        PlayerSkill.berserkerSkillSetting = berserkerSkillSetting;
        PlayerSkill.gunnerSkillSetting = gunnerSkillSetting;
    }
    
    public void DefaultSkillKeySetting()
    {
        escKey = KeyCode.Escape;
        tabKey = KeyCode.Tab;
        spaceKey = KeyCode.Space;
        attributeKey = KeyCode.I;
        markKey = KeyCode.Return;
        confirmKey = KeyCode.Return;
        
        leftMoveKey = KeyBinding.LoadKey(ConstValues.LeftMoveKey, KeyCode.LeftArrow);
        rightMoveKey = KeyBinding.LoadKey(ConstValues.RightMoveKey, KeyCode.RightArrow);
        attackKey = KeyBinding.LoadKey(ConstValues.AttackKey, KeyCode.X);
        jumpKey = KeyBinding.LoadKey(ConstValues.JumpKey, KeyCode.C);
        downKey = KeyBinding.LoadKey(ConstValues.DownKey, KeyCode.DownArrow);
        
        changeCharacterKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterKey, KeyCode.LeftShift);
        dashKey = KeyBinding.LoadKey(ConstValues.DashKey, KeyCode.Z);
        optionKey = KeyBinding.LoadKey(ConstValues.OptionKey, KeyCode.Escape);
        
        skillKey1 = KeyBinding.LoadKey(ConstValues.SkillKey1, KeyCode.A);
        skillKey2 = KeyBinding.LoadKey(ConstValues.SkillKey2, KeyCode.S);
        skillKey3 = KeyBinding.LoadKey(ConstValues.SkillKey3, KeyCode.D);
        skillKey4 = KeyBinding.LoadKey(ConstValues.SkillKey4, KeyCode.F);
        //skillKey5 = KeyBinding.LoadKey(ConstValues.SkillKey5, KeyCode.Q);
        //skillKey6 = KeyBinding.LoadKey(ConstValues.SkillKey6, KeyCode.W);
        //skillKey7 = KeyBinding.LoadKey(ConstValues.SkillKey7, KeyCode.E);
        //skillKey8 = KeyBinding.LoadKey(ConstValues.SkillKey8, KeyCode.R);
        
        interactionKey = KeyBinding.LoadKey(ConstValues.InteractionKey, KeyCode.UpArrow);

        // 각 플레이어들의 스킬 키 세팅 초기화
        List<SkillKey> berserkerSkillKeyList = new List<SkillKey>();
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerDash, dashKey));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey1));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey2));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey3));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey4));
        //berserkerSkillKeyList.Add(SetSkillKey(default, skillKey5));
        //berserkerSkillKeyList.Add(SetSkillKey(default, skillKey6));
        //berserkerSkillKeyList.Add(SetSkillKey(default, skillKey7));
        //berserkerSkillKeyList.Add(SetSkillKey(default, skillKey8));
        
        //berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerUpperSlash, skillKey6));
        //berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerFireStrike, skillKey7));
        //berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerChargeCrash, skillKey4));
        //berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerCrash, skillKey8));
        PlayerSkillKey.berserkerSkillKeyList = berserkerSkillKeyList;
        
        List<SkillKey> gunnerSkillKeyList = new List<SkillKey>();
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerDash, dashKey));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey1));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey2));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey3));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey4));
        //gunnerSkillKeyList.Add(SetSkillKey(default, skillKey5));
        //gunnerSkillKeyList.Add(SetSkillKey(default, skillKey6));
        //gunnerSkillKeyList.Add(SetSkillKey(default, skillKey7));
        //gunnerSkillKeyList.Add(SetSkillKey(default, skillKey8));
        
        //gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerGrenade, skillKey6));
        //gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerKnockBackShot, skillKey7));
        //gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerBigShot, skillKey4));
        //gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerCrazyShot, skillKey8));
        PlayerSkillKey.gunnerSkillKeyList = gunnerSkillKeyList;
    }

    private void DefaultMapSetting()
    {
        MiniMapCheckers.Clear();
        RoomInfoList.Clear();
        SavePoint = default;
    }

    private void DefaultNpcSetting()
    {
        NpcInfoInfoList.Clear();
    }

    private SkillKey SetSkillKey(string skillId, KeyCode keyCode)
    {
        var skillKey = new SkillKey()
        {
            skillId = skillId,
            keyCode = keyCode,
        };
        return skillKey;
    }

    public KeyCode BerserkerSkillKey(string skillId)
    {
        KeyCode keyCode = default;
        var targetSkill = saveData.playerSkillKey.berserkerSkillKeyList.Find(x => x.skillId == skillId);
        if (targetSkill != null)
            keyCode = targetSkill.keyCode;

        return keyCode;
    }
   

    public void AddNewSkill(string id)
    {
        // 키 저장
        var skillKeyData = TableManager.Instance.skillTable.Skill.Find(x => x.id == id);
        
        // 이미 가지고 있는 스킬이라면 무시해버린다
        switch (skillKeyData.caster)
        {
            case ConstValues.Berserker:
            {
                if (PlayerSkill.berserkerSkillSetting.skillList.Exists(x => x.skillId == id))
                    return;
                
                int idx = EmptySkillIdx(PlayerSkillKey.berserkerSkillKeyList);
                PlayerSkillKey.berserkerSkillKeyList[idx].skillId = id;

                Skill newSkill = new Skill();
                newSkill.skillId = id;
                newSkill.attributeList = new List<string>();
                PlayerSkill.berserkerSkillSetting.skillList.Add(newSkill);
                break;
            }
            case ConstValues.Gunner:
            {
                if (PlayerSkill.gunnerSkillSetting.skillList.Exists(x => x.skillId == id))
                    return;
                
                int idx = EmptySkillIdx(PlayerSkillKey.gunnerSkillKeyList);
                PlayerSkillKey.gunnerSkillKeyList[idx].skillId = id;
                
                Skill newSkill = new Skill();
                newSkill.skillId = id;
                newSkill.attributeList = new List<string>();
                PlayerSkill.gunnerSkillSetting.skillList.Add(newSkill);
                break;
            }
        }
        RefreshSkill();
        
        // 게임 저장
        SaveGame();
    }

    public void RemoveSkill(string id)
    {
        // 키 저장
        var skillKeyData = TableManager.Instance.skillTable.Skill.Find(x => x.id == id);
        
        // 가지고 있지 않은 스킬이라면 무시한다
        switch (skillKeyData.caster)
        {
            case ConstValues.Berserker:
            {
                var targetKey = PlayerSkillKey.berserkerSkillKeyList.Find(x => x.skillId == id);
                if (targetKey != null)
                    targetKey.skillId = default;

                var targetSkill = PlayerSkill.berserkerSkillSetting.skillList.Find(x => x.skillId == id);
                if (targetSkill != null)
                    PlayerSkill.berserkerSkillSetting.skillList.Remove(targetSkill);
                break;
            }
            case ConstValues.Gunner:
            {
                var targetKey = PlayerSkillKey.gunnerSkillKeyList.Find(x => x.skillId == id);
                if (targetKey != null)
                    targetKey.skillId = default;
                
                var targetSkill = PlayerSkill.gunnerSkillSetting.skillList.Find(x => x.skillId == id);
                if (targetSkill != null)
                    PlayerSkill.gunnerSkillSetting.skillList.Remove(targetSkill);
                break;
            }
        }
        RefreshSkill();
        // 게임 저장
        SaveGame();
    }
    
    private int EmptySkillIdx(List<SkillKey> skillKeyList)
    {
        int idx = 1;
        for (int i = 1; i < skillKeyList.Count; i++)
        {
            if (string.IsNullOrEmpty(skillKeyList[i].skillId))
            {
                idx = i;
                break;
            }
        }
        return idx;
    }

    public void SetSkillId(KeyCode keyCode, string skillId)
    {
        if (curPlayer.BasicStat.id == ConstValues.Berserker)
        {
            var berserkerSkillKey = PlayerSkillKey.berserkerSkillKeyList.Find(x => x.keyCode == keyCode);
            if (berserkerSkillKey != null)
                berserkerSkillKey.skillId = skillId;
        }
        else if (curPlayer.BasicStat.id == ConstValues.Gunner)
        {
            var gunnerSkillKey = PlayerSkillKey.gunnerSkillKeyList.Find(x => x.keyCode == keyCode);
            if (gunnerSkillKey != null)
                gunnerSkillKey.skillId = skillId;
        }
        
        // 저장
        //SaveGame();
    }
    public List<SettingSkill> GetSettingSkillList()
    {
        List<SkillKey> keyList = null;
        
        if(curPlayer.BasicStat.id == ConstValues.Berserker)
            keyList = PlayerSkillKey.berserkerSkillKeyList;
        else if(curPlayer.BasicStat.id == ConstValues.Gunner)
            keyList = PlayerSkillKey.gunnerSkillKeyList;
        
        List<SettingSkill> settingSkillList = new List<SettingSkill>();
        foreach (var key in keyList)
        {
            SettingSkill settingSkill = new SettingSkill()
            {
                skillId = key.skillId,
                keyCode = key.keyCode,
            };
            settingSkillList.Add(settingSkill);
        }
        
        var playerSkillList = curPlayer.GetSkillList();
        foreach (var playerSkill in playerSkillList)
        {
            var matchSkillList = settingSkillList.FindAll(x => x.skillId == playerSkill.id);
            foreach (var matchSkill in matchSkillList)
            {
                matchSkill.playerSkill = playerSkill;
            }
        }

        return settingSkillList;
    }

    private void InitAtlas(SpriteAtlas spriteAtlas)
    {
        // Atlas 안에 들어있는 스프라이트 개수만큼 배열 생성
        cloneSprites = new Sprite[spriteAtlas.spriteCount];
        // GetSprites 호출 시 배열에 모두 채워진다
        spriteAtlas.GetSprites(cloneSprites);
        foreach (var sprite in cloneSprites)
        {
            var keyName = sprite.name.Split(ConstValues.AtlasClone)[0];
            atlasDic.Add(keyName, sprite);
        }
    }
    public Sprite GetAtlasSprite(string id)
    {
        return atlasDic[id];
    }

    private void InitManager() 
    {
        tableManager = TableManager.Instance;
        tableManager.Init();
    }

    // 복제체 데이터
    private void SetCopyData()
    {
        foreach (var skillAttribute in tableManager.skillAttributeTable.SkillAttribute)
        {
            var data = new SkillAttributeInfo();
            data.id = skillAttribute.id;
            data.skill = skillAttribute.skill;
            data.targetObject = skillAttribute.targetObject;
            data.cost = skillAttribute.cost;
            
            if (!string.IsNullOrWhiteSpace(skillAttribute.passiveId))
            {
                var passiveIdSplit = skillAttribute.passiveId.Split(';');
                foreach (var passiveId in passiveIdSplit)
                    data.passiveId.Add(passiveId);
            }

            data.addObjectId = skillAttribute.addObjectId;
            data.objectId = skillAttribute.objectId;
            data.objectCount = skillAttribute.objectCount;
            
            if (!string.IsNullOrWhiteSpace(skillAttribute.upgradeId))
            {
                var upgradeIdSplit = skillAttribute.upgradeId.Split(';');
                foreach (var upgradeId in upgradeIdSplit)
                    data.upgradeId.Add(upgradeId);
            }

            if (!string.IsNullOrWhiteSpace(skillAttribute.upgradeValue))
            {
                var upgradeValueSplit = skillAttribute.upgradeValue.Split(';');
                foreach (var upgradeValue in upgradeValueSplit)
                    data.upgradeValue.Add(int.Parse(upgradeValue));
            }
            
            data.buffId = skillAttribute.buffId;
            data.buffTime = skillAttribute.buffTime;
            data.buffValue = skillAttribute.buffValue;
            data.talk = skillAttribute.talk;
            data.explainTalk = skillAttribute.explainTalk;
            
            skillAttributeList.Add(data);
        }
    }

    // 플레이어
    private void InitPlayer()
    {
        foreach (var player in players)
        {
            player.InitBasicStat();
            player.InitBonusStat();
            player.InitSkill();
            player.SkillAttributeCheck();
        }
    }
    
    public void MovePlayer()
    {
        ControlStart = true;
        CurPlayer.Immortal = false;
    }
    public void StopPlayer()
    {
        ControlStart = false;
        CurPlayer.Immortal = true;
    }

    public void SetPlayerHp(int hp)
    {
        foreach (var player in players)
            player.BasicStat.hp = hp;
    }
    
    public void SetPlayerOrder(string first, string second)
    {
        FirstPlayer = first;
        SecondPlayer = second;
        curPlayer = GetPlayer(FirstPlayer);
    }

    public void InitPlayerStat()
    {
        foreach (var player in players)
        {
            player.InitBasicStat();
            player.ResetSkillCoolTime();
        }
    }

    public void SetPlayerAttribute()
    {
        foreach (var player in players)
            player.SkillAttributeCheck();
    }
    
    public void SpawnPlayer(string playerName)
    {
        ActivePlayer(playerName);
    }
    public Player GetPlayer(string playerName)
    {
        foreach (var player in players)
        {
            if (player.name == playerName)
                return player;
        }
        return null;
    }

    private void ActivePlayer(string playerName)
    {
        foreach (var player in players)
        {
            player.gameObject.SetActive(player.name == playerName);
            if (player.name == playerName)
                player.Flip(1);
        }
    }

    public void ArrivePlayer()
    {
        foreach (var player in players)
        {
            player.MyBoxCollider.enabled = true;
            player.Immortal = false;
            player.IsDie = false;
        }
    }

    public void ReduceSkillPlayer()
    {
        ChangeSkill.playerSkill.ReducingCooldown();
        foreach (var player in players)
            player.ReduceSkillCoolTime();
    }

    public void InitCamera(FollowCamera targetCamera)
    {
        mainCamera = targetCamera;
        uiObjectCanvas.worldCamera = targetCamera.GetComponent<Camera>();
    }

    public void CameraShake(float amountX, float amountY, float time)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("[CameraShake] mainCamera == null. InitCamera가 아직 안 됐거나, RoomManager.mainCameraFollow가 비어있습니다.");
            return;
        }
        mainCamera.Shake(amountX, amountY, time);
    }

    public Monster SpawnMonster(string id, Vector3 monsterVector, bool isExplosion = true, bool isBoss = false, Action<string> bossProduct = null)
    {
        var monster = SpawnToObjectPool(id, monsterVector).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.IsBoss = isBoss;
        //monster.SpawnHpBar();
        monster.Appear(bossProduct);
        monsterList.Add(monster);
        return monster;
    }

    public Monster ActiveAndHideMonster(string id, Vector3 monsterVector, bool isExplosion = true, bool isBoss = false)
    {
        var monster = SpawnToPoolInstantiate(id, objectPool, monsterVector).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.IsBoss = isBoss;
        monster.gameObject.SetActive(false);
        monsterList.Add(monster);
        return monster;
    }
    public Monster ActiveAndHideMonster(string id, Transform monsterTransform, Vector3 monsterVector, bool isActive, bool isExplosion = true, bool isBoss = false)
    {
        var monster = SpawnToMonster(id, monsterTransform, monsterVector, isActive).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.IsBoss = isBoss;
        monster.gameObject.SetActive(false);
        monsterList.Add(monster);
        return monster;
    }
    
    public void ActiveMonster(Monster monster, Action<string> bossProduct = null)
    {
        monster.gameObject.SetActive(true);
        //monster.SpawnHpBar();
        monster.Appear(bossProduct);
    }
    
    public void SetMonster(Monster monster, bool isBoss, bool isExplosion)
    {
        monster.IsBoss = isBoss;
        monster.IsExplosion = isExplosion;
        //monster.SpawnHpBar();
        monsterList.Add(monster);
    }

    public void RemoveMonster(Monster monster)
    {
        monsterList.Remove(monster);
    }
    public void ClearMonsterList()
    {
        foreach (var monster in monsterList)
            monster.gameObject.SetActive(false);
        
        monsterList.Clear();
    }
    
    public void InputDataTrap(string trapId, BoxCollider2D trapObject)
    {
        string originId = trapId.Split(' ')[0];
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == originId);
        if (objectData != null)
        {
            var spawnedObject = trapObject.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = trapObject.AddComponent<SpawnedObject>();
            
            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
        }

        var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == originId);
        if (attackData != null)
        {
            var attack = trapObject.GetComponent<Attack>();
            if (!attack)
            {
                attack = trapObject.AddComponent<Attack>();
                attack.SetupData(attackData);
            }

            attack.EnableSetting();
        }
    }

    public void DisActiveObjectList()
    {
        foreach (var obj in objectList)
            obj.SetActive(false);
    }
    // 일반 오브젝트
    public GameObject SpawnToObjectPool(string id, Transform objTransform)
    {
        return SpawnToPool(id, objectPool, objTransform);
    }
    public GameObject SpawnToObjectPool(string id, Vector3 objVector)
    {
        return SpawnToPool(id, objectPool, objVector);
    }
    // 일반 UI오브젝트
    public GameObject SpawnToUIObjectPool(string id, Transform objTransform)
    {
        return SpawnToPool(id, uiObjectPool, objTransform);
    }
    public GameObject SpawnToUIObjectPool(string id, Vector2 objVector)
    {
        return SpawnToPool(id, uiObjectPool, objVector);
    }
    public GameObject SpawnToUIObjectPoolInstantiate(string id, Transform objTransform)
    {
        return SpawnToPoolInstantiate(id, uiObjectPool, objTransform);
    }
    public GameObject SpawnToUIObjectPoolInstantiate(string id, Vector2 objVector)
    {
        return SpawnToPoolInstantiate(id, uiObjectPool, objVector);
    }

    // UI화면
    public GameObject SpawnToUIPool(string id, Vector2 objVector)
    {
        return SpawnToPool(id, uiPool, objVector);
    }
    public GameObject SpawnToUIPool(eUIType type, Transform objTransform)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objTransform);
        return go;
    }
    public GameObject SpawnToUIPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objVector);
        return go;
    }
    // UI팝업화면
    public GameObject SpawnToPopupPool(eUIType type, Transform objTransform)
    {
        var go = SpawnToPool(type.ToString(), popupPool, objTransform);
        return go;
    }
    public GameObject SpawnToPopupPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), popupPool, objVector);
        return go;
    }
    // 최상위 UI오브젝트
    public GameObject SpawnToHighestPool(string id, Transform objTransform)
    {
        return SpawnToPool(id, highestPool, objTransform);
    }
    public GameObject SpawnToHighestPool(string id, Vector2 objVector)
    {
        return SpawnToPool(id, highestPool, objVector);
    }
    public GameObject SpawnToHighestPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), highestPool, objVector);
        return go;
    }
    
    public GameObject SpawnToRaw(string id, Vector2 objVector)
    {
        return SpawnToPool(id, null, objVector);
    }

    public BoxCollider2D ObjectCollider(string id)
    {
        var go = prefabList.Find(x => x.name == id).gameObject;
        if (go != null)
        {
            var targetCollider = go.GetComponent<BoxCollider2D>();
            return targetCollider;
        }
        return null;
    }

    private void FirstCashing()
    {
        foreach (var prefab in prefabList)
        {
            GameObject go = Instantiate(prefab, objectPool);
            Destroy(go);
        }

        for (int i = 0; i < 30; i++)
        {
            var font = SpawnToUIObjectPoolInstantiate(ConstValues.TextFont, Vector2.zero);
            font.SetActive(false);
        }
        
        var guide = SpawnToPopupPool(eUIType.Popup_Guide, Vector3.zero);
        guide.SetActive(false);
        
        var skillExplosion = SpawnToObjectPool(ConstValues.GetSkillExplosion, Vector2.zero);
        skillExplosion.SetActive(false);
    }

    private GameObject SpawnToPool(string id, Transform pool, Transform objTransform)
    {
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
            if(prefabList.Find(x => x.name == id) == null)
                Debug.LogWarning($"{id}가 프리팹 리스트에 없다");
            go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
            objectList.Add(go);
        }
        else
        {
            var recycleObj = isSearch.Find(x => !x.activeSelf);
            if (recycleObj == null)
            {
                go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
                objectList.Add(go);
            }
            else
            {
                go = recycleObj;
            }
        }
        
        go.transform.position = objTransform.position;
        go.SetActive(true);
        return go;
    }
    private GameObject SpawnToPool(string id, Transform pool, Vector3 objVector)
    { 
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
            if(prefabList.Find(x => x.name == id) == null)
                Debug.LogWarning($"{id}가 프리팹 리스트에 없다");
            go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
            objectList.Add(go);
        }
        else
        {
            var recycleObj = isSearch.Find(x => !x.activeSelf);
            if (recycleObj == null)
            {
                go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
                objectList.Add(go);
            }
            else
            {
                go = recycleObj;
            }
        }
        
        go.transform.position = objVector;
        go.SetActive(true);
        return go;
    }
    public GameObject SpawnToMonster(string id, Transform pool, Vector3 objVector, bool isActive)
    {
        GameObject go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
        
        go.transform.position = objVector;
        go.SetActive(isActive);
        return go;
    }
    
    public GameObject SpawnToPoolInstantiate(string id, Transform pool, Transform objTransform)
    {
        GameObject go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
        objectList.Add(go);
        
        go.transform.position = objTransform.transform.position;
        go.SetActive(true);
        return go;
    }
    
    public GameObject SpawnToPoolInstantiate(string id, Transform pool, Vector3 objVector)
    {
        GameObject go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
        objectList.Add(go);
        
        go.transform.position = objVector;
        go.SetActive(true);
        return go;
    }
    
    // UI관련 코드
    public void SpawnGameInterface()
    {
        if (!uiInterface)
        {
            uiInterface = SpawnToUIPool(eUIType.UI_Interface, Vector2.zero).GetComponent<UI_Interface>();
            uiInterface.Setup(eUIType.UI_Interface);
        }

        var comboInterface = uiInterface.ComboView.ConvertTo<IUIComboView>();
        var comboModel = new UIComboModel()
        {
            comboCount = 0
        };
        var comboPresenter = new UIComboPresenter(comboInterface, comboModel);
        uiInterface.SetComboPresenter(comboPresenter);
        comboPresenter.SetCombo();

        RefreshFace();
        RefreshPlayerHp();
                    
        var bossHpInterface = uiInterface.BossHpView.ConvertTo<IUIBossHpView>();
        var bossHpPresenter = new UIBossHpPresenter(bossHpInterface);
        uiInterface.SetBossHpPresenter(bossHpPresenter);
        bossHpPresenter.HideHp();
            
        var changeInterface = uiInterface.ChangeSkillView.ConvertTo<IUISkillView>();
        var skillInterfaces = uiInterface.SkillViews.ConvertAll(v => (IUISkillView)v);
        var skillModel = new UISkillModel
        {
            changeSkill = this.changeSkill,
            settingSkillList = GetSettingSkillList()
        };
        var skillPresenter = new UISkillPresenter(changeInterface, skillInterfaces, skillModel);
        uiInterface.SetSkillPresenter(skillPresenter);
        skillPresenter.SetSkillInfo();
        
        //ChangingFalse();
    }

    public async UniTask SpawnWarningPopup(string message)
    {
        if (popupWarning)
        {
            popupWarning.gameObject.SetActive(true);
        }
        else
        {
            popupWarning = SpawnToHighestPool(eUIType.Popup_Warning, Vector3.zero).GetComponent<Popup_Warning>();
        }
        
        var warningInterface = popupWarning.WarningView.ConvertTo<IPopupWarningView>();
        var warningModel = new PopupWarningModel()
        {
            message = message,
        };
        var warningPresenter = new PopupWarningPresenter(warningInterface, warningModel);
        popupWarning.SetMinimapPresenter(warningPresenter);
        await popupWarning.PopupWarningPresenter.SetMessage();
    }

    private void RefreshFace()
    {
        var faceInterface = uiInterface.CharacterFaceView.ConvertTo<ICharacterFace>();
        var faceModel = new UICharacterFaceModel()
        {
            firstCharacter = FirstPlayer,
            secondCharacter = SecondPlayer,
        };
        var facePresenter = new UICharacterFacePresenter(faceInterface, faceModel);
        uiInterface.SetCharacterFacePresenter(facePresenter);
        
        if(curPlayer.BasicStat.id == FirstPlayer)
            facePresenter.SetFirstFace();
        else if(curPlayer.BasicStat.id == SecondPlayer)
            facePresenter.SetSecondFace();
    }
    
    public void RefreshPlayerHp()
    {
        var hpInterface = uiInterface.HpView.ConvertTo<IUIHpView>();
        var hpModel = new UIHpModel()
        {
            character = CurPlayer
        };
        var hpPresenter = new UIHpPresenter(hpInterface, hpModel);
        uiInterface.SetHpPresenter(hpPresenter);
        hpPresenter.SetHp();
        hpPresenter.SetHpText();
    }

    public void RefillPlayerHp()
    {
        foreach (var player in players)
            player.BasicStat.hp = player.BasicStat.maxHp;

        RefreshPlayerHp();
    }

    public void GetGold(int getGold, int totalGold)
    {
        var goodsInterface = uiInterface.GoodsView.ConvertTo<IUIGoodsView>();
        var goodsModel = new UIGoodsModel()
        {
            getGold = getGold,
            totalGold = totalGold,
        };
        var goodsPresenter = new UIGoodsPresenter(goodsInterface, goodsModel);
        uiInterface.SetGoodsPresenter(goodsPresenter);
        goodsPresenter.PlusGoldText();
    }

    public void RefreshGoods()
    {
        Gold = saveData.gold;

        var goodsInterface = uiInterface.GoodsView.ConvertTo<IUIGoodsView>();
        var goodsModel = new UIGoodsModel()
        {
            totalGold = Gold,
        };
        var goodsPresenter = new UIGoodsPresenter(goodsInterface, goodsModel);
        uiInterface.SetGoodsPresenter(goodsPresenter);
        goodsPresenter.SetGoldText();
    }

    public GameObject GetUI(eUIType type)
    {
        GameObject result = null;
        foreach (var go in objectList)
        {
            if (go.GetComponent<UIBase>() && go.GetComponent<UIBase>().GetUIType() == type)
            {
                result = go;
                break;
            }
        }
        return result;
    }

    private void InitChangeSkill()
    {
        changeSkill = new SettingSkill()
        {
            skillId = ConstValues.ChangeCharacter,
            keyCode = changeCharacterKey,
        };
        
        foreach (var skill in TableManager.Instance.skillTable.Skill)
        {
            if (skill.id != ConstValues.ChangeCharacter)
                continue;
            
            PlayerSkill addedSkill = new PlayerSkill();
            addedSkill.id = skill.id;
            var coolTimeArray = skill.coolTime.Split(',');
            foreach (var coolTime in coolTimeArray)
            {
                addedSkill.maxCoolTime.Add(float.Parse(coolTime));
                addedSkill.curCoolTime.Add(float.Parse(coolTime));
            }
            
            addedSkill.talk = GetTalk(skill.talk);
            addedSkill.explainTalk = GetTalk(skill.explainTalk);
            changeSkill.playerSkill = addedSkill;
            break;
        }
    }

    // 해당 아이템을 가지고 있는가?
    public bool IsHaveItem(string id)
    {
        return ItemList.Find(x => x.id == id) != null;
    }

    public string GetSkillName(string id)
    {
        string skillName = default;
        foreach (var skill in TableManager.Instance.skillTable.Skill)
        {
            if (skill.id != id)
                continue;

            skillName = GetTalk(skill.talk);
            break;
        }

        return skillName;
    }

    public void CharacterChange(bool changeAttack = true)
    {
        var pastPlayer = curPlayer;
        var changePos = curPlayer.transform.position;
        var nextPlayerId = SecondPlayer;
        if (curPlayer.BasicStat.id == SecondPlayer)
            nextPlayerId = FirstPlayer;

        pastPlayer.AllBuffCancel();

        curPlayer = GetPlayer(nextPlayerId);
        // 교체 시 유지해야하는 데이터 받아오기
        curPlayer.ReceiveChangeData(pastPlayer);
        ActivePlayer(nextPlayerId);

        curPlayer.transform.position = changePos;
        curPlayer.transform.localScale = pastPlayer.transform.localScale;
        curPlayer.JumpAttackCount = 0;
        
        RefreshFace();

        if (changeAttack)
            curPlayer.ChangeAttack();

        RefreshSkill();
        SetCameraTarget(curPlayer.transform);
    }
    
    public void SetCharacterOrder(string first, string second)
    {
        var pastPlayer = curPlayer;
        var changePos = curPlayer.transform.position;
        
        SetPlayerOrder(first, second);
        ActivePlayer(FirstPlayer);
        curPlayer.transform.position = changePos;
        curPlayer.transform.localScale = pastPlayer.transform.localScale;
        
        RefreshSkill();
        SetCameraTarget(curPlayer.transform);
    }

    public void SetCameraTarget(Transform targetTransform)
    {
        mainCamera.SetTarget(targetTransform);
    }

    private void RefreshSkill()
    {
        var uiInterfaceObj = GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;
        
        var changeInterface = uiInterface.ChangeSkillView.ConvertTo<IUISkillView>();
        var skillInterfaces = uiInterface.SkillViews.ConvertAll(v => (IUISkillView)v);
        var skillModel = new UISkillModel
        {
            changeSkill = changeSkill,
            settingSkillList = GetSettingSkillList()
        };
        var skillPresenter = new UISkillPresenter(changeInterface, skillInterfaces, skillModel);
        uiInterface.SetSkillPresenter(skillPresenter);
        skillPresenter.SetSkillInfo();
    }

    public SpeechFrame GetSpeechFrame(string frameName)
    {
        GameObject speechFrame;
        if(frameName == ConstValues.SpeechFrameTitle)
            speechFrame = SpawnToHighestPool(frameName, Vector2.zero);
        else
            speechFrame = SpawnToUIObjectPool(frameName, Vector2.zero);
        
        var frameClass = speechFrame.GetComponent<SpeechFrame>();
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == frameName);
        if (objectData == null)
            return frameClass;
        
        var spawnedObject = speechFrame.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = speechFrame.AddComponent<SpawnedObject>();
            
        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();

        if (spawnedObject.GetTrace())
        {
            var trace = speechFrame.GetComponent<Trace>();
            if(!trace)
                speechFrame.AddComponent<Trace>();
        }
        
        return frameClass;
    }

    public void RoomMoveSetting()
    {
        foreach (var list in objectList)
        {
            if(list.activeSelf && (list.GetComponent<Missile>() || list.GetComponent<Grenade>()))
                list.SetActive(false);
        }
    }

    public void InitProductCancellation()
    {
        productCancellation = new CancellationTokenSource();
    }
    public void InitFadeCancellation()
    {
        fadeCancellation = new CancellationTokenSource();
    }
    public void InitWaitCancellation()
    {
        waitCancellation = new CancellationTokenSource();
    }
    
    public async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    public async UniTask IgnoreTimeScaleDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), delayType: DelayType.Realtime, cancellationToken: tokenSource.Token);
    }
    
    public async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }
    
    // 대기 딜레이
    public async UniTask WaitUntilDelay(Func<bool> condition, CancellationTokenSource tokenSource)
    {
        await UniTask.WaitUntil(condition, cancellationToken: tokenSource.Token);
    }

    public void GetSkillProduct(string id, Action<string> customAction)
    {
        var skillName = GetSkillName(id);
        CurPlayer.SpawnObject(ConstValues.GetSkillExplosion, CurPlayer.CenterPos.position);
        customAction.Invoke(skillName);
    }
    
    public void GetAttributeProduct(int count, Action<int> customAction)
    {
        CurPlayer.SpawnObject(ConstValues.GetSkillExplosion, CurPlayer.CenterPos.position);
        customAction.Invoke(count);
    }

    public void GetItemProduct(string id)
    {
        string getMessage = string.Format(GetTalk(30207), GetItemTalk(id));
        SpawnWarningPopup(getMessage);
    }

    public string GetThousandCommaText(int data)
    {
        if (data == 0)
            return 0.ToString();
        
        return $"{data:#,###}";
    }

    public void SpawnHighestObject(string id, Vector2 pos, int zAngle = 0)
    {
        var obj = SpawnToHighestPool(id, pos);
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);

        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();

        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting(true);
        if (zAngle != 0)
        {
            var finalAngle = zAngle;
            if (transform.localScale.x < 0)
                finalAngle = -zAngle;

            var objectAngle = spawnedObject.transform.eulerAngles;
            spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
        }
    }

    public void HideHighestObjects()
    {
        for (int i = 0; i < highestPool.childCount; i++)
            highestPool.GetChild(i).gameObject.SetActive(false);
    }

    public void SpawnSelect(string message, Sprite goodsSprite, int cost, Action yesAction, Action noAction)
    {
        var uiBase = SpawnToPopupPool(eUIType.Popup_Select, Vector3.zero).GetComponent<UIBase>();
        
        if (uiBase is Popup_Select popupSelect)
        {
            var selectInterface = popupSelect.SelectView.ConvertTo<IPopupSelectView>();
            var selectModel = new PopupSelectModel()
            {
                message = message,
                goods = goodsSprite,
                cost = cost,
                startAction = HideHighestObjects,
                yesAction = () =>
                {
                    uiBase.Close();
                    yesAction();
                },
                noAction = ()=>
                {
                    uiBase.Close();
                    noAction();
                },
                moveSoundAction = () =>
                {
                    SoundManager.Instance.PlaySound(ConstValues.Jump1, true);
                }
            };
            var selectPresenter = new PopupSelectPresenter(selectInterface, selectModel);
            popupSelect.SetSelectPresenter(selectPresenter);
            selectPresenter.Expansion(() =>
            {
                uiBase.ExpansionOpen(false, false);
            });
            selectPresenter.SetModel();
            selectPresenter.SetAction();
        }
    }

    public FadeSystem fadeUI()
    {
        return SpawnToUIPool(ConstValues.FadeUI, Vector3.zero).GetComponent<FadeSystem>();
    }
}
