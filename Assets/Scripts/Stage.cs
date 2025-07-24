using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class EpisodeStep
{
    // 에피소드 제목을 봤는가?
    public int episodeTitle = 0;
    // 대화 스탭
    public int dialogStep = 0;
    // 플레이어의 시작위치
    public int playerStep = 0;
    // 플레이어가 연출 상 이동하는 위치의 x값
    public int customMoveStep = 0;
    // 이벤트 스탭(스테이지 트리거의 콜라이더를 사라지게 하는 용도로만 쓰임)
    public int eventStep = 0;
}

public abstract class Stage : MonoBehaviour
{
    protected string episodeName;
    protected string episodeTitle;
    protected string clearString;
    protected string buttonString;
    
    [SerializeField] protected EpisodeStep episodeStep;
    [SerializeField] protected Transform[] playerPos;
    [SerializeField] protected ProductTrigger[] stepTrigger;
    [SerializeField] protected Transform[] customMovePos;
    [SerializeField] protected Transform[] monsterPos;
    [SerializeField] protected Transform[] stageWallPos;
    [SerializeField] protected Transform[] trapPos;
    [SerializeField] protected Transform[] bossPos;
    [SerializeField] protected Transform[] strongSpeechPos;
    
    protected CancellationTokenSource dialogCancellation;
    protected CancellationTokenSource waitCancellation;
    private CancellationTokenSource dieCancellation;
    
    [SerializeField] protected bool monsterSpawning;

    protected float dialogDelay1 = 2.5f;
    protected float dialogDelay2 = 1.0f;

    protected List<SpeechFrame> speechFrame1 = new List<SpeechFrame>();
    protected List<SpeechFrame> speechFrame2 = new List<SpeechFrame>();
    [SerializeField] protected SpeechFrame speechFrameStrong;
    [SerializeField] protected SpeechFrame speechFrameTitle;
    [SerializeField] protected List<GameObject> stageWalls = new List<GameObject>();
    [SerializeField] protected SpriteRenderer[] bgSpriteRenderers;
    
    protected Player curPlayer;
    
    // 프로퍼티
    public EpisodeStep EpisodeStep => episodeStep;
    
    protected abstract void StageClearButtonAction();

    private void Awake()
    {
        if (GameManager.Instance)
        {
            //GameManager.Instance.ClearMonsterList();
            GameManager.Instance.DisActiveObjectList();
            CashingSpeechFrame();
        }
        SetEpisodeName();
    }

    protected virtual void Start()
    {
        GameManager.Instance.InitPlayerStat();
    }

