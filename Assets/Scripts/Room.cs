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
    private bool isHaveProduct;
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

    [SerializeField] private Transform savePointPos;
    
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

    [SerializeField] protected Transform monsterLimitLeft;
    [SerializeField] protected Transform monsterLimitRight;
    
    [SerializeField] protected ProductTrigger[] productTrigger;
    [SerializeField] protected Transform[] customMovePos;
    [SerializeField] protected Transform[] trapPos;
    [SerializeField] protected Transform[] bossPos;
    [SerializeField] protected Transform[] strongSpeechPos;
    
    [SerializeField] protected SpriteRenderer[] bgSpriteRenderers;
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

    // 세이브 포인트가 없을때만 적용, 1번맵 전용
    public async void FirstStart()
    {
        BgmOn();
        isFading = true;
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos.position;
        SetCameraLimit();
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
        GameManager.Instance.CurPlayer.transform.position = savePointPos.position;
        SetCameraLimit();
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
            leftEntrance.SetAction(() => leftRoom.SetPlayerPos(EntranceDir.Right, gameObject));
        if(rightEntrance)
            rightEntrance.SetAction(() => rightRoom.SetPlayerPos(EntranceDir.Left, gameObject));
        if(upEntrance)
            upEntrance.SetAction(() => upRoom.SetPlayerPos(EntranceDir.Down, gameObject));
        if(downEntrance)
            downEntrance.SetAction(() => downRoom.SetPlayerPos(EntranceDir.Up, gameObject));
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
            isHaveProduct = true;
        
        if (isHaveProduct)
        {
            for (int i = 0; i < roomInfo.productCount; i++)
            {
                productTrigger[i].gameObject.SetActive(false);
            }
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

    private async void SetPlayerPos(EntranceDir dir, GameObject pastRoom)
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

    public void SpawnMonster(bool isExplosion = true)
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
    public void SpawnBoss(bool isExplosion = true, Action bossProduct = null)
    {
        for (var i = 0; i < bosses.Length; i++)
        {
            bosses[i].transform.position = firstBossPosList[i];
            bosses[i].IsExplosion = isExplosion;
            bosses[i].IsBoss = true;
            bosses[i].SpawnHpBar();
            bosses[i].gameObject.SetActive(true);
            bosses[i].Appear(bossProduct);
        }
    }

    public Monster SpawnMonster(string id, Vector3 monsterVector, Action removeAction, bool isExplosion = true, bool isBoss = false, Action bossProduct = null)
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
    public void ActiveMonster(Monster monster, Action bossProduct = null)
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

    protected void BgSpriteChange(string bgName)
    {
        foreach (var bgSpriteRenderer in bgSpriteRenderers)
        {
            bgSpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(bgName);
        }
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
        }
    }
    
    // BGM실행
    private void BgmOn()
    {
        PlayBGM(ConstValues.BGMEpisode1);
    }
    
    // 연출1, 해당하는 방 = Room1_1
    private async void Product1()
    {
        // 연출 시작 전 세팅
        StopBGM();
        roomCustomObjects[0].SetActive(false);
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
        var sunObject = RoomManager.Instance.SunObject;
        sunObject.gameObject.transform.position = bossPos[0].position;
        sunObject.gameObject.SetActive(true);
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

        var sunPos = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
        SpawnSpeechFrame(speechFrame2[0], sunPos, dialog4);
        await NextDialog(speechFrame2[0]);

        PlaySound(ConstValues.MonsterSunLaugh);
        var sunMoveVector = new Vector2(sunObject.transform.position.x + 7.5f, sunObject.transform.position.y);
        sunObject.transform.DOMove(sunMoveVector, 2.0f);
        if (await NormalDelay(2.0f, dialogCancellation).SuppressCancellationThrow())
            return;
        sunObject.gameObject.SetActive(false);

        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;

        // 게임 시작
        roomCustomObjects[0].SetActive(true);
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);

        roomInfo.productCount += 1;
        SaveRoom();
    }
}
