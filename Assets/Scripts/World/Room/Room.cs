using System;
using System.Collections.Generic;
using System.Linq;
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

public partial class Room : MonoBehaviour
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

    // 미니맵 공개 상태. 방문한 셀과 원본 타일 캐시를 전부 여기서 들고 있다.
    // Awake 에서 만들고, 세이브 데이터가 붙는 AddRoomData 에서 Bind 한다.
    private RoomMinimap minimap;

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
    
    [SerializeField] private DemoText demoText;

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
    public string Place => GameManager.Instance.GetPlaceName(TableParse.Enum<ePlace>(roomsData.place));

    // 패스트 트래블용: 세이브 포인트 활성화 여부와 세이브 오브젝트 접근자
    public bool SavePointCheck => roomInfo != null && roomInfo.IsRevealed(EMinimapObjectType.SavePoint);
    public SaveObject SaveObject => saveObject;

    // 포탈 이동용: 포탈 발견 여부와 포탈 오브젝트 접근자
    public bool PortalCheck => roomInfo != null && roomInfo.IsRevealed(EMinimapObjectType.Portal);
    public PortalObject PortalObject => portalObject;

    // 보스 처치 집계용: 이 방의 bosses 배열 크기
    public int BossCount => bosses?.Length ?? 0;

    private void Awake()
    {
        // 미니맵은 카메라 유무와 무관하게 만들어 둔다.
        // 세이브 복원(AddRoomData)이 카메라보다 먼저 올 수 있기 때문이다.
        minimap = new RoomMinimap(minimapFrameTilemap, minimapInTilemap, shortcutFrameTileMaps, hiddenAreas);

        if (!RoomManager.Instance.MainCamera)
            return;

        gameCamera = RoomManager.Instance.MainCamera;

        // 그려진 타일을 캐싱한 뒤 전부 지운다. 방문한 만큼만 다시 칠하는 방식이다.
        minimap.CacheTiles();

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
                minimap.SaveHiddenAreaData();
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

    private void OnApplicationQuit()
    {
        minimap.SaveAll();
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
            GameLog.Info("꽥");
        
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

        minimap.Bind(roomInfo);
        minimap.Restore();
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
    
    //   1) 세이브 스키마 맞추기 - 저장된 개수와 실제 배치된 오브젝트 개수를 동기화
    //   2) 저장 상태 반영     - 이미 획득/개방한 것을 씬에 적용
    public void InfoSetting()
    {
        // 저장되는 룸만 불러온다
        roomsData = TableManager.Instance.GetRoom(name);
        if (roomsData == null)
            return;

        SyncSaveSchema();
        ApplySavedState();
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
        
        // 데모 텍스트
        if(demoText)
            demoText.RefreshTalkText();
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
        // 입장 걷기 시작점: 도착위치에서 바깥(왼쪽)으로 떨어뜨려 두고, 이후 도착위치까지 걸어 들어온다.
        var pos = leftPlayerPos[idx].position;
        pos.x -= EnterWalkOffsetX;
        GameManager.Instance.CurPlayer.transform.position = pos;
    }

    private void SetRightPlayerPos(int idx)
    {
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
            GameManager.Instance.RefillPlayerHp();
            GameManager.Instance.SetPotionCount();
            saveObject.InteractionObject.FadeOut();
            SoundManager.Instance.PlaySound(ConstValues.SlotEquip);
            GameManager.Instance.SaveGame();

            // 데모 마지막 구역: 완주 집계 후 최초 1회 위시리스트를 유도하고,
            // 팝업이 닫힌 다음에 패스트 트래블 선택지를 연다
            if (GameManager.Instance.isDemo && name == ConstValues.DemoLastSaveRoom)
            {
                // 여러 번 세이브해도 값이 1로 유지되도록 덮어쓴다
                SteamWorksManager.Instance.SetStat(ConstValues.StatDemoCleared, 1);
                SteamWorksManager.Instance.StoreStats();

                if (SteamWorksManager.Instance.TryShowWishlistPopup(OnWishlistPopupClosed))
                {
                    // 팝업의 방향키 입력과 플레이어 조작이 겹치지 않도록 잠근다
                    GameManager.Instance.ControlStart = false;
                    return;
                }
            }

            OpenFastTravel();
        });
    }

    // 위시리스트 팝업이 닫힌 뒤: 패스트 트래블이 이어지면 조작 잠금을 유지하고, 아니면 되돌린다
    private void OnWishlistPopupClosed()
    {
        if (GameManager.Instance.IsHaveItem(ConstValues.SaveTravel))
        {
            OpenFastTravel();
            return;
        }

        GameManager.Instance.ControlStart = true;
    }

    private void OpenFastTravel()
    {
        if (!GameManager.Instance.IsHaveItem(ConstValues.SaveTravel))
            return;

        GameManager.Instance.ControlStart = false;
        saveObject.SetFastTravelAction();
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
        
        roomsData = TableManager.Instance.GetRoom(name);
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
        roomsData = TableManager.Instance.GetRoom(name);
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
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(GameManager.Instance.enterKey), cancellationToken: GameManager.Instance.ProductCancellation.Token).SuppressCancellationThrow())
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
        GameManager.Instance.Flow.BaseTimeScale = value;
    }


    // 보스연출
    private void SpawnBossMessage(string bossName, EMonsterType monsterType)
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_BossMessage, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_BossMessage bossMessageView)
        {
            var bossMessageModel = new UIBossMessageModel()
            {
                bossName = bossName,
                monsterType = monsterType
            };
            var bossMessagePresenter = bossMessageView.BossMessageView.Bind(bossMessageModel);
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

    // 3. 카메라 뷰에 걸친 미니맵 요소를 공개한다.
    //    타일(테두리/내부/숏컷/숨겨진 구역)은 RoomMinimap 이 맡고,
    //    여기서는 방이 소유한 마커(세이브 포인트·포탈·상인·획득물)만 처리한다.
    private void RevealCellsInView()
    {
        if (!gameCamera)
            return;

        Vector3 camPos = gameCamera.transform.position;
        float halfH = gameCamera.orthographicSize;
        float halfW = halfH * gameCamera.aspect;

        var cameraRect = new Rect(camPos.x - halfW, camPos.y - halfH, halfW * 2, halfH * 2);

        // 타일 공개가 판정 사각형을 위로 넓히고, 그 넓혀진 사각형을 마커도 함께 쓴다
        Rect viewRect = minimap.RevealFrameAndInCells(cameraRect);

        RevealMinimapMarkers(viewRect);

        minimap.RevealShortcutsAndHidden(viewRect);
    }

    // 미니맵 마커는 방이 소유한 오브젝트라 Room 에 남겼다.
    // 공개 조건이 "이미 먹었는가" 같은 방 상태에 걸려 있어 미니맵 쪽으로 옮기면 결합이 늘어난다.
    private void RevealMinimapMarkers(Rect viewRect)
    {
        // 콜라이더 크기가 없는 마커는 이 반경으로 판정한다
        var defaultHalf = new Vector2(3.5f, 3.5f);

        if (saveObject && RoomMinimap.Overlaps(viewRect, saveObject.transform.position, saveObject.ColSize))
            SaveSaveObject();

        if (portalObject && RoomMinimap.Overlaps(viewRect, portalObject.transform.position, portalObject.ColSize))
            SavePortalObject();

        if (merchantObject && RoomMinimap.Overlaps(viewRect, merchantObject.transform.position, defaultHalf))
            SaveMerchantObject();

        for (var i = 0; i < roomObjects.Length; i++)
        {
            if (RoomMinimap.Overlaps(viewRect, roomObjects[i].transform.position, defaultHalf))
                SaveAttributePointCheck(i);
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
