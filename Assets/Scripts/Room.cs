using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

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
    [SerializeField] protected RoomItem[] roomItem;
    [SerializeField] protected RoomTreasureBox[] roomTreasureBox;
    [SerializeField] protected Elevator[] elevators;
    [SerializeField] protected LockDoor[] lockDoors;
    [SerializeField] protected Arena[] arenas;
    [SerializeField] protected GoldObject[] goldObjects;
    
    [SerializeField] private Transform minCameraLimitX;
    [SerializeField] private Transform maxCameraLimitX;
    [SerializeField] private Transform minCameraLimitY;
    [SerializeField] private Transform maxCameraLimitY;

    [SerializeField] private SaveObject saveObject;
    [SerializeField] private PortalObject portalObject;
    [SerializeField] private MerchantObject merchantObject;
    
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
    [SerializeField] protected List<Collider2D> trapList = new List<Collider2D>();
    [SerializeField] protected ShortcutObject[] shortCutObjects;
    
    [SerializeField] protected Transform monsterLimitLeft;
    [SerializeField] protected Transform monsterLimitRight;
    
    [SerializeField] protected ProductTrigger[] productTriggers;
    [SerializeField] protected GuideObject[] guideObjects;
    [SerializeField] protected Transform[] customMovePos;
    [SerializeField] protected Transform[] strongSpeechPos;
    
    [SerializeField] protected SpriteRenderer[] bgSpriteRenderers;
    [SerializeField] private TileFactory bossTilemap;
    [SerializeField] private GameObject[] roomCustomObjects;
    [SerializeField] private RoomInfo roomInfo;

    private RoomsData roomsData;

    private Vector2 firstMaxLimit;
    private Vector2 firstMinLimit;

    public string Id    => roomInfo.roomId;
    public string Place => GameManager.Instance.GetPlaceName(roomsData.place);
    
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
    }

    private void Update()
    {
        if(roomGameObject.activeSelf)
            RevealCellsInView();
    }

    private void OnApplicationQuit()
    {
        SaveVisitedFrameCells();
        SaveVisitedInCells();
        SaveVisitedShortcutCells();
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
        foreach (var character in npc)
        {
            character.AddData();
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

    public async void InfoSetting()
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
        else if (shortCutObjects.Length < roomInfo.shortCut.Count)
        {
            roomInfo.shortCut.Clear();
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
                    var skillAndPassive = new SkillAndPassive()
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
                    
                    var treasureBox = new TreasureBox()
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
        
        // 맵에 널려있는 아이템 세팅
        var itemArray = roomsData.item.Split('ㅗ');
        if (roomInfo.item.Count < itemArray.Length)
        {
            if (!string.IsNullOrWhiteSpace(itemArray[0]))
            {
                for (int i = 0; i < itemArray.Length; i++)
                {
                    var itemInfoArray = itemArray[i].Split(';');
                    var itemName = itemInfoArray[0];
                    var itemCount = int.Parse(itemInfoArray[1]);
                    
                    var item = new Item()
                    {
                        id = itemName,
                        count = itemCount,
                        alreadyGet = false,
                    };
                    roomInfo.item.Add(item);
                }
                GameManager.Instance.SaveGame();
            }
        }
        else if (itemArray.Length < roomInfo.item.Count)
        {
            roomInfo.item.Clear();
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
                    idx = 0
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
        
        // 아이템을 얻었으면, 그 아이템은 비활성화
        for (var i = 0; i < roomItem.Length; i++)
        {
            int idx = i;

            if (roomInfo.item[idx].alreadyGet)
            {
                roomItem[idx].gameObject.SetActive(false);
            }
            else
            {
                roomItem[idx].SetInteractionAction();
                roomItem[idx].SetAction(() =>
                {
                    roomItem[idx].IsGet = true;
                    roomInfo.item[idx].alreadyGet = true;
                    roomItem[idx].ReduceInteractionObject();
                    GetItem(roomInfo.item[idx].id, roomInfo.item[idx].count);
                    SoundManager.Instance.PlaySound(ConstValues.Pickup);
                    GameManager.Instance.GetItemProduct(roomInfo.item[idx].id);
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
                        case ConstValues.AttributePoint:
                            GameManager.Instance.GetAttributeProduct(roomInfo.treasureBox[idx].count, GetAttributeEvent);
                            break;
                        case ConstValues.Gold:
                            GameManager.Instance.GetGoldProduct(roomInfo.treasureBox[idx].count, roomTreasureBox[idx].transform.position, GetGoldEvent);
                            break;
                    }

                    GameManager.Instance.SaveGame();
                });
            }
        }
        
        // 엘리베이터
        for (int i = 0; i < roomInfo.elevators.Count; i++)
        {
            int idx = i;
            
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

    public void RefreshTalk()
    {
        // 여기서 npc 활성화
        foreach (var person in npc)
            person.RefreshTalkText();

        // 아이템을 얻었으면, 그 아이템은 비활성화
        foreach (var item in roomItem)
            item.RefreshTalkText();

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

    public void RefreshKey()
    {
        // 여기서 npc 활성화
        foreach (var person in npc)
            person.RefreshKeyText(GameManager.Instance.upKey);

        // 아이템을 얻었으면, 그 아이템은 비활성화
        foreach (var item in roomItem)
            item.RefreshKeyText(GameManager.Instance.upKey);

        // 보물상자를 열었으면, 열린 상태로 나오게 조정
        foreach (var treasureBox in roomTreasureBox)
            treasureBox.RefreshKeyText(GameManager.Instance.upKey);

        // 엘리베이터
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

    private async void MovePortal(string roomId)
    {
        var targetRoom = RoomManager.Instance.TargetRoom(roomId);
        GameManager.Instance.StopPlayer();

        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.FadeCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.BangEffect, GameManager.Instance.CurPlayer.CenterPos.position);
        GameManager.Instance.CurPlayer.gameObject.SetActive(false);
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.FadeCancellation).SuppressCancellationThrow())
            return;
        
        await GameManager.Instance.Fading(0, 1, 0.25f, false, ConstValues.BlackColor);

        ObjectActive(false);
        GameManager.Instance.CurPlayer.RoomMoveState();
        
        targetRoom.ObjectActive(true);
        targetRoom.SetCameraLimit();
        targetRoom.SetPortal();
        targetRoom.PortalSoundActive(false);

        RoomManager.Instance.CurrentRoom = targetRoom;
        RoomManager.Instance.CurrentRoom.SetGroundVector();

        GameManager.Instance.CurPlayer.transform.position = targetRoom.portalObject.PlayerPos.position;
        
        GameManager.Instance.InitFadeCancellation();
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.FadeCancellation).SuppressCancellationThrow())
            return;
        
        await GameManager.Instance.Fading(1, 0, 0.25f, true, ConstValues.BlackColor);
        
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.FadeCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.CurPlayer.SpawnObject(ConstValues.BangEffect, GameManager.Instance.CurPlayer.CenterPos.position);
        GameManager.Instance.CurPlayer.gameObject.SetActive(true);
        
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.FadeCancellation).SuppressCancellationThrow())
            return;
        
        targetRoom.PortalSoundActive(true);
        
        GameManager.Instance.CurPlayer.GravityChange(ConstValues.BasicGravity);
        GameManager.Instance.MovePlayer();
        GameManager.Instance.CurPlayer.ClearLastPlatform();
        
        RoomManager.Instance.ActivePlaceName();
        GameManager.Instance.HidePlaceName();
        if(roomsData.place != targetRoom.roomsData.place)
        {
            GameManager.Instance.RefreshPlaceName();
        }
    }

    private async void SettingRoom(int idx, EntranceDir dir, Room pastRoom)
    {
        GameManager.Instance.StopPlayer();
        GameManager.Instance.RoomMoveSetting();

        // 모든 몬스터들의 행동 정지
        foreach (var monster in monsters)
            monster.CancelMotion();
        
        await GameManager.Instance.Fading(0, 1, 0.25f, false, ConstValues.BlackColor);

        switch (dir)
        {
            case EntranceDir.Left:
                SetLeftPlayerPos(idx);
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Right:
                SetRightPlayerPos(idx);
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

        GameManager.Instance.InitFadeCancellation();
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.FadeCancellation).SuppressCancellationThrow())
            return;
        
        await GameManager.Instance.Fading(1, 0, 0.25f, true, ConstValues.BlackColor);
        GameManager.Instance.CurPlayer.GravityChange(ConstValues.BasicGravity);

        switch (dir)
        {
            case EntranceDir.Up:
                await GameManager.Instance.CurPlayer.EntranceDown();
                break;

            case EntranceDir.Down:
                await GameManager.Instance.CurPlayer.EntranceJump();
                break;
        }
        GameManager.Instance.MovePlayer();
        GameManager.Instance.CurPlayer.ClearLastPlatform();
        
        // 여기서 BGM재생
        SetBgm(false);
        RoomManager.Instance.ActivePlaceName();
        GameManager.Instance.HidePlaceName();
        if(pastRoom.roomsData.place != roomsData.place)
        {
            GameManager.Instance.RefreshPlaceName();
        }
    }
    
    private void SetLeftPlayerPos(int idx)
    {
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos[idx].position;
    }

    private void SetRightPlayerPos(int idx)
    {
        GameManager.Instance.CurPlayer.transform.position = rightPlayerPos[idx].position;
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
        GameManager.Instance.MainCamera.SetCameraLimit(firstMaxLimit, firstMinLimit);
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

    private void GetItem(string id, int count)
    {
        var itemInfo = new HaveItemInfo()
        {
            id = id,
            count = count,
        };
        GameManager.Instance.ItemList.Add(itemInfo);
    }

    private void GetTreasureBoxItem(string id, int count, Vector2 itemVector)
    {
        switch (id)
        {
            case ConstValues.Gold:
                PlusGold(count, itemVector);
                break;
            case ConstValues.AttributePoint:
                PlusAttributePoint(count);
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
    
    private void PlusAttributePoint(int attributePoint)
    {
        GameManager.Instance.PlusAttributePoint(attributePoint);
    }

    // 숏컷정보 저장
    public void ShortcutOpen(string id)
    {
        var targetShortcut = roomInfo.shortCut.Find(x => x.id == id);
        
        if (targetShortcut == null)
            return;
        
        targetShortcut.isOpened = true;
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

    // 숏컷과 세이브 오브젝트 진행도에 맞춰 변경(미니맵 포함)
    public void SetShortCutAndMinimapObject()
    {
        int idx = 0;
        for (int i = 0; i < shortCutObjects.Length; i++)
        {
            if (shortCutObjects[i].GetComponent<Shortcut_Crush>())
            {
                var crush = shortCutObjects[i].GetComponent<Shortcut_Crush>();
                crush.TargetRoom = shortCutRoom[idx];
                idx += 1;
            }
        }

        for (int i = 0; i < shortCutObjects.Length; i++)
            shortCutObjects[i].OpenSetting(roomInfo.shortCut[i].isOpened, ShortcutOpen);
        
        Transform tilemapObject = roomGameObject.transform.Find(ConstValues.TilemapObject);
        if (saveObject)
        {
            saveObject.SetParents(tilemapObject);
            // 저장 데이터 있음: 세이브 오브젝트 미니맵 활성화 / 저장 데이터 없음: 세이브 오브젝트 미니맵 비활성화
            saveObject.MinimapObject.SetActive(roomInfo.savePointCheck);
        }
        if (portalObject)
        {
            portalObject.SetParents(tilemapObject);
            // 저장 데이터 있음: 세이브 오브젝트 미니맵 활성화 / 저장 데이터 없음: 세이브 오브젝트 미니맵 비활성화
            portalObject.MinimapObject.SetActive(roomInfo.portalCheck);
        }
        if (merchantObject)
        {
            merchantObject.SetParents(tilemapObject);
            // 저장 데이터 있음: 세이브 오브젝트 미니맵 활성화 / 저장 데이터 없음: 세이브 오브젝트 미니맵 비활성화
            merchantObject.MinimapObject.SetActive(roomInfo.merchantCheck);
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

        return wallList[idx].name;
    }

    private void SetSavePoint()
    {
        if (!saveObject)
            return;
        
        saveObject.SetSaveAction(() =>
        {
            RoomManager.Instance.AllMonsterArrive();
            GameManager.Instance.SavePoint = name;
            GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30206)).Forget();
            GameManager.Instance.RefillPlayerHp();
            saveObject.InteractionObject.FadeOut();
            SoundManager.Instance.PlaySound(ConstValues.SlotEquip);
            GameManager.Instance.SaveGame();
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
            portalObject.SoundActive(false);
            portalObject.ReduceInteractionObject();
            MovePortal(portalObject.TargetRoom);
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
        if (isBossRoom)
            return;
        
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

    private void SpawnBoss(Monster boss, Vector2 pos, EMonsterType monsterType)
    {
        boss.transform.position = pos;
        boss.MonsterType = monsterType;
        boss.AlwaysAgro = true;
        boss.LimitLeft = monsterLimitLeft.position.x;
        boss.LimitRight = monsterLimitRight.position.x;
        boss.SetGoldAction(PlusGold);
        boss.SpawnHpBar();
        boss.gameObject.SetActive(true);
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

    // 맵에 있는 모든 몹을 잡았을 경우 발생하는 액션
    protected async void MonsterClearAction(Action action)
    {
        if (await WaitUntil(() => DieMonsterCount() == 0, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;
        action?.Invoke();
    }
    protected async void MonsterClearAction(Func<UniTask> asyncAction)
    {
        if (await WaitUntil(() => DieMonsterCount() == 0, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;
        asyncAction?.Invoke();
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
    private void PlaySound(string bgmName)
    {
        SoundManager.Instance.PlaySound(bgmName);
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
        roomInfo.savePointCheck = true;
    }
    
    private void SavePortalObject()
    {
        portalObject.MinimapObject.SetActive(true);
        roomInfo.portalCheck = true;
    }
    
    private void SaveMerchantObject()
    {
        merchantObject.MinimapObject.SetActive(true);
        roomInfo.merchantCheck = true;
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

    // 최초 스타트
    private async void Product1()
    {
        // 연출 시작 전 세팅
        GameManager.Instance.InitProductCancellation();
        
        firstStart = true;
        roomCustomObjects[0].SetActive(false);
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
        
        bosses[0].enabled = false;
        bosses[0].transform.position = bossPos[0].position;
        bosses[0].gameObject.SetActive(true);
        bosses[0].Flip(-1);
        
        // 에피소드 팝업부터 시작
        GameManager.Instance.ControlStart = false;
        
        if(await WaitUntil(() => !GameManager.Instance.FadeSystem.gameObject.activeSelf, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        StopBGM();
        PlayBGM(ConstValues.BGMEpisodeStart, true);
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        int titleTalk = TableManager.Instance.productDialogueTable.ProductDialogue.Find(x => x.id == ConstValues.Episode1Title).talk;
        string title = GameManager.Instance.GetTalk(titleTalk);
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product1);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(GameManager.Instance.GetTalk(productDialogue.talk));

        //await RoomManager.Instance.ProductEpisode(title);
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

        var berserkerPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        var speechFrame2 = SpeechFrame2();
        speechFrame2.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerPos, talkList[0]);
        await NextDialog(speechFrame1);

        SpawnSpeechFrame(speechFrame1, berserkerPos, talkList[1]);
        await NextDialog(speechFrame1);

        PlayBGM(ConstValues.BGMSunHill, true);
        PlaySound(ConstValues.PlayerScream);
        CameraShake(0.1f, 0.4f, 1.0f);
        SpawnSpeechFrame(speechFrame1, new Vector2(berserkerPos.x, berserkerPos.y + 0.5f), talkList[2]);
        for (int i = 0; i < 2; i++)
        {
            GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }

        await NextDialog(speechFrame1);

        var sunPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
        
        SpawnSpeechFrame(speechFrame2, sunPos, talkList[3]);
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
        roomCustomObjects[0].SetActive(true);
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
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitProductCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product2);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(GameManager.Instance.GetTalk(productDialogue.talk));
        
        UIOff();
            
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;

        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[0]);
        await NextDialog(speechFrame1);

        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[1]);
        await NextDialog(speechFrame1);

        RoomManager.Instance.Guide(40000);
        
        UIOn();
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
    }

    // 세이브 포인트 발견
    private async void Product3()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitProductCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product3);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(GameManager.Instance.GetTalk(productDialogue.talk));

        UIOff();
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[0]);
        await NextDialog(speechFrame1);

        RoomManager.Instance.Guide(40001);
        UIOn();
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
    }
    
    // 태양과 대결
    private async void Product4()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitProductCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product4);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(GameManager.Instance.GetTalk(productDialogue.talk));
        
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

        PlayBGM(ConstValues.BGMBoss, true);
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
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[0]);
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[1]);
            await NextDialog(speechFrame1);
            
            SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[2]); 
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
            await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1);

            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            GameManager.Instance.CurPlayer.ForceIdle();
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[3]); 
            await NextDialog(speechFrame2);

            SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[4]); 
            await NextDialog(speechFrame2);
        }
        
        // BGM 끄기
        StopBGM();
        
        if (roomInfo.roomProduct[0].count == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[5]);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(1, 0);
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[6]);
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
            
            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[7]);
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[8]);
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[9]);
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
        await fadeBg.Fade();
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.roomProduct[0].count == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[10]);
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[11]);
        }

        RoomManager.Instance.BgSpriteChange(ConstValues.BgSunHillNight);
        RoomManager.Instance.BgDecoActive(false);
        
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        fadeBg.gameObject.SetActive(false);
        BgmManager.Instance.Play();
        PlayBGM(ConstValues.BGMBoss, true);

        // 달 보스 소환
        SpawnBoss(bosses[1], new Vector2(bossPos[0].transform.position.x, bossPos[0].transform.position.y + 3.5f), EMonsterType.Boss);

        if (roomInfo.roomProduct[0].count == 1)
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
            
            berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
            moonSpeechPos = new Vector2(bosses[1].CenterPos.position.x - 2.0f, bosses[1].CenterPos.position.y);
            
            await NextDialog(speechFrame1);

            SpawnSpeechFrame(speechFrame2, moonSpeechPos, talkList[12]); 
            await NextDialog(speechFrame2);
            
            SpawnSpeechFrame(speechFrame2, moonSpeechPos, talkList[13]); 
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
        await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1);

        PlaySound($"{ConstValues.Scream}12");
        bosses[1].DieShake();
        bosses[1].GetComponent<Monster_Moon>().DieBomb();

        SpawnSpeechFrame(speechFrame2, moonSpeechPos, talkList[15]);
        await NextDialog(speechFrame2);

        SpawnSpeechFrame(speechFrame2, moonSpeechPos, talkList[16]);
        await NextDialog(speechFrame2);

        bosses[1].DieExplosion();
        BgmManager.Instance.Stop();
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        fadeBg.gameObject.SetActive(true);
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade();
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        RoomManager.Instance.BgSpriteChange(ConstValues.BgSunHill);
        RoomManager.Instance.BgDecoActive(true);
        
        berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[17]); 
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[18]); 
        await NextDialog(speechFrame1);
        
        PlayBGM(roomsData.bgm, true);
        PlaySound(ConstValues.ChickenCock);
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[19]); 
        await NextDialog(speechFrame1);
        
        PlaySound(ConstValues.RewardPage);
        npc[0].gameObject.SetActive(true);
        npc[0].transform.localScale = new Vector3(-1, 1, 1);
        var npcArrivePos = npc[0].transform.position;
        npc[0].gameObject.transform.position = new Vector2(npc[0].transform.position.x, npc[0].transform.position.y + 3.5f);
        await npc[0].EpisodeMove_Y(npcArrivePos, bosses[0].BasicStat.moveSpeed);
        sunSpeechPos = npc[0].SpeechPos.position;
        
        SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[20]); 
        await NextDialog(speechFrame2);
        
        SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[21]); 
        await NextDialog(speechFrame2);
        
        SpawnSpeechFrame(speechFrame2, sunSpeechPos, talkList[22]); 
        await NextDialog(speechFrame2);

        // 문 열기
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        roomInfo.eventNpc[0].isActive = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }

    // 거너를 만남
    private async void Product5()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitProductCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product5);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(GameManager.Instance.GetTalk(productDialogue.talk));

        UIOff();
        
        // 연출 시작 전 세팅
        // 에피소드 팝업부터 시작
        int titleTalk = TableManager.Instance.productDialogueTable.ProductDialogue.Find(x => x.id == ConstValues.Episode2Title).talk;
        string title = GameManager.Instance.GetTalk(titleTalk);
        //await RoomManager.Instance.ProductEpisode(title);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.SpeechPos.position;
        var gunnerSpeechPos = ((Character)npc[0]).SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[0]);
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[1]);
        await NextDialog(speechFrame1);
        
        npc[0].Flip(-1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, talkList[2]);
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, talkList[3]);
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[4]);
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, talkList[5]);
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, talkList[6]);
        await NextDialog(speechFrame1);
        
        npc[0].SpawnObject(ConstValues.BangEffect, npc[0].CenterPos.position);
        npc[0].gameObject.SetActive(false);
        
        // 2인 캐릭터 설정 및 저장
        GameManager.Instance.AddPlayer(ConstValues.Gunner);
        GameManager.Instance.SetCharacterOrder();
        RoomManager.Instance.Guide(40004);
        
        UIOn();
        roomInfo.roomProduct[0].isFinish = true;
        roomInfo.eventNpc[0].isActive = false;
        GameManager.Instance.SaveGame();
    }
    
    // 아레나 대전
    private void Product6()
    {
        StartArena();
    }
    
    private async void Product7()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
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

        PlayBGM(ConstValues.BGMBoss, true);
        
        // 암살자 보스 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.Boss);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        UIOn();
        
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;
        
        StopBGM();
        GameManager.Instance.InitWaitCancellation();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;

        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }
    
    private void Product8()
    {
        StartArena();
    }
    
    private async void Product9()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
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

        PlayBGM(ConstValues.BGMMiniBoss, true);
        
        // 스톤골렘 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.MiniBoss);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        UIOn();
        
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;
        
        StopBGM();
        GameManager.Instance.InitWaitCancellation();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;

        // 문 열기
        PlayBGM(roomsData.bgm, true);
        BossTileMapActive(false);
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }

    private async void Product10()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
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

        PlayBGM(ConstValues.BGMBoss, true);
        
        // 폭탄전차 소환
        SpawnBoss(bosses[0], new Vector2(bosses[0].transform.position.x, bosses[0].transform.position.y), EMonsterType.Boss);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        UIOn();
        
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;
        
        StopBGM();
        GameManager.Instance.InitWaitCancellation();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
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

        PlayBGM(ConstValues.BGMArena, true);
        
        GameManager.Instance.MovePlayer();
        await arenas[0].RoundStart();
        GameManager.Instance.StopPlayer();
        await arenas[0].RoundEnd();
        
        SetBgm(true);
        GameManager.Instance.MainCamera.SetCameraLimit(firstMaxLimit, firstMinLimit);
        
        roomInfo.roomProduct[0].isFinish = true;
        GameManager.Instance.SaveGame();
    }
    
    // 스킬 획득 후 이벤트
    private async void GetSkillEvent(string skillName)
    {
        string getMessage = string.Format(GameManager.Instance.GetTalk(30200), skillName);;
        
        if (GameManager.Instance.FirstGetSkill)
        {
            await GameManager.Instance.SpawnWarningPopup(getMessage);
        }
        else
        {
            GameManager.Instance.FirstGetSkill = true;
            //GameManager.Instance.SaveGame();

            UIOff();
            GameManager.Instance.CurPlayer.ForceProduct();
            await GameManager.Instance.SpawnWarningPopup(getMessage);
            
            GameManager.Instance.InitProductCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(40002);
            UIOn();
        }
    }
    
    // 특성 포인트 획득 후 이벤트
    private async void GetAttributeEvent(int pointCount)
    {
        string getMessage = string.Format(GameManager.Instance.GetTalk(30201), pointCount.ToString());
        
        if (GameManager.Instance.FirstGetAttribute)
        {
            await GameManager.Instance.SpawnWarningPopup(getMessage);
        }
        else
        {
            GameManager.Instance.FirstGetAttribute = true;
            GameManager.Instance.StopPlayer();

            UIOff();
            GameManager.Instance.CurPlayer.ForceProduct();
            await GameManager.Instance.SpawnWarningPopup(getMessage);
            
            GameManager.Instance.InitProductCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(40003);
            UIOn();
            GameManager.Instance.MovePlayer();
        }
    }
    
    // 골드 획득 후 이벤트
    private async void GetGoldEvent(int goldCount)
    {
        string getMessage = string.Format(GameManager.Instance.GetTalk(30210), goldCount.ToString());
        await GameManager.Instance.SpawnWarningPopup(getMessage);
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
        
        Transform interactionArray = roomGameObject.transform.Find(ConstValues.InteractionArray);
        if (interactionArray != null)
        {
            roomSkillAndPassive = interactionArray.GetComponentsInChildren<RoomSkillAndPassive>();
            roomTreasureBox = interactionArray.GetComponentsInChildren<RoomTreasureBox>();
            roomItem = interactionArray.GetComponentsInChildren<RoomItem>();
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
                trapList.Add(child.GetComponent<Collider2D>());
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
            case ConstValues.KeyForest:
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
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        GameManager.Instance.CurPlayer.ForceProduct();
        await GameManager.Instance.DialogueMove(1.5f);
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker);
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner);
        
        berserker.Flip(-1);
        gunner.Flip(-1);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = berserker.SpeechPos.position;
        var gunnerSpeechPos = gunner.SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10137));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10138));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10139));
        await NextDialog(speechFrame1);
        
        await openAction();

        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10140));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10141));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10142));
        await NextDialog(speechFrame1);
        
        GameManager.Instance.DialogueEnd();
        UIOn();
    }
    
    private async UniTask OpenDoorProduct2(Func<UniTask> openAction)
    {
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitProductCancellation();
        
        UIOff();
        GameManager.Instance.CurPlayer.ForceProduct();
        await GameManager.Instance.DialogueMove(1.5f);
        var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker);
        var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner);

        berserker.Flip(-1);
        gunner.Flip(-1);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = berserker.SpeechPos.position;
        var gunnerSpeechPos = gunner.SpeechPos.position;
        var fighterSpeechPos = npc[0].SpeechPos.position;
        
        var speechFrame1 = SpeechFrame1();
        speechFrame1.gameObject.SetActive(false);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10143));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10144));
        await NextDialog(speechFrame1);

        await openAction();
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10145));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10146));
        await NextDialog(speechFrame1);

        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10147));
        await NextDialog(speechFrame1);
        
        npc[0].Flip(-1);
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10148));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10149));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, fighterSpeechPos, GameManager.Instance.GetTalk(10150));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10151));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, gunnerSpeechPos, GameManager.Instance.GetTalk(10152));
        await NextDialog(speechFrame1);
        
        SpawnSpeechFrame(speechFrame1, berserkerSpeechPos, GameManager.Instance.GetTalk(10153));
        await NextDialog(speechFrame1);
        
        npc[0].SpawnObject(ConstValues.BangEffect, npc[0].CenterPos.position);
        npc[0].gameObject.SetActive(false);
        
        // 싸움꾼 합류 및 저장
        GameManager.Instance.AddPlayer(ConstValues.Fighter);
        GameManager.Instance.SetCharacterOrder();

        UIOn();
        roomInfo.eventNpc[0].isActive = false;
        GameManager.Instance.DialogueEnd();

        GameManager.Instance.SpawnWarningPopup("싸움꾼이 합류하였습니다!_");
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
