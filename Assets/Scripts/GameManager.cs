using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

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

    public static void Copy(string srcFileName, string dstFileName)
    {
        try
        {
            string srcPath = GetSavePath(srcFileName);
            string dstPath = GetSavePath(dstFileName);
            if (File.Exists(srcPath))
                File.Copy(srcPath, dstPath, overwrite: true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Copy failed ({srcFileName} → {dstFileName}): {e}");
        }
    }
}

public static class KeyBinding
{
    // 저장할 때
    public static void SaveKey(string prefKey, KeyCode key)
    {
        Debug.Log($"{prefKey}를 {key}로 저장");
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
        
        // 처음 실행 시 디폴트 키를 저장
        SaveKey(prefKey, defaultKey);
        return defaultKey;
    }
}

public static class VolumeBinding
{
    // 저장할 때
    public static void SaveVolume(string prefKey, float volume)
    {
        PlayerPrefs.SetFloat(prefKey, volume);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 볼륨도 지정 가능)
    public static float LoadVolume(string prefKey, float defaultVolume)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            Debug.Log($"저장된 {prefKey}: {PlayerPrefs.GetFloat(prefKey)}");
            return PlayerPrefs.GetFloat(prefKey);
        }
        
        // 처음 실행 시 디폴트 볼륨 저장
        Debug.Log($"최초 {prefKey} 설정: {defaultVolume}");
        SaveVolume(prefKey, defaultVolume);
        return defaultVolume;
    }
}

public static class SettingStringBinding
{
    // 저장할 때
    public static void SaveGameSetting(string prefKey, string value)
    {
        PlayerPrefs.SetString(prefKey, value);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 설정값도 지정 가능)
    public static string LoadSetting(string prefKey, string defaultValue)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            Debug.Log($"저장된 {prefKey}: {PlayerPrefs.GetString(prefKey)}");
            return PlayerPrefs.GetString(prefKey);
        }
        
        // 처음 실행 시 디폴트 설정 저장
        Debug.Log($"최초 {prefKey} 설정: {defaultValue}");
        SaveGameSetting(prefKey, defaultValue);
        return defaultValue;
    }
}

public static class SettingIntBinding
{
    // 저장할 때
    public static void SaveGameSetting(string prefKey, int value)
    {
        PlayerPrefs.SetInt(prefKey, value);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 설정값도 지정 가능)
    public static int LoadSetting(string prefKey, int defaultValue)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            Debug.Log($"저장된 {prefKey}: {PlayerPrefs.GetInt(prefKey)}");
            return PlayerPrefs.GetInt(prefKey);
        }
        
        // 처음 실행 시 디폴트 설정 저장
        Debug.Log($"최초 {prefKey} 설정: {defaultValue}");
        SaveGameSetting(prefKey, defaultValue);
        return defaultValue;
    }
}

[Serializable]
public class Skill
{
    public string skillId;
    public List<string> attributeList = new List<string>();
}

[Serializable]
public class SkillAttributeCopy
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
    public bool firstLock;
}

public enum eItemRank
{
    Normal,
    Rare,
}

[Flags]
public enum eItemStat
{
    Power,
    Defence,
    MoveSpeed,
    AttackSpeed,
    CriticalPercent,
    CriticalDamage,
    StaggerDamage,
}

[Serializable]
public class RelicCopy
{
    public string id;
    public int name;
    public int explain;
    public eItemRank rank;
    public List<eItemStat> statList = new List<eItemStat>();
    public List<int> valueList = new List<int>();
    public string specialValue;
}

[Serializable]
public class NpcCopy
{
    public string id;
    public int talk;
    public string firstDialog;
    public string startDialog;
    public List<string> dialogKey = new List<string>();
    public List<string> questItemId = new List<string>();
    public List<int> questItemCount = new List<int>();
    public string questClearChoice;
}

[Serializable]
public class DialogueChoiceCopy
{
    public string id;
    public string npc;
    public int talk;
    public List<string> checkKey = new List<string>();
    public List<bool> checkKeyValue = new List<bool>();
}

[Serializable]
public class GrenadeCopy
{
    public string id;
    public string minForce;
    public string maxForce;
    public bool spinGrenade;
    public bool dirObject;
    public string hitTag;
    public string spawnObject;
}

[Serializable]
public class PassiveCopy
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

public enum eItemType
{
    Normal,
    Relic,
}

[Serializable]
public class ItemCopy
{
    public string id;
    public int name;
    public int explain;
    public eItemRank rank;
    public eItemType type;
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
// 상황에 따라 껐다 켰다 하는 오브젝트
public class EventCustomObject
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
// 특성 포인트
public class AttributePoint
{
    public int count;
    public bool alreadyGet;
}

[Serializable]
// 유물
public class Relic
{
    public string id;
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
    public bool merchantCheck;                                       // 상인
    public bool attributePointCheck;                                 // 특성 포인트

    public List<RoomProduct> roomProduct = new List<RoomProduct>();
    public List<EventNpc> eventNpc = new List<EventNpc>();
    public List<EventCustomObject> customObject = new List<EventCustomObject>();
    public List<ShortCut> shortCut = new List<ShortCut>();
    public List<SkillAndPassive> skillAndPassive = new List<SkillAndPassive>();
    public List<TreasureBox> treasureBox = new List<TreasureBox>();
    public List<AttributePoint> attributePoint = new List<AttributePoint>();
    public List<Relic> relic = new List<Relic>();
    public List<Item> item = new List<Item>();
    public List<ElevatorData> elevators = new List<ElevatorData>();
    public List<LockDoorData> lockDoors = new List<LockDoorData>();
}

[Serializable]
public class HaveItemInfo
{
    public string id;
    public int count;
}

[Serializable]
public class AttributeLockInfo
{
    public string id;
    public bool isLock;
}

[Serializable]
public class NpcInfo
{
    public string id;
    public List<DialogKey> dialogKey = new List<DialogKey>();
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
    Popup_Pause,
    Popup_Setting,
    Popup_FastTravel,
}

[Serializable]
public class SaveData
{
    // 재화
    public int gold;

    // 세이브 포인트
    public string savePoint;

    // 마지막 저장 시각 (UTC, ISO 8601 round-trip 포맷)
    public string lastSavedAt;

    public bool firstGetSkill;
    public bool firstGetAttribute;
    public bool firstDamaged;

    public List<string> playerList = new List<string>();
    public List<HaveItemInfo> itemList = new List<HaveItemInfo>();
    public List<string> relicList = new List<string>();
    public List<AttributeLockInfo> lockAttributeList = new List<AttributeLockInfo>();
    
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
    public GameObject inGameDebugConsole;
    
    public KeyCode escKey;
    public KeyCode spaceKey;
    public KeyCode confirmKey;
    public KeyCode deleteKey;
    public KeyCode copyKey;
    
    public KeyCode leftKey;
    public KeyCode rightKey;
    public KeyCode upKey;
    public KeyCode downKey;
    public KeyCode miniMapKey;
    public KeyCode characterInfoKey;
    public KeyCode attackKey;
    public KeyCode jumpKey;

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
    
    public KeyCode potionKey;

    public KeyCode changeCharacterLeftKey;
    public KeyCode changeCharacterRightKey;
    
    public KeyCode pauseKey;

    public float masterVolume;
    public float sfxVolume;
    public float bgmVolume;

    public int resolutionX;
    public int resolutionY;
    public int fullScreen;
    public int vSync;

    public string language;
    public int cameraShaking;
   
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

    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>();
    [SerializeField] private List<GameObject> objectList = new List<GameObject>();
    [SerializeField] private List<Monster> monsterList = new List<Monster>();
    
    [SerializeField] private FadeSystem fadeSystem;

