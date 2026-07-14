using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public enum EntranceDir
{
    Left,
    Right,
    Up,
    Down,
}

public class Room : MonoBehaviour
{
    private bool firstStart;
    private bool nearBossRoom;
    private int productViewIdx;
    private float dialogDelay1 = 2.5f;
    private float dialogDelay2 = 1.0f;

    [Header("디자인 타일이 미리 그려진 미니맵 Tilemap")]
    [SerializeField] private Tilemap minimapFrameTilemap;
    [SerializeField] private Tilemap minimapInTilemap;
    [SerializeField] private Tilemap[] shortcutFrameTileMaps;
    
    [Header("카메라 & 저장키")]
    private Camera gameCamera;

    // 미니맵 프레임
    private List<Vector3Int> allFrameCells = new List<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> originalFrameTiles = new Dictionary<Vector3Int, TileBase>();
    private List<Vector3Int> visitedFrameCells = new List<Vector3Int>();
    
    // 미니맵 내부
    private List<Vector3Int> allInCells = new List<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> originalInTiles = new Dictionary<Vector3Int, TileBase>();
    private List<Vector3Int> visitedInCells = new List<Vector3Int>();
    
    // 미니맵 숏컷
    private List<Vector3Int> allshortcutCells = new List<Vector3Int>();
    private List<Dictionary<Vector3Int, TileBase>> originalshortcutTilesList = new List<Dictionary<Vector3Int, TileBase>>();
    private List<List<Vector3Int>> visitedShortcutCells = new List<List<Vector3Int>>();
    
    [SerializeField] private bool isBossRoom;
    [SerializeField] private GameObject roomGameObject;

    // 나중에 한번에 데이터 처리하기
    [SerializeField] protected RoomSkillAndPassive[] roomSkillAndPassive;
    [SerializeField] protected RoomTreasureBox[] roomTreasureBox;
    [SerializeField] protected RoomObject[] roomObjects;
    [SerializeField] protected Elevator[] elevators;
    [SerializeField] protected LockDoor[] lockDoors;
    [SerializeField] protected Arena[] arenas;
    [SerializeField] protected GoldObject[] goldObjects;

    [SerializeField] private Transform minCameraLimitX;
    [SerializeField] private Transform maxCameraLimitX;
    [SerializeField] private Transform minCameraLimitY;
    [SerializeField] private Transform maxCameraLimitY;

    [SerializeField] private Transform[] eventCameraPos;

    [Header("카메라 시야 확장 존")]
    [SerializeField] private CameraExpandZone[] cameraExpandZones;

    [Header("미니맵 숨겨진 구역")]
    [SerializeField] private HiddenArea[] hiddenAreas;

    [Header("투명벽")]
    [SerializeField] private TransparentWall[] transparentWalls;

    [SerializeField] private SaveObject saveObject;
    [SerializeField] private PortalObject portalObject;
    [SerializeField] private MerchantObject merchantObject;

    // 퇴장 연출: 과거 방에서 진행 방향으로 더 걸어 나가는 거리.
    // 너무 크면 문턱 밖(바닥 없는 곳/벽)으로 나가 떨어지거나 막히므로 바닥 안에서 작게 유지한다.
    private const float ExitWalkOffsetX = 2.56f;
    // 입장 연출: 새 방에서 도착위치(playerPos[idx])보다 바깥에서 시작해 걸어 들어오는 거리. 자유롭게 조절 가능.
    private const float EnterWalkOffsetX = 2.56f;

    [Header("플레이어 도착위치")]
    [SerializeField] private List<Transform> leftPlayerPos;
    [SerializeField] private List<Transform> rightPlayerPos;
    [SerializeField] private List<Transform> upPlayerPos;
    [SerializeField] private List<Transform> downPlayerPos;

    [Header("인접한 방")]
    [SerializeField] private Room[] leftRoom;
    [SerializeField] private Room[] rightRoom;
    [SerializeField] private Room[] upRoom;
    [SerializeField] private Room[] downRoom;

    [Header("방의 입구")]
    [SerializeField] private List<RoomEntrance> leftEntrance;
    [SerializeField] private List<RoomEntrance> rightEntrance;
    [SerializeField] private List<RoomEntrance> upEntrance;
    [SerializeField] private List<RoomEntrance> downEntrance;

    [Header("숏컷으로 뚫을 방")]
    [SerializeField] private Room[] shortCutRoom;
    
    [SerializeField] protected Monster[] monsters;
    [SerializeField] protected List<Vector2> firstMonsterPosList = new List<Vector2>();
    [SerializeField] protected Monster[] bosses;
    [SerializeField] protected Transform[] bossPos;
    
    [SerializeField] protected List<Vector2> firstBossPosList = new List<Vector2>();
    [SerializeField] protected Npc[] npc;
    [SerializeField] protected CustomObject[] customObjects;
    [SerializeField] protected List<Collider2D> trapList = new List<Collider2D>();
    [SerializeField] protected ShortcutObject[] shortCutObjects;
    
    [SerializeField] protected Transform monsterLimitLeft;
    [SerializeField] protected Transform monsterLimitRight;
    
    [SerializeField] protected ProductTrigger[] productTriggers;
    [SerializeField] protected GuideObject[] guideObjects;
    [SerializeField] protected Transform[] customMovePos;

    [SerializeField] protected SpriteRenderer[] bgSpriteRenderers;
    [SerializeField] private TileFactory bossTilemap;
    [SerializeField] private GameObject[] roomCustomObjects;
    [SerializeField] private RoomInfo roomInfo;

    private RoomsData roomsData;

    private Vector2 firstMaxLimit;
    private Vector2 firstMinLimit;

    // 존 판정용 이전 프레임 플레이어 위치 (이동 선분 통과 검사에 사용)
    private Vector2 prevPlayerPos;
    private bool hasPrevPlayerPos;

    public string Id    => roomInfo.roomId;
    public string Place => GameManager.Instance.GetPlaceName((ePlace)Enum.Parse(typeof(ePlace), roomsData.place));

    // 패스트 트래블용: 세이브 포인트 활성화 여부와 세이브 오브젝트 접근자
    public bool SavePointCheck => roomInfo != null && roomInfo.IsRevealed(EMinimapObjectType.SavePoint);
    public SaveObject SaveObject => saveObject;

    // 포탈 이동용: 포탈 발견 여부와 포탈 오브젝트 접근자
    public bool PortalCheck => roomInfo != null && roomInfo.IsRevealed(EMinimapObjectType.Portal);
    public PortalObject PortalObject => portalObject;

    private void Awake()
    {
        if (!RoomManager.Instance.MainCamera)
            return;
        
        gameCamera = RoomManager.Instance.MainCamera;
        
        // 1. 모든 그려진 타일 위치 저장 및 비활성화
        var frameBounds = minimapFrameTilemap.cellBounds;
        foreach (var pos in frameBounds.allPositionsWithin)
        {
            if (minimapFrameTilemap.HasTile(pos))
            {
                allFrameCells.Add(pos);
                originalFrameTiles[pos] = minimapFrameTilemap.GetTile(pos);
            }
        }
        minimapFrameTilemap.ClearAllTiles();
        
        var inBounds = minimapInTilemap.cellBounds;
        foreach (var pos in inBounds.allPositionsWithin)
        {
            if (minimapInTilemap.HasTile(pos))
            {
                allInCells.Add(pos);
                originalInTiles[pos] = minimapInTilemap.GetTile(pos);
            }
        }
        minimapInTilemap.ClearAllTiles();

        if (shortcutFrameTileMaps.Length > 0)
        {
            for (int i = 0; i < shortcutFrameTileMaps.Length; i++)
            {
                visitedShortcutCells.Add(new List<Vector3Int>());
                originalshortcutTilesList.Add(new Dictionary<Vector3Int, TileBase>()); // 딕셔너리 개별 생성

                var targetMap = shortcutFrameTileMaps[i];
                var bounds = targetMap.cellBounds;

                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (targetMap.HasTile(pos))
                    {
                        // i번째 타일맵 전용 딕셔너리에 저장
                        originalshortcutTilesList[i][pos] = targetMap.GetTile(pos);
                    
                        // 전체 좌표 리스트에도 추가 (중복 방지를 위해 확인 후 추가하거나 유지)
                        if (!allshortcutCells.Contains(pos))
                            allshortcutCells.Add(pos);
                    }
                }
                targetMap.ClearAllTiles();
            }
        }

        // 숨겨진 구역 미니맵 타일 캐싱 및 숨김
        if (hiddenAreas != null)
        {
            foreach (var hiddenArea in hiddenAreas)
                hiddenArea.CacheTiles();
        }

