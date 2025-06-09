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
}

public class GameManager : Singleton<GameManager>
{
    public Material defaultMaterial;
    public Material hitMaterial;
    
    public KeyCode escKey;
    
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
    [SerializeField] private List<Monster> monsterList = new List<Monster>();

    private bool secondStart;
    private string firstPlayer;
    private string secondPlayer = default;
    private bool controlStart;
    private int comboCount;
    private int groundLayerMask;
    private float groundPosY;
    
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

    public Player[] Players => players;

    public bool SecondStart
    {
        get => secondStart;
        set => secondStart = value;
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

    public int ComboCount
    {
        get => comboCount;
        set => comboCount = value;
    }

    public float GroundPosY
    {
        get => groundPosY;
        set => groundPosY = value;
    }

    public SkillKeyCollection PlayerSkillKeyCollection => playerSkillKeyCollection;

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

    protected override void Awake()
    {
        base.Awake();
        //QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        groundLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Ground);
        DefaultKeySetting();
        InitManager();
        InitAtlas();
        InitPlayer();
        InitChangeSkill();
        SetPrefabActive(false);
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

    public void SetGroundVector()
    {
        var downRay = Physics2D.Raycast(transform.position, Vector2.down, 100f, groundLayerMask);
        if (downRay.collider != null)
            groundPosY = downRay.point.y;
    }

    private void DefaultKeySetting()
    {
        PlayerPrefs.DeleteAll();

        escKey = KeyCode.Escape;
        
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
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey4));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey5));
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerUpperSlash, skillKey6));
        berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerCrash, skillKey7));
        berserkerSkillKeyList.Add(SetSkillKey(default, skillKey8));
        
        //berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerChargeCrash, skillKey4));
        //berserkerSkillKeyList.Add(SetSkillKey(ConstValues.BerserkerFireStrike, skillKey8));
        playerSkillKeyCollection.berserkerSkillKeyList = berserkerSkillKeyList;
        
        List<SkillKey> gunnerSkillKeyList = new List<SkillKey>();
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerDash, dashKey));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey1));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey2));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey3));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey4));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey5));
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerGrenade, skillKey6));
        gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerKnockBackShot, skillKey7));
        gunnerSkillKeyList.Add(SetSkillKey(default, skillKey8));
        
        //gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerBigShot, skillKey4));
        //gunnerSkillKeyList.Add(SetSkillKey(ConstValues.GunnerCrazyShot, skillKey8));
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
        SetPlayerOrder(ConstValues.Berserker, ConstValues.Gunner);

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

    private void InitPlayerStat()
    {
        foreach (var player in players)
            player.InitBasicStat();
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
            player.gameObject.SetActive(player.name == playerName);
    }

    public void ArrivePlayer()
    {
        foreach (var player in players)
        {
            player.Immortal = false;
            player.IsDie = false;
        }
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

    public Monster SpawnMonster(string id, Vector2 monsterVector, bool isBoss = false, Action bossProduct = null)
    {
        var monster = SpawnToObjectPool(id, monsterVector).GetComponent<Monster>();
        monster.IsBoss = isBoss;
        monster.SpawnHpBar();
        monster.Appear(bossProduct);
        monsterList.Add(monster);
        return monster;
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
    
    public void SpawnTrap(string id, Vector2 pos)
    {
        var trap = SpawnToObjectPool(id, pos); 
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData != null)
        {
            var spawnedObject = trap.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = trap.AddComponent<SpawnedObject>();
            
            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
        }

        var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == id);
        if (attackData != null)
        {
            var attack = trap.GetComponent<Attack>();
            if (!attack)
            {
                attack = trap.AddComponent<Attack>();
                attack.SetupData(null, attackData);
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
    public GameObject SpawnToUIPool(string id, Vector2 objVector)
    {
        return SpawnToPool(id, uiPool, objVector);
    }
    public GameObject SpawnToUIPool(eUIType type, Transform objTransform)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objTransform);
        SetUIorPopup(type, go);
        return go;
    }
    public GameObject SpawnToUIPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objVector);
        SetUIorPopup(type, go);
        return go;
    }
    // UI팝업화면
    public GameObject SpawnToPopupPool(eUIType type, Transform objTransform)
    {
        var go = SpawnToPool(type.ToString(), popupPool, objTransform);
        SetUIorPopup(type, go);
        return go;
    }
    public GameObject SpawnToPopupPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), popupPool, objVector);
        SetUIorPopup(type, go);
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
        
        go.transform.position = objVector;
        go.SetActive(true);
        return go;
    }

    // UI관련 코드
    // 바인딩(변하지 않는 UI만)
    private async void BindPresenter(eUIType type, UIBase uiBase)
    {
        switch (type)
        {
            case eUIType.UI_Interface:
                if (uiBase is UI_Interface interfaceView)
                {
                    var comboInterface = interfaceView.ComboView.ConvertTo<IUIComboView>();
                    var comboModel = new UIComboModel()
                    {
                        comboCount = 0
                    };
                    var comboPresenter = new UIComboPresenter(comboInterface, comboModel);
                    interfaceView.SetComboPresenter(comboPresenter);
                    comboPresenter.SetCombo();
                    
                    var hpInterface = interfaceView.HpView.ConvertTo<IUIHpView>();
                    var hpModel = new UIHpModel()
                    {
                        character = CurPlayer
                    };
                    var hpPresenter = new UIHpPresenter(hpInterface, hpModel);
                    interfaceView.SetHpPresenter(hpPresenter);
                    hpPresenter.SetHp();
                    hpPresenter.SetHpText();
                    
                    var bossHpInterface = interfaceView.BossHpView.ConvertTo<IUIBossHpView>();
                    var bossHpPresenter = new UIBossHpPresenter(bossHpInterface);
                    interfaceView.SetBossHpPresenter(bossHpPresenter);
                    bossHpPresenter.HideHp();

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

            case eUIType.Popup_GameOver:
                if (uiBase is Popup_GameOver gameOverPopup)
                {
                    var gameOverInterface = gameOverPopup.GameOverView.ConvertTo<IUIGameOverView>();
                    var hpModel = new PopupGameOverModel()
                    {
                        title = "게임 오버",
                        message = "다시 하기",
                        confirmAction = () =>
                        {
                            GoScene(ConstValues.BattleScene);
                            uiBase.Close();
                            InitPlayerStat();
                            controlStart = true;
                            Time.timeScale = 1;
                            BgmManager.Instance.ReplayBgm();
                        }
                    };
                    var gameOverPresenter = new PopupGameOverPresenter(gameOverInterface, hpModel);
                    gameOverPresenter.SetPopup();
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

    // 비활성화 된 UI나 Popup을 활성화 후, 바인딩
    private void SetUIorPopup(eUIType uiType, GameObject uiObject)
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

        if (changeAttack)
        {
            if(pastPlayer.NormalState == ENormalState.Jump)
                curPlayer.JumpChange(pastVelocity);
            else if(pastPlayer.MoveState == EMoveState.Moving)
                curPlayer.MoveChange();
            else
                curPlayer.ChangeAttack();
        }

        RefreshSkill();
        SetCameraTarget(curPlayer.transform);
    }

    // 단독 => 단독
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
        var changeInterface = uiInterface.ChangeCharacter.ConvertTo<IUISkillView>();
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
}
