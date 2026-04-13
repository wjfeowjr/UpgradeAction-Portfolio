using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public string deBuffId;
    public float buffTime;
    public int buffValue;
    public int talk;
    public int explainTalk;
}

[Serializable]
public class RelicInfo
{
    public string id;
    public int name;
    public int explain;
    public eEquipRank rank;
    public List<eEquipStat> statList = new List<eEquipStat>();
    public List<int> valueList = new List<int>();
    public string specialValue;
}

public enum eEquipRank
{
    Normal,
    Rare,
}

public enum eEquipStat
{
    Power,
    PowerPercent,
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
public class PlayerInfo
{
    public string playerId;
    public int attributePoint;
    public List<string> relicList = new List<string>();
    public List<Skill> skillList = new List<Skill>();
    public List<SkillKey> skillKeyList = new List<SkillKey>();
}

[Serializable]
public class SkillKey
{
    public string skillId;
    public KeyCode keyCode;
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
    public bool isFirstDialogFinish;
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
    Popup_Character,
    Popup_Select,
    Popup_Store,
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
    
    public List<string> playerList = new List<string>();
    public List<ItemInfo> itemList = new List<ItemInfo>();
    public List<string> relicList = new List<string>();
    
    // 플레이어 개별로 만들기(스킬, 스킬 키, 유물)
    public int totalAttributePoint;
    public List<PlayerInfo> playerInfoList = new List<PlayerInfo>();
    
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
    public KeyCode upKey;
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

    public KeyCode changeCharacterLeftKey;
    public KeyCode changeCharacterRightKey;
    
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
    
    private List<SpeechFrame> speechFrame1 = new List<SpeechFrame>();
    private List<SpeechFrame> speechFrame2 = new List<SpeechFrame>();
    private SpeechFrame speechFrameStrong;
    private SpeechFrame speechFrameTitle;

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
    public List<RelicInfo> relicList = new List<RelicInfo>();
    
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

    public Player[] Players
    {
        get => players;
    }
    
    public List<SpeechFrame> SpeechFrame1 => speechFrame1;
    public List<SpeechFrame> SpeechFrame2 => speechFrame2;
    public SpeechFrame SpeechFrameStrong => speechFrameStrong;
    public SpeechFrame SpeechFrameTitle => speechFrameTitle;

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
    
    public List<string> PlayerList
    {
        get => saveData.playerList;
        set => saveData.playerList = value;
    }

    public List<string> RelicList
    {
        get => saveData.relicList;
    }

    public List<Vector2> MiniMapCheckers
    {
        get => saveData.miniMapCheckers;
        set => saveData.miniMapCheckers = value;
    }

    public List<ItemInfo> ItemList => saveData.itemList;

    public List<PlayerInfo> PlayerInfoList => saveData.playerInfoList;

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
        CashingSpeechFrame();
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
        DefaultRelicSetting();
        DefaultMapSetting();
        DefaultNpcSetting();
        AddPlayer(ConstValues.Berserker);
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
        curPlayer = GetPlayer(saveData.playerList[0]);
        
#if UNITY_EDITOR
        // if(!saveData.playerList.Contains(ConstValues.Fighter))
        //     AddPlayer(ConstValues.Fighter);
#endif
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
        saveData.playerList.Clear();
        saveData.gold = 0;
        saveData.itemList.Clear();
    }