    private UI_Interface uiInterface;
    private Popup_Warning popupWarning;
    
    // 세이브 넘버
    private string curSaveFileName;

    [SerializeField] private bool inGame;
    [SerializeField] private bool controlStart;
    private bool standLock;
    private bool bossProduct;
    private bool timeProduct;
    private int comboCount;

    [SerializeField] private SaveData saveData;
    
    // 등록된 스킬 및 키 세팅 목록
    private SettingSkill changeSkill;
    
    // 복제체 데이터들
    public List<SkillAttributeCopy> skillAttributeCopyList = new List<SkillAttributeCopy>();
    public List<ItemCopy> itemCopyList = new List<ItemCopy>();
    public List<RelicCopy> relicCopyList = new List<RelicCopy>();
    public List<NpcCopy> npcCopyList = new List<NpcCopy>();
    public List<DialogueChoiceCopy> dialogueChoiceCopyList = new List<DialogueChoiceCopy>();
    public List<GrenadeCopy> grenadeCopyList = new List<GrenadeCopy>();
    public List<PassiveCopy> passiveCopyList = new List<PassiveCopy>();
    
    // 매니저들
    public TableManager tableManager;

    // 카메라
    private FollowCamera mainCamera;
    [SerializeField] private Transform miniMapCamera;
    [SerializeField] private Canvas uiObjectCanvas;

    private CancellationTokenSource productCancellation;

    // 프로퍼티
    public Player CurPlayer
    {
        get => curPlayer;
        set => curPlayer = value;
    }

    public FadeSystem FadeSystem => fadeSystem;

    public bool InGame 
    {
        get => inGame;
        set => inGame = value;
    }
    
    public bool ControlStart
    {
        get => controlStart;
        set
        {
            controlStart = value;
            // 연출 진입(false 전환) 시 누른 채였던 Move 상태/잔여 속도만 정리.
            // 입력 플래그(isLeftMove/isRightMove)는 유지 → 방 이동 후 ControlStart가 다시 true가 되면
            // 키를 다시 누르지 않아도 홀드 중인 방향으로 이동이 이어진다.
            if (!value && CurPlayer)
            {
                CurPlayer.Stop();
                CurPlayer.StopVelocity_X();
            }
        }
    }

