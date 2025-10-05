using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class RoomManager : Singleton<RoomManager>
{
    private int groundLayerMask;
    private float groundPosY;
    
    [SerializeField] private Camera mainCamera;
    [SerializeField] private FollowCamera mainCameraFollow;
    [SerializeField] private SpriteRenderer bgSprite;
    [SerializeField] private GameObject bgDeco;
    [SerializeField] private Room currentRoom;
    [SerializeField] private FadeSystem fadeUI;
    [SerializeField] private TotalRoom totalRoom;

    private List<SpeechFrame> speechFrame1 = new List<SpeechFrame>();
    private List<SpeechFrame> speechFrame2 = new List<SpeechFrame>();
    private SpeechFrame speechFrameStrong;
    private SpeechFrame speechFrameTitle;

    private UI_Episode uiEpisode;
    private Popup_Minimap popupMinimap;
    private CancellationTokenSource dieCancellation;

    // 프로퍼티
    public float GroundPosY
    {
        get => groundPosY;
        set => groundPosY = value;
    }

    public Camera MainCamera => mainCamera;
    
    public Room CurrentRoom
    {
        get => currentRoom;
        set => currentRoom = value;
    }

    public List<SpeechFrame> SpeechFrame1 => speechFrame1;
    public List<SpeechFrame> SpeechFrame2 => speechFrame2;
    public SpeechFrame SpeechFrameStrong => speechFrameStrong;
    public SpeechFrame SpeechFrameTitle => speechFrameTitle;

    protected override void Awake()
    {
        if (!SceneChanger.Instance)
        {
            SceneManager.LoadScene(ConstValues.TitleScene);
            return;
        }

        if (GameManager.Instance)
            GameManager.Instance.InitCamera(mainCameraFollow);
        
        groundLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Ground);
    }

    public void Start()
    {
        BgmManager.Instance.Play();
        // 최초 캐릭터 세팅
        GameManager.Instance.SetPlayerOrder(GameManager.Instance.FirstPlayer, GameManager.Instance.SecondPlayer);

        GameManager.Instance.SpawnGameInterface();
        GameManager.Instance.InitPlayerStat();
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer);
        GameManager.Instance.RefreshPlayerHp();
        GameManager.Instance.RefreshGoods();

        fadeUI = GameManager.Instance.SpawnToUIPool(ConstValues.FadeUI, Vector3.zero).GetComponent<FadeSystem>();
        fadeUI.gameObject.SetActive(false);
        
        CashingSpeechFrame();
        
        foreach (var room in totalRoom.RoomArray)
        {
            room.SpeechFrameSetting();
            room.EntranceSetting();
            room.InfoSetting();
            room.MonsterPosSetting();
            room.BossPosSetting();
            room.ObjectActive(false);
        }

        if (string.IsNullOrEmpty(SavePointBinding.LoadSavePoint()))
        {
            currentRoom = totalRoom.RoomArray[0];
            currentRoom.ObjectActive(true);
            currentRoom.FirstStart();
        }
        else
        {
            var savePointName = SavePointBinding.LoadSavePoint();
            foreach (var room in totalRoom.RoomArray)
            {
                if (room.name != savePointName)
                    continue;
                
                currentRoom = room;
                currentRoom.ObjectActive(true);
                currentRoom.SaveStart();
                break;
            }
        }

        // 세팅
        GameOverCycle();
    }
    
    protected virtual void Update()
    {
        GameManager.Instance.ReduceSkillPlayer();

        if ((!popupMinimap || !popupMinimap.gameObject.activeSelf) && GameManager.Instance.ControlStart && !GameManager.Instance.BossProduct && Input.GetKeyDown(GameManager.Instance.tabKey))
            SpawnMinimap();
    }

    public void BgSpriteChange(string spriteName)
    {
        bgSprite.sprite =  GameManager.Instance.GetAtlasSprite(spriteName);
    }

    public void BgDecoActive(bool active)
    {
        bgDeco.SetActive(active);
    }
    
    public void SetGroundVector()
    {
        var downRay = Physics2D.Raycast(currentRoom.transform.position, Vector2.down, 100f, groundLayerMask);
        if (downRay.collider != null)
            groundPosY = downRay.point.y;
    }
    
    // 페이드 아웃
    public async UniTask FadeOut(Color settingColor)
    {
        fadeUI.ColorInput(settingColor);
        fadeUI.gameObject.SetActive(true);
        fadeUI.SetParameter(0, 1, 0.25f, false);
        await fadeUI.Fade();
    }
    
    // 페이드 인
    public async UniTask FadeIn(Color settingColor)
    {
        fadeUI.ColorInput(settingColor);
        fadeUI.gameObject.SetActive(true);
        fadeUI.SetParameter(1, 0, 0.25f, false);
        await fadeUI.Fade();
    }

    // 페이드 루프
    public async UniTask EntranceFadeLoop()
    {
        fadeUI.gameObject.SetActive(true);
        fadeUI.SetParameter(1, 0, 0.35f, true, 1);
        await fadeUI.Fade();
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

    private void SpawnMinimap()
    {
        var playerPos = GameManager.Instance.CurPlayer.CenterPos.position;
        var minimapCameraPos = GameManager.Instance.MiniMapCamera.transform.position;
        GameManager.Instance.MiniMapCamera.transform.position = new Vector3(playerPos.x, playerPos.y, minimapCameraPos.z);
        if (popupMinimap)
        {
            popupMinimap.gameObject.SetActive(true);
        }
        else
        {
            popupMinimap = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Minimap, Vector3.zero).GetComponent<Popup_Minimap>();
        
            var minimapInterface = popupMinimap.MinimapView.ConvertTo<IPopupMinimapView>();
            var minimapModel = new PopupMinimapModel()
            {
                checkString = "마크: Enter",
                closeString = "닫기: Esc",
                moveAction = MinimapCameraMove,
                checkAction = SpawnCheckMark,
            };
            var minimapPresenter = new PopupMinimapPresenter(minimapInterface, minimapModel);
            popupMinimap.SetMinimapPresenter(minimapPresenter);
            popupMinimap.PopupMinimapPresenter.SetMinimapText();
        }
        popupMinimap.PopupMinimapPresenter.OpenAction();
    }

    private void MinimapCameraMove()
    {
        var speed = 60.0f;
        var boolArray = new bool[4];
        var minimapCameraPos = GameManager.Instance.MiniMapCamera.position;
        
        float leftLimit = -100;
        float rightLimit = 250;
        float upLimit = 50;
        float downLimit = -50;
        
        if(Input.GetKey(KeyCode.LeftArrow) && minimapCameraPos.x > leftLimit)
            GameManager.Instance.MiniMapCamera.Translate(Vector2.left * (speed * Time.unscaledDeltaTime));
        if(Input.GetKey(KeyCode.RightArrow) && minimapCameraPos.x < rightLimit)
            GameManager.Instance.MiniMapCamera.Translate(Vector2.right * (speed * Time.unscaledDeltaTime));
        if(Input.GetKey(KeyCode.UpArrow) && minimapCameraPos.y < upLimit)
            GameManager.Instance.MiniMapCamera.Translate(Vector2.up * (speed * Time.unscaledDeltaTime));
        if(Input.GetKey(KeyCode.DownArrow) && minimapCameraPos.y > downLimit)
            GameManager.Instance.MiniMapCamera.Translate(Vector2.down * (speed * Time.unscaledDeltaTime));
        
        boolArray[0] = minimapCameraPos.x <= leftLimit;
        boolArray[1] = minimapCameraPos.x >= rightLimit;
        boolArray[2] = minimapCameraPos.y >= upLimit;
        boolArray[3] =minimapCameraPos.y <= downLimit;
        popupMinimap.PopupMinimapPresenter.LimitAction(boolArray);
    }

    private void SpawnCheckMark()
    {
        totalRoom.SpawnChecker();
    }
    
    protected async void GameOverCycle()
    {
        await UniTask.WaitUntil(() => GameManager.Instance.CurPlayer.IsDie);
        GameManager.Instance.ControlStart = false;
        dieCancellation = new CancellationTokenSource();
        if (await NormalDelay(1.0f, dieCancellation).SuppressCancellationThrow())
            return;

        SpawnGameOver();
        Time.timeScale = 0;
    }

    public void SpawnGameOver()
    {
        BgmManager.Instance.Stop();
        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_GameOver, Vector3.zero).GetComponent<UIBase>();
        
        if (uiBase is Popup_GameOver popupGameOver)
        {
            var gameOverInterface = popupGameOver.GameOverView.ConvertTo<IPopupGameOverView>();
            var gameOverModel = new PopupGameOverModel()
            {
                title = "게임 오버",
                message = "다시 하기(Space)",
                replayAction = () =>
                {
                    GameManager.Instance.GoScene(ConstValues.BattleScene);
                    uiBase.Close();
                    GameManager.Instance.ControlStart = true;
                    Time.timeScale = 1;
                }
            };
            var gameOverPresenter = new PopupGameOverPresenter(gameOverInterface, gameOverModel);
            popupGameOver.SetGuidePresenter(gameOverPresenter);
            gameOverPresenter.SetPopup();
        }
    }
    
    // 에피소드 소환
    public async UniTask ProductEpisode(string episodeName)
    {
        if (uiEpisode)
            uiEpisode.gameObject.SetActive(true);
        else
            uiEpisode = GameManager.Instance.SpawnToUIPool(eUIType.UI_Episode, Vector3.zero).GetComponent<UI_Episode>();
        
        // 바인딩
        var episodeInterface = uiEpisode.EpisodeView.ConvertTo<IUIEpisodeView>();
        var episodeModel = new UIEpisodeModel()
        {
            episodeName = episodeName,
        };
        var episodePresenter = new UIEpisodePresenter(episodeInterface, episodeModel);
        uiEpisode.SetEpisodePresenter(episodePresenter);
        episodePresenter.SetEpisode();
        
        await uiEpisode.EpisodePresenter.EpisodeProduct(() => { SoundManager.Instance.PlaySound(ConstValues.Upgrade); });
    }

    protected async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    /// <summary>
    /// 가이드 구현 구간
    /// </summary>
    private void SpawnGuide(PopupGuideModel model)
    {
        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Guide, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is Popup_Guide popupGuide)
        {
            var guideInterface = popupGuide.GuideView.ConvertTo<IPopupGuideView>();
            var guideModel = new PopupGuideModel()
            {
                closeAction = () => { uiBase.ReductionClose(true); }
            };
            var guidePresenter = new PopupGuidePresenter(guideInterface, guideModel);
            popupGuide.SetGuidePresenter(guidePresenter);
            guidePresenter.Expansion(() => { uiBase.ExpansionOpen(true); });
            guidePresenter.SetModel(model.guideMessage, model.imgName);
            guidePresenter.SetAction(guideModel.closeAction);
        }
    }
    public void Guide(int idx)
    {
        var guideModel = new PopupGuideModel();
        string guideName = $"{ConstValues.Guide}{idx}";

        switch (idx)
        {
            case 1:
                guideModel = new PopupGuideModel()
                {
                    guideMessage = "<color=#F36B6B>'Z'</color>키를 입력하여 대시를 할 수 있습니다.\n대시 도중에는 <color=#F36B6B>'무적'</color>입니다.\n<color=#F36B6B>피격, 넘어짐 상태에서도 사용할 수 있습니다.</color>",
                    imgName = guideName,
                };
                break;
            case 2:
                guideModel = new PopupGuideModel()
                {
                    guideMessage = "지역을 탐험하며 새로운 스킬을 획득 할 수 있습니다.\n스킬은 퀵슬롯에 등록되며, 다른 키에 등록하여 사용 할 수도 있습니다.",
                    imgName = guideName,
                };
                break;
            case 3:
                guideModel = new PopupGuideModel()
                {
                    guideMessage = "<color=#F36B6B>'Tab'</color>키를 입력하여 미니맵을 볼 수 있습니다.\n플레이어가 지나왔던 구역의 시야가 밝혀집니다.</color>",
                    imgName = guideName,
                };
                break;
            case 4:
                guideModel = new PopupGuideModel()
                {
                    guideMessage = "세이브 포인트에서 <color=#F36B6B>'↑'</color>키를 입력하여 체크포인트를 설정 할 수 있습니다.\n세이브 시 모든 체력이 회복됩니다.",
                    imgName = guideName,
                };
                break;
            case 5:
                guideModel = new PopupGuideModel()
                {
                    guideMessage = "<color=#F36B6B>'Shift'</color>키를 입력하여 캐릭터를 교체 할 수 있습니다.\n<color=#F36B6B>캐릭터의 체력은 서로 공유됩니다.</color>",
                    imgName = guideName,
                };
                break;
        }
        SpawnGuide(guideModel);
    }
}
