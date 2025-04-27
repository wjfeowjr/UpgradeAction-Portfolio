using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.U2D;

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

public static class SkillBinding
{
    // 저장할 때
    public static void SaveKey(string skillCollection)
    {
        PlayerPrefs.SetString(ConstValues.PlayerSkill, skillCollection);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static string LoadSkillCollection(string defaultCollection)
    {
        if (PlayerPrefs.HasKey(ConstValues.PlayerSkill))
        {
            Debug.Log($"저장된 스킬 리스트가 존재");
            return PlayerPrefs.GetString(ConstValues.PlayerSkill);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"스킬 리스트 최초 생성");
            SaveKey(defaultCollection);
            return defaultCollection;
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
    UI_Skill,
    
    // 팝업
    Popup_Common,
}

public class GameManager : Singleton<GameManager>
{
    public Material defaultMaterial;
    public Material hitMaterial;
    
    public KeyCode leftMoveKey;
    public KeyCode rightMoveKey;
    public KeyCode attackKey;
    public KeyCode jumpKey;
    
    public KeyCode dashKey;
    public KeyCode skillKey1;
    public KeyCode skillKey2;
    public KeyCode skillKey3;
    public KeyCode skillKey4;
    public KeyCode skillKey5;
    public KeyCode skillKey6;
    public KeyCode skillKey7;
    public KeyCode skillKey8;

    public KeyCode optionKey;

    [SerializeField] private SpriteAtlas uiAtlas;
    private Sprite[] cloneSprites;
    private Dictionary<string, Sprite> atlasDic = new Dictionary<string, Sprite>();

    [SerializeField] private Player curPlayer;
    [SerializeField] private Transform objectPool;
    [SerializeField] private Transform uiObjectPool;
    [SerializeField] private Transform uiPool;
    [SerializeField] private Transform popupPool;
    [SerializeField] private Transform highestPool;

    [SerializeField] private Player[] players;
    [SerializeField] private List<GameObject> prefabList;
    [SerializeField] private List<GameObject> objectList = new List<GameObject>();

    private string firstPlayer;
    private string secondPlayer = default;

    // 등록된 스킬 목록
    public SkillKeyCollection playerSkillKeyCollection;

    // 매니저들
    public TableManager tableManager;
    //public UIManager uiManager;
    public ResourceManager resourceManager;
    
    // 프로퍼티
    public Player CurPlayer
    {
        get => curPlayer;
        set => curPlayer = value;
    }

    public string FirstPlayer
    {
        get => firstPlayer;
        set => firstPlayer = value;
    }

    protected override void Awake()
    {
        base.Awake();
        Application.targetFrameRate = 60;
        DefaultKeySetting(); 
        InitManager();
        InitAtlas();
        InitPlayer();
    }
    
    // private async void LoadAllPrefabsByLabel()
    // {
    //     await Addressables.InitializeAsync();
    //     
    //     var handle = Addressables.LoadAssetsAsync<GameObject>(
    //         "AllPrefabs",
    //         prefab => objectList.Add(prefab)
    //     );
    //     handle.Completed += OnAllPrefabsLoaded;
    // }
    // private void OnAllPrefabsLoaded(AsyncOperationHandle<IList<GameObject>> handle)
    // {
    //     if (handle.Status == AsyncOperationStatus.Succeeded)
    //         Debug.Log($"총 {objectList.Count}개 프리팹 로드 완료");
    //     else
    //         Debug.LogError("프리팹 일괄 로드 실패");
    // }

    public void GoScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void DefaultKeySetting()
    {
        PlayerPrefs.DeleteAll();
        
        leftMoveKey = KeyBinding.LoadKey(ConstValues.LeftMoveKey, KeyCode.LeftArrow);
        rightMoveKey = KeyBinding.LoadKey(ConstValues.RightMoveKey, KeyCode.RightArrow);
        attackKey = KeyBinding.LoadKey(ConstValues.AttackKey, KeyCode.X);
        jumpKey = KeyBinding.LoadKey(ConstValues.JumpKey, KeyCode.C);
        dashKey = KeyBinding.LoadKey(ConstValues.DashKey, KeyCode.Z);
        optionKey = KeyBinding.LoadKey(ConstValues.OptionKey, KeyCode.Escape);
        
        skillKey1 = KeyBinding.LoadKey(ConstValues.SkillKey1, KeyCode.Q);
        skillKey2 = KeyBinding.LoadKey(ConstValues.SkillKey2, KeyCode.W);
        skillKey3 = KeyBinding.LoadKey(ConstValues.SkillKey3, KeyCode.E);
        skillKey4 = KeyBinding.LoadKey(ConstValues.SkillKey4, KeyCode.R);
        skillKey5 = KeyBinding.LoadKey(ConstValues.SkillKey5, KeyCode.A);
        skillKey6 = KeyBinding.LoadKey(ConstValues.SkillKey6, KeyCode.S);
        skillKey7 = KeyBinding.LoadKey(ConstValues.SkillKey7, KeyCode.D);
        skillKey8 = KeyBinding.LoadKey(ConstValues.SkillKey8, KeyCode.F);

        InitSkillCollection();
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
    private void InitSkillCollection()
    {
        List<SkillKey> berserkerSkillKeyList = new List<SkillKey>();
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerDash, dashKey));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey1));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey2));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey3));
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerChargeCrash, skillKey4));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey5));
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerUpperSlash, skillKey6));
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerCrash, skillKey7));
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerFireStrike, skillKey8));
        playerSkillKeyCollection.berserkerSkillKeyList = berserkerSkillKeyList;
        
        List<SkillKey> gunnerSkillKeyList = new List<SkillKey>();
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerDash, dashKey));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey1));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey2));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey3));
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerBigShot, skillKey4));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey5));
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerGrenade, skillKey6));
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerKnockBackShot, skillKey7));
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerCrazyShot, skillKey8));
        playerSkillKeyCollection.gunnerSkillKeyList = gunnerSkillKeyList;
        
        // json화
        string json = JsonUtility.ToJson(playerSkillKeyCollection, true);
        var loadJson = SkillBinding.LoadSkillCollection(json);
        
        var loadedSkillKeyCollection = JsonUtility.FromJson<SkillKeyCollection>(loadJson);
        playerSkillKeyCollection = loadedSkillKeyCollection;
    }
    public void SetSkillId(KeyCode keyCode, string skillId)
    {
        var berserkerSkillKey = playerSkillKeyCollection.berserkerSkillKeyList.Find(x => x.keyCode == keyCode);
        if (berserkerSkillKey != null)
            berserkerSkillKey.skillId = skillId;
        
        var gunnerSkillKey = playerSkillKeyCollection.gunnerSkillKeyList.Find(x => x.keyCode == keyCode);
        if (gunnerSkillKey != null)
            gunnerSkillKey.skillId = skillId;

        // 저장
        string json = JsonUtility.ToJson(playerSkillKeyCollection, true);
        SkillBinding.SaveKey(json);
    }
    public List<SettingSkill> GetSettingSkillList()
    {
        List<SkillKey> keyList = null;
        
        if(curPlayer.BasicStat.id == ConstValues.Berserker)
            keyList = playerSkillKeyCollection.berserkerSkillKeyList;
        else if(curPlayer.BasicStat.id == ConstValues.Gunner)
            keyList = playerSkillKeyCollection.gunnerSkillKeyList;
        
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
            var matchSkillList = settingSkillList.FindAll(x => x.skillId == playerSkill.skillName);
            foreach (var matchSkill in matchSkillList)
            {
                matchSkill.playerSkill = playerSkill;
            }
        }

        return settingSkillList;
    }

    public List<SkillKey> GetBerserkerSkillKeyList()
    {
        return playerSkillKeyCollection.berserkerSkillKeyList;
    }

    private void InitAtlas()
    {
        // Atlas 안에 들어있는 스프라이트 개수만큼 배열 생성
        cloneSprites = new Sprite[uiAtlas.spriteCount];

        // GetSprites 호출 시 배열에 모두 채워진다
        uiAtlas.GetSprites(cloneSprites);

        foreach (var sprite in cloneSprites)
        {
            var keyName = sprite.name.Split(ConstValues.AtlasClone)[0];
            atlasDic.Add(keyName, sprite);
        }
    }
    public Sprite GetUISprite(string id)
    {
        return atlasDic[id];
    }

    private async void InitManager() 
    {
        tableManager = TableManager.Instance;
        //resourceManager = ResourceManager.Instance;
        //uiManager = UIManager.Instance;
        
        tableManager.Init();
        //uiManager.Init();
        //await resourceManager.Init();
    }
    
    private async void OpenUI()
    {
        //await UIManager.Instance.OpenAsync(eUIType.UI_Skill, model);
    }

    // 플레이어
    private void InitPlayer()
    {
        FirstPlayer = ConstValues.Berserker;
        curPlayer = GetPlayer(FirstPlayer);
        foreach (var player in players)
        {
            player.InitBasicStat();
            player.InitSkill();
        }
    }
    public void SpawnPlayer(string playerName, Transform playerPos)
    {
        ActivePlayer(playerName);
        curPlayer.transform.position = playerPos.position;
    }

    private Player GetPlayer(string playerName)
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
            player.gameObject.SetActive(player.name == playerName);
    }
    
    // 일반 오브젝트
    public GameObject SpawnToObjectPool(string id, Transform objTransform)
    {
        return SpawnToPool(id, objectPool, objTransform);
    }
    public GameObject SpawnToObjectPool(string id, Vector2 objVector)
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
    // UI화면
    public GameObject SpawnToUIPool(eUIType type, Transform objTransform)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objTransform);
        SetUI(type, go);
        return go;
    }
    public GameObject SpawnToUIPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objVector);
        SetUI(type, go);
        return go;
    }
    // UI팝업화면
    public GameObject SpawnToPopupPool(eUIType type, Transform objTransform)
    {
        return SpawnToPool(type.ToString(), popupPool, objTransform);
    }
    public GameObject SpawnToPopupPool(eUIType type, Vector2 objVector)
    {
        return SpawnToPool(type.ToString(), popupPool, objVector);
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

    private GameObject SpawnToPool(string id, Transform pool, Transform objTransform)
    {
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
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
                go.SetActive(true);
            }
        }
        go.transform.position = objTransform.position;
        return go;
    }
    private GameObject SpawnToPool(string id, Transform pool, Vector2 objVector)
    { 
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
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
                go.SetActive(true);
            }
        }
        go.transform.position = objVector;
        return go;
    }

    // UI관련 코드
    // 바인딩
    private async void BindPresenter(eUIType type, UIBase uiBase)
    {
        switch (type)
        {
            case eUIType.UI_Skill:
                if (uiBase is UI_Skill skillView)
                {
                    var skillModel = new UISkillModel
                    {
                        settingSkillList = GetSettingSkillList()
                    };
                    // 뷰 리스트를 인터페이스로 변환
                    var viewInterfaces = skillView.SkillViews.ConvertAll(v => (IUISkillView)v);
                    var presenter = new UISkillPresenter(viewInterfaces, skillModel);
                    skillView.SetPresenter(presenter);
                    presenter.SetSkillInfo();
                }
                break;
        }
    }

    private void SetUI(eUIType uiType, GameObject uiObject)
    {
        var uiBase = uiObject.GetComponent<UIBase>();
        uiBase.Setup(uiType);
        if (uiBase != null)
            BindPresenter(uiType, uiBase);
    }
}
