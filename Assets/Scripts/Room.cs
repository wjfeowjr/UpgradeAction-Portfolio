using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public enum EntranceDir
{
    Left,
    Right,
    Up,
    Down
}

[Serializable]
public class RoomInfo
{
    public int productCount;
    public List<SkillAndPassive> skillAndPassive = new List<SkillAndPassive>();
    public List<TreasureBox> treasureBox = new List<TreasureBox>();
}

[Serializable]
// 스킬 및 패시브
public class SkillAndPassive
{
    public string id;
    public bool alreadyGet;
}

[Serializable]
// 재화나 아이템(보물상자)
public class TreasureBox
{
    public string id;
    public int count;
    public bool alreadyGet;
}

public class Room : MonoBehaviour
{
    private bool isFading;
    private int productViewIdx;
    private float dialogDelay1 = 2.5f;
    private float dialogDelay2 = 1.0f;
    
    [SerializeField] protected RoomInfo roomInfo;
    
    [SerializeField] protected RoomSkillAndPassive[] roomSkillAndPassive;
    [SerializeField] protected RoomTreasureBox[] roomTreasureBox;

    [SerializeField] private Transform minCameraLimitX;
    [SerializeField] private Transform maxCameraLimitX;
    [SerializeField] private Transform minCameraLimitY;
    [SerializeField] private Transform maxCameraLimitY;

    [SerializeField] private SaveObject saveObject;

    [SerializeField] private Transform leftPlayerPos;
    [SerializeField] private Transform rightPlayerPos;
    [SerializeField] private Transform upPlayerPos;
    [SerializeField] private Transform downPlayerPos;

    [SerializeField] private Room leftRoom;
    [SerializeField] private Room rightRoom;
    [SerializeField] private Room upRoom;
    [SerializeField] private Room downRoom;

    [SerializeField] private RoomEntrance leftEntrance;
    [SerializeField] private RoomEntrance rightEntrance;
    [SerializeField] private RoomEntrance upEntrance;
    [SerializeField] private RoomEntrance downEntrance;
    
    [SerializeField] protected Monster[] monsters;
    [SerializeField] protected List<Vector2> firstMonsterPosList = new List<Vector2>();
    [SerializeField] protected Monster[] bosses;
    [SerializeField] protected List<Vector2> firstBossPosList = new List<Vector2>();
    [SerializeField] protected GameObject[] traps;

    [SerializeField] protected Transform monsterLimitLeft;
    [SerializeField] protected Transform monsterLimitRight;
    
    [SerializeField] protected ProductTrigger[] productTrigger;
    [SerializeField] protected Transform[] customMovePos;
    [SerializeField] protected Transform[] bossPos;
    [SerializeField] protected Transform[] strongSpeechPos;
    
    [SerializeField] protected SpriteRenderer[] bgSpriteRenderers;
    [SerializeField] private GameObject roomDoor;
    [SerializeField] private GameObject[] roomCustomObjects;

    private List<SpeechFrame> speechFrame1 = new List<SpeechFrame>();
    private List<SpeechFrame> speechFrame2 = new List<SpeechFrame>();
    private SpeechFrame speechFrameStrong;
    private SpeechFrame speechFrameTitle;
    
    private CancellationTokenSource fadeCancellation;
    private CancellationTokenSource dialogCancellation;
    private CancellationTokenSource waitCancellation;

    private RoomsData roomsData;

    // 프로퍼티
    public RoomInfo RoomInfo => roomInfo;

    private void OnEnable()
    {
        // 여기서 보스 비활성화
        BossSetting();
    }