    private void DefaultSkillSetting()
    {
        FirstGetSkill = false;
        FirstGetAttribute = false;

        // 캐릭터가 3마리니까 이것도 3개
        for (int i = 0; i < 3; i++)
        {
            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.attributePoint = 0;
            playerInfo.skillList = new List<Skill>();
            playerInfo.skillKeyList = new List<SkillKey>();
            
            switch (i)
            {
                case 0:
                    playerInfo.playerId = ConstValues.Berserker;
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.BerserkerDash, dashKey));
                    break;
                
                case 1:
                    playerInfo.playerId = ConstValues.Gunner;
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.GunnerDash, dashKey));
                    break;
                
                case 2:
                    playerInfo.playerId = ConstValues.Fighter;
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.FighterDash, dashKey));
                    break;
            }
            playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey1));
            playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey2));
            playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey3));
            playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey4));
            saveData.playerInfoList.Add(playerInfo);
        }
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
        upKey = KeyBinding.LoadKey(ConstValues.UpKey, KeyCode.UpArrow);
        
        changeCharacterKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterKey, KeyCode.LeftShift);
        dashKey = KeyBinding.LoadKey(ConstValues.DashKey, KeyCode.Z);
        optionKey = KeyBinding.LoadKey(ConstValues.OptionKey, KeyCode.Escape);
        
        skillKey1 = KeyBinding.LoadKey(ConstValues.SkillKey1, KeyCode.A);
        skillKey2 = KeyBinding.LoadKey(ConstValues.SkillKey2, KeyCode.S);
        skillKey3 = KeyBinding.LoadKey(ConstValues.SkillKey3, KeyCode.D);
        skillKey4 = KeyBinding.LoadKey(ConstValues.SkillKey4, KeyCode.F);

        interactionKey = KeyBinding.LoadKey(ConstValues.InteractionKey, KeyCode.UpArrow);
        
        changeCharacterLeftKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterLeftKey, KeyCode.Q);
        changeCharacterRightKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterRightKey, KeyCode.E);
    }
    
    private void DefaultRelicSetting()
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            playerInfo.relicList.Add(default);
        }
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

    public KeyCode GetSkillKey(string skillId)
    {
        KeyCode keyCode = default;
        
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var targetSkill = playerInfo.skillKeyList.Find(x => x.skillId == skillId);
            if (targetSkill != null)
            {
                keyCode = targetSkill.keyCode;
                break;
            }
        }
        
        return keyCode;
    }
   

    public void AddNewSkill(string id)
    {
        // 키 저장
        var skillKeyData = tableManager.skillTable.Skill.Find(x => x.id == id);
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == skillKeyData.caster);
        
        // 이미 가지고 있는 스킬이라면 무시해버린다
        if (playerInfo.skillList.Exists(x => x.skillId == id))
            return;
                
        int idx = EmptySkillIdx(playerInfo.skillKeyList);
        playerInfo.skillKeyList[idx].skillId = id;

        Skill newSkill = new Skill();
        newSkill.skillId = id;
        newSkill.attributeList = new List<string>();
        playerInfo.skillList.Add(newSkill);
        
        RefreshSkill();
        
        // 게임 저장
        SaveGame();
    }

    // 스킬 제거(테스트용)
    public void RemoveSkill(string id)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var targetKey = playerInfo.skillKeyList.Find(x => x.skillId == id);
            if (targetKey != null)
                targetKey.skillId = default;

            var targetSkill = playerInfo.skillList.Find(x => x.skillId == id);
            if (targetSkill != null)
                playerInfo.skillList.Remove(targetSkill);
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

    // 스킬 칸 교체
    public void SetSkillId(KeyCode keyCode, string skillId)
    {
        PlayerInfo playerInfo = new PlayerInfo();

        playerInfo = saveData.playerInfoList.Find(x => x.playerId == curPlayer.BasicStat.id);
        
        var skillKey = playerInfo.skillKeyList.Find(x => x.keyCode == keyCode);
        if (skillKey != null)
            skillKey.skillId = skillId;

        // 저장
        //SaveGame();
    }
    public List<SettingSkill> GetSettingSkillList()
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == curPlayer.BasicStat.id);

        List<SettingSkill> settingSkillList = new List<SettingSkill>();
        foreach (var skillKey in playerInfo.skillKeyList)
        {
            SettingSkill settingSkill = new SettingSkill()
            {
                skillId = skillKey.skillId,
                keyCode = skillKey.keyCode,
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
    
    public void EquipRelic(string playerId, string relicId)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        if (playerInfo.relicList.Contains(relicId))
            return;
        
        for (var i = 0; i < playerInfo.relicList.Count; i++)
        {
            // 빈칸에 자동 장착
            if (string.IsNullOrWhiteSpace(playerInfo.relicList[i]))
            {
                playerInfo.relicList[i] = relicId;
                var relicTableData = TableManager.Instance.relicTable.Relic.Find(x => x.id == relicId);
                var relicName = GetTalk(relicTableData.name);
                Debug.Log($"{relicName}장착");
                break;
            }
        }

        foreach (var player in players)
            player.InitBonusStat();
        
        // 게임 저장
        SaveGame();
    }
    public void TargetEquipRelic(string playerId, string relicId, int idx)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        playerInfo.relicList[idx] = relicId;
        var relicTableData = TableManager.Instance.relicTable.Relic.Find(x => x.id == relicId);
        var relicName = GetTalk(relicTableData.name);
        Debug.Log($"{relicName}장착");
        
        foreach (var player in players)
            player.InitBonusStat();
        
        // 게임 저장
        SaveGame();
    }

    public void UnEquipRelic(string playerId, string relicId)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        if (!playerInfo.relicList.Contains(relicId))
        {
            Debug.Log("해당 유물을 장착하고 있지 않음");
            return;
        }
        
        for (var i = 0; i < playerInfo.relicList.Count; i++)
        {
            if (playerInfo.relicList[i] == relicId)
            {
                playerInfo.relicList[i] = default;
                var relicTableData = TableManager.Instance.relicTable.Relic.Find(x => x.id == relicId);
                var relicName = GetTalk(relicTableData.name);
                Debug.Log($"{relicName}해제");
                
                foreach (var player in players)
                    player.InitBonusStat();
                break;
            }
        }
        
        // 게임 저장
        SaveGame();
    }

    public List<string> GetPlayerRelicList(string playerId)
    {
        return saveData.playerInfoList.Find(x => x.playerId == playerId).relicList;
    }

    public string GetEquipRelicPlayer(string relicId) 
    {
        string player = default;
        foreach (var playerInfo in saveData.playerInfoList)
        {
            if (playerInfo.relicList.Contains(relicId))
            {
                player = playerInfo.playerId;
                break;
            }
        }
        
        return player;
    }
    
    // 현재 캐릭터가 해당 유물을 장착하고 있는가
    public bool GetIsEquippedRelic(string curPlayerId, string relicId) 
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == curPlayerId);

        return playerInfo.relicList.Contains(relicId);
    }
    
    // 현재 캐릭터의 유물 장착슬롯에 공간이 있는가
    public bool GetCanEquipSlot(string playerId) 
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);
        bool canEquipSlot = false;
        foreach (var relic in playerInfo.relicList)
        {
            if (string.IsNullOrWhiteSpace(relic))
            {
                canEquipSlot = true;
                break;
            }
        }
        
        return canEquipSlot;
    }
    
    // 현재 캐릭터의 모든 유물칸이 비어있는가?
    public bool GetIsEmptyRelicList(string playerId)
    {
        bool isEmpty = true;
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);
        foreach (var relic in playerInfo.relicList)
        {
            if (!string.IsNullOrWhiteSpace(relic))
            {
                isEmpty = false;
                break;
            }
        }
        return isEmpty;
    }

    // 현재 캐릭터의 해당 인덱스에 있는 유물의 Id
    public string GetEquippedRelicId(string playerId, int idx)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        if (playerInfo.relicList.Count < idx + 1)
            return ConstValues.Lock;
        
        return playerInfo.relicList[idx];
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
            data.deBuffId = skillAttribute.deBuffId;
            data.buffTime = skillAttribute.buffTime;
            data.buffValue = skillAttribute.buffValue;
            data.talk = skillAttribute.talk;
            data.explainTalk = skillAttribute.explainTalk;
            
            skillAttributeList.Add(data);
        }

        foreach (var relic in tableManager.relicTable.Relic)
        {
            var data = new RelicInfo();
            data.id = relic.id;
            data.name = relic.name;
            data.explain = relic.explain;
            data.rank = (eEquipRank)Enum.Parse(typeof(eEquipRank), relic.rank);

            var statSplit = relic.stat.Split(';');
            foreach (var stat in statSplit)
                data.statList.Add((eEquipStat)Enum.Parse(typeof(eEquipStat), stat));
            
            var valueSplit = relic.value.Split(';');
            foreach (var value in valueSplit)
                data.valueList.Add(int.Parse(value));

            data.specialValue = relic.specialValue;
            
            relicList.Add(data);
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
            player.InitAnimation();
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
    
    public void AddPlayer(string player)
    {
        saveData.playerList.Add(player);
    }
    // 어떤 타입이든 받을 수 있는 회전 메서드
    private void RotatePlayerList()
    {
        // 1. 요소가 없거나 1개뿐이면 회전할 필요가 없음
        if (saveData.playerList.Count <= 1)
            return;

        // 2. 맨 앞의 아이템(0번 인덱스)을 임시 저장
        string firstIdx = saveData.playerList[0];

        // 3. 맨 앞의 아이템을 리스트에서 삭제 (남은 요소들이 앞으로 한 칸씩 당겨짐)
        saveData.playerList.RemoveAt(0);

        // 4. 저장해둔 아이템을 리스트의 맨 마지막에 추가
        saveData.playerList.Add(firstIdx);
    }

    public void InitPlayerStat()
    {
        foreach (var player in players)
        {
            player.InitBasicStat();
            player.InitBonusStat();
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
    
    private void CashingSpeechFrame()
    {
        int count = 3;
        for (int i = 0; i < count; i++)
        {
            speechFrame1.Add(GetSpeechFrame(ConstValues.SpeechFrame1));
            speechFrame2.Add(GetSpeechFrame(ConstValues.SpeechFrame2));
        }
        for (int i = 0; i < count; i++)
        {
            speechFrame1[i].gameObject.SetActive(false);
            speechFrame2[i].gameObject.SetActive(false); 
        }
        speechFrameStrong = GetSpeechFrame(ConstValues.SpeechFrameStrong);
        speechFrameStrong.gameObject.SetActive(false);
        
        speechFrameTitle = GetSpeechFrame(ConstValues.SpeechFrameTitle);
        speechFrameTitle.gameObject.SetActive(false);
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
            playerList = PlayerList,
        };
        var facePresenter = new UICharacterFacePresenter(faceInterface, faceModel);
        uiInterface.SetCharacterFacePresenter(facePresenter);
        
        facePresenter.SetChangeFace();
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
        
        foreach (var skill in tableManager.skillTable.Skill)
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
        foreach (var skill in tableManager.skillTable.Skill)
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
        var nextPlayerId = saveData.playerList[1];
        pastPlayer.AllBuffCancel();

        curPlayer = GetPlayer(nextPlayerId);
        // 교체 시 유지해야하는 데이터 받아오기
        curPlayer.ReceiveChangeData(pastPlayer);
        ActivePlayer(nextPlayerId);

        curPlayer.transform.position = changePos;
        curPlayer.transform.localScale = pastPlayer.transform.localScale;
        curPlayer.JumpAttackCount = 0;
        
        RotatePlayerList();
        RefreshFace();
        
        if (changeAttack)
            curPlayer.ChangeAttack();

        RefreshSkill();
        SetCameraTarget(curPlayer.transform);
    }
    
    public void SetCharacterOrder()
    {
        var pastPlayer = curPlayer;
        var changePos = curPlayer.transform.position;
        
        ActivePlayer(PlayerList[0]);
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

    public void GetGoldProduct(int count, Vector2 boxPos, Action<int> customAction)
    {
        CurPlayer.SpawnObject(ConstValues.BangEffect, boxPos);
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
    
    public void PlusAttributePoint(int point)
    {
        saveData.totalAttributePoint += point;
        foreach (var playerInfo in saveData.playerInfoList)
            playerInfo.attributePoint += point;
    }
    
    public bool IsHaveSkill(string skillId)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill != null)
                return true;
        }
        
        return false;
    }
    public List<string> GetSkillAttribute(string skillId)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skillList = playerInfo.skillList.Find(x => x.skillId == skillId).attributeList;
            if (skillList != null)
                return skillList;
        }
        
        Debug.Log("검색되는 특성 없음");
        return null;
    }
    public bool IsHaveAttribute(string skillId, string attributeId)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill != null)
                return skill.attributeList.Contains(attributeId);
        }

        Debug.Log("해당 특성 자체가 없음");
        return false;
    }
    public async void BuyAttribute(string skillId, string attributeId, Vector3 effectPos)
    {
        var attributeData = skillAttributeList.FindAll(x => x.id == attributeId);

        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill != null)
            {
                var targetAttribute = skill.attributeList.Contains(attributeId);
                if (!targetAttribute)
                {
                    if (playerInfo.attributePoint < attributeData[0].cost)
                    {
                        SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
                        await GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30202));
                    }
                    else
                    {
                        skill.attributeList.Add(attributeId);
                        playerInfo.attributePoint -= attributeData[0].cost;
                        // 올리는 연출 넣기
                        SpawnHighestObject(ConstValues.AttributeUpEffect, effectPos);
                    }
                }
                break;
            }
        }
    }
    
    public async void SellAttribute(string skillId, string attributeId, Vector3 effectPos)
    {
        var attributeData = GameManager.Instance.skillAttributeList.FindAll(x => x.id == attributeId);

        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill != null)
            {
                var targetAttribute = skill.attributeList.Contains(attributeId);

                if (targetAttribute)
                {
                    var attributeList = attributeData.FindAll(x => x.skill == skillId);
                    var attribute = attributeList.Find(x => x.id == attributeId);
                    playerInfo.attributePoint += attribute.cost;
                    skill.attributeList.Remove(attributeId);
                    // 내리는 연출 넣기
                    GameManager.Instance.SpawnHighestObject(ConstValues.AttributeDownEffect, effectPos);
                }
            }
            break;
        }
    }

    public async void ResetAttribute()
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            foreach (var skillList in playerInfo.skillList)
                        skillList.attributeList.Clear();
            
            playerInfo.attributePoint = saveData.totalAttributePoint;
        }
        
        await SpawnWarningPopup(GameManager.Instance.GetTalk(30205));
    }
    
    // 해당 스킬의 패시브 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<string> GetAttributePassive(string id)
    {
        List<string> passiveList = new List<string>();
        string[] idSplit = id.Split('_');
        
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        string targetId = id;
        if (idSplit.Length > 2)
            targetId = $"{idSplit[0]}_{idSplit[1]}_{idSplit[2]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인하고, 그 데이터는 파생기를 포함하여 효과를 적용함
        var attributeData = skillAttributeList.FindAll(x => x.targetObject == targetId);
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
        attributeData = skillAttributeList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
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
        
        string targetId = id;
        if (idSplit.Length > 2)
            targetId = $"{idSplit[0]}_{idSplit[1]}_{idSplit[2]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인하고, 그 데이터는 파생기를 포함하여 효과를 적용함
        var attributeData = skillAttributeList.FindAll(x => x.targetObject == targetId);
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
        attributeData = skillAttributeList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
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
        
        string targetId = id;
        if (idSplit.Length > 2)
            targetId = $"{idSplit[0]}_{idSplit[1]}_{idSplit[2]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인
        var attributeData = skillAttributeList.FindAll(x => x.targetObject == targetId);
        
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
        attributeData = skillAttributeList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
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
            var attributeData = skillAttributeList.FindAll(x => x.skill == skillId);
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
    
    // 해당 스킬의 버프 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeBuffInfo> GetAttributeDeBuff(string id)
    {
        var buffList = new List<SkillAttributeBuffInfo>();
        string[] idSplit = id.Split('_');
        if (idSplit.Length > 1)
        {
            // 파생기도 해당 효과를 적용받음
            string skillId = $"{idSplit[0]}_{idSplit[1]}";
            var attributeData = skillAttributeList.FindAll(x => x.skill == skillId);
            foreach (var attribute in attributeData)
            {
                if (string.IsNullOrWhiteSpace(attribute.deBuffId) || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                var buffInfo = new SkillAttributeBuffInfo
                {
                    buffId = attribute.deBuffId,
                    buffTime = attribute.buffTime,
                    buffValue = attribute.buffValue,
                };
                buffList.Add(buffInfo);
            }
        }
        return buffList;
    }
    
    // 대화
    private Character GetCharacter(string characterId, Npc[] npc)
    {
        Character character = null;
        foreach (var player in Players)
        {
            if (player.name == characterId)
            {
                character = player;
                break;
            }
        }
        foreach (var targetNpc in npc)
        {
            if (targetNpc.name == characterId)
            {
                character = targetNpc;
                break;
            }
        }

        if(character == null)
            Debug.Log($"{characterId}가 존재하지 않는다");
        
        return character;
    }
    
    // 대화 세팅 연출
    public async UniTask NpcDialogue(string choice, Npc[] npc, NpcInfo npcInfo, Action finishAction)
    {
        var talkDataList = TableManager.Instance.dialogueTable.Dialogue.FindAll(x => x.choiceGroupId == choice);
        string checkKey = talkDataList[0].checkKey;
        string endEvent = talkDataList[0].endEvent;
        string eventReward = talkDataList[0].reward;
        bool checkKeyValue = npcInfo.dialogKey.isUse;
        
        List<DialogueData> talkList = new List<DialogueData>();
        if (string.IsNullOrWhiteSpace(checkKey))
        {
            talkList.AddRange(talkDataList);
        }
        else
        {
            talkList.AddRange(talkDataList.FindAll(x => x.checkKey == checkKey && x.checkKeyValue == checkKeyValue));
        }
        
        foreach (var talk in talkList)
        {
            var speechFrame = speechFrame1[0];
            switch (talk.speechFrame)
            {
                case ConstValues.SpeechFrame2:
                    speechFrame = speechFrame2[0];
                    break;
            }

            var speechCharacter = GetCharacter(talk.speaker, npc);
            List<Character> poseCharacterList = new List<Character>();

            var poseCharacters = talk.poseCharacter.Split(';');
            foreach (var poseCharacter in poseCharacters)
            {
                if (!string.IsNullOrWhiteSpace(poseCharacter))
                {
                    poseCharacterList.Add(GetCharacter(poseCharacter, npc));
                }
            }
            
            List<string> speechPoseList = new List<string>();
            var speechPoses = talk.speechPose.Split(';');
            foreach (var speechPose in speechPoses)
            {
                if (!string.IsNullOrWhiteSpace(speechPose))
                {
                    speechPoseList.Add(speechPose);
                }
            }
            
            var speechPos = speechCharacter.SpeechPos;

            for (var i = 0; i < poseCharacterList.Count; i++)
                poseCharacterList[i].CustomAnimTrigger(ENormalState.Idle, speechPoseList[i], ConstValues.Idle);

            if(!string.IsNullOrWhiteSpace(talk.sound))
                SoundManager.Instance.PlaySound(talk.sound);
            
            var cameraShakeArray = talk.cameraShake.Split(';');
            var cameraShake = new Vector2(float.Parse(cameraShakeArray[0]), float.Parse(cameraShakeArray[1]));
            if(cameraShake != Vector2.zero)
                CameraShake(cameraShake.x, cameraShake.y, talk.shakeTime);
            
            SpawnSpeechFrame(speechFrame, speechPos.position, GetTalk(talk.talk));
            await NextDialog(speechFrame);
            if (talk.isEnd)
                break;
        }
        ControlStart = true;

        if (string.IsNullOrWhiteSpace(endEvent))
        {
            finishAction();
        }
        else
        {
            if(checkKeyValue)
                finishAction();
            else
                PlayEndEvent(npcInfo, endEvent, eventReward);
        }
    }
    
    private void PlayEndEvent(NpcInfo npcInfo, string eventKey, string reward)
    {
        switch (eventKey)
        {
            case ConstValues.GetSkill:
                npcInfo.dialogKey.isUse = true;
                AddNewSkill(reward);
                GetSkillProduct(reward, GetSkillDialogue);
                break;
        }
    }

    public async UniTask NpcFirstTalk(string startDialog, Transform speechPos)
    {
        var firstTalk = TableManager.Instance.dialogueTable.Dialogue.Find(x => x.id == startDialog);
        var speechFrame = speechFrame1[0];
        switch (firstTalk.speechFrame)
        {
            case ConstValues.SpeechFrame2:
                speechFrame = speechFrame2[0];
                break;
        }
            
        SpawnSpeechFrame(speechFrame, speechPos.position, GameManager.Instance.GetTalk(firstTalk.talk));
        await NextDialog(speechFrame);
    }
    
    private void SpawnSpeechFrame(SpeechFrame speechFrame, Vector2 speechPos, string dialog)
    {
        speechFrame.SetPos(speechPos);
        speechFrame.Speech(dialog);
    }

    private async UniTask NextDialog(SpeechFrame speechFrame)
    {
        speechFrame.NextObjectActive();
        // 스페이스바를 누르면 넘어간다
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: GameManager.Instance.ProductCancellation.Token).SuppressCancellationThrow())
        {
            speechFrame.SpeechEnd();
            return;
        }
        speechFrame.SpeechEnd();
    }
    
    // 스킬 획득 문구
    private async void GetSkillDialogue(string skillName)
    {
        string getMessage = string.Format(GetTalk(30200), skillName);
        await SpawnWarningPopup(getMessage);
    }

    public async UniTask DialogueMove(float xPos)
    {
        // 항상 광전사가 플레이어의 위치에 있어야함
        var berserker = GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();

        var berserkerPos = curPlayer.transform.position;
        var gunnerPos = new Vector2(berserkerPos.x + xPos, berserkerPos.y);

        berserker.gameObject.SetActive(true);
        berserker.transform.position = berserkerPos;
        berserker.Flip(1);

        gunner.gameObject.SetActive(true);
        gunner.transform.position = berserkerPos;
        await gunner.EpisodeMove(gunnerPos, gunner.BasicStat.moveSpeed, 1);
    }
    
    public void DialogueEnd()
    {
        var berserker = GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        
        if (curPlayer == berserker)
        {
            gunner.SpawnObject(ConstValues.BangEffect, gunner.CenterPos.position);
            gunner.gameObject.SetActive(false);
        }
        else if (curPlayer == gunner)
        {
            berserker.SpawnObject(ConstValues.BangEffect, berserker.CenterPos.position);
            berserker.gameObject.SetActive(false);
        }
    }
}