    protected virtual void Update()
    {
        CheckCurPlayer();
        GameManager.Instance.ReduceSkillPlayer();
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
    
    // 맵에 있는 모든 몹을 잡았을 경우 발생하는 액션
    protected async void MonsterClearAction(Action action)
    {
        // if (await WaitUntil(() => !monsterSpawning && GameManager.Instance.MonsterList.Count == 0, waitCancellation).SuppressCancellationThrow())
        //     return;
        action?.Invoke();
    }
    protected async void MonsterClearAction(Func<UniTask> asyncAction)
    {
        // if (await WaitUntil(() => !monsterSpawning && GameManager.Instance.MonsterList.Count == 0, waitCancellation).SuppressCancellationThrow())
        //     return;
        asyncAction?.Invoke();
    }
    
    private void CheckCurPlayer()
    {
        if (GameManager.Instance.CurPlayer != null && curPlayer != GameManager.Instance.CurPlayer)
            curPlayer = GameManager.Instance.CurPlayer;
    }

    private void CashingSpeechFrame()
    {
        int count = 3;
        for (int i = 0; i < count; i++)
        {
            speechFrame1.Add(GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrame1));
            speechFrame2.Add(GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrame2));
        }
        for (int i = 0; i < count; i++)
        {
            speechFrame1[i].gameObject.SetActive(false);
            speechFrame2[i].gameObject.SetActive(false); 
        }
        speechFrameStrong = GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrameStrong);
        speechFrameStrong.gameObject.SetActive(false);
        
        speechFrameTitle = GameManager.Instance.GetSpeechFrame(ConstValues.SpeechFrameTitle);
        speechFrameTitle.gameObject.SetActive(false);
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

    protected virtual void SetEpisodeName()
    {
        clearString = "클리어!!";
        buttonString = "종료";
        GameManager.Instance.EpisodeName = episodeName;
    }
    
    // 에피소드 저장
    protected void SaveEpisode()
    {
        // json화
        string json = JsonUtility.ToJson(episodeStep, true);
        EpisodeBinding.SaveEpisode(episodeName, json);
    }
    // 에피소드 불러오기
    protected void LoadEpisode()
    {
        // json화
        string json = JsonUtility.ToJson(episodeStep, true);
        var loadJson = EpisodeBinding.LoadEpisode(episodeName, json);
        // json 불러오기
        var loadedEpisode = JsonUtility.FromJson<EpisodeStep>(loadJson);
        episodeStep = loadedEpisode;
    }
    
    protected void SpawnEpisode(string episodeName)
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_Episode, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_Episode episodeView)
        {
            var episodeInterface = episodeView.EpisodeView.ConvertTo<IUIEpisodeView>();
            var episodeModel = new UIEpisodeModel()
            {
                episodeName = episodeName,
            };
            var episodePresenter = new UIEpisodePresenter(episodeInterface, episodeModel);
            episodeView.SetEpisodePresenter(episodePresenter);
            episodePresenter.SetEpisode();
        }
    }
    protected void ProductEpisode()
    {
        if (episodeStep.episodeTitle != 0)
            return;
        
        var uiEpisodeObj = GameManager.Instance.GetUI(eUIType.UI_Episode);
        if (uiEpisodeObj == null)
            return;

        var uiInterface = uiEpisodeObj.GetComponent<UI_Episode>();
        uiInterface.EpisodePresenter.HandelEpisodeEnd(EpisodeEnd);
        uiInterface.EpisodePresenter.EpisodeProduct(() => { SoundManager.Instance.PlaySound(ConstValues.Upgrade); });
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
    }
    private void EpisodeEnd()
    {
        episodeStep.episodeTitle = 1;
        SaveEpisode();
    }

    protected void SpawnStageClear()
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_StageClear, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_StageClear stageClearView)
        {
            var stageClearInterface = stageClearView.StageClearView.ConvertTo<IUIStageClearView>();
            var stageClearModel = new UIStageClearModel()
            {
                episodeName = episodeTitle,
                clearString = "클리어!!",
                buttonString = "종료(Space)",
                confirmAction = StageClearButtonAction
            };
            var stageClearPresenter = new UIStageClearPresenter(stageClearInterface, stageClearModel);
            stageClearView.SetStageClearPresenter(stageClearPresenter);
            stageClearPresenter.SetStageClear();
        }
    }
    protected void ProductStageClear(int saveStage)
    {
        StageBinding.SaveStage(saveStage);
        episodeStep = new EpisodeStep();
        var uiEpisodeObj = GameManager.Instance.GetUI(eUIType.UI_StageClear);
        if (uiEpisodeObj == null)
            return;
        
        var uiInterface = uiEpisodeObj.GetComponent<UI_StageClear>();
        uiInterface.ViewActive();
        uiInterface.StageClearPresenter.StageClearProduct(() =>
        {
            SoundManager.Instance.PlaySound(ConstValues.Upgrade);
        });
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
    }

    protected void SpawnBossMessage(string bossName)
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
            var episodePresenter = new UIBossMessagePresenter(bossMessageInterface, bossMessageModel);
            bossMessageView.SetEpisodePresenter(episodePresenter);
            bossMessageView.ViewActive();
            episodePresenter.SetBossMessage();
            episodePresenter.BossMessageProduct(() => { SoundManager.Instance.PlaySound(ConstValues.WarningSound); });
        }
    }

    protected void SpawnGuide(PopupGuideModel model)
    {
        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Guide, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is Popup_Guide guideView)
        {
            var guideInterface = guideView.GuideView.ConvertTo<IPopupGuideView>();
            var guideModel = new PopupGuideModel()
            {
                closeAction = () => { uiBase.ReductionClose(true); }
            };
            var guidePresenter = new PopupGuidePresenter(guideInterface, guideModel);
            guideView.SetGuidePresenter(guidePresenter);
            guidePresenter.Expansion(() => { uiBase.ExpansionOpen(true); });
            guidePresenter.SetModel(model.guideMessage, model.imgName);
            guidePresenter.SetAction(guideModel.closeAction);
        }
    }
    
    protected async void GameOverCycle()
    {
        await UniTask.WaitUntil(() => curPlayer.IsDie);
        GameManager.Instance.ControlStart = false;
        dieCancellation = new CancellationTokenSource();
        if (await NormalDelay(1.0f, dieCancellation).SuppressCancellationThrow())
            return;

        RoomManager.Instance.SpawnGameOver();
        Time.timeScale = 0;
    }

    // 대화 단계 증가
    protected void DialogStepUp()
    {
        episodeStep.dialogStep++;
    }
    // 플레이어 시작위치 다음 위치로 변경
    protected void PlayerStepUp()
    {
        episodeStep.playerStep++;
    }
    // 연출 단계 증가
    protected void CustomMoveStepUp()
    {
        episodeStep.customMoveStep++;
    }
    // 이벤트 단계 설정
    protected void SetEventStep(int idx)
    {
        episodeStep.eventStep = idx;
    }

    protected void BgSpriteChange(string bgName)
    {
        foreach (var bgSpriteRenderer in bgSpriteRenderers)
        {
            bgSpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(bgName);
        }
    }
    
    protected async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    protected async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }

    protected void StopBGM()
    {
        BgmManager.Instance.Stop();
    }
    protected void PlayBGM(string bgmName)
    {
        BgmManager.Instance.PlayBgm(bgmName);
    }
    protected void PlaySound(string bgmName)
    {
        SoundManager.Instance.PlaySound(bgmName);
    }
    protected void CameraShake(float amount, float time)
    {
        GameManager.Instance.CameraShake(amount, time);
    }
    protected void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }
}
