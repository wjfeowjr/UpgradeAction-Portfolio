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
    Left2,
    Right,
    Right2,
    Up,
    Up2,
    Down,
    Down2
}

public class Room : MonoBehaviour
{
    private bool isFading;
    private bool nearBossRoom;
    private int productViewIdx;
    private float dialogDelay1 = 2.5f;
    private float dialogDelay2 = 1.0f;

    [Header("디자인 타일이 미리 그려진 미니맵 Tilemap")]
    [SerializeField] private Tilemap minimapFrameTilemap;
    [SerializeField] private Tilemap minimapInTilemap;
    [SerializeField] private Tilemap shortcutFrameTileMap;
    
    [Header("카메라 & 저장키")]
    private Camera gameCamera;

    // 내부 저장용
    private HashSet<Vector3Int> allRoomCells = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();
    private HashSet<Vector3Int> visitedCells = new HashSet<Vector3Int>();
    
    private HashSet<Vector3Int> allshortcutCells = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> originalshortcutTiles = new Dictionary<Vector3Int, TileBase>();
    private HashSet<Vector3Int> visitedShortcutCells = new HashSet<Vector3Int>();

    [SerializeField] private bool isBossRoom;
    [SerializeField] private GameObject roomGameObject;

    [SerializeField] protected RoomSkillAndPassive[] roomSkillAndPassive;
    [SerializeField] protected RoomTreasureBox[] roomTreasureBox;
    [SerializeField] protected Elevator[] elevators;
    [SerializeField] protected LockDoor[] lockDoors;
    
    [SerializeField] private Transform minCameraLimitX;
    [SerializeField] private Transform maxCameraLimitX;
    [SerializeField] private Transform minCameraLimitY;
    [SerializeField] private Transform maxCameraLimitY;

    [SerializeField] private SaveObject saveObject;
    
    [SerializeField] private GameObject leftBossGate;
    [SerializeField] private GameObject leftBossGate2;
    [SerializeField] private GameObject rightBossGate;
    [SerializeField] private GameObject rightBossGate2;
    [SerializeField] private GameObject upBossGate;
    [SerializeField] private GameObject downBossGate;
    
    [SerializeField] private Transform leftPlayerPos;
    [SerializeField] private Transform leftPlayerPos2;
    [SerializeField] private Transform rightPlayerPos;
    [SerializeField] private Transform rightPlayerPos2;
    [SerializeField] private Transform upPlayerPos;
    [SerializeField] private Transform upPlayerPos2;
    [SerializeField] private Transform downPlayerPos;
    [SerializeField] private Transform downPlayerPos2;
    
    [Header("인접한 방")]
    [SerializeField] private Room leftRoom;
    [SerializeField] private Room leftRoom2;
    [SerializeField] private Room rightRoom;
    [SerializeField] private Room rightRoom2;
    [SerializeField] private Room upRoom;
    [SerializeField] private Room upRoom2;
    [SerializeField] private Room downRoom;
    [SerializeField] private Room downRoom2;
    
    [Header("방의 입구")]
    [SerializeField] private RoomEntrance leftEntrance;
    [SerializeField] private RoomEntrance leftEntrance2;
    [SerializeField] private RoomEntrance rightEntrance;
    [SerializeField] private RoomEntrance rightEntrance2;
    [SerializeField] private RoomEntrance upEntrance;
    [SerializeField] private RoomEntrance upEntrance2;
    [SerializeField] private RoomEntrance downEntrance;
    [SerializeField] private RoomEntrance downEntrance2;
    
    [SerializeField] protected Monster[] monsters;
    [SerializeField] protected List<Vector2> firstMonsterPosList = new List<Vector2>();
    [SerializeField] protected Monster[] bosses;
    [SerializeField] protected Transform[] bossPos;
    
    [SerializeField] protected List<Vector2> firstBossPosList = new List<Vector2>();
    [SerializeField] protected Npc[] npc;
    [SerializeField] protected BoxCollider2D[] traps;
    [SerializeField] protected ShortcutObject[] shortCutObjects;
    
    [SerializeField] protected Transform monsterLimitLeft;
    [SerializeField] protected Transform monsterLimitRight;
    
    [SerializeField] protected ProductTrigger[] productTriggers;
    [SerializeField] protected Transform[] customMovePos;
    [SerializeField] protected Transform[] strongSpeechPos;
    
    [SerializeField] protected SpriteRenderer[] bgSpriteRenderers;
    [SerializeField] private GameObject roomDoor;
    [SerializeField] private GameObject[] roomCustomObjects;
    [SerializeField] private RoomInfo roomInfo;
    
    private List<SpeechFrame> speechFrame1 = new List<SpeechFrame>();
    private List<SpeechFrame> speechFrame2 = new List<SpeechFrame>();
    private SpeechFrame speechFrameStrong;
    private SpeechFrame speechFrameTitle;

