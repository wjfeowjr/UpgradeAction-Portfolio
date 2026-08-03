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
    [SerializeField] private TotalRoom totalRoom;
    [SerializeField] private Room currentRoom;

    private UI_Episode uiEpisode;
    private Popup_Minimap popupMinimap;
    private Popup_Character popupCharacter;
    private Popup_Pause popupPause;
    private CancellationTokenSource dieCancellation;

    [SerializeField] private int popupLayer;

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

    // TotalRoom의 방 정렬 순서 (roomInfoList 정렬 기준으로 사용)
    public Room[] RoomArray => totalRoom ? totalRoom.RoomArray : null;

    public int PopupLayer
    {
        get => popupLayer;
        set => popupLayer = value;
    }

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

    public async void Start()
    {
        GameManager.Instance.SpawnGameInterface();
        GameManager.Instance.InitPlayerStat();
        GameManager.Instance.SpawnPlayer(GameManager.Instance.PlayerList[0]);
        GameManager.Instance.RefreshPlayerHp();
        GameManager.Instance.RefreshPlayerResource();
        GameManager.Instance.RefreshGoods();
        GameManager.Instance.RefreshPlayerIgnorePlatform();

        foreach (var room in totalRoom.RoomArray)
        {
            room.AddRoomData();
            room.AddNpcData();
            room.EntranceSetting();
            room.InfoSetting();
            room.MonsterPosSetting();
            room.BossPosSetting();
            room.ObjectActive(false);
            room.SetMinimapObject();
        }
        // 방 데이터 정리까지 한방에
        GameManager.Instance.SortRoomInfo();
        
        SetPlaceName();
        ActivePlaceName();

        if (string.IsNullOrEmpty(GameManager.Instance.SavePoint))
        {
            currentRoom = totalRoom.RoomArray[0];
            currentRoom.ObjectActive(true);
            currentRoom.FirstStart();
        }
        else
        {
            var savePointName = GameManager.Instance.SavePoint;
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

        if (await GameManager.Instance.Fading(1, 0, 0.75f, true, ConstValues.BlackColor).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.InGame = true;
    }
    
    protected virtual void Update()
    {
        GameManager.Instance.ReduceSkillPlayer();
        
        if (GameManager.Instance.ControlStart && !GameManager.Instance.BossProduct && !GameManager.Instance.TimeProduct)
        {
            if (popupLayer == 0)
            {
                if (Input.GetKeyDown(GameManager.Instance.miniMapKey))
                    SpawnMinimap();

                if (Input.GetKeyDown(GameManager.Instance.characterInfoKey))
                    SpawnCharacterPopup();
            
                if (Input.GetKeyDown(GameManager.Instance.pauseKey))
                    SpawnPausePopup();
            }
        }
    }

    public Room TargetRoom(string id)
    {
        return totalRoom.TargetRoom(id);
    }

    // 세이브 포인트가 활성화된 방을 RoomArray(idx) 순서대로 반환 (패스트 트래블용)
    public List<Room> GetSavePointRooms()
    {
        var list = new List<Room>();
        foreach (var room in totalRoom.RoomArray)
        {
            if (room.SavePointCheck && room.SaveObject)
                list.Add(room);
        }
        return list;
    }

    // 포탈이 발견(portalCheck)된 방을 RoomArray(idx) 순서대로 반환 (포탈 이동용)
    public List<Room> GetPortalRooms()
    {
        var list = new List<Room>();
        foreach (var room in totalRoom.RoomArray)
        {
            if (room.PortalCheck && room.PortalObject)
                list.Add(room);
        }
        return list;
    }

    public void AllMonsterArrive()
    {
        foreach (var room in totalRoom.RoomArray)
            room.AllMonsterArrive();
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

    public void SetCameraPos()
    {
        Vector2 playerPos = GameManager.Instance.CurPlayer.CenterPos.position;
        mainCameraFollow.transform.position = new Vector3(playerPos.x, playerPos.y, mainCameraFollow.transform.position.z);
    }

    protected void SpawnBossMessage(string bossName)
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_BossMessage, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_BossMessage bossMessageView)
        {
            var bossMessageModel = new UIBossMessageModel()
            {
                bossName = bossName
            };
            var episodePresenter = bossMessageView.BossMessageView.Bind(bossMessageModel);
            bossMessageView.SetEpisodePresenter(episodePresenter);
            bossMessageView.ViewActive();
            episodePresenter.SetBossMessage();
            episodePresenter.BossMessageProduct(() => { SoundManager.Instance.PlaySound(ConstValues.WarningSound); });
        }
    }

    private void SpawnPausePopup()
    {
        popupPause = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Pause, Vector3.zero).GetComponent<Popup_Pause>();
        popupPause.FadeOpen(true, true, 0.2f, false).Forget();

        var common = new PopupCommonActions
        {
            PlayMoveSound   = () => SoundManager.Instance.PlaySound(ConstValues.Jump1,        true),
            PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true),
            PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton,  true),
        };

        var model = new PopupPauseModel
        {
            resumeAction  = ResumeAction,
            settingAction = OpenSettingPopup,
            returnAction  = ReturnToMenu,
            commonActions = common,
        };

        var presenter = popupPause.PauseView.Bind(model);
        popupPause.SetPausePresenter(presenter);
        presenter.SetAction();

        PopupLayerOn();
    }

    private async void ResumeAction()
    {
        await popupPause.FadeClose(true, true, 0.2f, true);
        PopupLayerReset();
    }

    private void PopupLayerReset()
    {
        popupLayer = 0;
    }
    
    private void PopupLayerOn()
    {
        popupLayer = 1;
    }
    
    private void OpenSettingPopup()
    {
        popupPause.PausePresenter.SetSettingOpen(true);
        var popup = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Setting, Vector3.zero).GetComponent<Popup_Setting>();
        popup.FadeOpen(false, false, 0.2f, false).Forget();
        popup.InitPresenters(
            () =>
            {
                popupPause.PausePresenter.SetSettingOpen(false); 
                popupPause.PausePresenter.SetButtonText();
            },
            LanguageSetting,
            KeyboardSetting);
        
    }

    private async void ReturnToMenu()
    {
        BgmManager.Instance.Stop();
        SoundManager.Instance.PlaySound(ConstValues.Upgrade, true);
        
        if (await GameManager.Instance.Fading(0, 1, 0.5f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;

        GameManager.Instance.PoolDisActive();
        GameManager.Instance.GoScene(ConstValues.TitleScene);
    }

    private void LanguageSetting()
    {
        foreach (var room in totalRoom.RoomArray)
            room.RefreshTalk();
        
        SetPlaceName();
    }

    private void KeyboardSetting()
    {
        foreach (var room in totalRoom.RoomArray)
            room.RefreshKey();
    }

    private void SpawnMinimap()
    {
        var playerPos = GameManager.Instance.CurPlayer.CenterPos.position;
        var minimapCameraPos = GameManager.Instance.MiniMapCamera.transform.position;
        GameManager.Instance.MiniMapCamera.transform.position = new Vector3(playerPos.x, playerPos.y, minimapCameraPos.z);
        
        popupMinimap = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Minimap, Vector3.zero).GetComponent<Popup_Minimap>();
        
        var minimapModel = new PopupMinimapModel()
        {
            checkString = string.Format(GameManager.Instance.GetTalk(30101), GameManager.Instance.GetKeyCode(GameManager.Instance.enterKey)),
            closeString = string.Format(GameManager.Instance.GetTalk(30102), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey)),
            moveAction = MinimapCameraMove,
            checkAction = SpawnCheckMark,
            closeAction = PopupLayerReset,
        };
        var minimapPresenter = popupMinimap.MinimapView.Bind(minimapModel);
        popupMinimap.SetMinimapPresenter(minimapPresenter);
        popupMinimap.PopupMinimapPresenter.SetMinimapText();

        popupMinimap.OpenAction(PopupLayerReset);
        PopupLayerOn();
    }

    private void SpawnCharacterPopup()
    {
        popupCharacter = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Character, Vector3.zero).GetComponent<Popup_Character>();
        string initialPlayerId = GameManager.Instance.CurPlayer.BasicStat.id;

        // 메인 팝업에 주입 및 초기화 실행
        popupCharacter.ExpansionOpen(true, true).Forget();
        popupCharacter.InitPresenters(initialPlayerId, PopupLayerReset);
        PopupLayerOn();
    }

    private void MinimapCameraMove()
    {
        var speed = 60.0f;
        var boolArray = new bool[4];
        var minimapCameraPos = GameManager.Instance.MiniMapCamera.position;
        
        float leftLimit = -350;
        float rightLimit = 350;
        float upLimit = 100;
        float downLimit = -100;
        
        if(Input.GetKey(GameManager.Instance.leftKey) && minimapCameraPos.x > leftLimit)
            GameManager.Instance.MiniMapCamera.Translate(Vector2.left * (speed * Time.unscaledDeltaTime));
        if(Input.GetKey(GameManager.Instance.rightKey) && minimapCameraPos.x < rightLimit)
            GameManager.Instance.MiniMapCamera.Translate(Vector2.right * (speed * Time.unscaledDeltaTime));
        if(Input.GetKey(GameManager.Instance.upKey) && minimapCameraPos.y < upLimit)
            GameManager.Instance.MiniMapCamera.Translate(Vector2.up * (speed * Time.unscaledDeltaTime));
        if(Input.GetKey(GameManager.Instance.downKey) && minimapCameraPos.y > downLimit)
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

    private async void GameOverCycle()
    {
        await UniTask.WaitUntil(() => GameManager.Instance.InGame && GameManager.Instance.CurPlayer.IsDie);
        GameManager.Instance.ControlStart = false;
        dieCancellation = new CancellationTokenSource();
        if (await NormalDelay(1.0f, dieCancellation).SuppressCancellationThrow())
            return;

        SpawnGameOver();
    }

    public void SpawnGameOver()
    {
        BgmManager.Instance.Stop();
        SoundManager.instance.PlaySound(ConstValues.Lose);
        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_GameOver, Vector3.zero).GetComponent<UIBase>();
        
        if (uiBase is Popup_GameOver popupGameOver)
        {
            var gameOverModel = new PopupGameOverModel()
            {
                title = GameManager.Instance.GetTalk(30019),
                message = string.Format(GameManager.Instance.GetTalk(30106), GameManager.Instance.GetKeyCode(GameManager.Instance.enterKey)),
                replayAction = () =>
                {
                    CloseGameOverAsync(uiBase).Forget();
                }
            };
            var gameOverPresenter = popupGameOver.GameOverView.Bind(gameOverModel);
            popupGameOver.SetGuidePresenter(gameOverPresenter);
            gameOverPresenter.Open(() =>
            {
                uiBase.FadeOpen(true, true, 0.75f, false).Forget();
            });
            gameOverPresenter.SetModel();
        }
    }
    private async UniTaskVoid CloseGameOverAsync(UIBase uiBase)
    {
        await uiBase.FadeClose(true, true, 0.75f, true);
        GameManager.instance.FadeObjectActiveImmediately(true);
        GameManager.Instance.GameOverReset();
        GameManager.Instance.GoScene(ConstValues.BattleScene);
    }

    // 에피소드 소환
    public async UniTask ProductEpisode(string episodeName)
    {
        if (uiEpisode)
            uiEpisode.gameObject.SetActive(true);
        else
            uiEpisode = GameManager.Instance.SpawnToUIPool(eUIType.UI_Episode, Vector3.zero).GetComponent<UI_Episode>();
        
        // 바인딩
        var episodeModel = new UIEpisodeModel()
        {
            episodeName = episodeName,
        };
        var episodePresenter = uiEpisode.EpisodeView.Bind(episodeModel);
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
            // 닫기 연타 시 중복 호출 및 닫는 도중 다른 팝업이 열리는 것을 막기 위한 플래그
            var isClosing = false;
            var guideModel = new PopupGuideModel()
            {
                guideTitle = model.guideTitle,
                guideMessage = model.guideMessage,
                imgNameList = model.imgNameList,
                closeAction = () =>
                {
                    if (isClosing)
                        return;
                    isClosing = true;
                    CloseGuideAsync(uiBase).Forget();
                }
            };
            var guidePresenter = popupGuide.GuideView.Bind(guideModel);
            popupGuide.SetGuidePresenter(guidePresenter);
            guidePresenter.Open(() =>
            {
                uiBase.FadeOpen(true, true, 0.25f).Forget();
            });
            guidePresenter.SetModel();
            guidePresenter.SetAction(guideModel.closeAction);
            PopupLayerOn();
        }
    }
    // 가이드 팝업의 닫기 트윈이 끝난 뒤에 popupLayer를 해제해야
    // 닫는 도중 ESC 연타로 PausePopup이 함께 뜨는 문제를 막을 수 있다.
    private async UniTaskVoid CloseGuideAsync(UIBase uiBase)
    {
        await uiBase.FadeClose(true, true, 0.25f);
        PopupLayerReset();
    }
    public void Guide(int idx)
    {
        int talkIdx = idx + 40000;
        int explainIdx = 100;
        string guideTitle = GameManager.Instance.GetTalk(talkIdx);
        string guideMessage;
        List<string> imagNameList = new List<string>();
        
        switch (idx)
        {
            case 0:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.miniMapKey));
                imagNameList.Add($"{ConstValues.Guide}{0}");
                break;
            case 1:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.potionKey));
                imagNameList.Add($"{ConstValues.Guide}{1}");
                break;
            case 2:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.characterInfoKey));
                imagNameList.Add($"{ConstValues.Guide}{2}");
                break;
            case 3:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey));
                imagNameList.Add($"{ConstValues.Guide}{3}");
                break;
            case 4:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.characterInfoKey));
                imagNameList.Add($"{ConstValues.Guide}{4}");
                break;
            case 5:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.changeCharacterKey));
                imagNameList.Add($"{ConstValues.Guide}{5}");
                imagNameList.Add($"{ConstValues.Guide}{6}");
                break;
            case 6:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.characterInfoKey));
                imagNameList.Add($"{ConstValues.Guide}{7}");
                imagNameList.Add($"{ConstValues.Guide}{8}");
                break;
            case 7:
                guideMessage = string.Format(GameManager.Instance.GetTalk(talkIdx + explainIdx), GameManager.Instance.GetKeyCode(GameManager.Instance.dashKey));
                imagNameList.Add($"{ConstValues.Guide}{9}");
                imagNameList.Add($"{ConstValues.Guide}{10}");
                break;
            default:
                guideMessage = GameManager.Instance.GetTalk(talkIdx + 100);
                break;
        }

        var guideModel = new PopupGuideModel()
        {
            guideTitle = guideTitle,
            guideMessage = guideMessage,
            imgNameList = imagNameList,
        };
        SpawnGuide(guideModel);
    }

    private void SetPlaceName()
    {
        totalRoom.SetPlaceName();
    }

    public void ActivePlaceName()
    {
        totalRoom.ActivePlaceName();
    }
}