    public bool StandLock
    {
        get => standLock;
        set => standLock = value;
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
    
    public bool FirstDamaged
    {
        get => saveData.firstDamaged;
        set => saveData.firstDamaged = value;
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

    public List<AttributeLockInfo> LockAttributeList
    {
        get => saveData.lockAttributeList;
    }

    public List<Vector2> MiniMapCheckers
    {
        get => saveData.miniMapCheckers;
        set => saveData.miniMapCheckers = value;
    }

    public List<HaveItemInfo> ItemList => saveData.itemList;

    public List<PlayerInfo> PlayerInfoList => saveData.playerInfoList;

    public List<RoomInfo> RoomInfoList => saveData.roomInfoList;
    public List<NpcInfo> NpcInfoList => saveData.npcInfoList;

    public SettingSkill ChangeSkill => changeSkill;

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

    protected override void Awake()
    {
        base.Awake();
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 60;
        
        InitManager();
        SetCopyData();
        InitAtlas(uiAtlas);
        InitAtlas(bgAtlas);
        SetPrefabActive(false);
        DefaultKeySetting();
        FirstCashing();
        //CashingSpeechFrame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
            inGameDebugConsole.SetActive(!inGameDebugConsole.activeSelf);

        // Alt+Enter: 전체화면 <-> 창모드 토글
        if (InputHelper.IsAltPressed && (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            ToggleFullScreen();
    }

    // 전체화면 상태를 토글하고 저장 (Alt+Enter)
    // SetResolution으로 창을 다시 만들어야 창모드 복귀 시 크기 조절 핸들이 정상 복원된다
    private void ToggleFullScreen()
    {
        fullScreen = fullScreen == 1 ? 0 : 1;
        SettingIntBinding.SaveGameSetting(ConstValues.FullScreen, fullScreen);

        Vector2Int resolution = PopupVideoView.ClampToDisplay(resolutionX, resolutionY);
        Screen.SetResolution(resolution.x, resolution.y, fullScreen == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    private void OnDestroy()
    {
        SetPrefabActive(true);
    }
    
    public void SaveGame()
    {
        // 저장 시각 갱신 (UTC, 로케일 무관 round-trip 포맷)
        saveData.lastSavedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        // json화
        SaveSystem.Save(curSaveFileName, saveData);
    }

    public SaveData LoadGame(string fileName)
    {
        SaveData data = null;
        // json화
        if(SaveSystem.TryLoad(fileName, out SaveData loadData))
            data = loadData;

        return data;
    }

    private void DataPatch(SaveData data)
    {
        // 패치 필요 여부 확인 (마지막 저장 시각이 패치 기준 시각보다 이전이면 패치 필요)
        if (data != null && DateTime.TryParse(data.lastSavedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime lastSavedAt))
        {
            // 기준 시각: 한국 시간(KST, UTC+9) 2026년 5월 29일 00시
            DateTime patchTime1 = new DateTime(2026, 6, 1, 7, 0, 0);
            if (lastSavedAt.ToUniversalTime() < patchTime1)
            {
                Debug.Log("1차 패치 필요");
                // 특성 초기화
                foreach (var playerInfo in data.playerInfoList)
                {
                    switch (playerInfo.playerId)
                    {
                        case ConstValues.Berserker:
                            AddDashSkill(ConstValues.BerserkerDash, playerInfo);
                            break;
                
                        case ConstValues.Gunner:
                            AddDashSkill(ConstValues.GunnerDash, playerInfo);
                            break;
                
                        case ConstValues.Fighter:
                            AddDashSkill(ConstValues.FighterDash, playerInfo);
                            break;
                    }

                    playerInfo.attributePoint = data.totalAttributePoint;
                    foreach (var skill in playerInfo.skillList)
                        skill.attributeList.Clear();
                }
            }
            
            DateTime patchTime2 = new DateTime(2026, 6, 8, 5, 20, 0);
            if (lastSavedAt.ToUniversalTime() < patchTime2)
            {
                Debug.Log("2차 패치 필요");
                // 세이브 이동 아이템 추가
                var bossRoom2 = saveData.roomInfoList.Find(x => x.roomId == ConstValues.RoomBoss2);
                if (bossRoom2 != null && bossRoom2.roomProduct[0].isFinish)
                    GetItem(ConstValues.SaveTravel, 1);
            }
            
            DateTime patchTime3 = new DateTime(2026, 6, 8, 19, 55, 0);
            if (lastSavedAt.ToUniversalTime() < patchTime3)
            {
                Debug.Log("3차 패치 필요");
                // 기존 보물상자의 데이터와 새로운 특성포인트를 동기화 시키기
                foreach (var roomInfo in data.roomInfoList)
                {
                    List<TreasureBox> removingTreasureBoxList = new List<TreasureBox>();
                    foreach (var treasureBox in roomInfo.treasureBox)
                    {
                        if (treasureBox.id != ConstValues.AttributePoint)
                            continue;
                        
                        removingTreasureBoxList.Add(treasureBox);
                        roomInfo.attributePoint.Clear();
                        AttributePoint attributePoint = new AttributePoint
                        {
                            count = treasureBox.count,
                            alreadyGet = treasureBox.alreadyGet
                        };
                        roomInfo.attributePoint.Add(attributePoint);
                    }

                    foreach (var removingTreasureBox in removingTreasureBoxList)
                        roomInfo.treasureBox.Remove(removingTreasureBox);
                    
                }
            }
        }
    }
    
    public void DeleteData()
    {
        // json화
        SaveSystem.Delete(curSaveFileName);
    }

    public void CopyData(int srcIdx, int dstIdx)
    {
        string srcName = $"{ConstValues.User}_{srcIdx}";
        string dstName = $"{ConstValues.User}_{dstIdx}";
#if UNITY_EDITOR
        srcName = $"{ConstValues.User}_{srcIdx}_Editor";
        dstName = $"{ConstValues.User}_{dstIdx}_Editor";
#endif
        if (!SaveSystem.Exists(srcName))
            return;
        SaveSystem.Copy(srcName, dstName);
    }

    private void FirstStart()
    {
        DefaultDataSetting();
        DefaultSkillSetting();
        DefaultRelicSetting();
        DefaultMapSetting();
        DefaultNpcSetting();
        AddPlayer(ConstValues.Berserker);
        SaveGame();
    }

    public async void GameStart()
    {
        controlStart = true;
        CreatePlayer();
        GameStartSetting();
        InitPlayer();
        InitChangeSkill();
        
        BgmManager.Instance.Stop();
        SoundManager.Instance.PlaySound(ConstValues.Upgrade, true);
        if (await Fading(0, 1, 0.75f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;
        
        GoScene(ConstValues.BattleScene);
    }

    public string SaveFileName(int idx)
    {
        string fileName = default;
        
        fileName = $"{ConstValues.User}_{idx}";
#if UNITY_EDITOR
        fileName = $"{ConstValues.User}_{idx}_Editor";
#endif
        curSaveFileName = fileName;
        
        if (!SaveSystem.Exists(fileName))
            fileName = default;
        
        return fileName;
    }

    private void CreatePlayer()
    {
        players.Add(SpawnToObjectPool(ConstValues.Berserker, Vector2.zero).GetComponent<Player>());
        players.Add(SpawnToObjectPool(ConstValues.Gunner, Vector2.zero).GetComponent<Player>());
        players.Add(SpawnToObjectPool(ConstValues.Fighter, Vector2.zero).GetComponent<Player>());
        foreach (var player in players)
        {
            var playerSplit = player.name.Split('(');
            player.name = playerSplit[0];
            player.gameObject.SetActive(false);
        }
    }

    private void GameStartSetting()
    {
        if (SaveSystem.Exists(curSaveFileName))
        {
            saveData = LoadGame(curSaveFileName);
            DataPatch(saveData);
        }
        else
        {
            FirstStart(); 
        }
        
        LockAttributeSetting();
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
        string talk = default;
        switch (language)
        {
            case ConstValues.Korean:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).kr;
                break;
            
            case ConstValues.English:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).en;
                break;
        }
        
        return talk;
    }
    public string GetCharacterTalk(string id)
    {
        string talk = default;
        switch (id)
        {
            case ConstValues.Berserker:
                talk = GetTalk(50000);
                break;
            
            case ConstValues.Gunner:
                talk = GetTalk(50001);
                break;
            
            case ConstValues.Fighter:
                talk = GetTalk(50002);
                break;
        }

        return talk;
    }
    public string GetItemTalk(string id)
    {
        int itemName = TableManager.Instance.itemTable.Item.Find(x => x.id == id).name;
        return GetTalk(itemName);
    }
    
    public string GetItemExplain(string id)
    {
        int itemExplain = TableManager.Instance.itemTable.Item.Find(x => x.id == id).explain;
        return GetTalk(itemExplain);
    }

    public string GetStatName(string statId)
    {
        string value = "Null!";
        switch (statId)
        {
            case ConstValues.CritPercent:
                return GetTalk(50105);
        }

        return value;
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
        saveData.playerInfoList = new List<PlayerInfo>();
        
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
                    AddDashSkill(ConstValues.BerserkerDash, playerInfo);
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.BerserkerDash, dashKey));
                    break;
                
                case 1:
                    playerInfo.playerId = ConstValues.Gunner;
                    AddDashSkill(ConstValues.GunnerDash, playerInfo);
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.GunnerDash, dashKey));
                    break;
                
                case 2:
                    playerInfo.playerId = ConstValues.Fighter;
                    AddDashSkill(ConstValues.FighterDash, playerInfo);
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

    private void LockAttributeSetting()
    {
        if (skillAttributeCopyList.Count > saveData.lockAttributeList.Count)
        {
            foreach (var skillAttributeCopy in skillAttributeCopyList)
            {
                if (!saveData.lockAttributeList.Exists(x => x.id == skillAttributeCopy.id))
                {
                    var attributeLockInfo = new AttributeLockInfo();
                    attributeLockInfo.id = skillAttributeCopy.id;
                    attributeLockInfo.isLock = skillAttributeCopy.firstLock;
                    saveData.lockAttributeList.Add(attributeLockInfo);
                }
            }
        }
        else
        {
            foreach (var skillAttributeCopy in skillAttributeCopyList)
            {
                var targetAttribute = saveData.lockAttributeList.Find(x => x.id == skillAttributeCopy.id);
                if (targetAttribute == null)
                    continue;
                
                if (targetAttribute.isLock && !skillAttributeCopy.firstLock)
                    targetAttribute.isLock = false;
            }
        }
    }

    public void UnLockAttributeSlot(string attributeId)
    {
        var targetAttribute = saveData.lockAttributeList.Find(x => x.id == attributeId);
        targetAttribute.isLock = false;
    }

    private void DefaultKeySetting()
    {
        escKey = KeyCode.Escape;
        spaceKey = KeyCode.Space;
        confirmKey = KeyCode.Return;
        deleteKey = KeyCode.X;
        copyKey = KeyCode.C;
        
        changeCharacterLeftKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterLeftKey, KeyCode.Q);
        changeCharacterRightKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterRightKey, KeyCode.E);
        
        // 게임
        language = SettingStringBinding.LoadSetting(ConstValues.Language, Application.systemLanguage.ToString());
        cameraShaking = SettingIntBinding.LoadSetting(ConstValues.CameraShaking, 1);
        
        // 오디오
        masterVolume = VolumeBinding.LoadVolume(ConstValues.MasterVolume, 0.8f);
        sfxVolume = VolumeBinding.LoadVolume(ConstValues.SFXVolume, 1.0f);
        bgmVolume = VolumeBinding.LoadVolume(ConstValues.BGMVolume, 1.0f);
        
        // 비디오
        resolutionX = SettingIntBinding.LoadSetting(ConstValues.ResolutionX, 1920);
        resolutionY = SettingIntBinding.LoadSetting(ConstValues.ResolutionY, 1080);
        fullScreen = SettingIntBinding.LoadSetting(ConstValues.FullScreen, 1);
        vSync = SettingIntBinding.LoadSetting(ConstValues.Vsync, 1);
        
        // 키 코드
        leftKey = KeyBinding.LoadKey(ConstValues.LeftKey, KeyCode.LeftArrow);
        rightKey = KeyBinding.LoadKey(ConstValues.RightKey, KeyCode.RightArrow);
        upKey = KeyBinding.LoadKey(ConstValues.UpKey, KeyCode.UpArrow);
        downKey = KeyBinding.LoadKey(ConstValues.DownKey, KeyCode.DownArrow);
        miniMapKey = KeyBinding.LoadKey(ConstValues.MiniMapKey, KeyCode.Tab);
        characterInfoKey = KeyBinding.LoadKey(ConstValues.CharacterInfoKey, KeyCode.I);
        attackKey = KeyBinding.LoadKey(ConstValues.AttackKey, KeyCode.X);
        jumpKey = KeyBinding.LoadKey(ConstValues.JumpKey, KeyCode.C);
        changeCharacterKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterKey, KeyCode.LeftShift);
        dashKey = KeyBinding.LoadKey(ConstValues.DashKey, KeyCode.Z);
        skillKey1 = KeyBinding.LoadKey(ConstValues.SkillKey1, KeyCode.A);
        skillKey2 = KeyBinding.LoadKey(ConstValues.SkillKey2, KeyCode.S);
        skillKey3 = KeyBinding.LoadKey(ConstValues.SkillKey3, KeyCode.D);
        skillKey4 = KeyBinding.LoadKey(ConstValues.SkillKey4, KeyCode.F);
        potionKey = KeyBinding.LoadKey(ConstValues.PotionKey, KeyCode.R);
        pauseKey = KeyBinding.LoadKey(ConstValues.PauseKey, KeyCode.Escape);
    }
    public void SetDefaultGame()
    {
        SettingStringBinding.SaveGameSetting(ConstValues.Language, Application.systemLanguage.ToString());
        SettingIntBinding.SaveGameSetting(ConstValues.CameraShaking, 1);
        
        language = SettingStringBinding.LoadSetting(ConstValues.Language, Application.systemLanguage.ToString());
        cameraShaking = SettingIntBinding.LoadSetting(ConstValues.CameraShaking, 1);
    }
    public void SetDefaultAudio()
    {
        VolumeBinding.SaveVolume(ConstValues.MasterVolume, 0.8f);
        VolumeBinding.SaveVolume(ConstValues.SFXVolume, 1.0f);
        VolumeBinding.SaveVolume(ConstValues.BGMVolume, 1.0f);
        
        masterVolume = VolumeBinding.LoadVolume(ConstValues.MasterVolume, 0.8f);
        sfxVolume = VolumeBinding.LoadVolume(ConstValues.SFXVolume, 1.0f);
        bgmVolume = VolumeBinding.LoadVolume(ConstValues.BGMVolume, 1.0f);
    }
    public void SetDefaultVideo()
    {
        SettingIntBinding.SaveGameSetting(ConstValues.ResolutionX, 1920);
        SettingIntBinding.SaveGameSetting(ConstValues.ResolutionY, 1080);
        SettingIntBinding.SaveGameSetting(ConstValues.FullScreen, 1);
        SettingIntBinding.SaveGameSetting(ConstValues.Vsync, 1);
        
        resolutionX = SettingIntBinding.LoadSetting(ConstValues.ResolutionX, 1920);
        resolutionY = SettingIntBinding.LoadSetting(ConstValues.ResolutionY, 1080);
        fullScreen = SettingIntBinding.LoadSetting(ConstValues.FullScreen, 1);
        vSync = SettingIntBinding.LoadSetting(ConstValues.Vsync, 1);
    }
    public void SetDefaultKeyboard()
    {
        KeyBinding.SaveKey(ConstValues.LeftKey, KeyCode.LeftArrow);
        KeyBinding.SaveKey(ConstValues.RightKey, KeyCode.RightArrow);
        KeyBinding.SaveKey(ConstValues.UpKey, KeyCode.UpArrow);
        KeyBinding.SaveKey(ConstValues.DownKey, KeyCode.DownArrow);
        KeyBinding.SaveKey(ConstValues.MiniMapKey, KeyCode.Tab);
        KeyBinding.SaveKey(ConstValues.CharacterInfoKey, KeyCode.I);
        KeyBinding.SaveKey(ConstValues.AttackKey, KeyCode.X);
        KeyBinding.SaveKey(ConstValues.JumpKey, KeyCode.C);
        KeyBinding.SaveKey(ConstValues.ChangeCharacterKey, KeyCode.LeftShift);
        KeyBinding.SaveKey(ConstValues.DashKey, KeyCode.Z);
        KeyBinding.SaveKey(ConstValues.SkillKey1, KeyCode.A);
        KeyBinding.SaveKey(ConstValues.SkillKey2, KeyCode.S);
        KeyBinding.SaveKey(ConstValues.SkillKey3, KeyCode.D);
        KeyBinding.SaveKey(ConstValues.SkillKey4, KeyCode.F);
        KeyBinding.SaveKey(ConstValues.PotionKey, KeyCode.R);
        KeyBinding.SaveKey(ConstValues.PauseKey, KeyCode.Escape);
        
        leftKey = KeyBinding.LoadKey(ConstValues.LeftKey, KeyCode.LeftArrow);
        rightKey = KeyBinding.LoadKey(ConstValues.RightKey, KeyCode.RightArrow);
        upKey = KeyBinding.LoadKey(ConstValues.UpKey, KeyCode.UpArrow);
        downKey = KeyBinding.LoadKey(ConstValues.DownKey, KeyCode.DownArrow);
        miniMapKey = KeyBinding.LoadKey(ConstValues.MiniMapKey, KeyCode.Tab);
        characterInfoKey = KeyBinding.LoadKey(ConstValues.CharacterInfoKey, KeyCode.I);
        attackKey = KeyBinding.LoadKey(ConstValues.AttackKey, KeyCode.X);
        jumpKey = KeyBinding.LoadKey(ConstValues.JumpKey, KeyCode.C);
        changeCharacterKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterKey, KeyCode.LeftShift);
        dashKey = KeyBinding.LoadKey(ConstValues.DashKey, KeyCode.Z);
        skillKey1 = KeyBinding.LoadKey(ConstValues.SkillKey1, KeyCode.A);
        skillKey2 = KeyBinding.LoadKey(ConstValues.SkillKey2, KeyCode.S);
        skillKey3 = KeyBinding.LoadKey(ConstValues.SkillKey3, KeyCode.D);
        skillKey4 = KeyBinding.LoadKey(ConstValues.SkillKey4, KeyCode.F);
        potionKey = KeyBinding.LoadKey(ConstValues.PotionKey, KeyCode.R);
        pauseKey = KeyBinding.LoadKey(ConstValues.PauseKey, KeyCode.Escape);
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
        NpcInfoList.Clear();
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

