using System;
using System.Collections;
using System.Collections.Generic;
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
    public SkillKey changeKey;
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
    UI_Interface,
    
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
    public KeyCode downKey;

    public KeyCode changeCharacterKey;
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
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>();
    [SerializeField] private List<GameObject> objectList = new List<GameObject>();
    [SerializeField] private List<Collider2D> platformColliderList = new List<Collider2D>();

    private string firstPlayer;
    private string secondPlayer = default;
    private bool controlStart;

    // 등록된 스킬 목록
    private SettingSkill changeSkill;
    [SerializeField] private SkillKeyCollection playerSkillKeyCollection;

    // 매니저들
    public TableManager tableManager;
    //public UIManager uiManager;
    //public ResourceManager resourceManager;
    
    // 카메라
    private FollowCamera mainCamera;
    [SerializeField] private Canvas uiObjectCanvas;

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
    
    public string SecondPlayer
    {
        get => secondPlayer;
    }

    public bool ControlStart
    {
        get => controlStart;
        set => controlStart = value;
    }

    public SkillKeyCollection PlayerSkillKeyCollection => playerSkillKeyCollection;

    public SettingSkill ChangeSkill => changeSkill;

    public List<Collider2D> PlatformColliderList
    {
        get => platformColliderList;
        set => platformColliderList = value;
    }

    protected override void Awake()
    {
        base.Awake();
        Application.targetFrameRate = 60;
        DefaultKeySetting();
        InitManager();
        InitAtlas();
        InitPlayer();
        InitChangeSkill();
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

    public List<GameObject> GetPrefabList()
    {
        return prefabList;
    }

    // 재귀 순회하여 모든 자식 GameObject 추가
    private static void CollectRecursive(Transform parent, List<GameObject> list)
    {
        foreach (Transform child in parent)
        {
            list.Add(child.gameObject);
            CollectRecursive(child, list);
        }
    }

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
        downKey = KeyBinding.LoadKey(ConstValues.DownKey, KeyCode.DownArrow);
        
        changeCharacterKey = KeyBinding.LoadKey(ConstValues.ChangeCharacterKey, KeyCode.LeftShift);
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
        playerSkillKeyCollection.changeKey = SetSkillKey(ConstValues.ChangeCharacterKey, changeCharacterKey);
        
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
        if (curPlayer.BasicStat.id == ConstValues.Berserker)
        {
            var berserkerSkillKey = playerSkillKeyCollection.berserkerSkillKeyList.Find(x => x.keyCode == keyCode);
            if (berserkerSkillKey != null)
                berserkerSkillKey.skillId = skillId;
        }
        else if (curPlayer.BasicStat.id == ConstValues.Gunner)
        {
            var gunnerSkillKey = playerSkillKeyCollection.gunnerSkillKeyList.Find(x => x.keyCode == keyCode);
            if (gunnerSkillKey != null)
                gunnerSkillKey.skillId = skillId;
        }
        
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
        firstPlayer = ConstValues.Berserker;
        //secondPlayer = ConstValues.Gunner;
        
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

    public void InitCamera(FollowCamera targetCamera)
    {
        mainCamera = targetCamera;
        uiObjectCanvas.worldCamera = targetCamera.GetComponent<Camera>();
    }

    public void CameraShake(float amount, float time)
    {
        mainCamera.Shake(amount, time);
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
    private void SetSkillUI(eUIType uiType)
    {
        UIBase uiBase = null;
        foreach (var list in objectList)
        {
            if (list.GetComponent<UIBase>() && list.GetComponent<UIBase>().GetUIType() == uiType)
            {
                uiBase = list.GetComponent<UIBase>();
                break;
            }
        }
        if (uiBase != null)
            BindPresenter(uiType, uiBase); 
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
            }
        }
        
        // 미사일의 잔상버그를 막기 위한 조치
        go.SetActive(false);
        go.transform.position = objTransform.position;
        go.SetActive(true);
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
            }
        }
        
        // 미사일의 잔상버그를 막기 위한 조치
        go.SetActive(false);
        go.transform.position = objVector;
        go.SetActive(true);
        return go;
    }

    // UI관련 코드
    // 바인딩
    private async void BindPresenter(eUIType type, UIBase uiBase)
    {
        switch (type)
        {
            case eUIType.UI_Interface:
                if (uiBase is UI_Interface interfaceView)
                {
                    var hpInterface = interfaceView.HpView.ConvertTo<IUIHpView>();
                    var hpModel = new UIHpModel()
                    {
                        character = CurPlayer
                    };
                    var hpPresenter = new UIHpPresenter(hpInterface, hpModel);
                    interfaceView.SetHpPresenter(hpPresenter);
                    hpPresenter.SetHp();
                    hpPresenter.SetHpText();
                    
                    // 뷰 리스트를 인터페이스로 변환
                    var changeInterface = interfaceView.ChangeCharacter.ConvertTo<IUISkillView>();
                    var skillInterfaces = interfaceView.SkillViews.ConvertAll(v => (IUISkillView)v);
                    var skillModel = new UISkillModel
                    {
                        changeSkill = this.changeSkill,
                        settingSkillList = GetSettingSkillList()
                    };
                    var skillPresenter = new UISkillPresenter(changeInterface, skillInterfaces, skillModel);
                    interfaceView.SetSkillPresenter(skillPresenter);
                    skillPresenter.SetSkillInfo();
                }
                break;
        }
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

    private void SetUI(eUIType uiType, GameObject uiObject)
    {
        var uiBase = uiObject.GetComponent<UIBase>();
        uiBase.Setup(uiType);
        if (uiBase != null)
            BindPresenter(uiType, uiBase); 
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
            addedSkill.skillName = skill.id;
            var coolTimeArray = skill.coolTime.Split(',');
            foreach (var coolTime in coolTimeArray)
                addedSkill.coolTime.Add(float.Parse(coolTime));

            addedSkill.icon = skill.icon;
            changeSkill.playerSkill = addedSkill;
            break;
        }
    }

    public void CharacterChange()
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
        
        if(pastPlayer.NormalState == ENormalState.Jump)
            curPlayer.JumpChange(pastVelocity);
        else if(pastPlayer.MoveState == EMoveState.Moving)
            curPlayer.MoveChange();
        else
            curPlayer.ChangeAttack();
        
        SetSkillUI(eUIType.UI_Interface);
    }

    public SpeechFrame SpawnSpeechFrame(Vector2 speechVector, string dialog)
    {
        var speechFrame = SpawnToUIObjectPool(ConstValues.SpeechFrame, speechVector);
        var frameClass = speechFrame.GetComponent<SpeechFrame>();
        frameClass.Speech(dialog);
        return frameClass;
    }
}