    // 세이브 포인트가 없을때만 적용, 1번맵 전용
    public async void FirstStart()
    {
        BgmOn();
        isFading = true;
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos.position;
        SetCameraLimit();
        SetTrap();
        SetSavePoint();
        await RoomManager.Instance.EntranceFadeIn();
        GameManager.Instance.ControlStart = true;
        isFading = false;
    }
    // 세이브 포인트가 있을때 적용
    public async void SaveStart()
    {
        BgmOn();
        isFading = true;
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = saveObject.SavePointPos.position;
        SetCameraLimit();
        SetTrap();
        SetSavePoint();
        await RoomManager.Instance.EntranceFadeIn();
        GameManager.Instance.ControlStart = true;
        isFading = false;
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
        if(leftEntrance)
            leftEntrance.SetAction(() => leftRoom.SettingRoom(EntranceDir.Right, gameObject));
        if(rightEntrance)
            rightEntrance.SetAction(() => rightRoom.SettingRoom(EntranceDir.Left, gameObject));
        if(upEntrance)
            upEntrance.SetAction(() => upRoom.SettingRoom(EntranceDir.Down, gameObject));
        if(downEntrance)
            downEntrance.SetAction(() => downRoom.SettingRoom(EntranceDir.Up, gameObject));
    }

    public void InfoSetting()
    {
        // 저장되는 룸만 불러온다
        roomsData = TableManager.Instance.roomsTable.Rooms.Find(x => x.id == name);
        if (roomsData == null)
            return;
        
        var productCount = roomsData.productCount;
        var productIdxArray = roomsData.productIdx.Split(',');
        List<int> productIdxList = new List<int>();
        foreach (var productIdx in productIdxArray)
            productIdxList.Add(int.Parse(productIdx));

        // 연출 트리거에, 해당하는 인덱스의 프로덕트 연출 삽입
        for (var i = 0; i < productTrigger.Length; i++)
        {
            int idx = i;
            productTrigger[i].SetAction(()=> 
            {
                ProductAction(productIdxList[idx]);
            });
        }

        var skillArray = roomsData.skill.Split(';');
        var treasureBoxArray = roomsData.treasureBox.Split('ㅗ');
        
        // 저장된 데이터가 없는 경우
        if (!PlayerPrefs.HasKey(name))
        {
            roomInfo.productCount = 0;
            for (var i = 0; i < roomSkillAndPassive.Length; i++)
            {
                var skillAndPassive = new SkillAndPassive()
                {
                    id = skillArray[i],
                    alreadyGet = false,
                };
                roomInfo.skillAndPassive.Add(skillAndPassive);
            }

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
            }
            SaveRoom();
        }
        else
        {
            Debug.Log("불러오기");
            LoadRoom();
        }

        // 연출을 봤다면, 다시 나오지 않게 조정
        if (productCount > 0)
        {
            if(roomInfo.productCount >= productCount)
                productTrigger[0].gameObject.SetActive(false);
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
                    GameManager.Instance.AddNewSkill(roomInfo.skillAndPassive[idx].id);
                    roomInfo.skillAndPassive[idx].alreadyGet = true;
                    GetSkillProduct(roomInfo.skillAndPassive[idx].id);

                    SaveRoom();
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

    private async void SettingRoom(EntranceDir dir, GameObject pastRoom)
    {
        isFading = true;
        GameManager.Instance.ControlStart = false;

        await RoomManager.Instance.EntranceFadeOut();

        switch (dir)
        {
            case EntranceDir.Left:
                SetLeftPlayerPos();
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Right:
                SetRightPlayerPos();
                GameManager.Instance.CurPlayer.ForceIdle();
                break;
            case EntranceDir.Up:
                SetUpPlayerPos();
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                GameManager.Instance.CurPlayer.SetJumpState();
                break;
            case EntranceDir.Down:
                SetDownPlayerPos();
                GameManager.Instance.CurPlayer.ForceJump();
                GameManager.Instance.CurPlayer.ZeroVelocity();
                GameManager.Instance.CurPlayer.GravityChange(0);
                break;
        }
        SetCameraLimit();
        pastRoom.SetActive(false);
        gameObject.SetActive(true);
        RoomManager.Instance.CurrentRoom = this;
        // 여기서 몹 소환
        SpawnMonster();
        // 여기서 트랩 데이터 넣기
        SetTrap();
        // 여기서 세이브포인트 데이터 넣기
        SetSavePoint();

        fadeCancellation = new CancellationTokenSource();
        if (await NormalDelay(0.5f, fadeCancellation).SuppressCancellationThrow())
            return;
        
        await RoomManager.Instance.EntranceFadeIn();
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
    }
    
    private void SetLeftPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos.position;
    }
    