    private void AddDashSkill(string id, PlayerInfo playerInfo)
    {
        // 이미 가지고 있는 스킬이라면 무시해버린다
        if (playerInfo.skillList.Exists(x => x.skillId == id))
            return;

        Skill newSkill = new Skill();
        newSkill.skillId = id;
        newSkill.attributeList = new List<string>();
        playerInfo.skillList.Add(newSkill);
        
        var dashSkill = playerInfo.skillList[^1];
        playerInfo.skillList.RemoveAt(playerInfo.skillList.Count - 1);
        playerInfo.skillList.Insert(0, dashSkill);
        
        RefreshSkill();
        // 게임 저장
        SaveGame();
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
                var itemData = itemCopyList.Find(x => x.id == relicId);
                var relicName = GetTalk(itemData.name);
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
        var itemData = itemCopyList.Find(x => x.id == relicId);
        var relicName = GetTalk(itemData.name);
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
                var itemData = itemCopyList.Find(x => x.id == relicId);
                var relicName = GetTalk(itemData.name);
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

    public string GetRelicStat(RelicCopy relicCopy, int idx)
    {
        StringBuilder sb = new StringBuilder();
        switch (relicCopy.statList[idx])
        {
            case eItemStat.Power:
                sb.Append(GetTalk(50101));
                break;
                
            case eItemStat.Defence:
                sb.Append(GetTalk(50102));
                break;
            
            case eItemStat.MoveSpeed:
                sb.Append(GetTalk(50103));
                break;
            
            case eItemStat.AttackSpeed:
                sb.Append(GetTalk(50104));
                break;
            
            case eItemStat.CriticalPercent:
                sb.Append(GetTalk(50105));
                break;
            
            case eItemStat.CriticalDamage:
                sb.Append(GetTalk(50106));
                break;
            
            case eItemStat.StaggerDamage:
                sb.Append(GetTalk(50107));
                break;
        }
                
        if(relicCopy.valueList[idx] > 0)
            sb.Append($" +{relicCopy.valueList[idx]}");
        else
            sb.Append($" -{relicCopy.valueList[idx]}");
                
        switch (relicCopy.statList[idx])
        {
            case eItemStat.MoveSpeed:
                sb.Append('%');
                break;
            case eItemStat.AttackSpeed:
                sb.Append('%');
                break;
            case eItemStat.CriticalPercent:
                sb.Append('%');
                break;
            case eItemStat.CriticalDamage:
                sb.Append('%');
                break;
            case eItemStat.StaggerDamage:
                sb.Append('%');
                break;
        }

        return sb.ToString();
    }

    public void UnLockRelicSlot(string playerId)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);
        playerInfo.relicList.Add(default);
    }