        // 투명벽 타일 좌표 캐싱 및 색상 잠금 해제
        if (transparentWalls != null)
        {
            foreach (var wall in transparentWalls)
                wall.Init();
        }
    }

    private void Update()
    {
        if (roomGameObject.activeSelf)
        {
            RevealCellsInView();

            // 존 판정은 이동 선분(이전 위치 → 현재 위치) 기준이므로 이전 위치를 함께 추적한다.
            // 플레이어가 꺼져 있으면(포탈/패스트 트래블 연출 중) 추적을 끊어서,
            // 텔레포트 전후 위치가 하나의 이동 선분으로 이어져 가짜 통과 판정이 생기는 것을 막는다
            var curPlayer = GameManager.Instance.CurPlayer;
            if (curPlayer && curPlayer.gameObject.activeSelf)
            {
                Vector2 playerPos = curPlayer.CenterPos.position;
                if (!hasPrevPlayerPos)
                    prevPlayerPos = playerPos;

                UpdateCameraExpandZones(prevPlayerPos, playerPos);
                CheckHiddenAreas(prevPlayerPos, playerPos);
                CheckTransparentWalls(playerPos);

                prevPlayerPos = playerPos;
                hasPrevPlayerPos = true;
            }
            else
            {
                hasPrevPlayerPos = false;
            }
        }
        else
        {
            hasPrevPlayerPos = false;
        }
    }

    // 플레이어가 숨겨진 구역을 지나가는 순간 발견 처리한다
    private void CheckHiddenAreas(Vector2 prevPos, Vector2 curPos)
    {
        if (hiddenAreas == null || hiddenAreas.Length == 0)
            return;

        foreach (var hiddenArea in hiddenAreas)
        {
            if (hiddenArea.CheckDiscover(prevPos, curPos))
            {
                SaveHiddenAreaData();
                GameManager.Instance.SaveGame();
            }
        }
    }

    // 플레이어가 투명벽에 닿아 있는 동안 반투명 처리한다 (순수 연출, 발견 처리는 CheckHiddenAreas가 담당)
    private void CheckTransparentWalls(Vector2 playerPos)
    {
        if (transparentWalls == null || transparentWalls.Length == 0)
            return;

        foreach (var wall in transparentWalls)
            wall.CheckTouch(playerPos);
    }

    // 플레이어가 시야 확장 존 안에 있으면 해당 방향의 카메라 리밋을 서서히 넓히고, 벗어나면 되돌린다
    private void UpdateCameraExpandZones(Vector2 prevPos, Vector2 curPos)
    {
        if (cameraExpandZones == null || cameraExpandZones.Length == 0)
            return;

        // 오프셋이 변하는 동안에만 리밋을 다시 계산한다
        bool changed = false;
        foreach (var zone in cameraExpandZones)
            changed |= zone.UpdateExpand(prevPos, curPos);

        if (!changed)
            return;

        ApplyCameraLimit();
    }

    // 빠른 세이브 이동
    public async void FastTravelMove(string roomId)
    {
       var targetRoom = RoomManager.Instance.TargetRoom(roomId);
        GameManager.Instance.StopPlayer();
        
        StopBGM();
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.BangEffect, GameManager.Instance.CurPlayer.CenterPos.position);
        GameManager.Instance.CurPlayer.gameObject.SetActive(false);
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        if(await GameManager.Instance.Fading(0, 1, 0.25f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        ObjectActive(false);
        GameManager.Instance.CurPlayer.RoomMoveState();
        
        targetRoom.ObjectActive(true);
        targetRoom.SetCameraLimit();
        targetRoom.SetPortal();
        
        // 여기서 몹 소환
        targetRoom.SpawnMonster();
        // 여기서 트랩 데이터 넣기
        targetRoom.SetTrap();
        // 여기서 세이브포인트 데이터 넣기
        targetRoom.SetSavePoint();
        // 여기서 골드오브젝트 액션 넣기
        targetRoom.SetActionGoldObject();
        // 골드오브젝트 초기화
        targetRoom.RefreshGoldObject();
        // 도착한 방의 모든 입구 콜라이더 활성화
        targetRoom.ResetEntranceColliders();

        RoomManager.Instance.CurrentRoom = targetRoom;
        RoomManager.Instance.CurrentRoom.SetGroundVector();

        GameManager.Instance.CurPlayer.transform.position = targetRoom.saveObject.SavePointPos.position;
        
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        if (await GameManager.Instance.Fading(1, 0, 0.25f, true, ConstValues.BlackColor).SuppressCancellationThrow())
            return;
        
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.BangEffect, GameManager.Instance.CurPlayer.CenterPos.position);
        GameManager.Instance.CurPlayer.gameObject.SetActive(true);
        GameManager.Instance.CurPlayer.ForceIdle();
        
        targetRoom.SetBgm(false);
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.GravityChange(ConstValues.BasicGravity);
        GameManager.Instance.MovePlayer();
        GameManager.Instance.CurPlayer.ClearLastPlatform();
        GameManager.Instance.CurPlayer.MyRigidbody.WakeUp();
        
        RoomManager.Instance.ActivePlaceName();
        GameManager.Instance.HidePlaceName();
        if(roomsData.place != targetRoom.roomsData.place)
            GameManager.Instance.RefreshPlaceName();
        
        GameManager.Instance.SavePoint = roomId;
        GameManager.Instance.SaveGame();
    }

    private void OnApplicationQuit()
    {
        SaveVisitedFrameCells();
        SaveVisitedInCells();
        SaveVisitedShortcutCells();
        SaveHiddenAreaData();
    }

    public void ObjectActive(bool active)
    {
        roomGameObject.SetActive(active);
        if (active)
        {
            // 여기서 보스 비활성화
            BossSetting();
            // 여기서 배경 설정
            BgSetting();
        }
    }

    public void AddRoomData()
    {
        var data = GameManager.Instance.RoomInfoList.Find(x => x.roomId == name);
        if(name == "Room_1_13")
            Debug.Log("꽥");
        
        if (data == null)
        {
            RoomInfo room = new RoomInfo();
            room.roomId = name;
            GameManager.Instance.RoomInfoList.Add(room);
            roomInfo = GameManager.Instance.RoomInfoList.Find(x => x.roomId == name);
        }
        else
        {
            roomInfo = data;
        }
        StartMinimap();
    }

    public void AddNpcData()
    {
        foreach (var person in npc)
        {
            person.AddData();
        }
    }
    
    // 세이브 포인트가 없을때만 적용, 1번맵 전용
    public async void FirstStart()
    {
        SetBgm(true);
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos[0].position;
        SetCameraLimit();
        RoomManager.Instance.SetCameraPos();
        
        SetTrap();
        SetSavePoint();
        SetActionGoldObject();
        RefreshGoldObject();
        
        GameManager.Instance.InitProductCancellation();
        if(await WaitUntil(() => !GameManager.Instance.FadeSystem.gameObject.activeSelf, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        if(!firstStart)
            GameManager.Instance.ControlStart = true;
    }
    // 세이브 포인트가 있을때 적용
    public async void SaveStart()
    {
        SetBgm(true);
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = saveObject.SavePointPos.position;
        SetCameraLimit();
        RoomManager.Instance.SetCameraPos();
        
        // 여기서 몹 소환
        SpawnMonster();
        // 여기서 트랩 데이터 넣기
        SetTrap();
        // 여기서 세이브포인트 데이터 넣기
        SetSavePoint();
        // 여기서 골드오브젝트 액션 넣기
        SetActionGoldObject();
        // 골드오브젝트 초기화
        RefreshGoldObject();
        
        GameManager.Instance.InitProductCancellation();
        if(await WaitUntil(() => !GameManager.Instance.FadeSystem.gameObject.activeSelf, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.RefreshPlaceName();
        GameManager.Instance.CurPlayer.MyRigidbody.WakeUp();
    }

    public void SetGroundVector()
    {
        RoomManager.Instance.SetGroundVector();
    }

    public void EntranceSetting()
    {
        if (leftEntrance.Count > 0 && leftRoom.Length > 0)
        {
            for (int i = 0; i < leftEntrance.Count; i++)
            {
                int idx = i;
                for (var j = 0; j < leftRoom[idx].rightRoom.Length; j++)
                {
                    int value = j;
                    if (leftRoom[i].rightRoom[value] == this)
                    {
                        leftEntrance[i].SetAction(() => leftRoom[idx].SettingRoom(value, EntranceDir.Right, this));
                        break;
                    }
                }
            }
        }

        if (rightEntrance.Count > 0 && rightRoom.Length > 0)
        {
            for (int i = 0; i < rightEntrance.Count; i++)
            {
                int idx = i;
                for (var j = 0; j < rightRoom[idx].leftRoom.Length; j++)
                {
                    int value = j;
                    if (rightRoom[i].leftRoom[value] == this)
                    {
                        rightEntrance[i].SetAction(() => rightRoom[idx].SettingRoom(value, EntranceDir.Left, this));
                        break;
                    }
                }
            }
        }
        
        if (upEntrance.Count > 0 && upRoom.Length > 0)
        {
            for (int i = 0; i < upEntrance.Count; i++)
            {
                int idx = i;
                for (var j = 0; j < upRoom[idx].downRoom.Length; j++)
                {
                    int value = j;
                    if (upRoom[i].downRoom[value] == this)
                    {
                        upEntrance[i].SetAction(() => upRoom[idx].SettingRoom(value, EntranceDir.Down, this));
                        break;
                    }
                }
            }
        }
        
        if (downEntrance.Count > 0 && downRoom.Length > 0)
        {
            for (int i = 0; i < downEntrance.Count; i++)
            {
                int idx = i;
                for (var j = 0; j < downRoom[idx].upRoom.Length; j++)
                {
                    int value = j;
                    if (downRoom[i].upRoom[value] == this)
                    {
                        downEntrance[i].SetAction(() => downRoom[idx].SettingRoom(value, EntranceDir.Up, this));
                        break;
                    }
                }
            }
        }
    }

    public void InfoSetting()
    {
        // 저장되는 룸만 불러온다
        roomsData = TableManager.Instance.roomsTable.Rooms.Find(x => x.id == name);
        if (roomsData == null)
            return;
        
        // 연출 트리거 세팅
        var productIdxArray = roomsData.productIdx.Split(';');
        List<int> productIdxList = new List<int>();
        foreach (var productIdx in productIdxArray)
            productIdxList.Add(int.Parse(productIdx));
        if (roomInfo.roomProduct.Count < productTriggers.Length)
        {
            foreach (var productIdx in productIdxList)
            {
                RoomProduct roomProduct = new RoomProduct();
                roomProduct.idx = productIdx;
                roomProduct.count = 0;
                roomProduct.isFinish = false;
                roomInfo.roomProduct.Add(roomProduct);
            }
        }
        else if (roomInfo.roomProduct.Count > productTriggers.Length)
        {
            roomInfo.roomProduct.Clear();
            GameManager.Instance.SaveGame();
        }
        // 연출 액션 넣기
        for (var i = 0; i < productTriggers.Length; i++)
        {
            int idx = i;
            productTriggers[i].SetAction(()=> 
            {
                ProductAction(productIdxList[idx]);
            });
        }
        
        // 이벤트가 있는 Npc세팅
        var eventNpcArray = roomsData.npc.Split('ㅗ');
        if (roomInfo.eventNpc.Count < eventNpcArray.Length)
        {
            if (!string.IsNullOrWhiteSpace(eventNpcArray[0]))
            {
                for (int i = 0; i < eventNpcArray.Length; i++)
                {
                    var npcArray = eventNpcArray[i].Split(';');
                    var npcId = npcArray[0];
                    var npcActive = bool.Parse(npcArray[1]);
                    
                    var eventNpc = new EventNpc()
                    {
                        id = npcId,
                        isActive = npcActive, 
                    };
                    roomInfo.eventNpc.Add(eventNpc);
                }
                GameManager.Instance.SaveGame();
            }
        }
        
        // 커스텀 오브젝트 세팅
        var customObjectArray = roomsData.customObject.Split('ㅗ');
        if (roomInfo.customObject.Count < customObjectArray.Length)
        {
            if (!string.IsNullOrWhiteSpace(customObjectArray[0]))
            {
                for (int i = 0; i < customObjectArray.Length; i++)
                {
                    var objectArray = customObjectArray[i].Split(';');
                    var objectId = objectArray[0];
                    var obejectActive = bool.Parse(objectArray[1]);
                    
                    var customObject = new EventCustomObject()
                    {
                        id = objectId,
                        isActive = obejectActive, 
                    };
                    roomInfo.customObject.Add(customObject);
                }
                GameManager.Instance.SaveGame();
            }
        }

        // 맵에 있는 숏컷 세팅
        if (roomInfo.shortCut.Count < shortCutObjects.Length)
        {
            foreach (var shortcutObject in shortCutObjects)
            {
                ShortCut addShortcut = new ShortCut();
                addShortcut.id = shortcutObject.name;
                addShortcut.type = shortcutObject.TypeString;
                addShortcut.isOpened = false;
                roomInfo.shortCut.Add(addShortcut);
            }
            GameManager.Instance.SaveGame();
        }
        else if (shortCutObjects.Length == roomInfo.shortCut.Count)
        {
            for (int i = 0; i < roomInfo.shortCut.Count; i++)
            {
                if (roomInfo != null)
                {
                    if (roomInfo != null && roomInfo.shortCut[i].id != shortCutObjects[i].name)
                        roomInfo.shortCut[i].id = shortCutObjects[i].name;
                    
                    if (roomInfo.shortCut != null && roomInfo.shortCut[i].type != shortCutObjects[i].Type.ToString())
                        roomInfo.shortCut[i].type = shortCutObjects[i].Type.ToString();
                }
            }
        }
        else if (shortCutObjects.Length < roomInfo.shortCut.Count)
        {
            var newList = new List<ShortCut>();
            foreach (var shortCut in shortCutObjects)
            {
                var shortClass = roomInfo.shortCut.Find(x => x.id == shortCut.name);
                if(shortClass != null)
                    newList.Add(shortClass);
            }
            roomInfo.shortCut = newList;

            GameManager.Instance.SaveGame();
        }

        // 맵에 널려있는 스킬 세팅
        var skillArray = roomsData.skill.Split(';');
        if (roomInfo.skillAndPassive.Count < skillArray.Length)
        {
            if (!string.IsNullOrWhiteSpace(skillArray[0]))
            {
                roomInfo.skillAndPassive.Clear();
                for (var i = 0; i < roomSkillAndPassive.Length; i++)
                {
                    var skillAndPassive = new RoomObjectClass()
                    {
                        id = skillArray[i],
                        alreadyGet = false,
                    };
                    roomInfo.skillAndPassive.Add(skillAndPassive);
                }
                GameManager.Instance.SaveGame();
            }
        }
        else if (skillArray.Length < roomInfo.skillAndPassive.Count)
        {
            roomInfo.skillAndPassive.Clear();
            GameManager.Instance.SaveGame();
        }
        
        // 맵에 널려있는 보물상자 세팅
        var treasureBoxArray = roomsData.treasureBox.Split('ㅗ');
        if (roomInfo.treasureBox.Count < treasureBoxArray.Length)
        {
            if (!string.IsNullOrWhiteSpace(treasureBoxArray[0]))
            {
                for (int i = 0; i < treasureBoxArray.Length; i++)
                {
                    var treasureArray = treasureBoxArray[i].Split(';');
                    var treasure = treasureArray[0];
                    var treasureCount = int.Parse(treasureArray[1]);
                    
                    var treasureBox = new RoomObjectClass()
                    {
                        id = treasure,
                        count = treasureCount,
                        alreadyGet = false, 
                    };
                    roomInfo.treasureBox.Add(treasureBox);
                }
                GameManager.Instance.SaveGame();
            }
        }
        else if (treasureBoxArray.Length < roomInfo.treasureBox.Count)
        {
            roomInfo.treasureBox.Clear();
            GameManager.Instance.SaveGame();
        }
        
        // 맵에 있는 오브젝트들(아이템, 유물, 특성, 포션 등) 설정
        // 특성 설정
        var attributeList = roomObjects.ToList().FindAll(x => x.RoomObjectType == ERoomObjectType.AttributePoint);
        if (roomInfo.attributePoint.Count < attributeList.Count)
        {
            for (int i = 0; i < attributeList.Count; i++)
            {
                var attributePoint = new RoomObjectClass()
                {
                    id = ConstValues.AttributePoint,
                    count = roomsData.attributePoint,
                    alreadyGet = false, 
                };
                roomInfo.attributePoint.Add(attributePoint);
            }
            GameManager.Instance.SaveGame();
        }
        else if (roomInfo.attributePoint.Count > attributeList.Count)
        {
            roomInfo.attributePoint.Clear();
            GameManager.Instance.SaveGame();
        }
        
        // 포션 설정
        var potionList = roomObjects.ToList().FindAll(x => x.RoomObjectType == ERoomObjectType.Potion);
        if (roomInfo.potion.Count < potionList.Count)
        {
            for (int i = 0; i < potionList.Count; i++)
            {
                var potion = new RoomObjectClass()
                {
                    id = ConstValues.Potion,
                    count = roomsData.potion,
                    alreadyGet = false, 
                };
                roomInfo.potion.Add(potion);
            }
            GameManager.Instance.SaveGame();
        }
        else if (roomInfo.potion.Count > potionList.Count)
        {
            roomInfo.potion.Clear();
            GameManager.Instance.SaveGame();
        }

        // 아이템 설정
        if (!string.IsNullOrWhiteSpace(roomsData.item))
        {
            var itemArray = roomsData.item.Split('ㅗ');
            if (roomInfo.item.Count < itemArray.Length)
            {
                for (int i = 0; i < itemArray.Length; i++)
                {
                    var itemInfoArray = itemArray[i].Split(';');
                    var itemName = itemInfoArray[0];
                    var itemCount = int.Parse(itemInfoArray[1]);
                    
                    var item = new RoomObjectClass()
                    {
                        id = itemName,
                        count = itemCount,
                        alreadyGet = false,
                    };
                    roomInfo.item.Add(item);
                }
                GameManager.Instance.SaveGame();
            }
            else if (roomInfo.item.Count > itemArray.Length)
            {
                roomInfo.item.Clear();
                GameManager.Instance.SaveGame();
            }
            // 수가 같을 때
            else  if (roomInfo.item.Count == itemArray.Length)
            {
                if (roomInfo.item.Count > 0 && itemArray.Length > 0)
                {
                    var itemInfoArray = itemArray[0].Split(';');
                    if (roomInfo.item[0].id != itemInfoArray[0])
                    {
                        roomInfo.item[0].id = itemInfoArray[0];
                        roomInfo.item[0].alreadyGet = false;
                        GameManager.Instance.SaveGame();
                    }
                }
            }
            
            // 해당 아이템을 이미 가지고 있을 때는 비활성화
            bool isHaveItem = false;
            foreach (var haveItem in GameManager.Instance.ItemList)
            {
                foreach (var item in roomInfo.item)
                {
                    if (haveItem.id != item.id)
                        continue;
                    
                    item.alreadyGet = true;
                    isHaveItem = true;
                }
            }
            if(isHaveItem)
                GameManager.Instance.SaveGame();
        }
        
         // 유물 설정
        if (!string.IsNullOrWhiteSpace(roomsData.relic))
        {
            var relicArray = roomsData.relic.Split('ㅗ');
            if (roomInfo.relic.Count < relicArray.Length)
            {
                for (int i = 0; i < relicArray.Length; i++)
                {
                    var relicInfoArray = relicArray[i].Split(';');
                    var relicName = relicInfoArray[0];
                    var relicCount = int.Parse(relicInfoArray[1]);
                    
                    var relic = new RoomObjectClass()
                    {
                        id = relicName,
                        count = relicCount,
                        alreadyGet = false,
                    };
                    roomInfo.relic.Add(relic);
                }
                GameManager.Instance.SaveGame();
            }
            else if (roomInfo.relic.Count > relicArray.Length)
            {
                roomInfo.relic.Clear();
                GameManager.Instance.SaveGame();
            }
            // 수가 같을 때
            else  if (roomInfo.relic.Count == relicArray.Length)
            {
                if (roomInfo.relic.Count > 0 && relicArray.Length > 0)
                {
                    var relicInfoArray = relicArray[0].Split(';');
                    if (roomInfo.relic[0].id != relicInfoArray[0])
                    {
                        roomInfo.relic[0].id = relicInfoArray[0];
                        roomInfo.relic[0].alreadyGet = false;
                        GameManager.Instance.SaveGame();
                    }
                }
            }
            
            // 해당 유물을 이미 가지고 있을 때는 비활성화
            bool isHaveRelic = false;
            foreach (var haveRelic in GameManager.Instance.RelicList)
            {
                foreach (var relic in roomInfo.relic)
                {
                    if (haveRelic != relic.id)
                        continue;
                    
                    relic.alreadyGet = true;
                    isHaveRelic = true;
                }
            }
            if(isHaveRelic)
                GameManager.Instance.SaveGame();
        }

        // 엘리베이터 설정
        if (roomInfo.elevators.Count < elevators.Length)
        {
            for (int i = 0; i < elevators.Length; i++)
            {
                int idx = i;

                var elevator = new ElevatorData()
                {
                    id = elevators[idx].name,
                    idx = elevators[idx].StartIndex,   // 최초 시작 인덱스 (엘리베이터별 설정)
                    startIdx = elevators[idx].StartIndex
                };
                roomInfo.elevators.Add(elevator);
            }
            GameManager.Instance.SaveGame();
        }
        else if (elevators.Length < roomInfo.elevators.Count)
        {
            roomInfo.elevators.Clear();
            GameManager.Instance.SaveGame();
        }

        // 잠긴 문 설정
        if (roomInfo.lockDoors.Count < lockDoors.Length)
        {
            for (int i = 0; i < lockDoors.Length; i++)
            {
                int idx = i;

                var door = new LockDoorData()
                {
                    id = lockDoors[idx].name,
                    isOpen = false
                };
                roomInfo.lockDoors.Add(door);
            }
            GameManager.Instance.SaveGame();
        }
        else if (lockDoors.Length < roomInfo.lockDoors.Count)
        {
            roomInfo.lockDoors.Clear();
            GameManager.Instance.SaveGame();
        }
        
        // 연출이 끝났다면, 연출 트리거를 제거
        for (int i = 0; i < roomInfo.roomProduct.Count; i++)
            productTriggers[i].gameObject.SetActive(!roomInfo.roomProduct[i].isFinish);

        // 여기서 npc 활성화
        foreach (var person in npc)
        {
            var targetNpc = roomInfo.eventNpc.Find(x => x.id == person.name);
            if (targetNpc != null)
                person.gameObject.SetActive(targetNpc.isActive);
            
            person.SetInteractionAction();
            person.SetSelectAction();
            person.SetStartTalkAction();
            person.SetAnotherNpc(npc);
        }
        
        // 여기서 커스텀 오브젝트 활성화
        foreach (var customObject in customObjects)
        {
            var targetObject = roomInfo.customObject.Find(x => x.id == customObject.name);
            if (customObject != null)
                customObject.gameObject.SetActive(targetObject.isActive);
        }

        // 스킬 및 패시브를 획득했으면, 나오지 않게 조정
        for (var i = 0; i < roomSkillAndPassive.Length; i++)
        {
            int idx = i;
            
            roomSkillAndPassive[i].SetSprite(roomInfo.skillAndPassive[idx].id, roomInfo.skillAndPassive[idx].alreadyGet);
            if (!roomInfo.skillAndPassive[idx].alreadyGet)
            {
                roomSkillAndPassive[i].SetAction(() =>
                {
                    roomInfo.skillAndPassive[idx].alreadyGet = true;
                    GameManager.Instance.AddNewSkill(roomInfo.skillAndPassive[idx].id);
                    GameManager.Instance.GetSkillProduct(roomInfo.skillAndPassive[idx].id, GetSkillEvent);
                    GameManager.Instance.SaveGame();
                });
            }
        }
        
        // 보물상자를 열었으면, 열린 상태로 나오게 조정
        for (var i = 0; i < roomTreasureBox.Length; i++)
        {
            int idx = i;
            roomTreasureBox[i].SetInteractionAction();

            if (roomInfo.treasureBox[idx].alreadyGet)
            {
                roomTreasureBox[i].IsOpen = true;
            }
            else
            {
                roomTreasureBox[i].SetAction(() =>
                {
                    roomTreasureBox[idx].OpenProduct();
                    roomTreasureBox[idx].ReduceInteractionObject();
                    roomInfo.treasureBox[idx].alreadyGet = true;
                    GetTreasureBoxItem(roomInfo.treasureBox[idx].id, roomInfo.treasureBox[idx].count, roomTreasureBox[idx].transform.position);
                    
                    switch (roomInfo.treasureBox[idx].id)
                    {
                        case ConstValues.Gold:
                            GameManager.Instance.GetGoldProduct(roomInfo.treasureBox[idx].count, roomTreasureBox[idx].transform.position, GetGoldEvent);
                            break;
                    }

                    GameManager.Instance.SaveGame();
                });
            }
        }
        
        // 특성 포인트를 얻었다면, 비활성화
        for (var i = 0; i < attributeList.Count; i++)
        {
            int idx = i;

            if (roomInfo.attributePoint[idx].alreadyGet)
            {
                attributeList[idx].gameObject.SetActive(false);
            }
            else
            {
                attributeList[idx].SetInteractionAction();
                attributeList[idx].SetAction(() =>
                {
                    attributeList[idx].IsGet = true;
                    roomInfo.attributePoint[idx].alreadyGet = true;
                    attributeList[idx].ReduceInteractionObject();
                    attributeList[idx].SpawnEffect(ConstValues.GetAttributeEffect, attributeList[idx].transform.position);
                    GameManager.Instance.PlusAttributePoint(roomInfo.attributePoint[idx].count);
                    GameManager.Instance.GetAttributeProduct(roomInfo.attributePoint[idx].count, GetAttributeEvent);
                    GameManager.Instance.SaveGame();
                });
            }
        }
        
        // 포션을 얻었다면, 비활성화
        for (var i = 0; i < potionList.Count; i++)
        {
            int idx = i;
        
            if (roomInfo.potion[idx].alreadyGet)
            {
                potionList[idx].gameObject.SetActive(false);
            }
            else
            {
                potionList[idx].SetInteractionAction();
                potionList[idx].SetAction(() =>
                {
                    potionList[idx].IsGet = true;
                    roomInfo.potion[idx].alreadyGet = true;
                    potionList[idx].ReduceInteractionObject();
                    potionList[idx].SpawnEffect(ConstValues.GetPotionEffect, potionList[idx].transform.position);
                    GameManager.Instance.PlusPotion();
                    GameManager.Instance.GetPotionProduct(GetPotionEvent);
                    GameManager.Instance.SaveGame();
                });
            }
        }
        
        // 아이템을 얻었으면, 그 아이템은 비활성화
        var itemList = roomObjects.ToList().FindAll(x => x.RoomObjectType == ERoomObjectType.Item);
        for (var i = 0; i < itemList.Count; i++)
        {
            int idx = i;
            
            if (roomInfo.item[idx].alreadyGet)
            {
                itemList[idx].gameObject.SetActive(false);
            }
            else
            {
                itemList[idx].SetInteractionAction();
                itemList[idx].SetSprite(roomInfo.item[idx].id);
                itemList[idx].SetAction(() =>
                {
                    itemList[idx].IsGet = true;
                    roomInfo.item[idx].alreadyGet = true;
                    itemList[idx].ReduceInteractionObject();
                    itemList[idx].SpawnEffect(ConstValues.GetItemEffect, itemList[idx].transform.position);
                    switch (roomInfo.item[idx].id)
                    {
                        case ConstValues.SaveTravel:
                            GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30218)).Forget();
                            break;
                    }
                    GameManager.Instance.GetItem(roomInfo.item[idx].id, roomInfo.item[idx].count);
                    GameManager.Instance.ProductObjectInfo(roomInfo.item[idx].id, GameManager.Instance.GetItemTalk(roomInfo.item[idx].id), roomInfo.item[idx].count);
                    GameManager.Instance.SaveGame();
                });
            }
        }
        
        // 유물을 얻었으면, 그 유물은 비활성화
        var relicList = roomObjects.ToList().FindAll(x => x.RoomObjectType == ERoomObjectType.Relic);
        for (var i = 0; i < relicList.Count; i++)
        {
            int idx = i;

            if (roomInfo.relic[idx].alreadyGet)
            {
                relicList[idx].gameObject.SetActive(false);
            }
            else
            {
                relicList[idx].SetInteractionAction();
                relicList[idx].SetSprite(roomInfo.relic[idx].id);
                relicList[idx].SetAction(() =>
                {
                    relicList[idx].IsGet = true;
                    roomInfo.relic[idx].alreadyGet = true;
                    relicList[idx].ReduceInteractionObject();
                    relicList[idx].SpawnEffect(ConstValues.GetRelicEffect, relicList[idx].transform.position);
                    GameManager.Instance.GetRelic(roomInfo.relic[idx].id);
                    GameManager.Instance.GetRelicProduct(roomInfo.relic[idx].id, GetRelicEvent);
                    GameManager.Instance.ProductObjectInfo(roomInfo.relic[idx].id, GameManager.Instance.GetItemTalk(roomInfo.relic[idx].id), roomInfo.relic[idx].count);
                    GameManager.Instance.SaveGame();
                });
            }
        }

        // 엘리베이터
        for (int i = 0; i < roomInfo.elevators.Count; i++)
        {
            int idx = i;

            // 기존 세이브 보정 + 프리팹의 startIndex 변경 반영: 시작 인덱스를 항상 최신으로 유지
            roomInfo.elevators[idx].startIdx = elevators[i].StartIndex;

            elevators[i].SetUpDown(roomInfo.elevators[idx].idx);
            elevators[i].PosSetting();
            elevators[i].SetInteractionAction();
            elevators[i].SetLeverAction();
            
            // 출발
            elevators[i].SetAction(() =>
            {
                elevators[idx].ReduceInteractionObject();
            });
            
            // 도착
            elevators[i].SetSaveAction(() =>
            {
                roomInfo.elevators[idx].idx = elevators[idx].TargetIdx;
                elevators[idx].MovingStop();
            });
        }

        // 잠긴 문
        for (int i = 0; i < roomInfo.lockDoors.Count; i++)
        {
            int idx = i;
            lockDoors[i].SetOpen(roomInfo.lockDoors[idx].isOpen);

            if (lockDoors[i].IsOpen)
            {
                lockDoors[i].DeleteDoor();
            }
            else
            {
                lockDoors[i].SetOpenProduct(() =>
                {
                    OpenDoor(idx, lockDoors[idx].KeyId, () => lockDoors[idx].OpenAction());
                });
                lockDoors[i].SetOpenAction(() =>
                {
                    // 열쇠를 가지고 있는 경우
                    if (GameManager.Instance.IsHaveItem(lockDoors[idx].KeyId))
                    {
                        lockDoors[idx].OpenDoor();
                    }
                    // 열쇠가 없는 경우
                    else
                    {
                        lockDoors[idx].LockMessage();
                    }
                });
                lockDoors[i].SetInteractionAction();
            }
        }
        
        // 가이드 오브젝트
        foreach (var guideObject in guideObjects)
            guideObject.Setting();
    }

    // 번역 직후 토크
    public void RefreshTalk()
    {
        // 여기서 npc 활성화
        foreach (var person in npc)
            person.RefreshTalkText();

        // 아이템을 얻었으면, 그 아이템은 비활성화
        foreach (var roomObject in roomObjects)
            roomObject.RefreshTalkText();

        // 보물상자를 열었으면, 열린 상태로 나오게 조정
        foreach (var treasureBox in roomTreasureBox)
            treasureBox.RefreshTalkText();

        // 엘리베이터
        for (int i = 0; i < roomInfo.elevators.Count; i++)
            elevators[i].RefreshTalkText();

        // 잠긴 문
        for (int i = 0; i < roomInfo.lockDoors.Count; i++)
            lockDoors[i].RefreshTalkText();
        
        // 포탈
        if (portalObject)
            portalObject.RefreshTalkText();
        
        // 세이브 오브젝트
        if(saveObject)
            saveObject.RefreshTalkText();
        
        // 가이드 오브젝트
        foreach (var guideObject in guideObjects)
            guideObject.Setting();
    }

    // 키설정 직후 키
    public void RefreshKey()
    {
        foreach (var person in npc)
            person.RefreshKeyText(GameManager.Instance.upKey);
        
        foreach (var roomObject in roomObjects)
            roomObject.RefreshKeyText(GameManager.Instance.upKey);
        
        foreach (var treasureBox in roomTreasureBox)
            treasureBox.RefreshKeyText(GameManager.Instance.upKey);
        
        for (int i = 0; i < roomInfo.elevators.Count; i++)
            elevators[i].RefreshKeyText(GameManager.Instance.upKey);

        // 잠긴 문
        for (int i = 0; i < roomInfo.lockDoors.Count; i++)
            lockDoors[i].RefreshKeyText(GameManager.Instance.upKey);
        
        // 포탈
        if (portalObject)
            portalObject.RefreshKeyText(GameManager.Instance.upKey);
        
        // 세이브 오브젝트
        if(saveObject)
            saveObject.RefreshKeyText(GameManager.Instance.upKey);
        
        // 가이드 오브젝트
        foreach (var guideObject in guideObjects)
            guideObject.Setting();
    }
    
    public void MonsterPosSetting()
    {
        if (firstMonsterPosList.Count > 0)
            return;
        
        foreach (var monster in monsters)
        {
            firstMonsterPosList.Add(monster.transform.position);
        }
    }

    // 보스가 죽을때, 모든 몬스터들도 같이 죽는 기능때문에 필요함
    public void BossPosSetting()
    {
        if (firstBossPosList.Count > 0)
            return;
        
        foreach (var boss in bosses)
        {
            firstBossPosList.Add(boss.transform.position);
        }
    }

    private bool AllMonsterDead()
    {
        foreach (var monster in monsters)
        {
            if (!monster.IsDie)
                return false;
        }
        return true;
    }

    public async void MovePortal(string roomId)
    {
        var targetRoom = RoomManager.Instance.TargetRoom(roomId);
        portalObject.SoundActive(false);
        GameManager.Instance.StopPlayer();

        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.BangEffect, GameManager.Instance.CurPlayer.CenterPos.position);
        GameManager.Instance.CurPlayer.gameObject.SetActive(false);
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        if(await GameManager.Instance.Fading(0, 1, 0.25f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        ObjectActive(false);
        GameManager.Instance.CurPlayer.RoomMoveState();
        
        targetRoom.ObjectActive(true);
        targetRoom.SetCameraLimit();
        targetRoom.SetPortal();
        targetRoom.PortalSoundActive(false);
        // 도착한 방의 모든 입구 콜라이더 활성화
        targetRoom.ResetEntranceColliders();

        RoomManager.Instance.CurrentRoom = targetRoom;
        RoomManager.Instance.CurrentRoom.SetGroundVector();

        GameManager.Instance.CurPlayer.transform.position = targetRoom.portalObject.PlayerPos.position;

        GameManager.Instance.InitProductCancellation();
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        if (await GameManager.Instance.Fading(1, 0, 0.25f, true, ConstValues.BlackColor).SuppressCancellationThrow())
            return;
        
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.BangEffect, GameManager.Instance.CurPlayer.CenterPos.position);
        GameManager.Instance.CurPlayer.gameObject.SetActive(true);
        GameManager.Instance.CurPlayer.ForceIdle();
        
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        targetRoom.PortalSoundActive(true);
        GameManager.Instance.CurPlayer.GravityChange(ConstValues.BasicGravity);
        GameManager.Instance.MovePlayer();
        GameManager.Instance.CurPlayer.ClearLastPlatform();
        GameManager.Instance.CurPlayer.MyRigidbody.WakeUp();
        
        RoomManager.Instance.ActivePlaceName();
        GameManager.Instance.HidePlaceName();
        if(roomsData.place != targetRoom.roomsData.place)
            GameManager.Instance.RefreshPlaceName();
    }

    private async void SettingRoom(int idx, EntranceDir dir, Room pastRoom)
    {
        // 좌우 이동은 퇴장 걷기가 곧바로 속도를 이어받으므로, StopPlayer의 속도 정리(멈칫)를 한 번만 건너뛴다.
        bool walkOver = dir is EntranceDir.Left or EntranceDir.Right;
        if (walkOver)
            GameManager.Instance.RoomMoving = true;
        GameManager.Instance.StopPlayer();
        if (walkOver)
            GameManager.Instance.RoomMoving = false;
        GameManager.Instance.RoomMoveSetting();
        GameManager.Instance.InitProductCancellation();

        // 모든 몬스터들의 행동 정지
        foreach (var monster in monsters)
            monster.CancelMotion();

        // 퇴장 연출: 페이드 아웃 전, 과거 방에서 진행 방향으로 더 걸어 나간다.
        // dir은 새 방의 입구 방향이라 실제 진행 방향은 그 반대다.
        // dir==Left  ← 오른쪽 문으로 진입 → 진행 방향 오른쪽(+)
        // dir==Right ← 왼쪽   문으로 진입 → 진행 방향 왼쪽(-)
        var curPlayer = GameManager.Instance.CurPlayer;
        if (curPlayer.NormalState is ENormalState.Idle or ENormalState.Move)
        {
            switch (dir)
            {
                case EntranceDir.Left:
                    curPlayer.EntranceWalk_X(curPlayer.transform.position.x + ExitWalkOffsetX).Forget();
                    break;
                case EntranceDir.Right:
                    curPlayer.EntranceWalk_X(curPlayer.transform.position.x - ExitWalkOffsetX).Forget();
                    break;
            }
        }
        
        if(await GameManager.Instance.Fading(0, 1, 0.1f, false, ConstValues.BlackColor, false).SuppressCancellationThrow())
            return;

        switch (dir)
        {
            case EntranceDir.Left:
                SetLeftPlayerPos(idx);
                // 점프/스킬로 공중 진입 시, 검은 화면 동안 잔여 속도/중력으로 시작 위치에서 떠내려가지 않도록 고정.
                // (중력은 페이드 후 GravityChange(BasicGravity)에서 복원됨)
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Right:
                SetRightPlayerPos(idx);
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Up:
                SetUpPlayerPos(idx);
                GameManager.Instance.CurPlayer.SetJumpState();
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                break;
            case EntranceDir.Down:
                SetDownPlayerPos(idx);
                GameManager.Instance.CurPlayer.ForceJump();
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                break;
        }
        GameManager.Instance.CurPlayer.RoomMoveState();
        var playerPos = GameManager.Instance.CurPlayer.transform.position;
        GameManager.Instance.MainCamera.SetPos(playerPos);

        SetCameraLimit();
        pastRoom.ObjectActive(false);
        ObjectActive(true);
        RoomManager.Instance.CurrentRoom = this;
        RoomManager.Instance.CurrentRoom.SetGroundVector();
        
        // 여기서 몹 소환
        SpawnMonster();
        // 여기서 트랩 데이터 넣기
        SetTrap();
        // 여기서 세이브포인트 데이터 넣기
        SetSavePoint();
        // 여기서 포탈 데이터 넣기
        SetPortal();
        // 여기서 골드오브젝트 액션 넣기
        SetActionGoldObject();
        // 골드오브젝트 초기화
        RefreshGoldObject();
        
        if (await GameManager.Instance.NormalDelay(0.1f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        GameManager.Instance.CurPlayer.GravityChange(ConstValues.BasicGravity);
        GameManager.Instance.CurPlayer.ZeroVelocity();
        GameManager.Instance.Fading(1, 0, 0.1f, true, ConstValues.BlackColor, false).Forget();
        
        switch (dir)
        {
            case EntranceDir.Up:
                await GameManager.Instance.CurPlayer.EntranceDown();
                break;

            case EntranceDir.Down:
                await GameManager.Instance.CurPlayer.EntranceJump();
                break;

            // 입장 연출: 도착위치(playerPos[idx])까지 걸어 들어온다.
            case EntranceDir.Left:
                await GameManager.Instance.CurPlayer.EntranceWalk_X(leftPlayerPos[idx].position.x);
                break;

            case EntranceDir.Right:
                await GameManager.Instance.CurPlayer.EntranceWalk_X(rightPlayerPos[idx].position.x);
                break;
        }
        GameManager.Instance.MovePlayer();
        GameManager.Instance.CurPlayer.ClearLastPlatform();
        
        ResetEntranceColliders();

        // 여기서 BGM재생
        SetBgm(false);
        RoomManager.Instance.ActivePlaceName();
        GameManager.Instance.HidePlaceName();
        if(pastRoom.roomsData.place != roomsData.place)
            GameManager.Instance.RefreshPlaceName();
    }
    
    // 방의 모든 입구 콜라이더를 다시 활성화
    public void ResetEntranceColliders()
    {
        foreach (var entrance in leftEntrance)
            entrance.ResetCollider();
        foreach (var entrance in rightEntrance)
            entrance.ResetCollider();
        foreach (var entrance in upEntrance)
            entrance.ResetCollider();
        foreach (var entrance in downEntrance)
            entrance.ResetCollider();
    }

    private void SetLeftPlayerPos(int idx)
    {
        // GameManager.Instance.CurPlayer.transform.position = leftPlayerPos[idx].position;
        // 입장 걷기 시작점: 도착위치에서 바깥(왼쪽)으로 떨어뜨려 두고, 이후 도착위치까지 걸어 들어온다.
        var pos = leftPlayerPos[idx].position;
        pos.x -= EnterWalkOffsetX;
        GameManager.Instance.CurPlayer.transform.position = pos;
    }

    private void SetRightPlayerPos(int idx)
    {
        // GameManager.Instance.CurPlayer.transform.position = rightPlayerPos[idx].position;
        // 입장 걷기 시작점: 도착위치에서 바깥(오른쪽)으로 떨어뜨려 두고, 이후 도착위치까지 걸어 들어온다.
        var pos = rightPlayerPos[idx].position;
        pos.x += EnterWalkOffsetX;
        GameManager.Instance.CurPlayer.transform.position = pos;
    }

    private void SetUpPlayerPos(int idx)
    {
        GameManager.Instance.CurPlayer.transform.position = upPlayerPos[idx].position;
    }

    private void SetDownPlayerPos(int idx)
    {
        GameManager.Instance.CurPlayer.transform.position = downPlayerPos[idx].position;
    }

    private void SetCameraLimit()
    {
        var maxLimit = new Vector2(maxCameraLimitX.position.x, maxCameraLimitY.position.y);
        var minLimit = new Vector2(minCameraLimitX.position.x, minCameraLimitY.position.y);
        
        firstMaxLimit = maxLimit;
        firstMinLimit = minLimit;

        // 확장 존 상태는 초기화하지 않는다. 확장된 채 방을 나갔다 들어와도
        // 복귀 존을 지나기 전까지는 넓혀진 시야가 유지된다
        ApplyCameraLimit();
    }

    // 시야 확장 존의 현재 오프셋을 반영해 카메라 리밋을 적용
    private void ApplyCameraLimit()
    {
        var maxLimit = firstMaxLimit;
        var minLimit = firstMinLimit;
        if (cameraExpandZones != null)
        {
            foreach (var zone in cameraExpandZones)
            {
                minLimit.x -= zone.LeftOffset;
                maxLimit.x += zone.RightOffset;
            }
        }
        GameManager.Instance.MainCamera.SetCameraLimit(maxLimit, minLimit);
    }

    public float SetCenterX()
    {
        return (maxCameraLimitX.position.x + minCameraLimitX.position.x) / 2;
    }
    public float SetCenterY()
    {
        return (maxCameraLimitY.position.y + minCameraLimitY.position.y) / 2;
    }

    private void BossTileMapActive(bool active)
    {
        if (active)
        {
            if (bossTilemap)
            {
                bossTilemap.gameObject.SetActive(true);
                bossTilemap.Build();
            }
        }
        else
        {
            if (bossTilemap)
            {
                bossTilemap.gameObject.SetActive(false);
                bossTilemap.Crash();
            }
        }
    }

    private async UniTask WaitUntil(Func<bool> condition, CancellationTokenSource tokenSource)
    {
        await UniTask.WaitUntil(condition, cancellationToken: tokenSource.Token);
    }

    private void GetTreasureBoxItem(string id, int count, Vector2 itemVector)
    {
        switch (id)
        {
            case ConstValues.Gold:
                PlusGold(count, itemVector);
                break;
        }
    }
    
    private void PlusGold(int gold, Vector2 goldPos)
    {
        if (gold == 0)
            return;
        
        GameManager.Instance.Gold += gold;
        // 골드가 날아가는 연출
        var followGold = GameManager.Instance.SpawnToObjectPool(ConstValues.FollowGold, goldPos).GetComponent<FollowGold>();
        followGold.SetAction(() =>
        {
            GameManager.Instance.GetGold(gold, GameManager.Instance.Gold);
        });
    }

    // 숏컷정보 저장
    public void ShortcutOpen(string id, bool isSave)
    {
        var targetShortcut = roomInfo.shortCut.Find(x => x.id == id);
        
        if (targetShortcut == null)
            return;
        
        targetShortcut.isOpened = true;
        
        if(isSave)
            GameManager.Instance.SaveGame();
        
        foreach (var shortCut in shortCutObjects)
        {
            if (shortCut.name == id)
            {
                shortCut.OpenProduct();
                break;
            }
        }
        //SetShortCut();
    }

    private async void SpawnMonster(bool isExplosion = true)
    {
        for (var i = 0; i < monsters.Length; i++)
        {
            if (!monsters[i].IsDie)
            {
                monsters[i].transform.position = firstMonsterPosList[i];
                monsters[i].IsExplosion = isExplosion;
                monsters[i].LimitLeft = monsterLimitLeft.position.x;
                monsters[i].LimitRight = monsterLimitRight.position.x;
                monsters[i].SetGoldAction(PlusGold);
                monsters[i].gameObject.SetActive(true);
                monsters[i].ForceIdle();
                monsters[i].AllBuffCancel();
                monsters[i].SetSortingGroup(-2 - i);
            }
        }
        
        await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
        for (var i = 0; i < monsters.Length; i++)
        {
            if (!monsters[i].IsDie)
                monsters[i].MonsterAwake();
        }
    }
    public void AllMonsterArrive()
    {
        foreach (var monster in monsters)
        {
            if (!monster.IsBoss)
            {
                monster.IsDie = false;
            }
        }
    }

    private void SetTrap()
    {
        foreach (var trap in trapList)
            GameManager.Instance.InputDataTrap(trap.name, trap);
    }

    // 미니맵에 나타나는 오브젝트 관리
    public void SetMinimapObject()
    {
        int idx = 0;
        for (int i = 0; i < shortCutObjects.Length; i++)
        {
            if (shortCutObjects[i].GetComponent<Shortcut_Crush>())
            {
                if(shortCutRoom.Length > 0)
                {
                    var crush = shortCutObjects[i].GetComponent<Shortcut_Crush>();
                    crush.TargetRoom = shortCutRoom[idx];
                    idx += 1;
                }
            }
        }
        for (int i = 0; i < shortCutObjects.Length; i++)
            shortCutObjects[i].OpenSetting(roomInfo.shortCut[i].isOpened, ShortcutOpen);
        
        if (saveObject)
        {
            saveObject.SetParents(transform);
            // 저장 데이터 있음: 세이브 오브젝트 미니맵 활성화 / 저장 데이터 없음: 세이브 오브젝트 미니맵 비활성화
            saveObject.MinimapObject.SetActive(roomInfo.IsRevealed(EMinimapObjectType.SavePoint));
        }
        if (portalObject)
        {
            portalObject.SetParents(transform);
            // 저장 데이터 있음: 세이브 오브젝트 미니맵 활성화 / 저장 데이터 없음: 세이브 오브젝트 미니맵 비활성화
            portalObject.MinimapObject.SetActive(roomInfo.IsRevealed(EMinimapObjectType.Portal));
        }
        if (merchantObject)
        {
            merchantObject.SetParents(transform);
            // 저장 데이터 있음: 세이브 오브젝트 미니맵 활성화 / 저장 데이터 없음: 세이브 오브젝트 미니맵 비활성화
            merchantObject.MinimapObject.SetActive(roomInfo.IsRevealed(EMinimapObjectType.Merchant));
        }

        if (roomObjects.Length > 0)
        {
            foreach (var roomObject in roomObjects)
            {
                roomObject.SetParents(transform);
                if (roomObject.MinimapObject)
                {
                    switch (roomObject.RoomObjectType)
                    {
                        case ERoomObjectType.AttributePoint:
                            roomObject.MinimapObject.SetActive(roomInfo.IsRevealed(EMinimapObjectType.AttributePoint) && !roomInfo.attributePoint[0].alreadyGet);
                            break;
                        case ERoomObjectType.Potion:
                            roomObject.MinimapObject.SetActive(roomInfo.IsRevealed(EMinimapObjectType.Potion) && !roomInfo.potion[0].alreadyGet);
                            break;
                    }
                }
            }
        }
    }

    public string GetWallShortCutName(int idx)
    {
        List<ShortcutObject> wallList = new List<ShortcutObject>();
        foreach (var shortCut in shortCutObjects)
        {
            if (shortCut.Type == ShortcutType.Wall)
            {
                wallList.Add(shortCut);
            }
        }

        if (wallList.Count == 0)
            return default;
        
        return wallList[idx].name;
    }

    private void SetSavePoint()
    {
        if (!saveObject)
            return;

        saveObject.SetSelectAction();
        
        saveObject.SetSaveAction(() =>
        {
            RoomManager.Instance.AllMonsterArrive();
            GameManager.Instance.SavePoint = name;
            //GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30206)).Forget();
            GameManager.Instance.RefillPlayerHp();
            GameManager.Instance.SetPotionCount();
            saveObject.InteractionObject.FadeOut();
            SoundManager.Instance.PlaySound(ConstValues.SlotEquip);
            GameManager.Instance.SaveGame();
            
            // 이곳
            if (GameManager.Instance.IsHaveItem(ConstValues.SaveTravel))
            {
                GameManager.Instance.ControlStart = false;
                saveObject.SetFastTravelAction();
            }
        });
    }
    
    private void SetActionGoldObject()
    {
        foreach (var goldObject in goldObjects)
            goldObject.SetAction(PlusGold);
    }
    
    private void RefreshGoldObject()
    {
        foreach (var goldObject in goldObjects)
            goldObject.ResetObject();
    }

    private void SetPortal()
    {
        if (!portalObject)
            return;
        
        portalObject.SetPortalAction(() =>
        {
            portalObject.ReduceInteractionObject();

            // 첫 포탈 사용 시에는 UI 없이 Room_3_2로 강제 이동 후 firstPortal 저장
            if (!GameManager.Instance.FirstPortal)
            {
                MovePortal(ConstValues.FirstPortalRoom);
                GameManager.Instance.FirstPortal = true;
                GameManager.Instance.SaveGame();
            }
            else
            {
                // 이후에는 포탈 선택 UI를 띄우고, 선택으로 MovePortal 실행
                portalObject.SpawnFastTravel();
            }
        });
    }

    private void PortalSoundActive(bool active)
    {
        if (!portalObject)
            return;

        portalObject.SoundActive(active);
    }
    
    private async void SetBgm(bool immediately)
    {
        // if (isBossRoom)
        //     return;
        
        roomsData = TableManager.Instance.roomsTable.Rooms.Find(x => x.id == name);
        if (roomsData == null)
            return;
        
        await UniTask.WaitUntil(() => GameManager.Instance.InGame && !firstStart);
        PlayBGM(roomsData.bgm, immediately);
    }

    private void BossSetting()
    {
        foreach (var boss in bosses)
            boss.gameObject.SetActive(false);
    }

    private void BgSetting()
    {
        roomsData = TableManager.Instance.roomsTable.Rooms.Find(x => x.id == name);
        if (roomsData != null)
        {
            GameManager.Instance.MainCamera.SetBg(roomsData.bgSprite);
            GameManager.Instance.MainCamera.SetBgDeco(roomsData.bgDeco);
        }
    }

    private void SpawnBoss(Monster boss, Vector2 pos, EMonsterType monsterType, bool isAppearAction = true)
    {
        boss.transform.position = pos;
        boss.MonsterType = monsterType;
        boss.AlwaysAgro = true;
        boss.LimitLeft = monsterLimitLeft.position.x;
        boss.LimitRight = monsterLimitRight.position.x;
        boss.SetGoldAction(PlusGold);
        boss.SpawnHpBar();
        boss.gameObject.SetActive(true);
        if(isAppearAction)
            boss.Appear(SpawnBossMessage);
    }

    private int DieMonsterCount()
    {
        int dieCount = 0;
        foreach (var monster in monsters)
        {
            if (monster.IsDie)
                dieCount++;
        }
        return dieCount;
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

    private async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }

    private void StopBGM()
    {
        BgmManager.Instance.Stop();
    }
    private void PlayBGM(string bgmName, bool immediately = false)
    {
        BgmManager.Instance.PlayBgm(bgmName, immediately);
    }
    private void PlaySound(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName);
    }
    private void CameraShake(float amountX, float amountY, float time)
    {
        GameManager.Instance.CameraShake(amountX, amountY, time);
    }
    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }

    // 대화 연출
    private void ProductAction(int idx)
    {
        // 기존 방식 (폴백)
        switch (idx)
        {
            case 1:
                Product1();
                break;
            case 2:
                Product2();
                break;
            case 3:
                Product3();
                break;
            case 4:
                Product4();
                break;
            case 5:
                Product5();
                break;
            case 6:
                Product6();
                break;
            case 7:
                Product7();
                break;
            case 8:
                Product8();
                break;
            case 9:
                Product9();
                break;
            case 10:
                Product10();
                break;
            case 11:
                Product11();
                break;
            case 12:
                Product12();
                break;
            case 13:
                Product13();
                break;
            case 14:
                Product14();
                break;
        }
    }

    // 보스연출
    private void SpawnBossMessage(string bossName, EMonsterType monsterType)
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_BossMessage, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_BossMessage bossMessageView)
        {
            var bossMessageInterface = bossMessageView.BossMessageView.ConvertTo<IUIBossMessageView>();
            var bossMessageModel = new UIBossMessageModel()
            {
                bossName = bossName,
                monsterType = monsterType
            };
            var bossMessagePresenter = new UIBossMessagePresenter(bossMessageInterface, bossMessageModel);
            bossMessageView.SetEpisodePresenter(bossMessagePresenter);
            bossMessageView.ViewActive();
            bossMessagePresenter.SetBossMessage();
            bossMessagePresenter.BossMessageProduct(() => { SoundManager.Instance.PlaySound(ConstValues.WarningSound); });
        }
    }

    /// <summary>
    /// 미니맵 구현 구간
    /// </summary>
    /// 

    // 2. 저장된 데이터 유무에 따라 초기 복원
    private void StartMinimap()
    {
        // 저장 데이터 없음: 모든 테두리 비활성화
        if (string.IsNullOrEmpty(roomInfo.visitedFrameCells))
        {
            minimapFrameTilemap.ClearAllTiles();
        }
        // 저장 데이터 있음: 불러와서 해당 테두리만 활성화
        else
        {
            LoadVisitedFrameCells();
            foreach (var cell in visitedFrameCells)
            {
                if (originalFrameTiles.TryGetValue(cell, out var inTile))
                    minimapFrameTilemap.SetTile(cell, inTile);
            }
        }
        
        // 저장 데이터 없음: 모든 내부 비활성화
        if (string.IsNullOrEmpty(roomInfo.visitedInCells))
        {
            minimapInTilemap.ClearAllTiles();
        }
        // 저장 데이터 있음: 불러와서 해당 내부만 활성화
        else
        {
            LoadVisitedInCells();
            foreach (var cell in visitedInCells)
            {
                if (originalInTiles.TryGetValue(cell, out var inTile))
                    minimapInTilemap.SetTile(cell, inTile);
            }
        }
        
        // 저장 데이터 없음: 모든 숏컷 비활성화
        // 저장 데이터 있음: 불러와서 해당 숏컷만 활성화
        if (roomInfo.visitedShortcutCells.Count > 0)
        {
            LoadVisitedShortcutCells();
            for (int i = 0; i < visitedShortcutCells.Count; i++)
            {
                if (i >= shortcutFrameTileMaps.Length) break;

                foreach (var cell in visitedShortcutCells[i])
                {
                    // i번째 딕셔너리에서 타일을 찾아 복원
                    if (originalshortcutTilesList[i].TryGetValue(cell, out var inTile))
                    {
                        shortcutFrameTileMaps[i].SetTile(cell, inTile);
                    }
                }
            }
        }

        // 숨겨진 구역: 발견 여부와 공개됐던 셀 복원
        if (hiddenAreas != null)
        {
            for (int i = 0; i < hiddenAreas.Length; i++)
            {
                if (i < roomInfo.hiddenAreaDiscovered.Count)
                    hiddenAreas[i].SetDiscovered(roomInfo.hiddenAreaDiscovered[i]);
                if (i < roomInfo.visitedHiddenCells.Count)
                    hiddenAreas[i].LoadVisitedCells(roomInfo.visitedHiddenCells[i]);
            }
        }
    }
    // 3. 카메라 뷰 영역에 조금이라도 겹치면 활성화
    private void RevealCellsInView()
    {
        Vector3 camPos = gameCamera.transform.position;
        float halfH = gameCamera.orthographicSize;
        float halfW = halfH * gameCamera.aspect;
        
        Rect viewRect = new Rect(camPos.x - halfW, camPos.y - halfH, halfW * 2, halfH * 2);
        
        // 미니맵 테두리
        float extraFrameVertical = minimapFrameTilemap.cellSize.y * 0.5f; // 0.5f
        viewRect.yMin += extraFrameVertical; //
        viewRect.yMax += extraFrameVertical * 3; //

        Vector2 halfFrameCell = minimapFrameTilemap.cellSize; // minimapTilemap.cellSize
        bool frameNew = false;
        foreach (var cell in allFrameCells)
        {
            if (visitedFrameCells.Contains(cell))
                continue;

            Vector3 center = minimapFrameTilemap.GetCellCenterWorld(cell);
            Vector2 min = new Vector2(center.x - halfFrameCell.x, center.y - halfFrameCell.y);
            Vector2 max = new Vector2(center.x + halfFrameCell.x, center.y + halfFrameCell.y);

            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                visitedFrameCells.Add(cell);
                minimapFrameTilemap.SetTile(cell, originalFrameTiles[cell]);
                frameNew = true;
            }
        }
        if (frameNew)
            SaveVisitedFrameCells();
        
        // 미니맵 내부
        float extraInVertical = minimapInTilemap.cellSize.y * 0.5f;
        viewRect.yMin += extraInVertical;
        viewRect.yMax += extraInVertical * 3;
        
        Vector2 halfInCell = minimapInTilemap.cellSize; // 
        bool inNew = false;
        foreach (var cell in allInCells)
        {
            if (visitedInCells.Contains(cell))
                continue;
        
            Vector3 center = minimapInTilemap.GetCellCenterWorld(cell);
            Vector2 min = new Vector2(center.x - halfInCell.x, center.y - halfInCell.y);
            Vector2 max = new Vector2(center.x + halfInCell.x, center.y + halfInCell.y);
        
            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                visitedInCells.Add(cell);
                minimapInTilemap.SetTile(cell, originalInTiles[cell]);
                inNew = true;
            }
        }
        if (inNew)
            SaveVisitedInCells();
        
        // 이곳에 세이브 포인트 뭐시기
        bool saveNew = false;
        if (saveObject)
        {
            Vector2 savePos = saveObject.transform.position;
            Vector2 saveSize = saveObject.ColSize;
            
            Vector2 min = new Vector2(savePos.x - saveSize.x, savePos.y - saveSize.y);
            Vector2 max = new Vector2(savePos.x + saveSize.x, savePos.y + saveSize.y);
            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                saveNew = true;
            }
        }
        if (saveNew)
            SaveSaveObject();

        bool portalNew = false;
        if (portalObject)
        {
            Vector2 portalPos = portalObject.transform.position;
            Vector2 saveSize = portalObject.ColSize;
            
            Vector2 min = new Vector2(portalPos.x - saveSize.x, portalPos.y - saveSize.y);
            Vector2 max = new Vector2(portalPos.x + saveSize.x, portalPos.y + saveSize.y);
            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                portalNew = true;
            }
        }
        if (portalNew)
            SavePortalObject();

        bool merchantNew = false;
        if(merchantObject)
        {
            Vector2 merchantPos = merchantObject.transform.position;
            Vector2 saveSize = new Vector2(3.5f, 3.5f);
            
            Vector2 min = new Vector2(merchantPos.x - saveSize.x, merchantPos.y - saveSize.y);
            Vector2 max = new Vector2(merchantPos.x + saveSize.x, merchantPos.y + saveSize.y);
            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                merchantNew = true;
            }
        }
        if (merchantNew)
            SaveMerchantObject();
        
        if(roomObjects.Length > 0)
        {
            for (var i = 0; i < roomObjects.Length; i++)
            {
                bool roomObjectNew = false;

                Vector2 attributePointPos = roomObjects[i].transform.position;
                Vector2 saveSize = new Vector2(3.5f, 3.5f);

                Vector2 min = new Vector2(attributePointPos.x - saveSize.x, attributePointPos.y - saveSize.y);
                Vector2 max = new Vector2(attributePointPos.x + saveSize.x, attributePointPos.y + saveSize.y);
                // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
                if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                    max.y >= viewRect.yMin && min.y <= viewRect.yMax)
                {
                    roomObjectNew = true;
                }

                if (roomObjectNew)
                    SaveAttributePointCheck(i);
            }
        }

        bool shortcutNew = false;
        foreach (var shortcutCell in allshortcutCells)
        {
            for (int i = 0; i < shortcutFrameTileMaps.Length; i++)
            {
                if (visitedShortcutCells[i].Contains(shortcutCell)) continue;

                // 해당 인덱스의 타일맵에 원래 이 좌표의 타일이 있었는지 확인
                if (originalshortcutTilesList[i].TryGetValue(shortcutCell, out var originalTile))
                {
                    var targetMap = shortcutFrameTileMaps[i];
                    Vector3 center = targetMap.GetCellCenterWorld(shortcutCell);
                    Vector2 min = new Vector2(center.x - halfFrameCell.x, center.y - halfFrameCell.y);
                    Vector2 max = new Vector2(center.x + halfFrameCell.x, center.y + halfFrameCell.y);

                    if (max.x >= viewRect.xMin && min.x <= viewRect.xMax && max.y >= viewRect.yMin && min.y <= viewRect.yMax)
                    {
                        visitedShortcutCells[i].Add(shortcutCell);
                        targetMap.SetTile(shortcutCell, originalTile); // 정확한 자기 타일 설치
                        shortcutNew = true;
                    }
                }
            }
        }
        if (shortcutNew)
            SaveVisitedShortcutCells();

        // 숨겨진 구역: 발견된 구역만 카메라 시야에 들어온 타일부터 점진 공개
        if (hiddenAreas != null)
        {
            bool hiddenNew = false;
            foreach (var hiddenArea in hiddenAreas)
                hiddenNew |= hiddenArea.RevealCellsInView(viewRect, halfFrameCell);

            if (hiddenNew)
                SaveHiddenAreaData();
        }
    }

    // 방문 테두리 저장
    private void SaveVisitedFrameCells()
    {
        var sb = new StringBuilder();
        foreach (var c in visitedFrameCells)
            sb.Append(c.x).Append('_').Append(c.y).Append('_').Append(c.z).Append(';');

        roomInfo.visitedFrameCells = sb.ToString();
    }
    // 방문 테두리 로드
    private void LoadVisitedFrameCells()
    {
        if (string.IsNullOrEmpty(roomInfo.visitedFrameCells))
            return;

        var entries = roomInfo.visitedFrameCells.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var e in entries)
        {
            var p = e.Split('_');
            if (p.Length == 3
             && int.TryParse(p[0], out int x)
             && int.TryParse(p[1], out int y)
             && int.TryParse(p[2], out int z))
            {
                visitedFrameCells.Add(new Vector3Int(x, y, z));
            }
        }
    }
    
    // 방문 내부 저장
    private void SaveVisitedInCells()
    {
        var sb = new StringBuilder();
        foreach (var c in visitedInCells)
            sb.Append(c.x).Append('_').Append(c.y).Append('_').Append(c.z).Append(';');
    
        roomInfo.visitedInCells = sb.ToString();
    }
    // 방문 내부 로드
    private void LoadVisitedInCells()
    {
        if (string.IsNullOrEmpty(roomInfo.visitedInCells))
            return;

        var entries = roomInfo.visitedInCells.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var e in entries)
        {
            var p = e.Split('_');
            if (p.Length == 3
                && int.TryParse(p[0], out int x)
                && int.TryParse(p[1], out int y)
                && int.TryParse(p[2], out int z))
            {
                visitedInCells.Add(new Vector3Int(x, y, z));
            }
        }
    }
    
    // 숏컷 셀 저장
    private void SaveVisitedShortcutCells()
    {
        roomInfo.visitedShortcutCells.Clear(); // 기존 데이터 클리어

        for (int i = 0; i < visitedShortcutCells.Count; i++)
        {
            var sb = new StringBuilder();
            foreach (var c in visitedShortcutCells[i])
            {
                sb.Append(c.x).Append('_').Append(c.y).Append('_').Append(c.z).Append(';');
            }
            // 각 인덱스별로 하나의 문자열로 묶어 리스트에 추가
            roomInfo.visitedShortcutCells.Add(sb.ToString());
        }
    }
    // 숨겨진 구역 저장 (발견 여부 + 공개된 셀)
    private void SaveHiddenAreaData()
    {
        if (roomInfo == null || hiddenAreas == null)
            return;

        roomInfo.hiddenAreaDiscovered.Clear();
        roomInfo.visitedHiddenCells.Clear();
        foreach (var hiddenArea in hiddenAreas)
        {
            roomInfo.hiddenAreaDiscovered.Add(hiddenArea.IsDiscovered);
            roomInfo.visitedHiddenCells.Add(hiddenArea.SaveVisitedCells());
        }
    }
    // 숏컷 셀 로드
    private void LoadVisitedShortcutCells()
    {
        if (roomInfo.visitedShortcutCells.Count == 0)
            return;

        for (int i = 0; i < roomInfo.visitedShortcutCells.Count; i++)
        {
            // 인덱스 안전망
            if (i >= visitedShortcutCells.Count) break;

            var entries = roomInfo.visitedShortcutCells[i].Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var e in entries)
            {
                var p = e.Split('_');
                if (p.Length == 3 && int.TryParse(p[0], out int x) && int.TryParse(p[1], out int y) && int.TryParse(p[2], out int z))
                {
                    visitedShortcutCells[i].Add(new Vector3Int(x, y, z));
                }
            }
        }
    }
    
    private void SaveSaveObject()
    {
        saveObject.MinimapObject.SetActive(true);
        roomInfo.Reveal(EMinimapObjectType.SavePoint);
    }
    
    private void SavePortalObject()
    {
        portalObject.MinimapObject.SetActive(true);
        roomInfo.Reveal(EMinimapObjectType.Portal);
    }
    
    private void SaveMerchantObject()
    {
        merchantObject.MinimapObject.SetActive(true);
        roomInfo.Reveal(EMinimapObjectType.Merchant);
    }
    
    private void SaveAttributePointCheck(int idx)
    {
        if(roomObjects[idx].MinimapObject)
        {
            switch (roomObjects[idx].RoomObjectType)
            {
                case ERoomObjectType.AttributePoint:
                    roomObjects[idx].MinimapObject.SetActive(!roomInfo.attributePoint[0].alreadyGet);
                    roomInfo.Reveal(EMinimapObjectType.AttributePoint);
                    break;
                case ERoomObjectType.Potion:
                    roomObjects[idx].MinimapObject.SetActive(!roomInfo.potion[0].alreadyGet);
                    roomInfo.Reveal(EMinimapObjectType.Potion);
                    break;
            }
        }
    }

    /// <summary>
    /// 연출 구현 구간
    /// </summary>
    // 연출1, 해당하는 방 = Room1_1
    private void UIOn()
    {
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
        GameManager.Instance.MovePlayer();
    }
    private void UIOff()
    {
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
        GameManager.Instance.StopPlayer();
    }

    private SpeechFrame SpeechFrame1()
    {
        return GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrame1);
    }
    
    private SpeechFrame SpeechFrame2()
    {
        return GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrame2);
    }

    private SpeechFrame SpeechFrame3()
    {
        return GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrame3);
    }
    
    private SpeechFrame StrongFrame()
    {
        return GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrameStrong);
    }
    
    // 최초 스타트
    private async void Product1()
    {
        // 연출 시작 전 세팅
        GameManager.Instance.InitProductCancellation();
        
        firstStart = true;
        roomCustomObjects[1].SetActive(false);
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

        GameManager.Instance.CurPlayer.SetSprite(false);
        
        bosses[0].enabled = false;
        bosses[0].transform.position = bossPos[0].position;
        bosses[0].gameObject.SetActive(true);
        bosses[0].Flip(-1);
        
        GameManager.Instance.CurPlayer.SetSprite(false);
        
        // 에피소드 팝업부터 시작
        GameManager.Instance.ControlStart = false;
        if(await WaitUntil(() => !GameManager.Instance.FadeSystem.gameObject.activeSelf, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        StopBGM();
        PlayBGM($"{ConstValues.BGM}_{ConstValues.EpisodeStart}", true);
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        // 문 열기
        PlaySound(ConstValues.DoorOpen);
        roomCustomObjects[0].SetActive(false);
        GameManager.Instance.CurPlayer.SetSprite(true);
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        // 문 닫기
        PlaySound(ConstValues.DoorClose);
        roomCustomObjects[0].SetActive(true);
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        var berserkerPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        var speechFrame2 = SpeechFrame2();
        speechFrame2.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerPos, GameManager.Instance.GetTalk(10100));
        await NextDialog(speechFrame1);

        SpawnSpeechFrame(speechFrame1, berserkerPos, GameManager.Instance.GetTalk(10101));
        await NextDialog(speechFrame1);

        PlayBGM($"{ConstValues.BGM}_{ConstValues.Forest}", true);
        PlaySound(ConstValues.PlayerScream);
        CameraShake(0.1f, 0.4f, 1.0f);
        SpawnSpeechFrame(speechFrame1, new Vector2(berserkerPos.x, berserkerPos.y + 0.5f), GameManager.Instance.GetTalk(10102));
        for (int i = 0; i < 2; i++)
        {
            GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }

        await NextDialog(speechFrame1);

        var sunPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
        
        SpawnSpeechFrame(speechFrame2, sunPos, GameManager.Instance.GetTalk(10103));
        await NextDialog(speechFrame2);

        PlaySound($"{ConstValues.MonsterSun}_{ConstValues.Laugh}");
        var sunMoveVector = new Vector2(bosses[0].transform.position.x + 7.5f, bosses[0].transform.position.y);
        bosses[0].transform.DOMove(sunMoveVector, 2.0f);
        if (await GameManager.Instance.NormalDelay(2.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        bosses[0].gameObject.SetActive(false);

        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 게임 시작
        roomCustomObjects[1].SetActive(true);
        UIOn();

        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        firstStart = false;
        
        RoomManager.Instance.ActivePlaceName();
        GameManager.Instance.RefreshPlaceName();
    }

    // 지도 가이드
    private async void Product2()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();

        UIOff();

        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;

        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10104));
        await NextDialog(speechFrame1);

        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10105));
        await NextDialog(speechFrame1);

        RoomManager.Instance.Guide(0);
        
        UIOn();
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
    }

    // 세이브 포인트 발견
    private async void Product3()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();

        UIOff();
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10106));
        await NextDialog(speechFrame1);

        RoomManager.Instance.Guide(3);
        UIOn();
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
    }
    
    // 아레나 대전
    private void Product4()
    {
        StartArena();
    }
    
    // 트리엔트와 대결
    private async void Product5()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        BgmManager.Instance.DelayStop(0.1f);
        float productDelay = 1.0f;
        if (await GameManager.Instance.NormalDelay(productDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 닫기
        BossTileMapActive(true);
        
        float productDelay2 = 1.5f;
        if (await GameManager.Instance.NormalDelay(productDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        PlayBGM($"{ConstValues.BGM}_{ConstValues.Tree}", true);
        
        // 트리엔트 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.HiddenBoss);
        
        if (await GameManager.Instance.NormalDelay(4.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.roomProduct[0].count == 0)
        {
            // 대화하는 주체들
            Vector2 berserkerSpeechPos;
            Vector2 gunnerSpeechPos;
            Vector2 treeSpeechPos;
        
            var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
            var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
            var tree = bosses[0].GetComponent<Monster_Tree>();
        
            var speechFrame1 = SpeechFrame1();
            speechFrame1.gameObject.SetActive(false);

            if (await GameManager.Instance.DialogueMove(1.5f).SuppressCancellationThrow())
                return; 
            
            berserkerSpeechPos = berserker.SpeechPos.position;
            gunnerSpeechPos = gunner.SpeechPos.position;
            treeSpeechPos = bosses[0].SpeechPos.position;
            
            bosses[0].CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        
            // 화난 포즈
            berserker.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            PlaySound(ConstValues.PlayerScream);
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10250));
            await NextDialog(speechFrame1);
            
            // 우워얽 포즈
            bosses[0].CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            CameraShake(0.3f, 0.3f, 1.0f);
            PlaySound($"{ConstValues.MonsterTree}_{ConstValues.Voice}1");
            SpawnSpeechFrame(speechFrame1, treeSpeechPos, GameManager.Instance.GetTalk(10251));
            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10252));
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10253));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10254));
            await NextDialog(speechFrame1);
            
            berserker.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            bosses[0].CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            
            // 게임 시작
            GameManager.Instance.DialogueEnd();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            roomInfo.roomProduct[0].count += 1;
            GameManager.Instance.SaveGame();
        }
        
        UIOn();
        
        // 보스 죽음
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.ForceProduct();
        StopBGM();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        // 연출이 있다면 이곳에 넣기

        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }
    
    // 태양과 대결
    private async void Product6()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        BgmManager.Instance.DelayStop(0.1f);
        float productDelay = 1.0f;
        if (await GameManager.Instance.NormalDelay(productDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 닫기
        BossTileMapActive(true);
        
        float productDelay2 = 1.5f;
        if (await GameManager.Instance.NormalDelay(productDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        PlayBGM($"{ConstValues.BGM}_{ConstValues.Sun}", true);
        // 태양 보스 소환
        SpawnBoss(bosses[0], new Vector2(bossPos[0].transform.position.x, bossPos[0].transform.position.y + 3.5f), EMonsterType.Boss);

        // 대화하는 주체들
        Vector2 berserkerSpeechPos;
        Vector2 sunSpeechPos;
        Vector2 moonSpeechPos;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        var speechFrame2 = SpeechFrame2();
        speechFrame2.gameObject.SetActive(false);
        
        if (roomInfo.roomProduct[0].count == 0)
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);

            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10107));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10108));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10109)); 
            await NextDialog(speechFrame2);

            // 게임 시작
            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            roomInfo.roomProduct[0].count += 1;
            GameManager.Instance.SaveGame();
        }
        else
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }
        UIOn();
        
        if(await WaitUntil(() => bosses[0].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.roomProduct[0].count == 1)
        {
            UIOff();
            
            // 이 부분 강제이동으로 변경
            bosses[0].CancelMotion();
            bosses[0].transform.DOMove(bossPos[0].position, 0.5f);
            if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            bosses[0].Flip(-1);

            // 전투 잔여 동작(공격 후딜·피격 회복 등) 코루틴을 강제 정리 —
            // 남은 코루틴의 CancelMotion이 EpisodeMove의 토큰을 끊어 이동이 중단되거나(위치·방향 미적용),
            // Idle 복구가 유실되어 WaitUntil이 무한 대기(소프트락)하는 것을 방지
            GameManager.Instance.CurPlayer.ForceProduct();
            if(await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1).SuppressCancellationThrow())
                return;

            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            GameManager.Instance.CurPlayer.ForceIdle();
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10110)); 
            await NextDialog(speechFrame2);

            SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10111)); 
            await NextDialog(speechFrame2);
        }
        
        // BGM 끄기
        StopBGM();
        
        if (roomInfo.roomProduct[0].count == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10112));
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(1, 0);
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10113));
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(2, 0.3f);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(2, 0.2f);
            bosses[0].DieShake();
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(10, 0.1f);
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            bosses[0].DieExplosion();
            speechFrame2.SpeechEnd();

            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10114));
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10115));
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10116));
            await NextDialog(speechFrame1);
        }
        else
        {
            GameManager.Instance.StopPlayer();
            bosses[0].GetComponent<Monster_Sun>().SunDie();
            if (await WaitUntil(() => !bosses[0].gameObject.activeSelf, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }

        var cameraPos = GameManager.Instance.MainCamera.transform.position;
        var fadePos = new Vector3(cameraPos.x, cameraPos.y, 0);
        var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, fadePos).GetComponent<FadeSystem>();
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade(false);
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.roomProduct[0].count == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10117));
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10118));
        }

        RoomManager.Instance.BgSpriteChange(ConstValues.BgForestNight);
        RoomManager.Instance.BgDecoActive(false);
        
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade(false);
        fadeBg.gameObject.SetActive(false);
        BgmManager.Instance.Play();
        PlayBGM($"{ConstValues.BGM}_{ConstValues.Sun}", true);

        // 달 보스 소환
        SpawnBoss(bosses[1], new Vector2(bossPos[0].transform.position.x, bossPos[0].transform.position.y + 3.5f), EMonsterType.Boss);

        if (roomInfo.roomProduct[0].count == 1)
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
            moonSpeechPos = new Vector2(bosses[1].CenterPos.position.x - 2.0f, bosses[1].CenterPos.position.y);
            
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame2, moonSpeechPos, GameManager.Instance.GetTalk(10119)); 
            await NextDialog(speechFrame2);
            
            SpawnSpeechFrame(speechFrame2, moonSpeechPos, GameManager.Instance.GetTalk(10120)); 
            await NextDialog(speechFrame2);
        
            // PlaySound(ConstValues.PlayerScream);
            // CameraShake(0.4f, 0.4f, 1.0f);
            // SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[14]);
            // for (int i = 0; i < 2; i++)
            // {
            //     GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
            //     GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);
            //
            //     if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            //         return;
            // }
            // await NextDialog(speechFrame1[0]);
            
            roomInfo.roomProduct[0].count += 1;
            GameManager.Instance.SaveGame();
        }
        else
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }
        UIOn();
        
        if (await WaitUntil(() => bosses[1].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        UIOff();
        GameManager.Instance.InitProductCancellation();
        bosses[1].CancelMotion();
        bosses[1].transform.DOMove(bossPos[0].position, 0.5f);
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        moonSpeechPos = new Vector2(bosses[1].CenterPos.position.x - 2.0f, bosses[1].CenterPos.position.y);

        bosses[1].Flip(-1);
        // 전투 잔여 동작(공격 후딜·피격 회복 등) 코루틴을 강제 정리 —
        // 남은 코루틴의 CancelMotion이 EpisodeMove의 토큰을 끊어 이동이 중단되거나(위치·방향 미적용),
        // Idle 복구가 유실되어 WaitUntil이 무한 대기(소프트락)하는 것을 방지
        GameManager.Instance.CurPlayer.ForceProduct();
        if (await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1).SuppressCancellationThrow())
            return;

        PlaySound($"{ConstValues.Scream}12");
        bosses[1].DieShake();
        bosses[1].GetComponent<Monster_Moon>().DieBomb();

        SpawnSpeechFrame(speechFrame2, moonSpeechPos, GameManager.Instance.GetTalk(10122));
        await NextDialog(speechFrame2);

        SpawnSpeechFrame(speechFrame2, moonSpeechPos, GameManager.Instance.GetTalk(10123));
        await NextDialog(speechFrame2);

        bosses[1].DieExplosion();
        BgmManager.Instance.Stop();
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        fadeBg.gameObject.SetActive(true);
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade(false);
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        RoomManager.Instance.BgSpriteChange(ConstValues.BgForest);
        RoomManager.Instance.BgDecoActive(true);
        
        berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10124)); 
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10125)); 
        await NextDialog(speechFrame1);
        
        PlayBGM(roomsData.bgm, true);
        PlaySound(ConstValues.ChickenCock);
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10126)); 
        await NextDialog(speechFrame1);
        
        PlaySound(ConstValues.RewardPage);
        npc[0].gameObject.SetActive(true);
        npc[0].transform.localScale = new Vector3(-1, 1, 1);
        var npcArrivePos = npc[0].transform.position;
        npc[0].gameObject.transform.position = new Vector2(npc[0].transform.position.x, npc[0].transform.position.y + 3.5f);
        await npc[0].EpisodeMove_Y(npcArrivePos, bosses[0].BasicStat.moveSpeed);
        sunSpeechPos = npc[0].SpeechPos.position;
        
        SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10127)); 
        await NextDialog(speechFrame2);
        
        SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10128)); 
        await NextDialog(speechFrame2);
        
        SpawnSpeechFrame(speechFrame2, sunSpeechPos, GameManager.Instance.GetTalk(10129)); 
        await NextDialog(speechFrame2);

        // 문 열기
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        roomInfo.eventNpc[0].isActive = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }

    // 거너를 만남
    private async void Product7()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        UIOff();

        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        var gunnerSpeechPos = npc[0].SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        npc[0].Flip(-1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10200));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10201));
        await NextDialog(speechFrame1);

        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10202));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10203));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10204));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10205));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10206));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10207));
        await NextDialog(speechFrame1);
        
        npc[0].SpawnObject(ConstValues.BangEffect, npc[0].CenterPos.position);
        npc[0].gameObject.SetActive(false);
        
        // 2인 캐릭터 설정 및 저장
        GameManager.Instance.AddPlayer(ConstValues.Gunner);
        GameManager.Instance.SetCharacterOrder();
        RoomManager.Instance.Guide(5);
        
        UIOn();
        roomInfo.roomProduct[0].isFinish = true;
        roomInfo.eventNpc[0].isActive = false;
        GameManager.Instance.SaveGame();
    }
    
    // 거대 권투맨 대결
    private async void Product8()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        BgmManager.Instance.DelayStop(0.1f);
        float productDelay = 1.0f;
        if (await GameManager.Instance.NormalDelay(productDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 닫기
        BossTileMapActive(true);
        
        float productDelay2 = 1.5f;
        if (await GameManager.Instance.NormalDelay(productDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        PlayBGM($"{ConstValues.BGM}_{ConstValues.Charge}", true);
        
        // 거대권투 보스 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.HiddenBoss);
        
        // 대화하는 주체들
        Vector2 berserkerSpeechPos;
        Vector2 gunnerSpeechPos;
        Vector2 chargeSpeechPos;
        
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        var chargeMonster = bosses[0].GetComponent<Monster_BigCharge>();

        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);

        if (roomInfo.roomProduct[0].count == 0)
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            if (await GameManager.Instance.DialogueMove(-1.5f).SuppressCancellationThrow())
                return; 
            
            berserkerSpeechPos = berserker.SpeechPos.position;
            gunnerSpeechPos = gunner.SpeechPos.position;
            chargeSpeechPos = bosses[0].SpeechPos.position;
            
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame1, chargeSpeechPos, GameManager.Instance.GetTalk(10300));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, chargeSpeechPos, GameManager.Instance.GetTalk(10301));
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10302));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10303));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10304));
            await NextDialog(speechFrame1);
            
            // 음악 끊기고 정적 + 권투맨 표정변화
            speechFrame1.gameObject.SetActive(false);
            StopBGM();
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Upset1);
            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            // 권투맨 표정변화2
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Upset2);
            SpawnSpeechFrame(speechFrame1, chargeSpeechPos, GameManager.Instance.GetTalk(10305));
            await NextDialog(speechFrame1);
            
            // 권투맨 준비자세
            PlayBGM($"{ConstValues.BGM}_{ConstValues.Charge}", true);
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.3f, 0.4f, 1.0f);
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.FightPose2);
            SpawnSpeechFrame(speechFrame1, chargeSpeechPos, GameManager.Instance.GetTalk(10306));
            await NextDialog(speechFrame1);

            // 게임 시작
            GameManager.Instance.DialogueEnd();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            roomInfo.roomProduct[0].count += 1;
            GameManager.Instance.SaveGame();
        }
        else
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }
        
        UIOn();
        // 보스 죽음
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.ForceProduct();
        StopBGM();
        
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        chargeSpeechPos = bosses[0].SpeechPos2.position;
        SpawnSpeechFrame(speechFrame1, chargeSpeechPos, GameManager.Instance.GetTalk(10307));
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        speechFrame1.gameObject.SetActive(false);

        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }
    
    // 아레나 대전
    private void Product9()
    {
        StartArena();
    }
    
    // 암살자와 대결
    private async void Product10()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        BgmManager.Instance.DelayStop(0.1f);
        float productDelay = 1.0f;
        if (await GameManager.Instance.NormalDelay(productDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 닫기
        BossTileMapActive(true);
        
        float productDelay2 = 1.5f;
        if (await GameManager.Instance.NormalDelay(productDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        PlayBGM($"{ConstValues.BGM}_{ConstValues.Knife}", true);
        
        // 암살자 보스 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.Boss);
        // 대화하는 주체들
        Vector2 berserkerSpeechPos;
        Vector2 gunnerSpeechPos;
        Vector2 knifeSpeechPos;
        
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        var knife = bosses[0].GetComponent<Monster_Knife>();

        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);

        if (roomInfo.roomProduct[0].count == 0)
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            if (await GameManager.Instance.DialogueMove(1.5f).SuppressCancellationThrow())
                return; 
            
            berserkerSpeechPos = berserker.SpeechPos.position;
            gunnerSpeechPos = gunner.SpeechPos.position;
            knifeSpeechPos = bosses[0].SpeechPos.position;
            
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame1, knifeSpeechPos, GameManager.Instance.GetTalk(10500));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10501));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, knifeSpeechPos, GameManager.Instance.GetTalk(10502));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10503));
            await NextDialog(speechFrame1);
            
            bosses[0].CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            SpawnSpeechFrame(speechFrame1, knifeSpeechPos, GameManager.Instance.GetTalk(10504));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10505));
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, knifeSpeechPos, GameManager.Instance.GetTalk(10506));
            await NextDialog(speechFrame1);

            // 게임 시작
            GameManager.Instance.DialogueEnd();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            roomInfo.roomProduct[0].count += 1;
            GameManager.Instance.SaveGame();
        }
        else
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }
        UIOn();
        
        // 보스 죽음
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.ForceProduct();
        StopBGM();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        bosses[0].EventStand();
        knifeSpeechPos = bosses[0].SpeechPos.position;
        bosses[0].CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
        SpawnSpeechFrame(speechFrame1, knifeSpeechPos, GameManager.Instance.GetTalk(10507));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, knifeSpeechPos, GameManager.Instance.GetTalk(10508));
        await NextDialog(speechFrame1);
        await knife.EventExit();
        
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }
    
    // 광부를 만남
    private async void Product11()
    {
        GameManager.Instance.InitProductCancellation();
        UIOff();
        GameManager.Instance.CurPlayer.ForceProduct();
        
        float productDelay1 = 2.0f;
        float productDelay2 = 1.5f;
        
        if(await GameManager.Instance.WaitUntilDelay(() => GameManager.Instance.CurPlayer.LandingState == ELandingState.Ground, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        if (await GameManager.Instance.DialogueMove(1.5f).SuppressCancellationThrow())
            return; 
        
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker);
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner);
        var fighter = GameManager.Instance.GetPlayer(ConstValues.Fighter);
        
        var berserkerSpeechPos = berserker.SpeechPos.position;
        var gunnerSpeechPos = gunner.SpeechPos.position;
        var fighterSpeechPos = fighter.SpeechPos.position;

        var miner = npc[0];
        var minerSpeechPos = npc[0].SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10700));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10701));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10702));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10703));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10704));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10705));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10706));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10707));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10708));
        await NextDialog(speechFrame1);
        
        // 입구 보여줌, 카메라 위치 조정
        var obstacle = shortCutObjects[2].GetComponent<Shortcut_Obstacle>();
        GameManager.Instance.MainCamera.SetTarget(obstacle.DestroyPos.transform);
        
        if (await GameManager.Instance.NormalDelay(productDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.MainCamera.SetTarget(GameManager.Instance.CurPlayer.transform);
        
        if (await GameManager.Instance.NormalDelay(productDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10709));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10710));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10711));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10712));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10713));
        await NextDialog(speechFrame1);

        var itemName1 = GameManager.Instance.GetItemTalk(ConstValues.SteelPlate);
        var itemName2 = GameManager.Instance.GetItemTalk(ConstValues.IronWheel);
        var itemName3 = GameManager.Instance.GetItemTalk(ConstValues.MiniBooster);
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, string.Format(GameManager.Instance.GetTalk(10714), itemName1, itemName2, itemName3));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10715));
        await NextDialog(speechFrame1);
        
        // 연출 종료
        GameManager.Instance.DialogueEnd();
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }
    
    private void Product12()
    {
        StartArena();
    }
    
    private async void Product13()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        BgmManager.Instance.DelayStop(0.1f);
        float productDelay = 1.0f;
        if (await GameManager.Instance.NormalDelay(productDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 닫기
        BossTileMapActive(true);
        
        float productDelay2 = 1.5f;
        if (await GameManager.Instance.NormalDelay(productDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        PlayBGM($"{ConstValues.BGM}_{ConstValues.Golem}", true);
        
        // 스톤골렘 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.MiniBoss);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        UIOn();
        
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        StopBGM();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }

    private async void Product14()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        BgmManager.Instance.DelayStop(0.1f);
        float productDelay = 1.0f;
        if (await GameManager.Instance.NormalDelay(productDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 닫기
        BossTileMapActive(true);
        
        float productDelay2 = 1.5f;
        if (await GameManager.Instance.NormalDelay(productDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        PlayBGM($"{ConstValues.BGM}_{ConstValues.Bomb}", true);
        
        // 폭탄전차 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.Boss);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        UIOn();
        
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        StopBGM();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }

    // 아레나 대전
    private async void StartArena()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitProductCancellation();
        GameManager.Instance.StopPlayer();

        await arenas[0].ReduceCameraLimitX(firstMaxLimit, firstMinLimit);
        BgmManager.Instance.DelayStop(0.1f);
        if (await GameManager.Instance.NormalDelay(1.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        arenas[0].CreateTile();
        
        if (await GameManager.Instance.NormalDelay(1.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        PlayBGM($"{ConstValues.BGM}_{ConstValues.Arena}", true);
        
        GameManager.Instance.MovePlayer();
        
        if (await arenas[0].RoundStart(GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.StopPlayer();
        
        if (await arenas[0].RoundEnd(GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        SetBgm(true);
        GameManager.Instance.MainCamera.SetCameraLimit(firstMaxLimit, firstMinLimit);
        
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
    }
    
    // 스킬 획득 후 이벤트
    private async void GetSkillEvent(string skillId, string skillName, int count = 1)
    {
        //string getMessage = string.Format(GameManager.Instance.GetTalk(30200), skillName);;
        
        if (GameManager.Instance.FirstGetSkill)
        {
            //await GameManager.Instance.SpawnWarningPopup(getMessage);
            GameManager.Instance.ProductObjectInfo(skillId, skillName, count);
        }
        else
        {
            GameManager.Instance.FirstGetSkill = true;
            GameManager.Instance.StopPlayer();
            GameManager.Instance.CurPlayer.ForceProduct();
            GameManager.Instance.ProductObjectInfo(skillId, skillName, count);
            //await GameManager.Instance.SpawnWarningPopup(getMessage);
            
            GameManager.Instance.InitProductCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(2);
            UIOn();
        }
    }
    
    // 마력석 획득 후 이벤트
    private async void GetAttributeEvent(int pointCount)
    {
        //string getMessage = string.Format(GameManager.Instance.GetTalk(30201), pointCount.ToString());
        
        if (GameManager.Instance.FirstGetAttribute)
        {
            //await GameManager.Instance.SpawnWarningPopup(getMessage);
            GameManager.Instance.ProductObjectInfo(ConstValues.AttributePoint, GameManager.Instance.GetTalk(99001), pointCount);
        }
        else
        {
            GameManager.Instance.FirstGetAttribute = true;
            GameManager.Instance.StopPlayer();
            GameManager.Instance.CurPlayer.ForceProduct();
            GameManager.Instance.ProductObjectInfo(ConstValues.AttributePoint, GameManager.Instance.GetTalk(99001), pointCount);
            //await GameManager.Instance.SpawnWarningPopup(getMessage);
            
            GameManager.Instance.InitProductCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(6);
            UIOn();
            GameManager.Instance.MovePlayer();
        }
    }
    
    // 포션 획득 후 이벤트
    private async void GetPotionEvent()
    {
        string getMessage = GameManager.Instance.GetTalk(30217);
        
        if (GameManager.Instance.FirstGetPotion)
        {
            GameManager.Instance.SpawnWarningPopup(getMessage).Forget();
        }
        else
        {
            GameManager.Instance.FirstGetPotion = true;
            GameManager.Instance.StopPlayer();
            GameManager.Instance.CurPlayer.ForceProduct();
            await GameManager.Instance.SpawnWarningPopup(getMessage);
            
            GameManager.Instance.InitProductCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(1);
            UIOn();
            GameManager.Instance.MovePlayer();
        }
    }
    
    // 유물 획득 후 이벤트
    private async void GetRelicEvent(string id)
    {
        if (GameManager.Instance.FirstGetRelic)
        {
            GameManager.Instance.ProductObjectInfo(id, GameManager.Instance.GetItemTalk(id), 1);
        }
        else
        {
            GameManager.Instance.FirstGetRelic = true;
            GameManager.Instance.StopPlayer();
            GameManager.Instance.CurPlayer.ForceProduct();
            GameManager.Instance.ProductObjectInfo(id, GameManager.Instance.GetItemTalk(id), 1);

            GameManager.Instance.InitProductCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(4);
            UIOn();
            GameManager.Instance.MovePlayer();
        }
    }
    
    // 골드 획득 후 이벤트
    private void GetGoldEvent(int goldCount)
    {
        GameManager.Instance.ProductObjectInfo(ConstValues.Gold, GameManager.Instance.GetTalk(99000), goldCount);
    }

    // 캐싱
    public void CacheObjects()
    {
        Transform monsterArray = roomGameObject.transform.Find(ConstValues.MonsterArray);
        if (monsterArray != null)
            monsters = monsterArray.GetComponentsInChildren<Monster>();
        
        Transform bossArray = roomGameObject.transform.Find(ConstValues.BossArray);
        if (bossArray != null)
            bosses = bossArray.GetComponentsInChildren<Monster>();
        
        Transform npcArray = roomGameObject.transform.Find(ConstValues.NpcArray);
        if (npcArray != null)
            npc = npcArray.GetComponentsInChildren<Npc>();
        
        Transform customObjectArray = roomGameObject.transform.Find(ConstValues.CustomObjectArray);
        if (customObjectArray != null)
            customObjects = customObjectArray.GetComponentsInChildren<CustomObject>();

        Transform interactionArray = roomGameObject.transform.Find(ConstValues.InteractionArray);
        if (interactionArray != null)
        {
            roomSkillAndPassive = interactionArray.GetComponentsInChildren<RoomSkillAndPassive>();
            roomTreasureBox = interactionArray.GetComponentsInChildren<RoomTreasureBox>();
            roomObjects = interactionArray.GetComponentsInChildren<RoomObject>();
            elevators = interactionArray.GetComponentsInChildren<Elevator>();
            arenas = interactionArray.GetComponentsInChildren<Arena>();
        }
        
        Transform goldObjectArray = roomGameObject.transform.Find(ConstValues.GoldObjectArray);
        if(goldObjectArray != null)
            goldObjects = goldObjectArray.GetComponentsInChildren<GoldObject>();
        
        Transform gridObject = roomGameObject.transform.Find(ConstValues.GridObject);
        if (gridObject != null)
        {
            productTriggers = gridObject.GetComponentsInChildren<ProductTrigger>();
            lockDoors = gridObject.GetComponentsInChildren<LockDoor>();
            
            leftEntrance = new List<RoomEntrance>();
            rightEntrance = new List<RoomEntrance>();
            upEntrance = new List<RoomEntrance>();
            downEntrance = new List<RoomEntrance>();

            RoomEntrance[] entranceArray = gridObject.GetComponentsInChildren<RoomEntrance>();
            foreach (var entrance in entranceArray)
            {
                if (entrance.name.Split(' ')[0] == $"{ConstValues.LeftEntrance}")
                    leftEntrance.Add(entrance);
                
                if (entrance.name.Split(' ')[0] == $"{ConstValues.RightEntrance}")
                    rightEntrance.Add(entrance);
                
                if (entrance.name.Split(' ')[0] == $"{ConstValues.UpEntrance}")
                    upEntrance.Add(entrance);
                
                if (entrance.name.Split(' ')[0] == $"{ConstValues.DownEntrance}")
                    downEntrance.Add(entrance);
            }
        }
        
        trapList.Clear();
        Transform[] allChildren = roomGameObject.GetComponentsInChildren<Transform>(true);
        foreach (var child in allChildren)
        {
            if (child.CompareTag(ConstValues.Trap))
            {
                var colliderList1 = child.GetComponentsInChildren<CompositeCollider2D>().ToList();
                var colliderList2 = child.GetComponentsInChildren<BoxCollider2D>().ToList();
                trapList.AddRange(colliderList1);
                trapList.AddRange(colliderList2);
            }
        }

        Transform playerPosArray = roomGameObject.transform.Find(ConstValues.PlayerPosArray);
        if (playerPosArray != null)
        {
            leftPlayerPos = new List<Transform>();
            rightPlayerPos = new List<Transform>();
            upPlayerPos = new List<Transform>();
            downPlayerPos = new List<Transform>();

            Transform[] possArray = playerPosArray.GetComponentsInChildren<Transform>();
            foreach (var poss in possArray)
            {
                if (poss.name.Split(' ')[0] == $"{ConstValues.LeftPlayerPos}")
                    leftPlayerPos.Add(poss);
                
                if (poss.name.Split(' ')[0] == $"{ConstValues.RightPlayerPos}")
                    rightPlayerPos.Add(poss);
                
                if (poss.name.Split(' ')[0] == $"{ConstValues.UpPlayerPos}")
                    upPlayerPos.Add(poss);
                
                if (poss.name.Split(' ')[0] == $"{ConstValues.DownPlayerPos}")
                    downPlayerPos.Add(poss);
            }
        }
        guideObjects = roomGameObject.GetComponentsInChildren<GuideObject>();

        var savePoint = roomGameObject.transform.Find(ConstValues.SavePoint);
        if(savePoint != null)
            saveObject = roomGameObject.transform.Find(ConstValues.SavePoint).GetComponentInChildren<SaveObject>();
        
        var portal = roomGameObject.transform.Find(ConstValues.PortalObject);
        if(portal != null) 
            portalObject = roomGameObject.transform.Find(ConstValues.PortalObject).GetComponentInChildren<PortalObject>();
    }
    
    // 방문 체크
    public string VisitedPlace()
    {
        if (!string.IsNullOrWhiteSpace(roomInfo.visitedInCells))
            return roomsData.place;

        return default;
    }
    
    // 문 열때 액션
    private async void OpenDoor(int idx, string keyId, Func<UniTask> openAction)
    {
        switch (keyId)
        {
            case ConstValues.KeyDungeon:
                await OpenDoorProduct1(openAction);
                break;
            case ConstValues.KeyMine:
                await OpenDoorProduct2(openAction);
                break;
        }
        
        // 이후 저장
        roomInfo.lockDoors[idx].isOpen = true;
        GameManager.Instance.SaveGame();
    }

    private async UniTask OpenDoorProduct1(Func<UniTask> openAction)
    {
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        GameManager.Instance.CurPlayer.ForceProduct();
        
        if (await GameManager.Instance.DialogueMove(-1.5f).SuppressCancellationThrow())
            return; 
        
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker);
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner);

        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = berserker.SpeechPos.position;
        var gunnerSpeechPos = gunner.SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10400));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10401));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10402));
        await NextDialog(speechFrame1);
        
        await openAction();

        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10403));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10404));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10405));
        await NextDialog(speechFrame1);
        
        GameManager.Instance.DialogueEnd();
        UIOn();
    }
    
    private async UniTask OpenDoorProduct2(Func<UniTask> openAction)
    {
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        GameManager.Instance.CurPlayer.ForceProduct();
        
        if (await GameManager.Instance.DialogueMove(1.5f).SuppressCancellationThrow())
            return; 
        
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker);
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner);

        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = berserker.SpeechPos.position;
        var gunnerSpeechPos = gunner.SpeechPos.position;
        var fighterSpeechPos = npc[0].SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10600));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10601));
        await NextDialog(speechFrame1);

        await openAction();
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10602));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10603));
        await NextDialog(speechFrame1);

        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10604));
        await NextDialog(speechFrame1);
        
        npc[0].Flip(-1);
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10605));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10606));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10607));
        await NextDialog(speechFrame1);
        
        berserker.Flip(1);
        gunner.Flip(1);
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10608));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10609));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10610));
        await NextDialog(speechFrame1);
        
        npc[0].SpawnObject(ConstValues.BangEffect, npc[0].CenterPos.position);
        npc[0].gameObject.SetActive(false);
        
        // 싸움꾼 합류 및 저장
        GameManager.Instance.AddPlayer(ConstValues.Fighter);
        GameManager.Instance.SetCharacterOrder();
        GameManager.Instance.CurPlayer.Flip(-1);

        UIOn();
        roomInfo.eventNpc[0].isActive = false;
        GameManager.Instance.DialogueEnd();

        GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30214)).Forget();
        UIOn();
    }
    
    private async UniTask EventRoomMove(Room pastRoom)
    {
        GameManager.Instance.StopPlayer();
        GameManager.Instance.RoomMoveSetting();

        // 모든 몬스터들의 행동 정지
        foreach (var monster in monsters)
            monster.CancelMotion();
        
        GameManager.Instance.CurPlayer.RoomMoveState();
        GameManager.Instance.CurPlayer.gameObject.SetActive(false);

        SetCameraLimit();
        pastRoom.ObjectActive(false);
        ObjectActive(true);
        RoomManager.Instance.CurrentRoom = this;
        RoomManager.Instance.CurrentRoom.SetGroundVector();
        
        // 여기서 몹 소환
        SpawnMonster();
        // 여기서 트랩 데이터 넣기
        SetTrap();
        // 여기서 세이브포인트 데이터 넣기
        SetSavePoint();
        // 여기서 포탈 데이터 넣기
        SetPortal();
        // 여기서 골드오브젝트 액션 넣기
        SetActionGoldObject();
        // 골드오브젝트 초기화
        RefreshGoldObject();

        GameManager.Instance.InitProductCancellation();
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.ClearLastPlatform();
        
        RoomManager.Instance.ActivePlaceName();
        GameManager.Instance.HidePlaceName();
        if(pastRoom.roomsData.place != roomsData.place)
            GameManager.Instance.RefreshPlaceName();
    }

    public async UniTask MinerQuestClearEvent()
    {
        foreach (var custom in roomInfo.customObject)
            custom.isActive = true;
        
        GameManager.Instance.SaveGame();
        GameManager.Instance.InitProductCancellation();

        float delay1 = 2.0f;
        float delay2 = 1.0f;
        
        UIOff();
        if (await GameManager.Instance.Fading(0, 1, 0.25f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        foreach (var customObject in customObjects)
            customObject.gameObject.SetActive(true);
        
        if (await GameManager.Instance.NormalDelay(delay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        if (await GameManager.Instance.Fading(1, 0, 0.25f, true, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        if (await GameManager.Instance.NormalDelay(delay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var minerSpeechPos = npc[0].SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10800));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10801));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10802));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10803));
        await NextDialog(speechFrame1);
        
        UIOn();
    }
    
    public async void BossEvent_Bomb()
    {
        GameManager.Instance.InitProductCancellation();

        float delay1 = 1.0f;
        float delay2 = 1.0f;
        float delay3 = 5.0f;
        float delay4 = 2.0f;
        float delay5 = 0.5f;
        float delay6 = 4.0f;
        
        var shortCutObject = shortCutObjects[2];
        var obstacle = shortCutObject.GetComponent<Shortcut_Obstacle>();

        var miner = npc[0];
        var minerFirstPos = miner.transform.position;
        var trolley = customObjects[0];

        UIOff();
        
        if (await GameManager.Instance.Fading(0, 1, 0.25f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        // 카메라 위치 조정
        GameManager.Instance.MainCamera.SetTarget(eventCameraPos[0].transform);
        trolley.transform.position = new Vector2(obstacle.DestroyPos.position.x + 3.0f, miner.transform.position.y);
        trolley.SetAnimationTrigger(ConstValues.Ride);
        miner.transform.position = new Vector2(obstacle.DestroyPos.position.x + 7.0f, miner.transform.position.y);
        miner.Flip(-1);
        var minerSpeechPos = npc[0].SpeechPos.position;

        if (await GameManager.Instance.NormalDelay(delay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        if (await GameManager.Instance.Fading(1, 0, 0.25f, true, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        if (await GameManager.Instance.NormalDelay(delay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        var berserkerSpeechPos = trolley.CustomTransforms[0].position;
        var gunnerSpeechPos = trolley.CustomTransforms[1].position;
        var fighterSpeechPos = trolley.CustomTransforms[2].position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);

        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10900));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10901));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10902));
        await NextDialog(speechFrame1);
        
        miner.CustomAnimTrigger(ENormalState.Idle, ConstValues.Point);
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10903));
        await NextDialog(speechFrame1);
        
        // 지진이 일어나는데 아무 일도 안 일어남
        trolley.SetAnimationTrigger(ConstValues.Ready);
        PlaySound(ConstValues.MonsterBigGhostEarthQuake);
        CameraShake(0.3f, 0.3f, 3.0f);
        if (await GameManager.Instance.NormalDelay(delay3, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        trolley.SetAnimationTrigger(ConstValues.Fail);
        if (await GameManager.Instance.NormalDelay(delay4, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10904));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10905));
        if (await GameManager.Instance.NormalDelay(delay5, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.TrolleyExplosion, trolley.CustomTransforms[3].position);
        trolley.SetAnimationTrigger(ConstValues.Start);

        var trolleyArrivePos = new Vector2(obstacle.DestroyPos.position.x - 10.0f, trolley.transform.position.y);
        Tween moveTween = trolley.transform.DOMove(trolleyArrivePos, 0.3f).SetEase(Ease.Linear);
        miner.CustomAnimTrigger(ENormalState.Idle, ConstValues.Afraid);
        if(await GameManager.Instance.WaitUntilDelay(() => trolley.transform.position.x < obstacle.DestroyPos.position.x, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        // 여기서 저장, 광차 삭제, 숏컷 삭제, Npc대화 변경
        trolley.gameObject.SetActive(false);
        foreach (var custom in roomInfo.customObject)
            custom.isActive = false;
        obstacle.BreakAndOpen();

        if (await GameManager.Instance.NormalDelay(delay6, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        miner.CustomAnimTrigger(ENormalState.Idle, ConstValues.Good);
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10906));
        if (await GameManager.Instance.NormalDelay(delay4, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        if (await GameManager.Instance.Fading(0, 1, 0.25f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;
        
        speechFrame1.SpeechEnd();
        miner.transform.position = minerFirstPos;
        moveTween.Kill();
        
        leftRoom[0].BossEvent_Bomb_Next(this);
    }

    // Dialogue.endEvent 중 Room 연출에 해당하는 키를 분기 실행
    // 새로운 Room 연출 endEvent를 추가할 때는 이 switch에만 case를 추가하면 됨
    public void PlayRoomEndEvent(string eventKey)
    {
        switch (eventKey)
        {
            case ConstValues.BossEvent1:
                BossEvent_Bomb();
                break;
        }
    }
    
    // 폭탄전차를 만나는 이벤트
    private async void BossEvent_Bomb_Next(Room pastRoom)
    {
        GameManager.Instance.InitProductCancellation();

        float delay1 = 1.0f;
        float delay2 = 1.0f;
        float delay3 = 5.0f;
        float delay4 = 6.0f;
        float delay5 = 4.0f;
        float delay6 = 2.0f;
        float delay7 = 0.3f;
        float delay8 = 4.0f;
        float delay9 = 5.0f;
        float delay10 = 1.0f;
        
        await EventRoomMove(pastRoom);
        foreach (var productTrigger in productTriggers)
            productTrigger.gameObject.SetActive(false);
        
        // 문 닫기
        bossTilemap.gameObject.SetActive(true);

        // 폭탄전차 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.Boss, false);
        bosses[0].Flip(-1);
        var monsterBomb = bosses[0].GetComponent<Monster_Bomb>();
        
        // 광부들 소환
        foreach (var person in npc)
        {
            person.gameObject.SetActive(true);
            person.CustomAnimTrigger(ENormalState.Idle, ConstValues.Afraid, ConstValues.Afraid);
        }
        
        // 카메라 위치 조정
        GameManager.Instance.MainCamera.SetTarget(eventCameraPos[0].transform);
        
        if (await GameManager.Instance.NormalDelay(delay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        if (await GameManager.Instance.Fading(1, 0, 0.25f, true, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        if (await GameManager.Instance.NormalDelay(delay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        var miner = npc[0];
        var minerSpeechPos = npc[0].SpeechPos.position;

        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        var speechFrame2 = SpeechFrame2();
        speechFrame2.gameObject.SetActive(false);
        
        var speechFrame3 = SpeechFrame3();
        speechFrame3.gameObject.SetActive(false);
        
        var strongFrame = StrongFrame();
        strongFrame.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, minerSpeechPos, GameManager.Instance.GetTalk(10907));
        await NextDialog(speechFrame1);

        PlaySound($"{monsterBomb.BasicStat.id}_{ConstValues.Pattern}");
        SpawnSpeechFrame(speechFrame1, monsterBomb.SpeechPos.position, GameManager.Instance.GetTalk(10908));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, monsterBomb.SpeechPos.position, GameManager.Instance.GetTalk(10909));
        monsterBomb.EventThrowBomb(npc);
        if (await GameManager.Instance.NormalDelay(delay3, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        PlaySound($"{monsterBomb.BasicStat.id}_{ConstValues.Pattern}");
        SpawnSpeechFrame(speechFrame1, monsterBomb.SpeechPos.position, GameManager.Instance.GetTalk(10910));
        await NextDialog(speechFrame1);
        speechFrame1.SpeechEnd();
        
        SpawnSpeechFrame(speechFrame2, monsterBomb.SpeechPos2.position, GameManager.Instance.GetTalk(10911));
        await NextDialog(speechFrame2);
        speechFrame2.SpeechEnd();
        
        PlaySound($"{monsterBomb.BasicStat.id}_{ConstValues.Pattern}");
        SpawnSpeechFrame(speechFrame1, monsterBomb.SpeechPos.position, GameManager.Instance.GetTalk(10912));
        await NextDialog(speechFrame1);
        speechFrame1.SpeechEnd();
        
        // 이벤트 쿵쿵
        monsterBomb.EventSmashGround();
        SpawnSpeechFrame(strongFrame, new Vector2(monsterBomb.SpeechPos2.position.x - 2.0f, monsterBomb.SpeechPos2.position.y + 1.0f), GameManager.Instance.GetTalk(10913));

        if (await GameManager.Instance.NormalDelay(delay4, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        strongFrame.SpeechEnd();
        
        PlaySound(ConstValues.MonsterBigGhostEarthQuake);
        CameraShake(0.3f, 0.3f, 3.0f);
        if (await GameManager.Instance.NormalDelay(delay5, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        monsterBomb.Flip(1);
        // 카메라 위치 조정
        GameManager.Instance.MainCamera.SetTarget(eventCameraPos[1].transform);
        if (await GameManager.Instance.NormalDelay(delay6, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        SpawnSpeechFrame(speechFrame1, monsterBomb.SpeechPos.position, GameManager.Instance.GetTalk(10914));
        await NextDialog(speechFrame1);
        speechFrame1.SpeechEnd();

        // 광차 등장
        PlaySound(ConstValues.TrolleyStart);
        var trolley = customObjects[0];
        trolley.gameObject.SetActive(true);
        trolley.SetAnimationTrigger(ConstValues.Start);
        var trolleyArrivePos = new Vector2(monsterBomb.transform.position.x, trolley.transform.position.y);
        Tween moveTween = trolley.transform.DOMove(trolleyArrivePos, delay7).SetEase(Ease.Linear);
        if (await GameManager.Instance.NormalDelay(delay7, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        trolley.gameObject.SetActive(false);
        // 항상 광전사가 맨 앞에 있어야 함
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        var fighter = GameManager.Instance.GetPlayer(ConstValues.Fighter).GetComponent<Player_Fighter>();

        var centerPos = monsterBomb.CenterPos.position;
        
        berserker.transform.position = centerPos;
        gunner.transform.position = centerPos;
        fighter.transform.position = centerPos;
        
        berserker.gameObject.SetActive(true);
        gunner.gameObject.SetActive(true);
        fighter.gameObject.SetActive(true);
        
        berserker.Flip(-1);
        gunner.Flip(-1);
        fighter.Flip(-1);
        
        // 쾅!
        GameManager.Instance.StandLock = true;
        PlaySound($"{monsterBomb.BasicStat.id}_{ConstValues.Die}");
        PlaySound($"{ConstValues.Player}_{ConstValues.Scream}");
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.TrolleyExplosion, centerPos);
        monsterBomb.Airborne(-2, 12, true);
        berserker.Airborne(3, 14, true);
        gunner.Airborne(4, 15, true);
        fighter.Airborne(2, 12, true);
        
        if (await GameManager.Instance.NormalDelay(delay8, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        // 카메라 위치 조정
        GameManager.Instance.MainCamera.SetTarget(eventCameraPos[0].transform);
        if (await GameManager.Instance.NormalDelay(delay6, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        miner.CustomAnimTrigger(ENormalState.Idle, ConstValues.Point);
        SpawnSpeechFrame(strongFrame, new Vector2(miner.SpeechPos.position.x, miner.SpeechPos.position.y + 2.0f), GameManager.Instance.GetTalk(10915));
        await NextDialog(strongFrame);
        strongFrame.SpeechEnd();

        // 광부들이 도망감
        PlaySound($"{ConstValues.Scream}6");
        CameraShake(0.1f, 0.1f, 3.0f);
        var arrivePos = new Vector2(rightPlayerPos[0].position.x + 3.0f, npc[0].transform.position.y);
        foreach (var person in npc)
        {
            float second = Random.Range(0.1f, 0.2f);
            float speed = Random.Range(7.0f, 9.0f);
            person.EpisodeMove_X(arrivePos, speed, 1).Forget();
            if (await GameManager.Instance.NormalDelay(second, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }
        if (await GameManager.Instance.NormalDelay(delay9, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        foreach (var person in npc)
            person.gameObject.SetActive(false);
        
        // 카메라 위치 조정
        GameManager.Instance.MainCamera.SetTarget(eventCameraPos[1].transform);
        if (await GameManager.Instance.NormalDelay(delay6, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        berserker.EventStand();
        gunner.EventStand();
        fighter.EventStand();
        
        if (await GameManager.Instance.NormalDelay(delay10, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        SpawnSpeechFrame(speechFrame1, fighter.SpeechPos.position, GameManager.Instance.GetTalk(10916));
        await NextDialog(speechFrame1);
        speechFrame1.SpeechEnd();

        monsterBomb.EventStand();
        monsterBomb.EventExplosion();
        
        if (await GameManager.Instance.NormalDelay(delay10, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        PlaySound($"{monsterBomb.BasicStat.id}_{ConstValues.Pattern}");
        SpawnSpeechFrame(speechFrame1, monsterBomb.SpeechPos.position, GameManager.Instance.GetTalk(10917));
        await NextDialog(speechFrame1);
        speechFrame1.SpeechEnd();
        
        PlaySound($"{monsterBomb.BasicStat.id}_laugh");
        SpawnSpeechFrame(speechFrame3, monsterBomb.SpeechPos2.position, GameManager.Instance.GetTalk(10918));
        await NextDialog(speechFrame3);
        speechFrame3.SpeechEnd();
        
        SpawnSpeechFrame(speechFrame1, fighter.SpeechPos.position, GameManager.Instance.GetTalk(10919));
        await NextDialog(speechFrame1);
        speechFrame1.SpeechEnd();

        moveTween.Kill();
        GameManager.Instance.DialogueEnd();
        GameManager.Instance.MainCamera.SetTarget(GameManager.Instance.CurPlayer.transform);
        BgmManager.Instance.DelayStop(0.1f);
        if (await GameManager.Instance.NormalDelay(delay10, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.StandLock = false;
        PlayBGM($"{ConstValues.BGM}_{ConstValues.Bomb}", true);
        GameManager.Instance.ControlStart = true;
        bosses[0].EventAppear(SpawnBossMessage);
        UIOn();
        
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        StopBGM();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }

    // 캐싱
    public void ObjectNameChange()
    {
        Transform playerPosArray = roomGameObject.transform.Find(ConstValues.PlayerPosArray);
        if (playerPosArray != null)
        {
            Transform[] possArray = playerPosArray.GetComponentsInChildren<Transform>();
            foreach (var poss in possArray)
            {
                if (poss.name == $"{ConstValues.LeftPlayerPos}_1")
                    poss.name = ConstValues.LeftPlayerPos;
                if (poss.name == $"{ConstValues.LeftPlayerPos}_2")
                    poss.name = $"{ConstValues.LeftPlayerPos} (1)";
                
                if (poss.name == $"{ConstValues.RightPlayerPos}_1")
                    poss.name = ConstValues.RightPlayerPos;
                if (poss.name == $"{ConstValues.RightPlayerPos}_2")
                    poss.name = $"{ConstValues.RightPlayerPos} (1)";
                
                if (poss.name == $"{ConstValues.UpPlayerPos}_1")
                    poss.name = ConstValues.UpPlayerPos;
                if (poss.name == $"{ConstValues.UpPlayerPos}_2")
                    poss.name = $"{ConstValues.UpPlayerPos} (1)";
                
                if (poss.name == $"{ConstValues.DownPlayerPos}_1")
                    poss.name = ConstValues.DownPlayerPos;
                if (poss.name == $"{ConstValues.DownPlayerPos}_2")
                    poss.name = $"{ConstValues.DownPlayerPos} (1)";
            }
        }
        
        Transform gridObject = roomGameObject.transform.Find(ConstValues.GridObject);
        if (gridObject != null)
        {
            RoomEntrance[] entranceArray = gridObject.GetComponentsInChildren<RoomEntrance>();
            foreach (var entrance in entranceArray)
            {
                if (entrance.name == $"{ConstValues.LeftEntrance}_1")
                    entrance.name = ConstValues.LeftEntrance;
                if (entrance.name == $"{ConstValues.LeftEntrance}_2")
                    entrance.name = $"{ConstValues.LeftEntrance} (1)";
                
                if (entrance.name == $"{ConstValues.RightEntrance}_1")
                    entrance.name = ConstValues.RightEntrance;
                if (entrance.name == $"{ConstValues.RightEntrance}_2")
                    entrance.name = $"{ConstValues.RightEntrance} (1)";
                
                if (entrance.name == $"{ConstValues.UpEntrance}_1")
                    entrance.name = ConstValues.UpEntrance;
                if (entrance.name == $"{ConstValues.UpEntrance}_2")
                    entrance.name = $"{ConstValues.UpEntrance} (1)";
                
                if (entrance.name == $"{ConstValues.DownEntrance}_1")
                    entrance.name = ConstValues.DownEntrance;
                if (entrance.name == $"{ConstValues.DownEntrance}_2")
                    entrance.name = $"{ConstValues.DownEntrance} (1)";
            }
        }
    }
}