    private void SetRightPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = rightPlayerPos.position;
    }
    
    private void SetUpPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = upPlayerPos.position;
    }
    
    private void SetDownPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = downPlayerPos.position;
    }

    private void SetCameraLimit()
    {
        GameManager.Instance.MainCamera.MaxXAndY = new Vector2(maxCameraLimitX.position.x, maxCameraLimitY.position.y);
        GameManager.Instance.MainCamera.MinXAndY = new Vector2(minCameraLimitX.position.x, minCameraLimitY.position.y);
    }

    public void CancelTask()
    {
        dialogCancellation?.Cancel();
        waitCancellation?.Cancel();
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
        var plusGold = GameManager.Instance.Gold + gold;
        GoldBinding.SaveGold(plusGold);
        // 골드가 날아가는 연출
        var followGold = GameManager.Instance.SpawnToObjectPool(ConstValues.FollowGold, goldPos).GetComponent<FollowGold>();
        followGold.SetAction(() => { GameManager.Instance.Gold = PlayerPrefs.GetInt(ConstValues.Gold);});
    }

    private void SpawnMonster(bool isExplosion = true)
    {
        for (var i = 0; i < monsters.Length; i++)
        {
            monsters[i].transform.position = firstMonsterPosList[i];
            monsters[i].IsExplosion = isExplosion;
            monsters[i].LimitLeft = monsterLimitLeft.position.x;
            monsters[i].LimitRight = monsterLimitRight.position.x;
            monsters[i].SetGoldAction(PlusGold);
            monsters[i].SpawnHpBar();
            monsters[i].gameObject.SetActive(true);
            monsters[i].MonsterAwake();
        }
    }

    private void SetTrap()
    {
        foreach (var trap in traps)
            GameManager.Instance.InputDataTrap(ConstValues.TrapPillar, trap);
    }

    private void SetSavePoint()
    {
        if (!saveObject)
            return;
        
        saveObject.SetSaveAction(() =>
        {
            SavePointBinding.SaveSavePoint(name);
            GameManager.Instance.SpawnWarningPopup("세이브 포인트가 저장되었습니다.").Forget();
            GameManager.Instance.RefillPlayerHp();
        });
    }

    private void BossSetting()
    {
        foreach (var boss in bosses)
            boss.gameObject.SetActive(false);
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
        monster.SpawnHpBar();
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
        monster.SpawnHpBar();
        monster.Appear(bossProduct);
    }
    
    public void SetMonster(Monster monster, bool isBoss, bool isExplosion)
    {
        monster.IsBoss = isBoss;
        monster.IsExplosion = isExplosion;
        monster.SpawnHpBar();
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
        if (await WaitUntil(() => DieMonsterCount() == 0, waitCancellation).SuppressCancellationThrow())
            return;
        action?.Invoke();
    }
    protected async void MonsterClearAction(Func<UniTask> asyncAction)
    {
        if (await WaitUntil(() => DieMonsterCount() == 0, waitCancellation).SuppressCancellationThrow())
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
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: dialogCancellation.Token).SuppressCancellationThrow())
        {
            speechFrame.SpeechEnd();
            return;
        }
        speechFrame.SpeechEnd();
    }

    // 룸 저장
    private void SaveRoom()
    {
        // json화
        string json = JsonUtility.ToJson(roomInfo, true);
        RoomBinding.SaveRoom(name, json);
    }
    // 룸 정보 불러오기
    private void LoadRoom()
    {
        // json화
        string json = JsonUtility.ToJson(roomInfo, true);
        var loadJson = RoomBinding.LoadRoom(name, json);
        // json 불러오기
        var loadedEpisode = JsonUtility.FromJson<RoomInfo>(loadJson);
        roomInfo = loadedEpisode;
    }

    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    private async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }

    private void StopBGM()
    {
        BgmManager.Instance.Stop();
    }
    private void PlayBGM(string bgmName)
    {
        BgmManager.Instance.PlayBgm(bgmName);
    }
    private void PlaySound(string bgmName)
    {
        SoundManager.Instance.PlaySound(bgmName);
    }
    private void CameraShake(float amount, float time)
    {
        GameManager.Instance.CameraShake(amount, time);
    }
    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }

    // 대화 연출
    private void ProductAction(int idx)
    {
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
        }
    }
    
    // BGM실행
    private void BgmOn()
    {
        PlayBGM(ConstValues.BGMEpisode1);
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

        PlayBGM(ConstValues.BGMEpisodeStart);
        await UniTask.WaitUntil(() => !isFading);
        
        // 에피소드 팝업부터 시작
        GameManager.Instance.ControlStart = false;
        await RoomManager.Instance.ProductEpisode("에피소드1: 날씨 좋은 날");
        string dialog1 = "날씨 참 좋다...";
        string dialog2 = "저 거지같은 태양만\n빼고말이야!";
        string dialog3 = "뿌셔버릴거야!!!";
        string dialog4 = "나 잡아봐라~";

        dialogCancellation = new CancellationTokenSource();
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

        var berserkerPos = GameManager.Instance.CurPlayer.FontPos.position;
        SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog1);
        await NextDialog(speechFrame1[0]);

        SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog2);
        await NextDialog(speechFrame1[0]);

        PlayBGM(ConstValues.BGMEpisode1);
        PlaySound(ConstValues.PlayerScream);
        CameraShake(0.4f, 1.0f);
        SpawnSpeechFrame(speechFrame1[0], new Vector2(berserkerPos.x, berserkerPos.y + 0.5f), dialog3);
        for (int i = 0; i < 2; i++)
        {
            GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
        }

        await NextDialog(speechFrame1[0]);

        var sunPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
        SpawnSpeechFrame(speechFrame2[0], sunPos, dialog4);
        await NextDialog(speechFrame2[0]);

        PlaySound(ConstValues.MonsterSunLaugh);
        var sunMoveVector = new Vector2(bosses[0].transform.position.x + 7.5f, bosses[0].transform.position.y);
        bosses[0].transform.DOMove(sunMoveVector, 2.0f);
        if (await NormalDelay(2.0f, dialogCancellation).SuppressCancellationThrow())
            return;
        bosses[0].gameObject.SetActive(false);

        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;

        // 게임 시작
        roomCustomObjects[0].SetActive(true);
        UIOn();

        roomInfo.productCount += 1;
        SaveRoom();
    }

    private async void Product2()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        waitCancellation = new CancellationTokenSource();
        dialogCancellation = new CancellationTokenSource();

        string dialog1 = "딱 봐도 엄청 좋은거다!";
        string dialog2 = "근데 이 불기둥을 어떻게 돌파하지?";
        string dialog3 = "대시를 사용해야겠어!";
        
        UIOff();
            
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.MainCamera.SetTarget(roomSkillAndPassive[0].transform);
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.MainCamera.SetTarget(GameManager.Instance.CurPlayer.transform);
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;

        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
        await NextDialog(speechFrame1[0]);

        RoomManager.Instance.Guide(1);
        
        UIOn();
        roomInfo.productCount += 1;
        SaveRoom();
    }
    
    private async void Product3()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        waitCancellation = new CancellationTokenSource();
        dialogCancellation = new CancellationTokenSource();
        
        string dialog1 = "그런데 여기가 어디야?";
        string dialog2 = "맞다! 나한테 지도가 있었지";
        string dialog3 = "이걸 확인해 봐야겠군";
        
        UIOff();
            
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;

        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
        await NextDialog(speechFrame1[0]);

        RoomManager.Instance.Guide(3);
        
        UIOn();
        roomInfo.productCount += 1;
        SaveRoom();
    }

    private async void Product4()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        waitCancellation = new CancellationTokenSource();
        dialogCancellation = new CancellationTokenSource();
        
        string dialog1 = "이게 뭐냐?\n옛날 물건 같은데";
        string dialog2 = "제작자의 연령대를\n알 수 있겠구만..";
        string dialog3 = "이곳에서 쉬어 갈 수 있겠어";
        
        UIOff();
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
        await NextDialog(speechFrame1[0]);
        
        SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
        await NextDialog(speechFrame1[0]);
        
        RoomManager.Instance.Guide(4);
        UIOn();
        roomInfo.productCount += 1;
        SaveRoom();
    }
    
    private async void Product5()
    {
        GameManager.Instance.CurPlayer.ForceProduct();
        waitCancellation = new CancellationTokenSource();
        dialogCancellation = new CancellationTokenSource();

        string dialog1 = "넌 표정이 마음에 안 들었어!";
        string dialog2 = "산산조각 내 주마!";
        string dialog3 = "덤벼보던가!";
        string dialog4 = "어허헝!! 태양은\n죽지 않아!!!";
        string dialog5 = "ㅋ";
        string dialog6 = "어!?";
        string dialog7 = "오오???!";
        string dialog8 = "무식하긴 ㅋ";
        string dialog9 = "이 세상에 영원한 건 없다.";
        string dialog10 = "흙으로 돌아가라 태양..";
        string dialog11 = "어둠이 찾아왔다..";
        string dialog12 = "?";
        string dialog13 = "으아아악!\n내 친구 태양을 뿌셔버리다니!";
        string dialog14 = "태양의 복수를 하러\n내가 찾아왔다!";
        string dialog15 = "악!!!!!!!";
        string dialog16 = "으아아아아악!!!!";
        string dialog17 = "난 돌아올 것이다!!!";
        string dialog18 = "진짜 어둠이 찾아왔다..";
        string dialog19 = "이제 가야지";
        string dialog20 = "9시간 뒤..";
        string dialog21 = "바보 같은 놈";
        string dialog22 = "밤이라서 잠깐\n없어진 거야";
        string dialog23 = "ㅋ";
        
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
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);

            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);

            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog3); 
            await NextDialog(speechFrame2[0]);

            // 게임 시작
            GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            UIOn();
            roomInfo.productCount += 1;
            SaveRoom();
        }
        
        if(await WaitUntil(() => bosses[0].IsDie, dialogCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.productCount == 1)
        {
            UIOff();
            bosses[0].transform.DOMove(bossPos[0].position, 0.5f);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            bosses[0].Flip(-1);
            await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1);

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog4); 
            await NextDialog(speechFrame2[0]);

            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog5); 
            await NextDialog(speechFrame2[0]);
        }
        
        // BGM 끄기
        StopBGM();

        if (roomInfo.productCount == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
            sunSpeechPos = new Vector2(bosses[0].CenterPos.position.x - 2.0f, bosses[0].CenterPos.position.y);
            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog6);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(1, 0);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            await NextDialog(speechFrame2[0]);

            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog7);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(2, 0.3f);
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(2, 0.2f);
            bosses[0].DieShake();
            await bosses[0].GetComponent<Monster_Sun>().DieBomb(10, 0.1f);
            await NextDialog(speechFrame2[0]);
            bosses[0].DieExplosion();
            if (await WaitUntil(() => !bosses[0].gameObject.activeSelf, dialogCancellation).SuppressCancellationThrow())
                return;

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog8);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog9);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog10);
            await NextDialog(speechFrame1[0]);
        }
        else
        {
            bosses[0].GetComponent<Monster_Sun>().SunDie();
            if (await WaitUntil(() => !bosses[0].gameObject.activeSelf, dialogCancellation).SuppressCancellationThrow())
                return;
        }

        var cameraPos = GameManager.Instance.MainCamera.transform.position;
        var fadePos = new Vector3(cameraPos.x, cameraPos.y, 0);
        var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, fadePos).GetComponent<FadeSystem>();
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade();
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;

        if (roomInfo.productCount == 1)
        {
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog11);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog12);
        }

        RoomManager.Instance.BgSpriteChange(ConstValues.BgTutorial2);
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        fadeBg.gameObject.SetActive(false);
        BgmManager.Instance.Play();

        // 달 보스 소환
        SpawnBoss(bosses[1], new Vector2(bossPos[0].transform.position.x, bossPos[0].transform.position.y + 3.5f));

        if (roomInfo.productCount == 1)
        {
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
            moonSpeechPos = new Vector2(bosses[1].CenterPos.position.x - 2.0f, bosses[1].CenterPos.position.y);
            
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, dialog13); 
            await NextDialog(speechFrame2[0]);
            
            SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, dialog14); 
            await NextDialog(speechFrame2[0]);
        
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog15);
            for (int i = 0; i < 2; i++)
            {
                GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
                GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);
            
            UIOn();
            roomInfo.productCount += 1;
            SaveRoom();
        }

        if (await WaitUntil(() => bosses[1].IsDie, dialogCancellation).SuppressCancellationThrow())
            return;

        UIOff();
        dialogCancellation = new CancellationTokenSource();

        bosses[1].transform.DOMove(bossPos[0].position, 0.5f);
        if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
            return;
        
        moonSpeechPos = new Vector2(bosses[1].CenterPos.position.x - 2.0f, bosses[1].CenterPos.position.y);
        
        bosses[1].Flip(-1);
        await GameManager.Instance.CurPlayer.EpisodeMove(customMovePos[0].position,
            GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1);

        bosses[1].DieShake();
        bosses[1].GetComponent<Monster_Moon>().DieBomb();

        SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, dialog16);
        await NextDialog(speechFrame2[0]);

        SpawnSpeechFrame(speechFrame2[0], moonSpeechPos, dialog17);
        await NextDialog(speechFrame2[0]);

        bosses[1].DieExplosion();
        BgmManager.Instance.Stop();
        if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
            return;

        fadeBg.gameObject.SetActive(true);
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade();
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        RoomManager.Instance.BgSpriteChange(ConstValues.BgTutorial);
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        
        UIOn();
        BgmManager.Instance.Play();
        // 문 열기
        DoorActive(false);
        roomInfo.productCount += 1;
        SaveRoom();
        
        // 이곳에서 세이브 포인트 연출

        // GameManager.Instance.SetCameraTarget(null);
        // var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;
        // SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog18); 
        // await NextDialog(speechFrame1[0]);
        //
        // SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog19); 
        // await NextDialog(speechFrame1[0]);
        //
        // var movePos = new Vector2(GameManager.Instance.CurPlayer.transform.position.x + 15.0f, GameManager.Instance.CurPlayer.transform.position.y);
        // await GameManager.Instance.CurPlayer.EpisodeMove(movePos, GameManager.Instance.CurPlayer.BasicStat.moveSpeed, 1);
        // if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
        //     return;
        //
        // var titleSpeechPos = Vector3.zero;
        // SpawnSpeechFrame(speechFrameTitle, titleSpeechPos, dialog20); 
        // await NextDialog(speechFrameTitle);
        //
        // BgmManager.Instance.Play();
        // PlaySound(ConstValues.ChickenCock);
        // fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        // await fadeBg.Fade();
        //
        // PlaySound(ConstValues.RewardPage);
        // bosses[0].gameObject.transform.position = new Vector2(bossPos[2].transform.position.x + 3.5f, bossPos[2].transform.position.y);
        // bosses[0].gameObject.SetActive(true);
        // await bosses[0].EpisodeMove_X(bossPos[2].transform.position, bosses[0].BasicStat.moveSpeed, -1);
        //
        // SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog21); 
        // await NextDialog(speechFrame2[0]);
        //
        // SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog22); 
        // await NextDialog(speechFrame2[0]);
        //
        // SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog23); 
        // await NextDialog(speechFrame2[0]);

        // roomInfo.productCount += 1;
        // SaveRoom();
    }
    
    // 스킬을 획득 연출
    private async void GetSkillProduct(string id)
    {
        var skillName = GameManager.Instance.GetSkillName(id);
        string getMessage = $"{skillName}을(를) 획득하였다!";
        
        if (id == ConstValues.BerserkerUpperSlash)
        {
            UIOff();
            await GameManager.Instance.SpawnWarningPopup(getMessage);
            dialogCancellation = new CancellationTokenSource();

            string dialog1 = "새로운 스킬이다!";
            string dialog2 = "나는 더 강해졌다!";
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            var berserkerSpeechPos = GameManager.Instance.CurPlayer.FontPos.position;

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
            await NextDialog(speechFrame1[0]);
        
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);

            RoomManager.Instance.Guide(2);
            UIOn();
        }
        else
        {
            await GameManager.Instance.SpawnWarningPopup(getMessage);
        }
    }
}