    private RoomsData roomsData;

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
                allRoomCells.Add(pos);
                originalTiles[pos] = minimapFrameTilemap.GetTile(pos);
            }
        }
        minimapFrameTilemap.ClearAllTiles();
        
        var inBounds = minimapInTilemap.cellBounds;
        foreach (var pos in inBounds.allPositionsWithin)
        {
            if (minimapInTilemap.HasTile(pos))
            {
                allRoomCells.Add(pos);
                originalTiles[pos] = minimapInTilemap.GetTile(pos);
            }
        }
        minimapInTilemap.ClearAllTiles();

        if (shortcutFrameTileMap)
        {
            var shortcutFrameBounds = shortcutFrameTileMap.cellBounds;
            foreach (var pos in shortcutFrameBounds.allPositionsWithin)
            {
                if (shortcutFrameTileMap.HasTile(pos))
                {
                    allshortcutCells.Add(pos);
                    originalshortcutTiles[pos] = shortcutFrameTileMap.GetTile(pos);
                }
            }
            shortcutFrameTileMap.ClearAllTiles();
        }
    }

    private void Update()
    {
        if(roomGameObject.activeSelf)
            RevealCellsInView();
    }

    private void OnApplicationQuit()
    {
        SaveVisitedCells();
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
        isFading = true;
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos.position;
        SetCameraLimit();
        RoomManager.Instance.SetCameraPos();
        
        SetTrap();
        SetSavePoint();
        SetBossGate();
        await RoomManager.Instance.FadeIn(ConstValues.BlackColor);
        GameManager.Instance.ControlStart = true;
        isFading = false;
    }
    // 세이브 포인트가 있을때 적용
    public async void SaveStart()
    {
        SetBgm(true);
        isFading = true;
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = saveObject.SavePointPos.position;
        SetCameraLimit();
        RoomManager.Instance.SetCameraPos();
        
        // 여기서 몹 소환
        SpawnMonster();
        // 여기서 트랩 데이터 넣기
        SetTrap();
        // 여기서 숏컷 제어
        SetShortCut();
        // 여기서 세이브포인트 데이터 넣기
         SetSavePoint();
        // 여기서 인접 방 확인하기
        SetBossGate();
        
        await RoomManager.Instance.FadeIn(ConstValues.BlackColor);
        GameManager.Instance.ControlStart = true;
        isFading = false;
    }

    public void SetGroundVector()
    {
        RoomManager.Instance.SetGroundVector();
    }

    public void SpeechFrameSetting()
    {
        speechFrame1 = RoomManager.Instance.SpeechFrame1;
        speechFrame2 = RoomManager.Instance.SpeechFrame2;
        speechFrameStrong = RoomManager.Instance.SpeechFrameStrong;
        speechFrameTitle = RoomManager.Instance.SpeechFrameTitle;
    }

    public void EntranceSetting()
    {
        if (leftEntrance != null)
            leftEntrance.SetAction(() => leftRoom.SettingRoom(EntranceDir.Right, this));
        
        if (leftEntrance2 != null)
            leftEntrance2.SetAction(() => leftRoom2.SettingRoom(EntranceDir.Right2, this));
        
        if (rightEntrance != null)
            rightEntrance.SetAction(() => rightRoom.SettingRoom(EntranceDir.Left, this));
        
        if (rightEntrance2 != null)
            rightEntrance2.SetAction(() => rightRoom2.SettingRoom(EntranceDir.Left2, this));
        
        if (upEntrance != null)
            upEntrance.SetAction(() => upRoom.SettingRoom(EntranceDir.Down, this));
        
        if (upEntrance2 != null)
            upEntrance2.SetAction(() => upRoom2.SettingRoom(EntranceDir.Down2, this));
        
        if (downEntrance != null)
            downEntrance.SetAction(() => downRoom.SettingRoom(EntranceDir.Up, this));
        
        if (downEntrance2 != null)
            downEntrance2.SetAction(() => downRoom2.SettingRoom(EntranceDir.Up2, this));
    }

    public void InfoSetting()
    {
        // 저장되는 룸만 불러온다
        roomsData = TableManager.Instance.roomsTable.Rooms.Find(x => x.id == name);
        if (roomsData == null)
            return;
        
        var productCount = roomsData.productCount;
        var npcAppearProductIdxArray = roomsData.npcAppearProductIdx.Split(';');
        List<int> npcAppearProductIdxList = new List<int>();
        foreach (var productIdx in npcAppearProductIdxArray)
            npcAppearProductIdxList.Add(int.Parse(productIdx));
        
        var productIdxArray = roomsData.productIdx.Split(';');
        List<int> productIdxList = new List<int>();
        foreach (var productIdx in productIdxArray)
            productIdxList.Add(int.Parse(productIdx));

        // 연출 트리거에, 해당하는 인덱스의 프로덕트 연출 삽입
        for (var i = 0; i < productTriggers.Length; i++)
        {
            int idx = i;
            productTriggers[i].SetAction(()=> 
            {
                ProductAction(productIdxList[idx]);
            });
        }

        // 맵에 있는 숏컷 세팅
        if (roomInfo.shortCut.Count < shortCutObjects.Length)
        {
            foreach (var shortcutObject in shortCutObjects)
            {
                ShortCut addShortcut = new ShortCut();
                addShortcut.type = shortcutObject.Type;
                addShortcut.isOpened = false;
                roomInfo.shortCut.Add(addShortcut);
            }
            GameManager.Instance.SaveGame();
        }

        // 맵에 널려있는 스킬 세팅
        var skillArray = roomsData.skill.Split(';');
        if (roomInfo.skillAndPassive.Count != skillArray.Length)
        {
            if (skillArray[0] != ConstValues.None)
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
        
        // 맵에 널려있는 보물상자 세팅
        var treasureBoxArray = roomsData.treasureBox.Split('ㅗ');
        if (roomInfo.treasureBox.Count < treasureBoxArray.Length)
        {
            if (treasureBoxArray[0] != ConstValues.None)
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

        // 연출을 봤다면, 다시 나오지 않게 조정
        if (productCount > 0)
        {
            if (roomInfo.productCount >= productCount)
            {
                productTriggers[0].gameObject.SetActive(false);
            }
        }

        // 여기서 npc 활성화
        foreach (var arr in npc)
        {
            foreach (var npcAppearProductIdx in npcAppearProductIdxList)
                arr.gameObject.SetActive(roomInfo.productCount == npcAppearProductIdx);

            arr.SetInteractionAction();
            arr.SetSelectAction();
            arr.SetStartTalkAction();
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
            
            roomTreasureBox[i].SetSprite(roomInfo.treasureBox[idx].alreadyGet);
            roomTreasureBox[i].SetInteractionAction();
            
            if (!roomInfo.treasureBox[idx].alreadyGet)
            {
                roomTreasureBox[i].SetAction(() =>
                {
                    if (AllMonsterDead())
                    {
                        roomTreasureBox[idx].IsOpen = true;
                        roomTreasureBox[idx].OpenSetting();
                        roomTreasureBox[idx].ReduceInteractionObject();
                        roomInfo.treasureBox[idx].alreadyGet = true;
                        GetTreasureBoxItem(roomInfo.treasureBox[idx].id, roomInfo.treasureBox[idx].count, roomTreasureBox[idx].transform.position);
                        GameManager.Instance.GetAttributeProduct(roomInfo.treasureBox[idx].count, GetAttributeEvent);
                        GameManager.Instance.SaveGame();
                    }
                    else
                    {
                        GameManager.Instance.SpawnWarningPopup("방 안의 모든 몬스터를 처치해야 합니다.");
                    }
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
                GameManager.Instance.ControlStart = true;
                GameManager.Instance.SaveGame();
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
                lockDoors[i].SetInteractionAction();
                lockDoors[i].SetAction(() =>
                {
                    // 열쇠를 가지고 있는 경우
                    if (GameManager.Instance.IsHaveItem(lockDoors[idx].KeyId))
                    {
                        lockDoors[idx].OpenMessage();
                        lockDoors[idx].OpenDoor();
                        lockDoors[idx].ReduceInteractionObject();
                        // 이후 연출
                        roomInfo.lockDoors[idx].isOpen = true;
                        GameManager.Instance.SaveGame();
                    }
                    // 열쇠가 없는 경우
                    else
                    {
                        lockDoors[idx].LockMessage();
                    }
                });
            }
        }
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

    private async void SettingRoom(EntranceDir dir, Room pastRoom)
    {
        isFading = true;
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.RoomMoveSetting();
        
        // 모든 몬스터들의 행동 정지
        foreach (var monster in monsters)
            monster.CancelMotion();
        
        await RoomManager.Instance.FadeOut(ConstValues.BlackColor);

        switch (dir)
        {
            case EntranceDir.Left:
                SetLeftPlayerPos();
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Left2:
                SetLeftPlayerPos2();
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Right:
                SetRightPlayerPos();
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Right2:
                SetRightPlayerPos2();
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Up:
                SetUpPlayerPos();
                GameManager.Instance.CurPlayer.SetJumpState();
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                break;
            case EntranceDir.Up2:
                SetUpPlayerPos2();
                GameManager.Instance.CurPlayer.SetJumpState();
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                break;
            case EntranceDir.Down:
                SetDownPlayerPos();
                GameManager.Instance.CurPlayer.ForceJump();
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                break;
            case EntranceDir.Down2:
                SetDownPlayerPos2();
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
        // 여기서 숏컷 제어
        SetShortCut();
        // 여기서 세이브포인트 데이터 넣기
        SetSavePoint();
        // 여기서 인접 방 확인하기
        SetBossGate();

        GameManager.Instance.InitFadeCancellation();
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.FadeCancellation).SuppressCancellationThrow())
            return;
        
        await RoomManager.Instance.FadeIn(ConstValues.BlackColor);
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
        isFading = false;
        GameManager.Instance.ControlStart = true;

        // 여기서 BGM재생
        SetBgm(false);
    }
    
    private void SetLeftPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos.position;
    }
    
    private void SetLeftPlayerPos2()
    {
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos2.position;
    }
    
    private void SetRightPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = rightPlayerPos.position;
    }
    
    private void SetRightPlayerPos2()
    {
        GameManager.Instance.CurPlayer.transform.position = rightPlayerPos2.position;
    }
    
    private void SetUpPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = upPlayerPos.position;
    }
    
    private void SetUpPlayerPos2()
    {
        GameManager.Instance.CurPlayer.transform.position = upPlayerPos2.position;
    }
    
    private void SetDownPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = downPlayerPos.position;
    }
    
    private void SetDownPlayerPos2()
    {
        GameManager.Instance.CurPlayer.transform.position = downPlayerPos2.position;
    }

    private void SetCameraLimit()
    {
        GameManager.Instance.MainCamera.MaxXAndY = new Vector2(maxCameraLimitX.position.x, maxCameraLimitY.position.y);
        GameManager.Instance.MainCamera.MinXAndY = new Vector2(minCameraLimitX.position.x, minCameraLimitY.position.y);
    }

    public float SetCenterX()
    {
        return (maxCameraLimitX.position.x + minCameraLimitX.position.x) / 2;
    }
    public float SetCenterY()
    {
        return (maxCameraLimitY.position.y + minCameraLimitY.position.y) / 2;
    }

    private void DoorActive(bool active)
    {
        if(roomDoor)
            roomDoor.SetActive(active);
    }
    
    protected async UniTask WaitUntil(Func<bool> condition, CancellationTokenSource tokenSource)
    {
        await UniTask.WaitUntil(condition, cancellationToken: tokenSource.Token);
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

    private void PlusAttributePoint(int attributePoint)
    {
        GameManager.Instance.PlayerSkill.PlusAttributePoint(attributePoint);

        //GameManager.Instance.SaveGame();
        //Debug.Log($"저장된 {ConstValues.AttributePoint} = {GameManager.Instance.PlayerSkill.totalAttributePoint}");
    }
    private void ReducePassivePoint(string character, int passivePoint)
    {
        int currentPoint = 0;
        switch (character)
        {
            case ConstValues.Berserker:
                GameManager.Instance.PlayerSkill.berserkerSkillSetting.attributePoint -= passivePoint;
                currentPoint = GameManager.Instance.PlayerSkill.berserkerSkillSetting.attributePoint;
                break;
            
            case ConstValues.Gunner:
                GameManager.Instance.PlayerSkill.gunnerSkillSetting.attributePoint -= passivePoint;
                currentPoint = GameManager.Instance.PlayerSkill.gunnerSkillSetting.attributePoint;
                break;
        }
        
        
        Debug.Log($"남은 {character}의 포인트: {currentPoint}");
    }

    // 숏컷 뚫기
    private void UnlockShortCut(ShortcutType type)
    {
        ShortcutOpen(type);

        switch (type)
        {
            case ShortcutType.CrushLeft:
                leftRoom.ShortcutOpen(ShortcutType.WallRight);
                break;
            
            case ShortcutType.CrushLeft2:
                leftRoom2.ShortcutOpen(ShortcutType.WallRight2);
                break;
            
            case ShortcutType.CrushRight:
                rightRoom.ShortcutOpen(ShortcutType.WallLeft);
                break;
            
            case ShortcutType.CrushRight2:
                rightRoom2.ShortcutOpen(ShortcutType.WallLeft2);
                break;
            
            case ShortcutType.CrushUp:
                upRoom.ShortcutOpen(ShortcutType.WallDown);
                break;
            
            case ShortcutType.CrushUp2:
                upRoom2.ShortcutOpen(ShortcutType.WallDown2);
                break;
            
            case ShortcutType.CrushDown:
                downRoom.ShortcutOpen(ShortcutType.WallUp);
                break;
            
            case ShortcutType.CrushDown2:
                downRoom2.ShortcutOpen(ShortcutType.WallUp2);
                break;
        }
    }
    
    // 숏컷정보 저장
    private void ShortcutOpen(ShortcutType type)
    {
        var targetShortcut = roomInfo.shortCut.Find(x => x.type == type.ToString());
        
        if (targetShortcut == null)
            return;
        
        targetShortcut.isOpened = true;
        GameManager.Instance.SaveGame();
        SetShortCut();
    }

    private async void SpawnMonster(bool isExplosion = true)
    {
        for (var i = 0; i < monsters.Length; i++)
        {
            monsters[i].transform.position = firstMonsterPosList[i];
            monsters[i].IsExplosion = isExplosion;
            monsters[i].LimitLeft = monsterLimitLeft.position.x;
            monsters[i].LimitRight = monsterLimitRight.position.x;
            monsters[i].SetGoldAction(PlusGold);
            //monsters[i].SpawnHpBar();
            monsters[i].gameObject.SetActive(true);
            monsters[i].ForceIdle();
        }
        
        await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
        for (var i = 0; i < monsters.Length; i++)
            monsters[i].MonsterAwake();
    }

    private void SetTrap()
    {
        foreach (var trap in traps)
            GameManager.Instance.InputDataTrap(trap.name, trap);
    }

    private void SetShortCut()
    {
        for (int i = 0; i < shortCutObjects.Length; i++)
            shortCutObjects[i].OpenSetting(roomInfo.shortCut[i].isOpened, UnlockShortCut);
    }

    private void SetSavePoint()
    {
        if (!saveObject)
            return;
        
        saveObject.SetSaveAction(() =>
        {
            GameManager.Instance.SavePoint = name;
            GameManager.Instance.SpawnWarningPopup("세이브 포인트가 저장되었습니다.").Forget();
            GameManager.Instance.RefillPlayerHp();
            saveObject.InteractionObject.FadeOut();
            SoundManager.Instance.PlaySound(ConstValues.SlotEquip);
            GameManager.Instance.SaveGame();
        });
    }
    
    private void SetBossGate()
    {
        bool alreadyBoss = false;
        if (leftBossGate != null)
        {
            alreadyBoss = leftRoom.isBossRoom;
            leftBossGate.SetActive(alreadyBoss && !leftRoom.roomInfo.bossClear);
        }
        if (!alreadyBoss && leftBossGate2 != null)
        {
            alreadyBoss = leftRoom2.isBossRoom;
            leftBossGate2.SetActive(alreadyBoss && !leftRoom2.roomInfo.bossClear);
        }
        if (!alreadyBoss && rightBossGate != null)
        {
            alreadyBoss = rightRoom.isBossRoom;
            rightBossGate.SetActive(alreadyBoss && !rightRoom.roomInfo.bossClear);
        }
        if (!alreadyBoss && rightBossGate2 != null)
        {
            alreadyBoss = rightRoom2.isBossRoom;
            rightBossGate2.SetActive(alreadyBoss && !rightRoom2.roomInfo.bossClear);
        }
        if (!alreadyBoss && upBossGate != null)
        {
            alreadyBoss = upRoom.isBossRoom;
            upBossGate.SetActive(alreadyBoss && !upRoom.roomInfo.bossClear);
        }
        if (!alreadyBoss && downBossGate != null)
        {
            alreadyBoss = downRoom.isBossRoom;
            downBossGate.SetActive(alreadyBoss && !downRoom.roomInfo.bossClear);
        }

        // if (isBossRoom)
        //     alreadyBoss = true;
        
        // nearBossRoom = alreadyBoss;
        //
        // if(nearBossRoom)
        //     BgmManager.Instance.Stop();
        // else if (!BgmManager.Instance.IsPlaying())
        //     BgmManager.Instance.Play();
    }

    private void SetBgm(bool immediately)
    {
        if (isBossRoom)
            return;
        
        roomsData = TableManager.Instance.roomsTable.Rooms.Find(x => x.id == name);
        if (roomsData == null)
            return;
        
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

    private void SpawnBoss(Monster boss, Vector2 bossPos)
    {
        boss.transform.position = bossPos;
        boss.IsBoss = true;
        boss.LimitLeft = monsterLimitLeft.position.x;
        boss.LimitRight = monsterLimitRight.position.x;
        boss.SetGoldAction(PlusGold);
        boss.SpawnHpBar();
        boss.gameObject.SetActive(true);
        boss.Appear(SpawnBossMessage);
    }

    public Monster SpawnMonster(string id, Vector3 monsterVector, Action removeAction, bool isExplosion = true, bool isBoss = false, Action<string> bossProduct = null)
    {
        var monster = GameManager.Instance.SpawnToObjectPool(id, monsterVector).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.IsBoss = isBoss;
        //monster.SpawnHpBar();
        monster.Appear(bossProduct);
        return monster;
    }
    
    public Monster ActiveAndHideMonster(string id, Vector3 monsterVector, bool isExplosion = true, bool isBoss = false)
    {
        var monster = GameManager.Instance.SpawnToPoolInstantiate(id, GameManager.Instance.ObjectPool, monsterVector).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.IsBoss = isBoss;
        monster.gameObject.SetActive(false);
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

    protected void SpawnSpeechFrame(SpeechFrame speechFrame, Vector2 speechPos, string dialog)
    {
        speechFrame.SetPos(speechPos);
        speechFrame.Speech(dialog);
    }
    protected async UniTask NextDialog(SpeechFrame speechFrame)
    {
        speechFrame.NextObjectActive();
        // 스페이스바를 누르면 넘어간다
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: GameManager.Instance.DialogCancellation.Token).SuppressCancellationThrow())
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
        }
    }

    // 보스연출
    private void SpawnBossMessage(string bossName)
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_BossMessage, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_BossMessage bossMessageView)
        {
            var bossMessageInterface = bossMessageView.BossMessageView.ConvertTo<IUIBossMessageView>();
            var bossMessageModel = new UIBossMessageModel()
            {
                bossName = bossName
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
        // 저장 데이터 없음: 모든 타일 비활성화
        if (string.IsNullOrEmpty(roomInfo.visitedCells))
        {
            minimapFrameTilemap.ClearAllTiles();
            minimapInTilemap.ClearAllTiles();
        }
        // 저장 데이터 있음: 불러와서 해당 셀만 활성화
        else
        {
            LoadVisitedCells();
            foreach (var cell in visitedCells)
            {
                if (originalTiles.TryGetValue(cell, out var inTile))
                    minimapInTilemap.SetTile(cell, inTile);
            }
        }
        
        // 저장 데이터 없음: 모든 숏컷 비활성화
        if (string.IsNullOrEmpty(roomInfo.visitedShortcutCells))
        {
            if(shortcutFrameTileMap)
                shortcutFrameTileMap.ClearAllTiles();
        }
        // 저장 데이터 있음: 불러와서 해당 숏컷만 활성화
        else
        {
            LoadVisitedShortcutCells();
            foreach (var shortcutCell in visitedShortcutCells)
            {
                if (originalshortcutTiles.TryGetValue(shortcutCell, out var inTile))
                    shortcutFrameTileMap.SetTile(shortcutCell, inTile);
            }
        }
        
        // 저장 데이터 있음: 세이브 오브젝트 미니맵 활성화
        if (roomInfo.savePointCheck)
        {
            if(saveObject)
                saveObject.MinimapObject.SetActive(true);
        }
        // 저장 데이터 없음: 세이브 오브젝트 미니맵 비활성화
        else
        {
            if(saveObject)
                saveObject.MinimapObject.SetActive(false);
        }
    }
    // 3. 카메라 뷰 영역에 조금이라도 겹치면 활성화
    private void RevealCellsInView()
    {
        Vector3 camPos = gameCamera.transform.position;
        float halfH = gameCamera.orthographicSize;
        
        float halfW = halfH * gameCamera.aspect;
        Rect viewRect = new Rect(camPos.x - halfW, camPos.y - halfH, halfW * 2, halfH * 2);
        
        float extraV = minimapFrameTilemap.cellSize.y;
        viewRect.yMin += extraV * -1;
        viewRect.yMax += extraV * 3;

        Vector2 halfCell = minimapFrameTilemap.cellSize; // * 0.5f minimapTilemap.cellSize
        
        bool anyNew = false;
        foreach (var cell in allRoomCells)
        {
            if (visitedCells.Contains(cell))
                continue;

            Vector3 center = minimapFrameTilemap.GetCellCenterWorld(cell);
            Vector2 min = new Vector2(center.x - halfCell.x, center.y - halfCell.y);
            Vector2 max = new Vector2(center.x + halfCell.x, center.y + halfCell.y);

            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                visitedCells.Add(cell);
                minimapFrameTilemap.SetTile(cell, originalTiles[cell]);
                minimapInTilemap.SetTile(cell, originalTiles[cell]);
                anyNew = true;
            }
        }
        if (anyNew)
            SaveVisitedCells();
        
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
        
        bool shortcutNew = false;
        foreach (var shortcutCell in allshortcutCells)
        {
            if (visitedShortcutCells.Contains(shortcutCell))
                continue;
            
            if (shortcutFrameTileMap)
            {
                Vector3 center = shortcutFrameTileMap.GetCellCenterWorld(shortcutCell);
                Vector2 min = new Vector2(center.x - halfCell.x, center.y - halfCell.y);
                Vector2 max = new Vector2(center.x + halfCell.x, center.y + halfCell.y);

                // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
                if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                    max.y >= viewRect.yMin && min.y <= viewRect.yMax)
                {
                    visitedShortcutCells.Add(shortcutCell);
                    shortcutFrameTileMap.SetTile(shortcutCell, originalshortcutTiles[shortcutCell]);
                    shortcutNew = true;
                }
            }
        }
        if (shortcutNew)
            SaveVisitedShortcutCells();
    }

    // 방문 셀 저장
    private void SaveVisitedCells()
    {
        var sb = new StringBuilder();
        foreach (var c in visitedCells)
            sb.Append(c.x).Append('_').Append(c.y).Append('_').Append(c.z).Append(';');

        roomInfo.visitedCells = sb.ToString();
    }
    // 방문 셀 로드
    private void LoadVisitedCells()
    {
        if (string.IsNullOrEmpty(roomInfo.visitedCells))
            return;

        var entries = roomInfo.visitedCells.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var e in entries)
        {
            var p = e.Split('_');
            if (p.Length == 3
             && int.TryParse(p[0], out int x)
             && int.TryParse(p[1], out int y)
             && int.TryParse(p[2], out int z))
            {
                visitedCells.Add(new Vector3Int(x, y, z));
            }
        }
    }
    
    // 숏컷 셀 저장
    private void SaveVisitedShortcutCells()
    {
        var sb = new StringBuilder();
        foreach (var c in visitedShortcutCells)
            sb.Append(c.x).Append('_').Append(c.y).Append('_').Append(c.z).Append(';');

        roomInfo.visitedShortcutCells = sb.ToString();
    }
    // 숏컷 셀 로드
    private void LoadVisitedShortcutCells()
    {
        if (string.IsNullOrEmpty(roomInfo.visitedShortcutCells))
            return;

        var entries = roomInfo.visitedShortcutCells.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var e in entries)
        {
            var p = e.Split('_');
            if (p.Length == 3
                && int.TryParse(p[0], out int x)
                && int.TryParse(p[1], out int y)
                && int.TryParse(p[2], out int z))
            {
                visitedShortcutCells.Add(new Vector3Int(x, y, z));
            }
        }
    }
    
    private void SaveSaveObject()
    {
        saveObject.MinimapObject.SetActive(true);
        roomInfo.savePointCheck = true;
    }

    /// <summary>
    /// 연출 구현 구간
    /// </summary>
    // 연출1, 해당하는 방 = Room1_1
    private void UIOn()
    {
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.CurPlayer.Immortal = false;
    }
    private void UIOff()
    {
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.Immortal = true;
    }

    private async void Product1()
    {
        // 연출 시작 전 세팅
        StopBGM();
        roomCustomObjects[0].SetActive(false);
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
        
        // var sunObject = RoomManager.Instance.SunObject;
        // sunObject.gameObject.transform.position = bossPos[0].position;
        // sunObject.gameObject.SetActive(true);
        // sunObject.BasicStat.moveSpeed = 0;
        bosses[0].enabled = false;
        bosses[0].transform.position = bossPos[0].position;
        bosses[0].gameObject.SetActive(true);
        bosses[0].Flip(-1);

        PlayBGM(ConstValues.BGMEpisodeStart, true);
        await UniTask.WaitUntil(() => !isFading);
        
        // 에피소드 팝업부터 시작
        GameManager.Instance.ControlStart = false;
        
        string title = TableManager.Instance.productDialogueTable.ProductDialogue.Find(x => x.id == ConstValues.Episode1Title).talk;
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product1);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(productDialogue.talk);

        await RoomManager.Instance.ProductEpisode(title);
        GameManager.Instance.InitDialogueCancellation();
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

        var berserkerPos = GameManager.Instance.CurPlayer.FontPos.position;
        SpawnSpeechFrame(speechFrame1[0], berserkerPos, talkList[0]);
        await NextDialog(speechFrame1[0]);

        SpawnSpeechFrame(speechFrame1[0], berserkerPos, talkList[1]);
        await NextDialog(speechFrame1[0]);

        PlayBGM(ConstValues.BGMSunHill, true);
        PlaySound(ConstValues.PlayerScream);
        CameraShake(0.1f, 0.4f, 1.0f);
        SpawnSpeechFrame(speechFrame1[0], new Vector2(berserkerPos.x, berserkerPos.y + 0.5f), talkList[2]);
        for (int i = 0; i < 2; i++)
        {
            GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;
        }

        await NextDialog(speechFrame1[0]);

        var sunPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
        SpawnSpeechFrame(speechFrame2[0], sunPos, talkList[3]);
        await NextDialog(speechFrame2[0]);

        PlaySound($"{ConstValues.MonsterSun}_{ConstValues.Laugh}");
        var sunMoveVector = new Vector2(bosses[0].transform.position.x + 7.5f, bosses[0].transform.position.y);
        bosses[0].transform.DOMove(sunMoveVector, 2.0f);
        if (await GameManager.Instance.NormalDelay(2.0f, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;
        bosses[0].gameObject.SetActive(false);

        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;

        // 게임 시작
        roomCustomObjects[0].SetActive(true);
        UIOn();

        roomInfo.productCount += 1;
        GameManager.Instance.SaveGame();
    }

    private async void Product2()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitDialogueCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product2);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(productDialogue.talk);
        
        UIOff();
            
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;

        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[0]);
        await NextDialog(speechFrame1[0]);

        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[1]);
        await NextDialog(speechFrame1[0]);

        RoomManager.Instance.Guide(0);
        
        UIOn();
        roomInfo.productCount += 1;
        GameManager.Instance.SaveGame();
    }

    private async void Product3()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitDialogueCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product3);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(productDialogue.talk);

        UIOff();
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[0]);
        await NextDialog(speechFrame1[0]);

        RoomManager.Instance.Guide(1);
        UIOn();
        roomInfo.productCount += 1;
        GameManager.Instance.SaveGame();
    }
    
    private async void Product4()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitDialogueCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product4);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(productDialogue.talk);

        // 문 닫기
        DoorActive(true);
        // 태양 보스 소환
        SpawnBoss(bosses[0], new Vector2(bossPos[0].transform.position.x, bossPos[0].transform.position.y + 3.5f));

        // 대화하는 주체들
        Vector2 berserkerSpeechPos;
        Vector2 sunSpeechPos;
        Vector2 moonSpeechPos; 
        
        if (roomInfo.productCount == 0)
        {
            UIOff();
            
            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;
            
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);

            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[0]);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[1]);
            await NextDialog(speechFrame1[0]);

            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[2]); 
            await NextDialog(speechFrame2[0]);

            // 게임 시작
            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            UIOn();
            roomInfo.productCount += 1;
            GameManager.Instance.SaveGame();
        }
        
        if(await WaitUntil(() => bosses[0].IsDie, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.productCount == 1)
        {
            UIOff();
            
            // 이 부분 강제이동으로 변경
            bosses[0].CancelMotion();
            bosses[0].transform.DOMove(bossPos[0].position, 0.5f);
            if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;
            bosses[0].Flip(-1);
            await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1);

            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;

            GameManager.Instance.CurPlayer.ForceIdle();
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[3]); 
            await NextDialog(speechFrame2[0]);

            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[4]); 
            await NextDialog(speechFrame2[0]);
        }
        
        // BGM 끄기
        StopBGM();

        if (roomInfo.productCount == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[5]);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(1, 0);
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[6]);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(2, 0.3f);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(2, 0.2f);
            bosses[0].DieShake();
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(10, 0.1f);
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;
            
            bosses[0].DieExplosion();
            speechFrame2[0].SpeechEnd();

            if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[7]);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[8]);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[9]);
            await NextDialog(speechFrame1[0]);
        }
        else
        {
            bosses[0].GetComponent<Monster_Sun>().SunDie();
            if (await WaitUntil(() => !bosses[0].gameObject.activeSelf, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;
        }

        var cameraPos = GameManager.Instance.MainCamera.transform.position;
        var fadePos = new Vector3(cameraPos.x, cameraPos.y, 0);
        var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, fadePos).GetComponent<FadeSystem>();
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade();
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.productCount == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[10]);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[11]);
        }

        RoomManager.Instance.BgSpriteChange(ConstValues.BgSunHillNight);
        RoomManager.Instance.BgDecoActive(false);
        
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        fadeBg.gameObject.SetActive(false);
        BgmManager.Instance.Play();

        // 달 보스 소환
        SpawnBoss(bosses[1], new Vector2(bossPos[0].transform.position.x, bossPos[0].transform.position.y + 3.5f));

        if (roomInfo.productCount == 1)
        {
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;
            
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
            moonSpeechPos = new Vector2(bosses[1].CenterPos.position.x - 2.0f, bosses[1].CenterPos.position.y);
            
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, talkList[12]); 
            await NextDialog(speechFrame2[0]);
            
            SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, talkList[13]); 
            await NextDialog(speechFrame2[0]);
        
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[14]);
            for (int i = 0; i < 2; i++)
            {
                GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
                GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

                if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);
            
            UIOn();
            roomInfo.productCount += 1;
            GameManager.Instance.SaveGame();
        }

        if (await WaitUntil(() => bosses[1].IsDie, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;

        UIOff();
        GameManager.Instance.InitDialogueCancellation();
        bosses[1].CancelMotion();
        bosses[1].transform.DOMove(bossPos[0].position, 0.5f);
        if (await GameManager.Instance.NormalDelay(0.5f, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;
        
        moonSpeechPos = new Vector2(bosses[1].CenterPos.position.x - 2.0f, bosses[1].CenterPos.position.y);
        
        bosses[1].Flip(-1);
        await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1);

        PlaySound($"{ConstValues.Scream}12");
        bosses[1].DieShake();
        bosses[1].GetComponent<Monster_Moon>().DieBomb();

        SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, talkList[15]);
        await NextDialog(speechFrame2[0]);

        SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, talkList[16]);
        await NextDialog(speechFrame2[0]);

        bosses[1].DieExplosion();
        BgmManager.Instance.Stop();
        if (await GameManager.Instance.NormalDelay(dialogDelay1, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;

        fadeBg.gameObject.SetActive(true);
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade();
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;
        
        RoomManager.Instance.BgSpriteChange(ConstValues.BgSunHill);
        RoomManager.Instance.BgDecoActive(true);
        
        berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[17]); 
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[18]); 
        await NextDialog(speechFrame1[0]);

        BgmManager.Instance.Play();
        PlaySound(ConstValues.ChickenCock);
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[19]); 
        await NextDialog(speechFrame1[0]);
        
        PlaySound(ConstValues.RewardPage);
        npc[0].gameObject.SetActive(true);
        npc[0].transform.localScale = new Vector3(-1, 1, 1);
        var npcArrivePos = npc[0].transform.position;
        npc[0].gameObject.transform.position = new Vector2(npc[0].transform.position.x, npc[0].transform.position.y + 3.5f);
        await npc[0].EpisodeMove_Y(npcArrivePos, bosses[0].BasicStat.moveSpeed);
        sunSpeechPos = npc[0].SpeechPos.position;
        
        SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[20]); 
        await NextDialog(speechFrame2[0]);
        
        SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[21]); 
        await NextDialog(speechFrame2[0]);
        
        SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, talkList[22]); 
        await NextDialog(speechFrame2[0]);

        // 문 열기
        DoorActive(false);
        roomInfo.productCount += 1;
        roomInfo.bossClear = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }

    private async void Product5()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitDialogueCancellation();
        
        var productDialogueList = TableManager.Instance.productDialogueTable.ProductDialogue.FindAll(x => x.id == ConstValues.Product5);
        List<string> talkList = new List<string>();
        foreach (var productDialogue in productDialogueList)
            talkList.Add(productDialogue.talk);

        UIOff();
        
        // 연출 시작 전 세팅
        StopBGM();
        PlayBGM(ConstValues.BGMEpisode2Battle);
        // 에피소드 팝업부터 시작
        string title = TableManager.Instance.productDialogueTable.ProductDialogue.Find(x => x.id == ConstValues.Episode2Title).talk;
        await RoomManager.Instance.ProductEpisode(title);
        
        if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
        var gunnerSpeechPos = npc[0].FontPos.position;
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[0]);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[1]);
        await NextDialog(speechFrame1[0]);
        
        npc[0].Flip(-1);
        
        SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, talkList[2]);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, talkList[3]);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[4]);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, talkList[5]);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, talkList[6]);
        await NextDialog(speechFrame1[0]);
        
        npc[0].SpawnObject(ConstValues.BangEffect, npc[0].CenterPos.position);
        npc[0].gameObject.SetActive(false);
        
        // 2인 캐릭터 설정 및 저장
        GameManager.Instance.SetCharacterOrder(ConstValues.Berserker, ConstValues.Gunner);
        RoomManager.Instance.Guide(4);
        
        UIOn();
        roomInfo.productCount += 1;
        GameManager.Instance.SaveGame();
    }
    
    private async void Product6()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        GameManager.Instance.InitWaitCancellation();
        GameManager.Instance.InitDialogueCancellation();

        // 문 닫기
        DoorActive(true);
        // 쥐새끼 보스 소환
        SpawnBoss(bosses[0], new Vector2(bossPos[0].transform.position.x, bossPos[0].transform.position.y));
        
        if(await GameManager.Instance.WaitUntilDelay(() => bosses[0].IsDie, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.InitWaitCancellation();
        if (await GameManager.Instance.NormalDelay(5.0f, GameManager.Instance.WaitCancellation).SuppressCancellationThrow())
            return;

        // 문 열기
        DoorActive(false);
        roomInfo.productCount += 1;
        roomInfo.bossClear = true;
        GameManager.Instance.SaveGame();
        UIOn();
    }
    
    // 스킬을 획득 후 이벤트
    private async void GetSkillEvent(string skillName)
    {
        string getMessage = $"{skillName}을(를) 획득하였다!";
        
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
            
            GameManager.Instance.InitDialogueCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(2);
            UIOn();
        }
    }
    
    // 특성 포인트 획득 후 이벤트
    private async void GetAttributeEvent(int pointCount)
    {
        string getMessage = $"특성 포인트 {pointCount}점을 획득하였다!";
        
        if (GameManager.Instance.FirstGetAttribute)
        {
            await GameManager.Instance.SpawnWarningPopup(getMessage);
        }
        else
        {
            GameManager.Instance.FirstGetAttribute = true;
            //GameManager.Instance.SaveGame();

            UIOff();
            GameManager.Instance.CurPlayer.ForceProduct();
            await GameManager.Instance.SpawnWarningPopup(getMessage);
            
            GameManager.Instance.InitDialogueCancellation();
            if (await GameManager.Instance.NormalDelay(dialogDelay2, GameManager.Instance.DialogCancellation).SuppressCancellationThrow())
                return;

            RoomManager.Instance.Guide(3);
            UIOn();
        }
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
        
        Transform trapArray = roomGameObject.transform.Find(ConstValues.TrapArray);
        if (trapArray != null)
            traps = trapArray.GetComponentsInChildren<BoxCollider2D>();
        
        Transform productTriggerArray = roomGameObject.transform.Find(ConstValues.ProductTriggerArray);
        if (productTriggerArray != null)
            productTriggers = productTriggerArray.GetComponentsInChildren<ProductTrigger>();

        Transform playerPosArray = roomGameObject.transform.Find(ConstValues.PlayerPosArray);
        if (playerPosArray != null)
        {
            leftPlayerPos = null;
            leftPlayerPos2 = null;
            rightPlayerPos = null;
            rightPlayerPos2 = null;
            upPlayerPos = null;
            upPlayerPos2 = null;
            downPlayerPos = null;
            downPlayerPos2 = null;
            
            Transform[] possArray = playerPosArray.GetComponentsInChildren<Transform>();
            foreach (var poss in possArray)
            {
                if (poss.name == $"{ConstValues.LeftPlayerPos}_1")
                    leftPlayerPos = poss;
                
                if (poss.name == $"{ConstValues.LeftPlayerPos}_2")
                    leftPlayerPos2 = poss;
                
                if (poss.name == $"{ConstValues.RightPlayerPos}_1")
                    rightPlayerPos = poss;
                
                if (poss.name == $"{ConstValues.RightPlayerPos}_2")
                    rightPlayerPos2 = poss;
                
                if (poss.name == $"{ConstValues.UpPlayerPos}_1")
                    upPlayerPos = poss;
                
                if (poss.name == $"{ConstValues.UpPlayerPos}_2")
                    upPlayerPos2 = poss;
                
                if (poss.name == $"{ConstValues.DownPlayerPos}_1")
                    downPlayerPos = poss;
                
                if (poss.name == $"{ConstValues.DownPlayerPos}_2")
                    downPlayerPos2 = poss;
            }
        }
        
        Transform entrancePosArray = roomGameObject.transform.Find(ConstValues.EntranceArray);
        if (entrancePosArray != null)
        {
            leftEntrance = null;
            leftEntrance2 = null;
            rightEntrance = null;
            rightEntrance2 = null;
            upEntrance = null;
            upEntrance2 = null;
            downEntrance = null;
            downEntrance2 = null;
            
            RoomEntrance[] entranceArray = entrancePosArray.GetComponentsInChildren<RoomEntrance>();
            foreach (var entrance in entranceArray)
            {
                if (entrance.name == $"{ConstValues.LeftEntrance}_1")
                    leftEntrance = entrance;
                
                if (entrance.name == $"{ConstValues.LeftEntrance}_2")
                    leftEntrance2 = entrance;
                
                if (entrance.name == $"{ConstValues.RightEntrance}_1")
                    rightEntrance = entrance;
                
                if (entrance.name == $"{ConstValues.RightEntrance}_2")
                    rightEntrance2 = entrance;
                
                if (entrance.name == $"{ConstValues.UpEntrance}_1")
                    upEntrance = entrance;
                
                if (entrance.name == $"{ConstValues.UpEntrance}_2")
                    upEntrance2 = entrance;
                
                if (entrance.name == $"{ConstValues.DownEntrance}_1")
                    downEntrance = entrance;
                
                if (entrance.name == $"{ConstValues.DownEntrance}_2")
                    downEntrance2 = entrance;
            }
        }
        
        Transform bossGateArray = roomGameObject.transform.Find(ConstValues.BossGateArray);
        if (bossGateArray != null)
        {

            Transform[] gateArray = bossGateArray.GetComponentsInChildren<Transform>();
            foreach (var gate in gateArray)
            {
                if (gate.name == $"{ConstValues.LeftBossGate}_1")
                    leftBossGate = gate.gameObject;
                
                if (gate.name == $"{ConstValues.LeftBossGate}_2")
                    leftBossGate2 = gate.gameObject;
                
                if (gate.name == $"{ConstValues.RightBossGate}_1")
                    rightBossGate = gate.gameObject;
                
                if (gate.name == $"{ConstValues.RightBossGate}_2")
                    rightBossGate2 = gate.gameObject;
                
                if (gate.name == $"{ConstValues.UpBossGate}_1")
                    upBossGate = gate.gameObject;
                
                if (gate.name == $"{ConstValues.DownBossGate}_1")
                    downBossGate = gate.gameObject;
            }
        }
    }
}