    // 구매 성공 시 true, 골드 부족 등으로 실패 시 false 반환
    public bool BuyItem(StoreItemData storeItemData)
    {
        var itemData = itemCopyList.Find(x => x.id == storeItemData.id);
        if (Gold < storeItemData.cost)
        {
            SpawnWarningPopup(GetTalk(30212)).Forget();
            SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
            return false;
        }

        switch (itemData.type)
        {
            case eItemType.Relic:
                saveData.relicList.Add(storeItemData.id);
                break;
        }
        Gold -= storeItemData.cost;
        SoundManager.Instance.PlaySound(ConstValues.ProductMailDelivery, true);
        SpawnWarningPopup(GetTalk(30216)).Forget();
        SaveGame();
        return true;
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
            var data = new SkillAttributeCopy();
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
            data.firstLock = skillAttribute.firstLock;
            
            skillAttributeCopyList.Add(data);
        }
        
        foreach (var item in tableManager.itemTable.Item)
        {
            var data = new ItemCopy();
            data.id = item.id;
            data.name = item.name;
            data.explain = item.explain;
            data.rank = (eItemRank)Enum.Parse(typeof(eItemRank), item.rank);
            data.type = (eItemType)Enum.Parse(typeof(eItemType), item.type);
            
            itemCopyList.Add(data);
        }

        foreach (var relic in tableManager.relicTable.Relic)
        {
            var data = new RelicCopy();
            data.id = relic.id;
            
            var itemData = itemCopyList.Find(x => x.id == relic.id);
            data.name = itemData.name;
            data.explain = itemData.explain;
            data.rank = itemData.rank;

            var statSplit = relic.stat.Split(';');
            foreach (var stat in statSplit)
                data.statList.Add((eItemStat)Enum.Parse(typeof(eItemStat), stat));
            
            var valueSplit = relic.value.Split(';');
            foreach (var value in valueSplit)
                data.valueList.Add(int.Parse(value));

            data.specialValue = relic.specialValue;
            
            relicCopyList.Add(data);
        }

        foreach (var npc in tableManager.npcTable.Npc)
        {
            var data = new NpcCopy();
            data.id = npc.id;
            data.talk = npc.talk;
            data.firstDialog = npc.firstDialog;
            data.startDialog = npc.startDialog;

            if (!string.IsNullOrEmpty(npc.dialogKey))
            {
                var dialogKeySplit = npc.dialogKey.Split(';');
                foreach (var dialogKey in dialogKeySplit)
                {
                    data.dialogKey.Add(dialogKey);
                }
            }

            if (!string.IsNullOrEmpty(npc.questItemId))
            {
                var questItemSplit = npc.questItemId.Split(';');
                foreach (var questItem in questItemSplit)
                {
                    data.questItemId.Add(questItem);
                }
            }

            if (!string.IsNullOrEmpty(npc.questItemCount))
            {
                var itemCountSplit = npc.questItemCount.Split(';');
                foreach (var itemCount in itemCountSplit)
                {
                    data.questItemCount.Add(int.Parse(itemCount));
                }
            }

            data.questClearChoice = npc.questClearChoice;
            npcCopyList.Add(data);
        }
        
        foreach (var dialogueChoice in tableManager.dialogueChoiceTable.DialogueChoice)
        {
            var data = new DialogueChoiceCopy();
            data.id = dialogueChoice.id;
            data.npc = dialogueChoice.npc;
            data.talk = dialogueChoice.talk;

            if (!string.IsNullOrEmpty(dialogueChoice.checkKey))
            {
                var checkKeySplit = dialogueChoice.checkKey.Split(';');
                foreach (var checkKey in checkKeySplit)
                {
                    data.checkKey.Add(checkKey);
                }
            }

            if (!string.IsNullOrEmpty(dialogueChoice.checkKeyValue))
            {
                var checkKeyValueSplit = dialogueChoice.checkKeyValue.Split(';');
                foreach (var checkKeyValue in checkKeyValueSplit)
                {
                    data.checkKeyValue.Add(bool.Parse(checkKeyValue));
                }
            }
            
            dialogueChoiceCopyList.Add(data);
        }
        
        foreach (var grenade in tableManager.grenadeTable.Grenade)
        {
            var data = new GrenadeCopy();
            data.id = grenade.id;
            data.minForce = grenade.minForce;
            data.maxForce = grenade.maxForce;
            data.spinGrenade = grenade.spinGrenade;
            data.dirObject = grenade.dirObject;
            data.hitTag = grenade.hitTag;
            data.spawnObject = grenade.spawnObject;

            grenadeCopyList.Add(data);
        }

