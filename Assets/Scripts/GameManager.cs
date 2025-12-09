using System;
using System.Collections;
using System.Collections.Generic;
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
    public bool isGet;
    public List<SkillAttribute> attributeList = new List<SkillAttribute>();
}
[Serializable]
public class SkillAttribute
{
    public string attributeId;
    public int level;
}
[Serializable]
public class SkillSetting
{
    public int attributePoint;
    public List<Skill>skillList;
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
        {
            if (berserkerSkill.isGet)
                return true;
            
            return false;
        }

        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
        {
            if (gunnerSkill.isGet)
                return true;
            
            return false;
        }

        return false;
    }
    public List<SkillAttribute> GetSkillAttribute(string id)
    {
        var berserkerSkillList = berserkerSkillSetting.skillList.Find(x => x.skillId == id).attributeList;
        if (berserkerSkillList != null)
            return berserkerSkillList;
        
        var gunnerSkillList = gunnerSkillSetting.skillList.Find(x => x.skillId == id).attributeList;
        if (gunnerSkillList != null)
            return gunnerSkillList;

        Debug.Log("검색되는 특성 없음!");
        return null;
    }
    public bool IsHaveAttribute(string skillId, string attributeId)
    {
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (berserkerSkill != null)
            return berserkerSkill.attributeList.Find(x => x.attributeId == attributeId) != null;
        
        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
            return gunnerSkill.attributeList.Find(x => x.attributeId == attributeId) != null;
        
        Debug.Log("해당 특성 자체가 없음!");
        return false;
    }
    public int AttributeLv(string skillId, string attributeId)
    {
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (berserkerSkill != null)
        {
            if (berserkerSkill.attributeList.Find(x => x.attributeId == attributeId) == null)
            {
                Debug.Log($"{attributeId}특성을 배우지 않았음");
                return 0;
            }
            return berserkerSkill.attributeList.Find(x => x.attributeId == attributeId).level;
        }
            
        
        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
        {
            if (gunnerSkill.attributeList.Find(x => x.attributeId == attributeId) == null)
            {
                Debug.Log($"{attributeId}특성을 배우지 않았음");
                return 0;
            }
            return gunnerSkill.attributeList.Find(x => x.attributeId == attributeId).level;
        }
        Debug.Log("이 특성을 활성화하는 스킬 자체를 배우지 않았음");
        return 0;
    }

    public async void AttributeLvUp(string skillId, string attributeId)
    {
        var attributeData = TableManager.Instance.skillAttributeTable.SkillAttribute.FindAll(x => x.id == attributeId);
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);

        if (berserkerSkill != null)
        {
            var targetAttribute = berserkerSkill.attributeList.Find(x => x.attributeId == attributeId);
            
            if (targetAttribute == null)
            {
                if (berserkerSkillSetting.attributePoint < attributeData[0].cost)
                {
                    await GameManager.Instance.SpawnWarningPopup("특성 포인트가 부족합니다.");
                }
                else
                {
                    var newAttribute = new SkillAttribute();
                    newAttribute.attributeId = attributeId;
                    newAttribute.level = 1;
                    berserkerSkill.attributeList.Add(newAttribute);
                }
            }
            else
            {
                if(targetAttribute.level < attributeData[^1].level)
                {
                    var attributeList = attributeData.FindAll(x => x.id == skillId);
                    var attribute = attributeList.Find(x => x.level == targetAttribute.level + 1);
                    if (berserkerSkillSetting.attributePoint >= attribute.cost)
                    {
                        berserkerSkillSetting.attributePoint -= attribute.cost;
                        targetAttribute.level += 1;

                        string skillJson = JsonUtility.ToJson(GameManager.Instance.PlayerSkill, true);
                        SkillBinding.SaveSkill(skillJson);
                        // 올리는 연출 넣기
                    }
                    else
                    {
                        await GameManager.Instance.SpawnWarningPopup("특성 포인트가 부족합니다.");
                    }
                }
                else
                {
                    await GameManager.Instance.SpawnWarningPopup("특성이 최대 레벨입니다.");
                }
            }
        }
        
        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
        {
            var targetAttribute = gunnerSkill.attributeList.Find(x => x.attributeId == attributeId);
            
            if (targetAttribute == null)
            {
                if (gunnerSkillSetting.attributePoint < attributeData[0].cost)
                {
                    await GameManager.Instance.SpawnWarningPopup("특성 포인트가 부족합니다.");
                }
                else
                {
                    var newAttribute = new SkillAttribute();
                    newAttribute.attributeId = attributeId;
                    newAttribute.level = 1;
                    gunnerSkill.attributeList.Add(newAttribute);
                }
            }
            else
            {
                if(targetAttribute.level < attributeData[^1].level)
                {
                    var attributeList = attributeData.FindAll(x => x.id == skillId);
                    var attribute = attributeList.Find(x => x.level == targetAttribute.level + 1);
                    if (gunnerSkillSetting.attributePoint >= attribute.cost)
                    {
                        gunnerSkillSetting.attributePoint -= attribute.cost;
                        targetAttribute.level += 1;

                        string skillJson = JsonUtility.ToJson(GameManager.Instance.PlayerSkill, true);
                        SkillBinding.SaveSkill(skillJson);
                        // 올리는 연출 넣기
                    }
                    else
                    {
                        await GameManager.Instance.SpawnWarningPopup("특성 포인트가 부족합니다.");
                    }
                }
                else
                {
                    await GameManager.Instance.SpawnWarningPopup("특성이 최대 레벨입니다.");
                }
            }
        }
    }
    
    public async void AttributeLvDown(string skillId, string attributeId)
    {
        var attributeData = TableManager.Instance.skillAttributeTable.SkillAttribute.FindAll(x => x.id == attributeId);
        
        var berserkerSkill = berserkerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (berserkerSkill != null)
        {
            var targetAttribute = berserkerSkill.attributeList.Find(x => x.attributeId == attributeId);

            if (targetAttribute == null)
            {
                await GameManager.Instance.SpawnWarningPopup("특성레벨을 더 이상 내릴 수 없습니다.");
            }
            else
            {
                var attributeList = attributeData.FindAll(x => x.id == skillId);
                var attribute = attributeList.Find(x => x.level == targetAttribute.level);
                berserkerSkillSetting.attributePoint += attribute.cost;
                targetAttribute.level -= 1;
                if(targetAttribute.level == 0)
                    berserkerSkill.attributeList.Remove(targetAttribute);
                
                string skillJson = JsonUtility.ToJson(GameManager.Instance.PlayerSkill, true);
                SkillBinding.SaveSkill(skillJson);
                // 내리는 연출 넣기
            }
        }
        
        var gunnerSkill = gunnerSkillSetting.skillList.Find(x => x.skillId == skillId);
        if (gunnerSkill != null)
        {
            var targetAttribute = gunnerSkill.attributeList.Find(x => x.attributeId == attributeId);

            if (targetAttribute == null)
            {
                await GameManager.Instance.SpawnWarningPopup("특성레벨을 더 이상 내릴 수 없습니다.");
            }
            else
            {
                var attributeList = attributeData.FindAll(x => x.id == skillId);
                var attribute = attributeList.Find(x => x.level == targetAttribute.level);
                gunnerSkillSetting.attributePoint += attribute.cost;
                targetAttribute.level -= 1;
                if(targetAttribute.level == 0)
                    gunnerSkill.attributeList.Remove(targetAttribute);
                
                string skillJson = JsonUtility.ToJson(GameManager.Instance.PlayerSkill, true);
                SkillBinding.SaveSkill(skillJson);
                // 내리는 연출 넣기
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
        await GameManager.Instance.SpawnWarningPopup("특성이 초기화 되었습니다.");
    }
}

public static class SkillBinding
{
    // 저장할 때
    public static void SaveSkill(string skill)
    {
        PlayerPrefs.SetString(ConstValues.PlayerSkill, skill);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static string LoadSkill(string defaultSkill)
    {
        if (PlayerPrefs.HasKey(ConstValues.PlayerSkill))
        {
            Debug.Log($"저장된 스킬 존재");
            return PlayerPrefs.GetString(ConstValues.PlayerSkill);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"스킬 최초 생성");
            SaveSkill(defaultSkill);
            return defaultSkill;
        }
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

public static class SkillKeyBinding
{
    // 저장할 때
    public static void SaveKey(string skillKey)
    {
        PlayerPrefs.SetString(ConstValues.PlayerSkillKey, skillKey);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static string LoadSkillKey(string defaultSkillKey)
    {
        if (PlayerPrefs.HasKey(ConstValues.PlayerSkillKey))
        {
            Debug.Log($"저장된 스킬 키 세팅 존재");
            return PlayerPrefs.GetString(ConstValues.PlayerSkillKey);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"스킬 키 세팅 최초 생성");
            SaveKey(defaultSkillKey);
            return defaultSkillKey;
        }
    }
}

public static class StageBinding
{
    // 저장할 때
    public static void SaveStage(int num)
    {
        PlayerPrefs.SetInt(ConstValues.Stage, num);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static int LoadStage()
    {
        if (PlayerPrefs.HasKey(ConstValues.Stage))
        {
            Debug.Log($"저장된 스테이지가 존재{PlayerPrefs.GetInt(ConstValues.Stage)}");
            return PlayerPrefs.GetInt(ConstValues.Stage);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"스테이지 최초 저장");
            SaveStage(0);
            return 0;
        }
    }
}

public static class GoldBinding
{
    // 저장할 때
    public static void SaveGold(int count)
    {
        PlayerPrefs.SetInt(ConstValues.Gold, count);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static void LoadGold()
    {
        if (PlayerPrefs.HasKey(ConstValues.Gold))
        {
            Debug.Log($"저장된 골드가 존재{PlayerPrefs.GetInt(ConstValues.Gold)}");
            
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"골드 최초 저장");
            SaveGold(0);
        }
    }
}

public static class SavePointBinding
{
    // 저장할 때
    public static void SaveSavePoint(string savePointName)
    {
        PlayerPrefs.SetString(ConstValues.SavePoint, savePointName);
        PlayerPrefs.Save();
    }
  
    // 불러올 때
    public static string LoadSavePoint()
    {
        if (PlayerPrefs.HasKey(ConstValues.SavePoint))
        {
            Debug.Log($"저장된 세이브 포인트가 존재{PlayerPrefs.GetString(ConstValues.SavePoint)}");
            return PlayerPrefs.GetString(ConstValues.SavePoint);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"세이브 포인트 없음, 1번 맵으로 생성");
            return default;
        }
    }
}

public static class EpisodeBinding
{
    // 저장할 때
    public static void SaveEpisode(string episodeName, string episodeClass)
    {
        PlayerPrefs.SetString(episodeName, episodeClass);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static string LoadEpisode(string episodeName, string episodeClass)
    {
        if (PlayerPrefs.HasKey(episodeName))
        {
            Debug.Log($"저장된 에피소드 존재");
            return PlayerPrefs.GetString(episodeName);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"{episodeName}에피소드 최초 생성");
            SaveEpisode(episodeName, episodeClass);
            return episodeClass;
        }
    }
}

public static class RoomBinding
{
    // 저장할 때
    public static void SaveRoom(string roomName, string roomClass)
    {
        PlayerPrefs.SetString(roomName, roomClass);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static string LoadRoom(string roomName, string roomClass)
    {
        if (PlayerPrefs.HasKey(roomName))
        {
            Debug.Log($"저장된 룸 정보 존재");
            return PlayerPrefs.GetString(roomName);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"{roomName}룸 최초 생성");
            SaveRoom(roomName, roomClass);
            return roomClass;
        }
    }
}

public static class FirstGetSkillBinding
{
    // 저장할 때
    public static void SaveFirstGetSkill(int alreadyGet)
    {
        PlayerPrefs.SetInt(ConstValues.FirstGetSkill, alreadyGet);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static int LoadFirstGetSkill()
    {
        if (PlayerPrefs.HasKey(ConstValues.FirstGetSkill))
        {
            Debug.Log($"최초로 스킬을 획득한적이 있음");
            return PlayerPrefs.GetInt(ConstValues.FirstGetSkill);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"아무 스킬도 획득한 적이 없음");
            SaveFirstGetSkill(0);
            return 0;
        }
    }
}

public static class FirstGetAttributeBinding
{
    // 저장할 때
    public static void SaveFirstGetAttribute(int alreadyGet)
    {
        PlayerPrefs.SetInt(ConstValues.FirstGetAttribute, alreadyGet);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static int LoadFirstGetAttribute()
    {
        if (PlayerPrefs.HasKey(ConstValues.FirstGetAttribute))
        {
            Debug.Log($"최초로 특성을 획득한적이 있음");
            return PlayerPrefs.GetInt(ConstValues.FirstGetAttribute);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"아무 특성도 획득한 적이 없음");
            SaveFirstGetAttribute(0);
            return 0;
        }
    }
}

public static class CharacterOrderBinding
{
    // 저장할 때
    public static void SaveFirstCharacter(string first)
    {
        PlayerPrefs.SetString(ConstValues.FirstCharacter, first);
        PlayerPrefs.Save();
    }
    public static void SaveSecondCharacter(string second)
    {
        PlayerPrefs.SetString(ConstValues.SecondCharacter, second);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static string LoadFirstCharacter()
    {
        if (PlayerPrefs.HasKey(ConstValues.FirstCharacter))
        {
            Debug.Log($"첫 번째 캐릭터: {PlayerPrefs.GetString(ConstValues.FirstCharacter)}");
            return PlayerPrefs.GetString(ConstValues.FirstCharacter);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"최초 첫 캐릭터");
            SaveFirstCharacter(ConstValues.Berserker);
            return ConstValues.Berserker;
        }
    }
    
    public static string LoadSecondCharacter()
    {
        if (PlayerPrefs.HasKey(ConstValues.SecondCharacter))
        {
            Debug.Log($"두 번째 캐릭터: {PlayerPrefs.GetString(ConstValues.SecondCharacter)}");
            return PlayerPrefs.GetString(ConstValues.SecondCharacter);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"아직 두 번째 캐릭터 존재 안함");
            SaveSecondCharacter(default);
            return default;
        }
    }
}

public enum ePoolType
{
    None,
    UI,
    Popup,
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
}

public class GameManager : Singleton<GameManager>
{
    public Material defaultMaterial;
    public Material hitMaterial;
    
    public KeyCode escKey;
    public KeyCode tabKey;
    public KeyCode spaceKey;
    public KeyCode attributeKey;
    
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

    // 재화
    private int gold;

    // 최초획득
    private int alreadySkill;
    private int alreadyAttribute;
    
    private string episodeName;
    private string firstPlayer;
    private string secondPlayer;
    private bool controlStart;
    private bool bossProduct;
    private int comboCount;

    // 등록된 스킬 및 키 세팅 목록
    private SettingSkill changeSkill;
    [SerializeField] private SkillCollection playerSkill;
    [SerializeField] private SkillKeyCollection playerSkillKey;

    // 매니저들
    public TableManager tableManager;
    //public UIManager uiManager;
    //public ResourceManager resourceManager;
    
    // 카메라
    private FollowCamera mainCamera;
    [SerializeField] private Transform miniMapCamera;
    [SerializeField] private Canvas uiObjectCanvas;

    private CancellationTokenSource dialogCancellation;
    private CancellationTokenSource fadeCancellation;
    private CancellationTokenSource waitCancellation;

    // 프로퍼티
    public Player CurPlayer
    {
        get => curPlayer;
        set => curPlayer = value;
    }

    public int AlreadySkill
    {
        get => alreadySkill;
        set => alreadySkill = value;
    }
    
    public int AlreadyAttribute
    {
        get => alreadyAttribute;
        set => alreadyAttribute = value;
    }
    
    public int Gold
    {
        get => gold;
        set => gold = value;
    }

    public string EpisodeName
    {
        get => episodeName;
        set => episodeName = value;
    }

    public string FirstPlayer
    {
        get => firstPlayer;
        set => firstPlayer = value;
    }
    
    public string SecondPlayer
    {
        get => secondPlayer;
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

    public int ComboCount
    {
        get => comboCount;
        set => comboCount = value;
    }

    public SkillCollection PlayerSkill => playerSkill;
    public SkillKeyCollection PlayerSkillKey => playerSkillKey;

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

    public CancellationTokenSource DialogCancellation => dialogCancellation;
    public CancellationTokenSource FadeCancellation => fadeCancellation;
    public CancellationTokenSource WaitCancellation => waitCancellation;
    
    protected override void Awake()
    {
        base.Awake();
        //QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        InitManager();
        
        DefaultSkillSetting();
        DefaultSkillKeySetting();
        DefaultGoodsSetting();
        LoadPlayerPrefs();

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

    public void GoScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void DefaultGoodsSetting()
    {
        GoldBinding.LoadGold();
    }

    public void DefaultSkillSetting()
    {
        playerSkill.totalAttributePoint = 0;

        SkillSetting berserkerSkillSetting = new SkillSetting();
        berserkerSkillSetting.attributePoint = 0;
        berserkerSkillSetting.skillList = new List<Skill>();
        
        SkillSetting gunnerSkillSetting = new SkillSetting();
        gunnerSkillSetting.attributePoint = 0;
        gunnerSkillSetting.skillList = new List<Skill>();
        
        foreach (var skill in TableManager.Instance.skillTable.Skill)
        {
            if (skill.caster == ConstValues.Berserker && skill.type != ConstValues.Dash)
            {
                Skill berserkerSkill = new Skill();
                berserkerSkill.skillId = skill.id;
                berserkerSkill.isGet = false;
                berserkerSkill.attributeList = new List<SkillAttribute>();
                berserkerSkillSetting.skillList.Add(berserkerSkill);
            }

            if (skill.caster == ConstValues.Gunner && skill.type != ConstValues.Dash)
            {
                Skill gunnerSkill = new Skill();
                gunnerSkill.skillId = skill.id;
                gunnerSkill.isGet = false;
                gunnerSkill.attributeList = new List<SkillAttribute>();
                gunnerSkillSetting.skillList.Add(gunnerSkill);
            }
        }
        playerSkill.berserkerSkillSetting = berserkerSkillSetting;
        playerSkill.gunnerSkillSetting = gunnerSkillSetting;
        
        // json화
        string json = JsonUtility.ToJson(playerSkill, true);
        var loadJson = SkillBinding.LoadSkill(json);
        
        var loadedSkillCollection = JsonUtility.FromJson<SkillCollection>(loadJson);
        playerSkill = loadedSkillCollection;
    }
    
    public void DefaultSkillKeySetting()
    {
        escKey = KeyCode.Escape;
        tabKey = KeyCode.Tab;
        spaceKey = KeyCode.Space;
        attributeKey = KeyCode.I;
        
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

        InitSkillKey();
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
    private void InitSkillKey()
    {
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
        playerSkillKey.berserkerSkillKeyList = berserkerSkillKeyList;
        
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
        playerSkillKey.gunnerSkillKeyList = gunnerSkillKeyList;
        
        // json화
        string json = JsonUtility.ToJson(playerSkillKey, true);
        var loadJson = SkillKeyBinding.LoadSkillKey(json);
        
        var loadedSkillKeyCollection = JsonUtility.FromJson<SkillKeyCollection>(loadJson);
        playerSkillKey = loadedSkillKeyCollection;
    }

    public void AddNewSkill(string id)
    {
        // 키 저장
        var skillKeyData = TableManager.Instance.skillTable.Skill.Find(x => x.id == id);
        switch (skillKeyData.caster)
        {
            case ConstValues.Berserker:
            {
                int idx = EmptySkillIdx(playerSkillKey.berserkerSkillKeyList);
                playerSkillKey.berserkerSkillKeyList[idx].skillId = id;
                break;
            }
            case ConstValues.Gunner:
            {
                int idx = EmptySkillIdx(playerSkillKey.gunnerSkillKeyList);
                playerSkillKey.gunnerSkillKeyList[idx].skillId = id;
                break;
            }
        }
        RefreshSkill();
        string skillKeyJson = JsonUtility.ToJson(playerSkillKey, true);
        SkillKeyBinding.SaveKey(skillKeyJson);

        // 스킬 저장
        var berserkerSkillData = playerSkill.berserkerSkillSetting.skillList.Find(x => x.skillId == id);
        if (berserkerSkillData != null)
            berserkerSkillData.isGet = true;
        
        var gunnerSkillData = playerSkill.gunnerSkillSetting.skillList.Find(x => x.skillId == id);
        if (gunnerSkillData != null)
            gunnerSkillData.isGet = true;
        
        string skillJson = JsonUtility.ToJson(playerSkill, true);
        SkillBinding.SaveSkill(skillJson);
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
            var berserkerSkillKey = playerSkillKey.berserkerSkillKeyList.Find(x => x.keyCode == keyCode);
            if (berserkerSkillKey != null)
                berserkerSkillKey.skillId = skillId;
        }
        else if (curPlayer.BasicStat.id == ConstValues.Gunner)
        {
            var gunnerSkillKey = playerSkillKey.gunnerSkillKeyList.Find(x => x.keyCode == keyCode);
            if (gunnerSkillKey != null)
                gunnerSkillKey.skillId = skillId;
        }
        
        // 저장
        string json = JsonUtility.ToJson(playerSkillKey, true);
        SkillKeyBinding.SaveKey(json);
    }
    public List<SettingSkill> GetSettingSkillList()
    {
        List<SkillKey> keyList = null;
        
        if(curPlayer.BasicStat.id == ConstValues.Berserker)
            keyList = playerSkillKey.berserkerSkillKeyList;
        else if(curPlayer.BasicStat.id == ConstValues.Gunner)
            keyList = playerSkillKey.gunnerSkillKeyList;
        
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

    private void LoadPlayerPrefs()
    {
        alreadySkill = FirstGetSkillBinding.LoadFirstGetSkill();
        alreadyAttribute = FirstGetAttributeBinding.LoadFirstGetAttribute();
        
        firstPlayer = CharacterOrderBinding.LoadFirstCharacter();
        secondPlayer = CharacterOrderBinding.LoadSecondCharacter();
    }
    
    private void InitManager() 
    {
        tableManager = TableManager.Instance;
        tableManager.Init();
    }

    private async void OpenUI()
    {
        //await UIManager.Instance.OpenAsync(eUIType.UI_Skill, model);
    }

    // 플레이어
    private void InitPlayer()
    {
        foreach (var player in players)
        {
            player.InitBasicStat();
            player.InitAdditionalStat();
            player.InitSkill();
        }
    }

    public void SetPlayerHp(int hp)
    {
        foreach (var player in players)
            player.BasicStat.hp = hp;
    }
    
    public void SetPlayerOrder(string first, string second)
    {
        firstPlayer = first;
        secondPlayer = second;
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
    
    public void SpawnPlayer(string playerName)
    {
        ActivePlayer(playerName);
    }
    
    public void SpawnPlayer(string playerName, Vector2 playerPos)
    {
        ActivePlayer(playerName);
        curPlayer.transform.position = playerPos;
        curPlayer.transform.localScale = Vector3.one;
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
    public GameObject SpawnToPoolInstantiate(string id, Transform pool, Transform objTransform)
    { 
        var objectName = $"{id} (Clone)";
        GameObject go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
        objectList.Add(go);
        
        go.transform.position = objTransform.transform.position;
        go.SetActive(true);
        return go;
    }
    
    public GameObject SpawnToPoolInstantiate(string id, Transform pool, Vector3 objVector)
    { 
        var objectName = $"{id} (Clone)";
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
            
        var changeInterface = uiInterface.SkillView.ConvertTo<IUISkillView>();
        var skillInterfaces = uiInterface.SkillViews.ConvertAll(v => (IUISkillView)v);
        var skillModel = new UISkillModel
        {
            changeSkill = this.changeSkill,
            settingSkillList = GetSettingSkillList()
        };
        var skillPresenter = new UISkillPresenter(changeInterface, skillInterfaces, skillModel);
        uiInterface.SetSkillPresenter(skillPresenter);
        skillPresenter.SetSkillInfo();
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
            firstCharacter = firstPlayer,
            secondCharacter = secondPlayer,
        };
        var facePresenter = new UICharacterFacePresenter(faceInterface, faceModel);
        uiInterface.SetCharacterFacePresenter(facePresenter);
        
        if(curPlayer.BasicStat.id == firstPlayer)
            facePresenter.SetFirstFace();
        else if(curPlayer.BasicStat.id == secondPlayer)
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
        Gold = PlayerPrefs.GetInt(ConstValues.Gold);

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
            
            addedSkill.name = skill.name;
            addedSkill.explain = skill.explain;
            changeSkill.playerSkill = addedSkill;
            break;
        }
    }

    public string GetSkillName(string id)
    {
        string skillName = default;
        foreach (var skill in TableManager.Instance.skillTable.Skill)
        {
            if (skill.id != id)
                continue;

            skillName = skill.name;
            break;
        }

        return skillName;
    }

    public void CharacterChange(bool changeAttack = true)
    {
        var pastPlayer = curPlayer;
        var pastVelocity = pastPlayer.GetVelocity();
        var changePos = curPlayer.transform.position;
        var nextPlayerId = secondPlayer;
        if (curPlayer.BasicStat.id == secondPlayer)
            nextPlayerId = firstPlayer;
        
        ActivePlayer(nextPlayerId);
        curPlayer = GetPlayer(nextPlayerId);
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
        
        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        var changeInterface = uiInterface.SkillView.ConvertTo<IUISkillView>();
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
            if(list.activeSelf && list.GetComponent<Missile>())
                list.SetActive(false);
        }
    }

    public void InitDialogueCancellation()
    {
        dialogCancellation = new CancellationTokenSource();
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

    public string GetThousandCommaText(int data)
    {
        if (data == 0)
            return 0.ToString();
        
        return $"{data:#,###}";
    }
}
