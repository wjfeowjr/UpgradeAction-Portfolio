using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.U2D;


public partial class GameManager : Singleton<GameManager>
{
    public bool isDemo;
    public Material defaultMaterial;
    public Material hitMaterial;
    public GameObject inGameDebugConsole;
    
    // 설정 값은 GameSettings 가 소유한다.
    // 호출부 265 곳이 쓰는 기존 이름을 유지하려고 위임 프로퍼티로 남긴다.
    public KeyCode escKey => settings.EscKey;
    public KeyCode enterKey => settings.EnterKey;
    public KeyCode deleteKey => settings.DeleteKey;
    public KeyCode copyKey => settings.CopyKey;
    public KeyCode leftKey
    {
        get => settings.LeftKey;
        set => settings.LeftKey = value;
    }
    public KeyCode rightKey
    {
        get => settings.RightKey;
        set => settings.RightKey = value;
    }
    public KeyCode upKey
    {
        get => settings.UpKey;
        set => settings.UpKey = value;
    }
    public KeyCode downKey
    {
        get => settings.DownKey;
        set => settings.DownKey = value;
    }
    public KeyCode miniMapKey
    {
        get => settings.MiniMapKey;
        set => settings.MiniMapKey = value;
    }
    public KeyCode characterInfoKey
    {
        get => settings.CharacterInfoKey;
        set => settings.CharacterInfoKey = value;
    }
    public KeyCode attackKey
    {
        get => settings.AttackKey;
        set => settings.AttackKey = value;
    }
    public KeyCode jumpKey
    {
        get => settings.JumpKey;
        set => settings.JumpKey = value;
    }
    public KeyCode changeCharacterKey
    {
        get => settings.ChangeCharacterKey;
        set => settings.ChangeCharacterKey = value;
    }
    public KeyCode dashKey
    {
        get => settings.DashKey;
        set => settings.DashKey = value;
    }
    public KeyCode skillKey1
    {
        get => settings.SkillKey1;
        set => settings.SkillKey1 = value;
    }
    public KeyCode skillKey2
    {
        get => settings.SkillKey2;
        set => settings.SkillKey2 = value;
    }
    public KeyCode skillKey3
    {
        get => settings.SkillKey3;
        set => settings.SkillKey3 = value;
    }
    public KeyCode skillKey4
    {
        get => settings.SkillKey4;
        set => settings.SkillKey4 = value;
    }
    public KeyCode potionKey
    {
        get => settings.PotionKey;
        set => settings.PotionKey = value;
    }
    public KeyCode changeCharacterLeftKey
    {
        get => settings.ChangeCharacterLeftKey;
        set => settings.ChangeCharacterLeftKey = value;
    }
    public KeyCode changeCharacterRightKey
    {
        get => settings.ChangeCharacterRightKey;
        set => settings.ChangeCharacterRightKey = value;
    }
    public KeyCode pauseKey
    {
        get => settings.PauseKey;
        set => settings.PauseKey = value;
    }
    public float masterVolume
    {
        get => settings.MasterVolume;
        set => settings.MasterVolume = value;
    }
    public float sfxVolume
    {
        get => settings.SfxVolume;
        set => settings.SfxVolume = value;
    }
    public float bgmVolume
    {
        get => settings.BgmVolume;
        set => settings.BgmVolume = value;
    }
    public int resolutionX
    {
        get => settings.ResolutionX;
        set => settings.ResolutionX = value;
    }
    public int resolutionY
    {
        get => settings.ResolutionY;
        set => settings.ResolutionY = value;
    }
    public int fullScreen
    {
        get => settings.FullScreen;
        set => settings.FullScreen = value;
    }
    public int vSync
    {
        get => settings.VSync;
        set => settings.VSync = value;
    }
    public int cameraShaking
    {
        get => settings.CameraShaking;
        set => settings.CameraShaking = value;
    }

    // 언어는 LocalizationService 가 소유한다.
    public string language
    {
        get => localization.CurrentLanguage;
        set => localization.CurrentLanguage = value;
    }
   
