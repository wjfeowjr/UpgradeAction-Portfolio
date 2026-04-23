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
    [SerializeField] private TotalRoom totalRoom;

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
        GameManager.Instance.RefreshGoods();

        foreach (var room in totalRoom.RoomArray)
        {
            room.AddRoomData();
            room.AddNpcData();
            room.EntranceSetting();
            room.InfoSetting();
            room.MonsterPosSetting();
            room.BossPosSetting();
            room.ObjectActive(false);
            room.SetShortCutAndMinimapObject();
        }

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
        
        await GameManager.Instance.Fading(1, 0, 0.75f, true, ConstValues.BlackColor);
        GameManager.Instance.InGame = true;
    }
    
    protected virtual void Update()
    {
        GameManager.Instance.ReduceSkillPlayer();
        
        // 테스트 용도
        // if (Input.GetKeyDown(KeyCode.F3))
        // {
        //     GameManager.Instance.UnLockAttributeSlot("HeavySlash");
        //     GameManager.Instance.UnLockRelicSlot(ConstValues.Berserker);
        // }

        if (GameManager.Instance.ControlStart && !GameManager.Instance.BossProduct && !GameManager.Instance.TimeProduct)
        {
            if (popupLayer == 0)
            {
                if (Input.GetKeyDown(GameManager.Instance.miniMapKey))
                    SpawnMinimap();

                if (Input.GetKeyDown(GameManager.Instance.characterInfoKey) && GameManager.Instance.FirstGetAttribute)
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

    private void PopupLayerReset()
    {
        popupLayer = 0;
    }

    private async void ReturnToMenu()
    {
        BgmManager.Instance.Stop();
        SoundManager.Instance.PlaySound(ConstValues.Upgrade, true);
        await GameManager.Instance.Fading(0, 1, 0.5f, false, ConstValues.BlackColor);

        GameManager.Instance.PoolDisActive();
        GameManager.Instance.GoScene(ConstValues.TitleScene);
    }

    private void SpawnPausePopup()
    {
        popupPause = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Pause, Vector3.zero).GetComponent<Popup_Pause>();
        popupPause.ExpansionOpen(true, true);

        var common = new PopupCommonActions
        {
            PlayMoveSound   = () => SoundManager.Instance.PlaySound(ConstValues.Jump1,        true),
            PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true),
            PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton,  true),
        };

        var model = new PopupPauseModel
        {
            resumeAction  = () =>
            {
                popupPause.ReductionClose(true, true);
                PopupLayerReset();
            },
            settingAction = () =>
            {
                
            },
            returnAction  = ReturnToMenu,
            commonActions = common,
        };

        var presenter = new PopupPausePresenter(
            popupPause.PauseView.ConvertTo<IPopupPauseView>(), model);
        popupPause.SetPausePresenter(presenter);
        presenter.SetAction();

        popupLayer = 1;
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
                checkString = string.Format(GameManager.Instance.GetTalk(30101), GameManager.Instance.GetKeyCode(GameManager.Instance.markKey)),
                closeString = string.Format(GameManager.Instance.GetTalk(30102), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey)),
                moveAction = MinimapCameraMove,
                checkAction = SpawnCheckMark,
                closeAction = PopupLayerReset,
            };
            var minimapPresenter = new PopupMinimapPresenter(minimapInterface, minimapModel);
            popupMinimap.SetMinimapPresenter(minimapPresenter);
            popupMinimap.PopupMinimapPresenter.SetMinimapText();
        }
        popupMinimap.PopupMinimapPresenter.OpenAction();
        popupMinimap.PopupMinimapPresenter.SetAction();
        popupLayer = 1;
    }

    private void SpawnCharacterPopup()
    {
        popupCharacter = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Character, Vector3.zero).GetComponent<Popup_Character>();
        string initialPlayerId = GameManager.Instance.CurPlayer.BasicStat.id;

        // 메인 팝업에 주입 및 초기화 실행
        popupCharacter.ExpansionOpen(true, true);
        popupCharacter.InitPresenters(initialPlayerId, PopupLayerReset);
        popupLayer = 1;
    }

    private void MinimapCameraMove()
    {
        var speed = 60.0f;
        var boolArray = new bool[4];
        var minimapCameraPos = GameManager.Instance.MiniMapCamera.position;
        
        float leftLimit = -100;
        float rightLimit = 350;
        float upLimit = 50;
        float downLimit = -50;
        
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
                title = GameManager.Instance.GetTalk(30019),
                message = string.Format(GameManager.Instance.GetTalk(30106), GameManager.Instance.GetKeyCode(GameManager.Instance.spaceKey)),
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
            gameOverPresenter.SetModel();
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
                guideMessage = model.guideMessage,
                imgName = model.imgName,
                closeAction = () =>
                {
                    uiBase.ReductionClose(true, true);
                    PopupLayerReset();
                }
            };
            var guidePresenter = new PopupGuidePresenter(guideInterface, guideModel);
            popupGuide.SetGuidePresenter(guidePresenter);
            guidePresenter.Expansion(() =>
            {
                uiBase.ExpansionOpen(true, true);
            });
            guidePresenter.SetModel();
            guidePresenter.SetAction(guideModel.closeAction);
            popupLayer = 1;
        }
    }
    public void Guide(int idx)
    {
        string guideMessage;

        switch (idx)
        {
            case 40000:
                guideMessage = string.Format(GameManager.Instance.GetTalk(idx), GameManager.Instance.GetKeyCode(GameManager.Instance.miniMapKey));
                break;
            case 40001:
                guideMessage = string.Format(GameManager.Instance.GetTalk(idx), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey));
                break;
            case 40003:
                guideMessage = string.Format(GameManager.Instance.GetTalk(idx), GameManager.Instance.GetKeyCode(GameManager.Instance.characterInfoKey));
                break;
            case 40004:
                guideMessage = string.Format(GameManager.Instance.GetTalk(idx), GameManager.Instance.GetKeyCode(GameManager.Instance.changeCharacterKey));
                break;
            default:
                guideMessage = GameManager.Instance.GetTalk(idx);
                break;
        }

        string imgName = $"{ConstValues.Guide}{idx}";

        var guideModel = new PopupGuideModel()
        {
            guideMessage = guideMessage,
            imgName = imgName,
        };
        
        SpawnGuide(guideModel);
    }
}