        foreach (var passive in tableManager.passiveTable.Passive)
        {
            var data = new PassiveCopy();
            data.id = passive.id;
            data.valueResource = passive.valueResource;
            data.resourceStat = passive.resourceStat;
            data.resourceValue = passive.resourceValue;
            data.resourceUnit = passive.resourceUnit;
            data.getBuffResource = passive.getBuffResource;
            data.buffTime = passive.buffTime;
            data.buffId = passive.buffId;
            data.buffValue = passive.buffValue;
            data.buffUnit = passive.buffUnit;
            data.penaltyValue = passive.penaltyValue;
            data.passiveName = passive.passiveName;
            data.passiveExplain = passive.passiveExplain;
            passiveCopyList.Add(data);
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
            player.ApplyPassive();
        }
    }

    public void MovePlayer()
    {
        ControlStart = true;
        CurPlayer.Immortal = false;
        curPlayer.Dodge = false;
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
    
    // 고정 로테이션: Berserker → Gunner → Fighter → Berserker → ...
    private static readonly string[] PlayerRotation =
    {
        ConstValues.Berserker,
        ConstValues.Gunner,
        ConstValues.Fighter,
    };

    public void AddPlayer(string player)
    {
        // 이미 추가된 캐릭터면 무시
        if (saveData.playerList.Contains(player))
            return;

        // 빈 리스트면 그대로 추가
        if (saveData.playerList.Count == 0)
        {
            saveData.playerList.Add(player);
            return;
        }

        // 첫 번째 요소를 앵커로 삼아 로테이션 상의 상대 위치 기준으로 정렬한다.
        // 예) [Gunner, Berserker]에 Fighter 추가 시 → [Gunner, Fighter, Berserker]
        int anchorIdx = Array.IndexOf(PlayerRotation, saveData.playerList[0]);
        int cycleLen = PlayerRotation.Length;

        saveData.playerList.Add(player);
        saveData.playerList.Sort((a, b) =>
        {
            int ra = (Array.IndexOf(PlayerRotation, a) - anchorIdx + cycleLen) % cycleLen;
            int rb = (Array.IndexOf(PlayerRotation, b) - anchorIdx + cycleLen) % cycleLen;
            return ra.CompareTo(rb);
        });
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
            player.ApplyPassive();
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

    public Monster ActiveAndHideMonster(string id, Vector3 monsterVector, bool isExplosion = true, EMonsterType monsterType = EMonsterType.Normal)
    {
        var monster = SpawnToPoolInstantiate(id, objectPool, monsterVector).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.MonsterType = monsterType;
        monster.gameObject.SetActive(false);
        monsterList.Add(monster);
        return monster;
    }
    public Monster ActiveAndHideMonster(string id, Transform monsterTransform, Vector3 monsterVector, bool isActive, bool isExplosion = true, EMonsterType monsterType = EMonsterType.Normal)
    {
        var monster = SpawnToMonster(id, monsterTransform, monsterVector, isActive).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.MonsterType = monsterType;
        monster.gameObject.SetActive(false);
        monsterList.Add(monster);
        return monster;
    }

    public void SetMonster(Monster monster, EMonsterType monsterType, bool isExplosion)
    {
        monster.MonsterType = monsterType;
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
    
    public void InputDataTrap(string trapId, Collider2D trapObject)
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
        var go = SpawnToPool(type.ToString(), popupPool, objVector, true);
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
        if (!fadeSystem)
        {
            fadeSystem = SpawnToHighestPool(ConstValues.FadeUI, Vector3.zero).GetComponent<FadeSystem>();
            fadeSystem.transform.localPosition = Vector3.zero;
        }
        fadeSystem.gameObject.SetActive(false);
    }

    public async UniTask Fading(float start, float end, float duration, bool delete, Color color, bool ignoreTime = true)
    {
        fadeSystem.ColorInput(color);
        fadeSystem.gameObject.SetActive(true);
        fadeSystem.SetParameter(start, end, duration, delete);
        if(await fadeSystem.Fade(ignoreTime).SuppressCancellationThrow())
            return;
    }

    public void FadeObjectActiveImmediately(bool active)
    {
        if (FadeSystem == null)
            return;
        
        fadeSystem.ColorInput(ConstValues.BlackColor);
        fadeSystem.gameObject.SetActive(active);
    }
    
    public void PoolDisActive()
    {
        inGame = false;
        
        players.Clear();
        objectList.Clear();
        monsterList.Clear();
        
        foreach (Transform child in objectPool)
            Destroy(child.gameObject);

        foreach (Transform child in uiObjectPool)
            Destroy(child.gameObject);

        foreach (Transform child in uiPool)
            Destroy(child.gameObject);
        
        foreach (Transform child in popupPool)
            Destroy(child.gameObject);

        // foreach (Transform child in highestPool)
        //     Destroy(child.gameObject);
    }
    
    // private void CashingSpeechFrame()
    // {
    //     int count = 3;
    //     for (int i = 0; i < count; i++)
    //     {
    //         speechFrame1.Add(GetSpeechFrame(ConstValues.SpeechFrame1));
    //         speechFrame2.Add(GetSpeechFrame(ConstValues.SpeechFrame2));
    //     }
    //     for (int i = 0; i < count; i++)
    //     {
    //         speechFrame1[i].gameObject.SetActive(false);
    //         speechFrame2[i].gameObject.SetActive(false); 
    //     }
    //     speechFrameStrong = GetSpeechFrame(ConstValues.SpeechFrameStrong);
    //     speechFrameStrong.gameObject.SetActive(false);
    //     
    //     speechFrameTitle = GetSpeechFrame(ConstValues.SpeechFrameTitle);
    //     speechFrameTitle.gameObject.SetActive(false);
    // }

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
        ResetParticles(go);
        return go;
    }

    private GameObject SpawnToPool(string id, Transform pool, Vector3 objVector, bool isHightest = false)
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
        ResetParticles(go);

        if(isHightest)
            go.transform.SetAsLastSibling();

        return go;
    }
    
    // 풀링 재사용 시 파티클 상태/이전 위치 추적값을 새 위치 기준으로 리셋해 텔레포트 잔상 제거
    private void ResetParticles(GameObject go)
    {
        var particles = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles)
        {
            particle.Clear(true);
            particle.Simulate(0f, true, true);
            particle.Play(true);
        }
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
        RefreshPlayerResource();
                    
        var bossHpInterface = uiInterface.BossHpView.ConvertTo<IUIBossHpView>();
        var bossHpPresenter = new UIBossHpPresenter(bossHpInterface);
        uiInterface.SetBossHpPresenter(bossHpPresenter);
        bossHpPresenter.HideHp();
            
        var placeNameInterface = uiInterface.PlaceNameView.ConvertTo<IUIPlaceNameView>();
        var placeNameModel = new UIPlaceNameModel();
        var placeNamePresenter = new UIPlaceNamePresenter(placeNameInterface, placeNameModel);
        uiInterface.SetPlaceNamePresenter(placeNamePresenter);
        placeNamePresenter.HideImmediate();
        
        var objectInfoInterface = uiInterface.ObjectInfoView.ConvertTo<IUIObjectInfoView>();
        var objectInfoModel = new UIObjectInfoModel();
        var objectInfoPresenter = new UIObjectInfoPresenter(objectInfoInterface, objectInfoModel);
        uiInterface.SetObjectInfoPresenter(objectInfoPresenter);
        objectInfoPresenter.HideImmediate();
        
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
        popupWarning.SetWarningPresenter(warningPresenter);
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
        facePresenter.SetChangeFace();
    }
    
    public void RefreshPlayerHp()
    {
        var hpInterface = uiInterface.HpView.ConvertTo<IUIHpView>();
        var hpModel = new UIHpModel()
        {
            player = CurPlayer
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

    // 캐릭 변경할때, 때릴때
    public void RefreshPlayerResource()
    {
        var hpInterface = uiInterface.HpView.ConvertTo<IUIHpView>();
        var hpModel = new UIHpModel()
        {
            player = CurPlayer
        };
        var hpPresenter = new UIHpPresenter(hpInterface, hpModel);
        uiInterface.SetHpPresenter(hpPresenter);
        hpPresenter.SetResource();
        hpPresenter.SetResourceText();
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
    
    public void RefreshPlayerIgnorePlatform()
    {
        foreach (var player in players)
            player.ClearIgnorePlatform();
    }

    public void RefreshPlaceName()
    {
        if (RoomManager.Instance == null || RoomManager.Instance.CurrentRoom == null)
            return;

        var placeNameInterface = uiInterface.PlaceNameView.ConvertTo<IUIPlaceNameView>();
        var placeNameModel = new UIPlaceNameModel()
        {
            placeName = RoomManager.Instance.CurrentRoom.Place,
        };
        var placeNamePresenter = new UIPlaceNamePresenter(placeNameInterface, placeNameModel);
        uiInterface.SetPlaceNamePresenter(placeNamePresenter);
        placeNamePresenter.SetPlaceText();
    }
    
    public void ProductObjectInfo(string id, string objectName, int count)
    {
        var getObjectInterface = uiInterface.ObjectInfoView.ConvertTo<IUIObjectInfoView>();
        var getObjectModel = new UIObjectInfoModel()
        {
            id = id,
            objectName = objectName,
            count = count,
        };
        var objectInfoPresenter = new UIObjectInfoPresenter(getObjectInterface, getObjectModel);
        uiInterface.SetObjectInfoPresenter(objectInfoPresenter);
        objectInfoPresenter.SetObjectText();
    }

    public void HidePlaceName()
    {
        uiInterface.PlaceNamePresenter?.HideImmediate();
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
        RefreshPlayerResource();
        curPlayer.ChangeApplyPassive();

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
        SpeechFrame speechFrame;
        if(frameName == ConstValues.SpeechFrameTitle)
            speechFrame = SpawnToHighestPool(frameName, Vector2.zero).GetComponent<SpeechFrame>();
        else
            speechFrame = SpawnToUIObjectPool(frameName, Vector2.zero).GetComponent<SpeechFrame>();

        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == frameName);
        if (objectData == null)
            return speechFrame;
        
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
        
        return speechFrame;
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

    public void GetSkillProduct(string id, Action<string, string, int> customAction)
    {
        CurPlayer.SpawnObject(ConstValues.GetSkillExplosion, CurPlayer.CenterPos.position);
        var skillName = GetSkillName(id);
        customAction.Invoke(id, skillName, 1);
    }
    
    public void GetAttributeProduct(int count, Action<int> customAction)
    {
        CurPlayer.SpawnObject(ConstValues.GetAttributeEffect, CurPlayer.CenterPos.position);
        customAction.Invoke(count);
    }

    public void GetGoldProduct(int count, Vector2 boxPos, Action<int> customAction)
    {
        CurPlayer.SpawnObject(ConstValues.BangEffect, boxPos);
        customAction.Invoke(count);
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

    public void SpawnSelect(string message, Sprite goodsSprite, int cost, Action yesAction, Action noAction, bool yes = true)
    {
        var uiBase = SpawnToPopupPool(eUIType.Popup_Select, Vector3.zero).GetComponent<UIBase>();
        
        if (uiBase is Popup_Select popupSelect)
        {
            var common = new PopupCommonActions
            {
                PlayMoveSound   = () => SoundManager.Instance.PlaySound(ConstValues.Jump1,         true),
                PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2,  true),
                PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton,   true),
            };
            var selectModel = new PopupSelectModel()
            {
                yes = yes,
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
                escAction = ()=>
                {
                    //uiBase.ReductionClose(false, false);
                    uiBase.Close();
                    noAction();
                },
                commonActions = common
            };
            
            var selectInterface = popupSelect.SelectView.ConvertTo<IPopupSelectView>();
            var selectPresenter = new PopupSelectPresenter(selectInterface, selectModel);
            popupSelect.SetSelectPresenter(selectPresenter);
            selectPresenter.Expansion(() =>
            {
                uiBase.ExpansionOpen(false, false).Forget();
            });
            selectPresenter.SetModel();
            selectPresenter.SetAction();
        }
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
        var attributeData = skillAttributeCopyList.FindAll(x => x.id == attributeId);

        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill == null)
                continue;

            var isLock = saveData.lockAttributeList.Find(x => x.id == attributeId).isLock;
            if (isLock)
            {
                SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
                await SpawnWarningPopup(GetTalk(30213));
                return;
            }

            var targetAttribute = skill.attributeList.Contains(attributeId);
            if (!targetAttribute)
            {
                if (playerInfo.attributePoint < attributeData[0].cost)
                {
                    SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
                    await SpawnWarningPopup(GetTalk(30202));
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
    
    public void SellAttribute(string skillId, string attributeId, Vector3 effectPos)
    {
        var attributeData = skillAttributeCopyList.FindAll(x => x.id == attributeId);

        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill == null)
                continue;
            
            var targetAttribute = skill.attributeList.Contains(attributeId);
            if (!targetAttribute)
                continue;
            
            var attributeList = attributeData.FindAll(x => x.skill == skillId);
            var attribute = attributeList.Find(x => x.id == attributeId);
            playerInfo.attributePoint += attribute.cost;
            skill.attributeList.Remove(attributeId);
            // 내리는 연출 넣기
            SpawnHighestObject(ConstValues.AttributeDownEffect, effectPos);
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
    
    // 해당 스킬의 Id찾기(내가 해당 특성을 가지고 있어야 함)
    public List<string> GetAttributePassive(string id)
    {
        List<string> idList = new List<string>();
        string[] idSplit = id.Split('_');
        
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        string targetId = id;
        if (idSplit.Length > 2)
            targetId = $"{idSplit[0]}_{idSplit[1]}_{idSplit[2]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인하고, 그 데이터는 파생기를 포함하여 효과를 적용함
        var attributeData = skillAttributeCopyList.FindAll(x => x.targetObject == targetId);
        if (attributeData.Count > 0)
        {
            foreach (var attribute in attributeData)
            {
                if (attribute.passiveId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                foreach (var passive in attribute.passiveId)
                {
                    idList.Add(passive);
                }
            }
        }
        
        // 파생기도 효과를 적용받음
        attributeData = skillAttributeCopyList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
        foreach (var attribute in attributeData)
        {
            if (attribute.passiveId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                continue;
                
            foreach (var passive in attribute.passiveId)
            {
                idList.Add(passive);
            }
        }
        return idList;
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
        var attributeData = skillAttributeCopyList.FindAll(x => x.targetObject == targetId);
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
        attributeData = skillAttributeCopyList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
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
        var attributeData = skillAttributeCopyList.FindAll(x => x.targetObject == targetId);
        
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
        attributeData = skillAttributeCopyList.FindAll(x => x.skill == skillId && string.IsNullOrWhiteSpace(x.targetObject));
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
            var attributeData = skillAttributeCopyList.FindAll(x => x.skill == skillId);
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
    
    // 해당 스킬의 디버프 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeBuffInfo> GetAttributeDeBuff(string id)
    {
        var buffList = new List<SkillAttributeBuffInfo>();
        string[] idSplit = id.Split('_');
        if (idSplit.Length > 1)
        {
            // 파생기도 해당 효과를 적용받음
            string skillId = $"{idSplit[0]}_{idSplit[1]}";
            var attributeData = skillAttributeCopyList.FindAll(x => x.skill == skillId);
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
        foreach (var player in players)
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
    public async UniTask NpcDialogue(string choice, Npc[] npc, NpcInfo npcInfo, Action onEndEvent = null)
    {
        bool handedOffToRoom = false;
        var talkDataList = TableManager.Instance.dialogueTable.Dialogue.FindAll(x => x.choiceGroupId == choice && IsDialogKeyMatched(x.checkKey, x.checkKeyValue, npcInfo));

        foreach (var talkData in talkDataList)
        {
            var speechFrame = GetSpeechFrame(ConstValues.SpeechFrame1);
            switch (talkData.speechFrame)
            {
                case ConstValues.SpeechFrame2:
                    speechFrame = GetSpeechFrame(ConstValues.SpeechFrame2);
                    break;
                case ConstValues.SpeechFrame3:
                    speechFrame = GetSpeechFrame(ConstValues.SpeechFrame3);
                    break;
            }

            var speechCharacter = GetCharacter(talkData.speaker, npc);
            List<Character> poseCharacterList = new List<Character>();

            var poseCharacters = talkData.poseCharacter.Split(';');
            foreach (var poseCharacter in poseCharacters)
            {
                if (!string.IsNullOrWhiteSpace(poseCharacter))
                {
                    poseCharacterList.Add(GetCharacter(poseCharacter, npc));
                }
            }
            
            List<string> speechPoseList = new List<string>();
            var speechPoses = talkData.speechPose.Split(';');
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

            if(!string.IsNullOrWhiteSpace(talkData.sound))
                SoundManager.Instance.PlaySound(talkData.sound);
            
            var cameraShakeArray = talkData.cameraShake.Split(';');
            var cameraShake = new Vector2(float.Parse(cameraShakeArray[0]), float.Parse(cameraShakeArray[1]));
            if(cameraShake != Vector2.zero)
                CameraShake(cameraShake.x, cameraShake.y, talkData.shakeTime);
            
            SpawnSpeechFrame(speechFrame, speechPos.position, GetTalk(talkData.talk));
            await NextDialog(speechFrame);
            
            string endEvent = talkData.endEvent;
            string eventReward = talkData.reward;
            if (!string.IsNullOrWhiteSpace(endEvent))
            {
                if (PlayEndEvent(npcInfo, endEvent, eventReward, talkData.checkKey))
                    handedOffToRoom = true;
                onEndEvent?.Invoke();
            }

            if (talkData.isEnd)
                break;
        }

        // Room 연출로 위임된 경우 컨트롤 복귀와 finishAction은 Room 측에서 책임짐
        if (handedOffToRoom)
            return;

        ControlStart = true;
        foreach (var person in npc)
            person.IsPlayerTouch = false;
        curPlayer.MyRigidbody.WakeUp();
    }
    
    // Room 연출로 위임됐으면 true (호출자는 ControlStart/finishAction을 스킵해야 함)
    private bool PlayEndEvent(NpcInfo npcInfo, string eventKey, string reward, string dialogKeyId)
    {
        var targetKey = string.IsNullOrWhiteSpace(dialogKeyId) ? null : npcInfo?.dialogKey?.Find(k => k.id == dialogKeyId);

        switch (eventKey)
        {
            case ConstValues.GetSkill:
                if (targetKey != null)
                    targetKey.isUse = true;
                AddNewSkill(reward);
                GetSkillProduct(reward, ProductObjectInfo);
                SaveGame();
                return false;
            case ConstValues.QuestClear:
                if (targetKey != null)
                    targetKey.isUse = true;
                ConsumeQuestItems(npcInfo.id);
                return false;
            default:
                // GameManager가 처리하지 않는 endEvent는 현재 Room에 위임
                // (BossEvent 등 Room 연출은 Room.PlayRoomEndEvent에서 분기)
                if (targetKey != null)
                    targetKey.isUse = true;
                RoomManager.Instance.CurrentRoom.PlayRoomEndEvent(eventKey);
                return true;
        }
    }

    // NpcInfo의 dialogKey 리스트에서 checkKey 매칭 여부 판단
    private bool IsDialogKeyMatched(string checkKey, bool checkKeyValue, NpcInfo npcInfo)
    {
        if (string.IsNullOrWhiteSpace(checkKey))
            return true;
        if (npcInfo?.dialogKey == null)
            return false;
        
        var key = npcInfo.dialogKey.Find(k => k.id == checkKey);
        return key != null && key.isUse == checkKeyValue;
    }

    private void ConsumeQuestItems(string npcId)
    {
        var data = npcCopyList.Find(x => x.id == npcId);
        if (data == null || data.questItemId == null)
            return;

        for (int i = 0; i < data.questItemId.Count; i++)
        {
            var itemId = data.questItemId[i];
            var consumeCount = i < data.questItemCount.Count ? data.questItemCount[i] : 0;
            var owned = ItemList.Find(x => x.id == itemId);
            if (owned == null)
                continue;

            owned.count -= consumeCount;
            if (owned.count <= 0)
                ItemList.Remove(owned);
        }
    }

    public async UniTask NpcFirstTalk(string startDialog, Transform speechPos)
    {
        var firstTalk = TableManager.Instance.dialogueTable.Dialogue.Find(x => x.id == startDialog);
        var speechFrame = GetSpeechFrame(ConstValues.SpeechFrame1);
        switch (firstTalk.speechFrame)
        {
            case ConstValues.SpeechFrame2:
                speechFrame = GetSpeechFrame(ConstValues.SpeechFrame2);
                break;
            case ConstValues.SpeechFrame3:
                speechFrame = GetSpeechFrame(ConstValues.SpeechFrame3);
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

    public async UniTask DialogueMove(float xPos)
    {
        // 항상 광전사가 맨 앞에 있어야 함
        var berserker = GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        var fighter = GetPlayer(ConstValues.Fighter).GetComponent<Player_Fighter>();

        var berserkerPos = curPlayer.transform.position;
        var gunnerPos = new Vector2(berserkerPos.x + xPos, berserkerPos.y);
        var fighterPos = new Vector2(gunnerPos.x + xPos, berserkerPos.y);

        if (saveData.playerList.Contains(ConstValues.Berserker))
        {
            berserker.gameObject.SetActive(true);
            berserker.transform.position = berserkerPos;
            if(curPlayer.transform.localScale.x >= 0)
                berserker.Flip(1);
            else
                berserker.Flip(-1);
        }

        if (saveData.playerList.Contains(ConstValues.Gunner))
        {
            gunner.gameObject.SetActive(true);
            gunner.transform.position = berserkerPos;
            if(xPos >= 0)
                gunner.Flip(1);
            else
                gunner.Flip(-1);
        }

        if (saveData.playerList.Contains(ConstValues.Fighter))
        {
            fighter.gameObject.SetActive(true);
            fighter.transform.position = berserkerPos;
            if(xPos >= 0)
                fighter.Flip(1);
            else
                fighter.Flip(-1);
        }

        if (gunner.gameObject.activeSelf)
        {
            int finishDir = 1;
            if (xPos < 0)
                finishDir = -1;

            if (await gunner.EpisodeMove(gunnerPos, gunner.BasicStat.moveSpeed, finishDir).SuppressCancellationThrow())
                return;
        }

        if (fighter.gameObject.activeSelf)
        {
            int finishDir = 1;
            if (xPos < 0)
                finishDir = -1;

            if (await fighter.EpisodeMove(fighterPos, fighter.BasicStat.moveSpeed, finishDir).SuppressCancellationThrow())
                return;
        }
    }
    
    public void DialogueEnd()
    {
        var berserker = GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        var fighter = GetPlayer(ConstValues.Fighter).GetComponent<Player_Fighter>();

        if (berserker.gameObject.activeSelf)
        {
            if (curPlayer != berserker)
            { 
                berserker.SpawnObject(ConstValues.BangEffect, berserker.CenterPos.position);
                berserker.gameObject.SetActive(false);
            }
        }
        if (gunner.gameObject.activeSelf)
        {
            if (curPlayer != gunner)
            { 
                gunner.SpawnObject(ConstValues.BangEffect, gunner.CenterPos.position);
                gunner.gameObject.SetActive(false);
            }
        }
        if (fighter.gameObject.activeSelf)
        {
            if (curPlayer != fighter)
            { 
                fighter.SpawnObject(ConstValues.BangEffect, fighter.CenterPos.position);
                fighter.gameObject.SetActive(false);
            }
        }
    }
    
    public void GetItem(string id, int count)
    {
        var itemInfo = new HaveItemInfo()
        {
            id = id,
            count = count,
        };
        // 해당 아이템을 포함하고 있지 않을 때만 추가
        if(!ItemList.Exists(x => x.id == id))
            ItemList.Add(itemInfo);
    }

    public string GetPlaceName(string place)
    {
        switch (place)
        {
            case ConstValues.SunHill:
                return GetTalk(130000);

            case ConstValues.BaseCamp:
                return GetTalk(130001);

            case ConstValues.Forest:
                return GetTalk(130002);

            case ConstValues.Mine:
                return GetTalk(130003);
            
            default:
                return "Non";
        }
    }

    public async void PlayerRespawn()
    {
        curPlayer.Immortal = true;
        ControlStart = false;

        float delay1 = 0.3f;
        float delay2 = 0.1f;

        InitProductCancellation();
        if(await NormalDelay(delay1, productCancellation).SuppressCancellationThrow())
            return;
        
        curPlayer.SpawnObject(ConstValues.BangEffect, curPlayer.CenterPos.position);
        curPlayer.gameObject.SetActive(false);
        if(await NormalDelay(delay1, productCancellation).SuppressCancellationThrow())
            return;
        
        // 이동기능 추가
        curPlayer.transform.position = curPlayer.GetLastMarkerPosition();
        if(await NormalDelay(delay1, productCancellation).SuppressCancellationThrow())
            return;
        
        curPlayer.SpawnObject(ConstValues.BangEffect, curPlayer.CenterPos.position);
        curPlayer.gameObject.SetActive(true);
        
        if(await NormalDelay(delay2, productCancellation).SuppressCancellationThrow())
            return;
        
        curPlayer.Immortal = false;
        curPlayer.Dodge = false;
        ControlStart = true;
    }
}