    [SerializeField] private SpriteAtlas uiAtlas;
    [SerializeField] private SpriteAtlas bgAtlas;
    [SerializeField] private SpriteAtlas guideAtlas;
    private Sprite[] cloneSprites;
    private Dictionary<string, Sprite> atlasDic = new Dictionary<string, Sprite>();

    [SerializeField] private Player curPlayer;
    [SerializeField] private Transform objectPool;
    [SerializeField] private Transform uiObjectPool;
    [SerializeField] private Transform uiPool;
    [SerializeField] private Transform popupPool;
    [SerializeField] private Transform highestPool;

    [SerializeField] private List<Player> players = new List<Player>();
    // PrefabCacher(에디터 툴)가 채운다. ObjectPoolService 생성 시 넘긴다.
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>();
    [SerializeField] private List<Monster> monsterList = new List<Monster>();
    
    [SerializeField] private FadeSystem fadeSystem;

    private UI_Interface uiInterface;
    private Popup_Warning popupWarning;
    
    // 세이브 넘버
    private string curSaveFileName;

    [SerializeField] private bool inGame;
    [SerializeField] private bool controlStart;
    // 방 이동(좌우) 연출 중에는 ControlStart=false 전환 시 속도 정리를 건너뛴다(이동 연속성 유지, 멈칫 방지)
    private bool roomMoving;
    private bool standLock;
    private bool bossProduct;
    private bool timeProduct;
    private int comboCount;

    [SerializeField] private SaveData saveData;
    
    // 등록된 스킬 및 키 세팅 목록
    [SerializeField] private SettingSkill changeSkill;
    [SerializeField] private SettingSkill potionSkill;
    
    // 복제체 데이터들
    // 복제본은 GameDataService 가 소유한다. 기존 이름은 위임으로 남긴다.
    public List<SkillAttributeCopy> skillAttributeCopyList => gameData.skillAttributeCopyList;
    public List<ItemCopy> itemCopyList => gameData.itemCopyList;
    public List<RelicCopy> relicCopyList => gameData.relicCopyList;
    public List<NpcCopy> npcCopyList => gameData.npcCopyList;
    public List<DialogueChoiceCopy> dialogueChoiceCopyList => gameData.dialogueChoiceCopyList;
    public List<GrenadeCopy> grenadeCopyList => gameData.grenadeCopyList;
    public List<PassiveCopy> passiveCopyList => gameData.passiveCopyList;
    
    // 매니저들
    public TableManager tableManager;

    // 분리된 서비스들 (MonoBehaviour 아님. InitManager 에서 생성한다)
    private LocalizationService localization;
    public LocalizationService Localization => localization;

    private ObjectPoolService pool;
    public ObjectPoolService Pool => pool;

    private GameFlowService flow;
    public GameFlowService Flow => flow;

    private GameSettings settings;
    public GameSettings Settings => settings;

