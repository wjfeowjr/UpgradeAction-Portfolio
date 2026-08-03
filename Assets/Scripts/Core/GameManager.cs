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
    
    public KeyCode escKey;
    public KeyCode enterKey;
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
            // 단, 좌우 방 이동 연출 중(roomMoving)에는 정리를 건너뛰어 한 프레임 멈칫을 막는다.
            if (!value && !roomMoving && CurPlayer)
            {
                CurPlayer.Stop();
                CurPlayer.StopVelocity_X();
            }
        }
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
        SetCopyData();
        InitAtlas(uiAtlas);
        InitAtlas(bgAtlas);
        SetPrefabActive(false);
        DefaultKeySetting();
        FirstCashing();
    }

    private void Update()
    {
        inGameDebugConsole.SetActive(!isDemo);
        
        if (Input.GetKeyDown(KeyCode.F12))
            inGameDebugConsole.SetActive(!inGameDebugConsole.activeSelf);

        // Alt+Enter: 전체화면 <-> 창모드 토글
        if (InputHelper.IsAltPressed && (Input.GetKeyDown(enterKey) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            ToggleFullScreen();

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
        curPlayer = GetPlayer(saveData.playerList[0]);
    }


    private void InitManager() 
    {
        tableManager = TableManager.Instance;
        tableManager.Init();
    }


    // 고정 로테이션: Berserker → Gunner → Fighter → Berserker → ...
    private static readonly string[] PlayerRotation =
    {
        ConstValues.Berserker,
        ConstValues.Gunner,
        ConstValues.Fighter,
    };
}