    private GameDataService gameData;
    public GameDataService GameData => gameData;

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
            // 단, 좌우 방 이동 연출 중(roomMoving)에는 정리를 건너뛰어 한 프레임 멈칫을 막는다.
            if (!value && !roomMoving && CurPlayer)
            {
                CurPlayer.Stop();
                CurPlayer.StopVelocity_X();
            }
        }
    }

    /// <summary>
    /// 팝업이 입력 잠금을 '요청'한다.
    /// 팝업 위에 팝업이 겹쳐도 마지막 하나가 닫힐 때까지 잠금이 유지된다.
    ///
    /// 컷신처럼 순차적으로 일어나는 연출은 ControlStart 를 직접 쓴다.
    /// 겹칠 일이 없어 요청 방식이 필요 없기 때문이다.
    /// </summary>
    public void LockControl(object owner)
    {
        flow.LockInput(owner);
        ControlStart = false;
    }

    /// <summary>내 요청만 푼다. 다른 팝업이 남아 있으면 잠금이 유지된다.</summary>
    public void UnlockControl(object owner)
    {
        flow.UnlockInput(owner);
        if (!flow.IsInputLocked)
            ControlStart = true;
    }

    /// <summary>
    /// 요청만 거두고 조작은 복구하지 않는다.
    /// 팝업이 닫힌 뒤 이어지는 연출이 조작을 직접 관리하는 경우에 쓴다
    /// (패스트 트래블처럼 닫자마자 다시 멈춰야 하는 흐름).
    ///
    /// 거두지 않으면 요청자로 영원히 남아, 이후 어떤 팝업이 닫혀도 잠금이 풀리지 않는다.
    /// </summary>
    public void ReleaseControl(object owner)
    {
        flow.UnlockInput(owner);
    }

    public bool RoomMoving
    {
        get => roomMoving;
        set => roomMoving = value;
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
    
    public int BossCount => saveData.bossCount;

    public int CurBossCount => saveData.curBossCount;

    public bool FirstPortal
    {
        get => saveData.firstPortal;
        set => saveData.firstPortal = value;
    }

    public bool IsWishlistAccepted
    {
        get => saveData.isWishlistAccepted;
        set => saveData.isWishlistAccepted = value;
    }

    public bool IsFirstWishlistShown
    {
        get => saveData.isFirstWishlistShown;
        set => saveData.isFirstWishlistShown = value;
    }

    public bool IsSecondWishlistShown
    {
        get => saveData.isSecondWishlistShown;
        set => saveData.isSecondWishlistShown = value;
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
    
    public bool FirstGetPotion
    {
        get => saveData.firstGetPotion;
        set => saveData.firstGetPotion = value;
    }
    
    public bool FirstGetRelic
    {
        get => saveData.firstGetRelic;
        set => saveData.firstGetRelic = value;
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
    
    public SettingSkill PotionSkill => potionSkill;
    
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
        
        InitManager();
        gameData.SetCopyData();
        InitAtlas(uiAtlas);
        InitAtlas(bgAtlas);
        InitAtlas(guideAtlas);
        SetPrefabActive(false);
        DefaultKeySetting();
        FirstCashing();
    }

    private void Update()
    {
        inGameDebugConsole.SetActive(!isDemo);
        
        if (Input.GetKeyDown(KeyCode.F12))
            inGameDebugConsole.SetActive(!inGameDebugConsole.activeSelf);

        // Alt+Enter 전체화면 토글은 보류.
        // Alt 를 게임 키로 바인딩할 수 있게 되면서 충돌하므로, 처리 방식을 정할 때까지 막아둔다.
        // 전체화면 전환은 설정 팝업의 비디오 항목에서 한다.
        // if (InputHelper.IsAltPressed && (Input.GetKeyDown(enterKey) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        //     ToggleFullScreen();

    }


    private void OnDestroy()
    {
        SetPrefabActive(true);
    }
    

    private void GameStartSetting()
    {
        if (SaveSystem.Exists(curSaveFileName))
        {
            saveData = LoadGame(curSaveFileName);
            DataPatch(saveData);
            // 저장된 위치와 무관하게, 불러오기 시 엘리베이터는 항상 시작 인덱스에서 시작
            ResetElevatorIdx();
        }
        else
        {
            FirstStart();
        }
        
        LockAttributeSetting();
        // 구버전 세이브는 스킬 슬롯 keyCode 가 최초 생성 시점의 키로 굳어 있으므로 현재 키 설정으로 맞춘다
        SyncSkillKeyCode();
        curPlayer = GetPlayer(saveData.playerList[0]);
    }


    private void InitManager()
    {
        tableManager = TableManager.Instance;
        tableManager.Init();

        // 테이블 로드 직후에 생성한다 (조회 캐시를 만들어야 하므로 순서가 중요하다)
        localization = new LocalizationService(tableManager.talkTable, tableManager.itemTable);

        // prefabList 는 PrefabCacher 가 에디터에서 채워둔 상태여야 한다
        // 테이블 복제본. SetCopyData 는 Awake 에서 따로 부른다.
        gameData = new GameDataService(tableManager);

        pool = new ObjectPoolService(prefabList);
        flow = new GameFlowService();
        settings = new GameSettings();
    }


    // 고정 로테이션: Berserker → Gunner → Fighter → Berserker → ...
    private static readonly string[] PlayerRotation =
    {
        ConstValues.Berserker,
        ConstValues.Gunner,
        ConstValues.Fighter,
    };
}
