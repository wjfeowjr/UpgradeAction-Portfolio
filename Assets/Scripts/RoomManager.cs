using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class RoomInfo
{
    public int episodeTitle;
    public List<RoomSkillAndPassive> skillAndPassive = new List<RoomSkillAndPassive>();
    public List<RoomTreasureBox> treasureBox = new List<RoomTreasureBox>();
}

[Serializable]
// 스킬 및 패시브
public class RoomSkillAndPassive
{
    public string id;
}

[Serializable]
// 재화나 아이템(보물상자)
public class RoomTreasureBox
{
    public string id;
    public int count;
}

public class RoomManager : Singleton<RoomManager>
{
    [SerializeField] private FollowCamera mainCamera;
    [SerializeField] private Room[] roomArray;
    [SerializeField] private Room currentRoom;
    [SerializeField] private FadeSystem fadeUI;
    
    protected List<SpeechFrame> speechFrame1 = new List<SpeechFrame>();
    protected List<SpeechFrame> speechFrame2 = new List<SpeechFrame>();
    [SerializeField] protected SpeechFrame speechFrameStrong;
    [SerializeField] protected SpeechFrame speechFrameTitle;

    // 프로퍼티
    public Room CurrentRoom => currentRoom;
    
    protected override void Awake()
    {
        if (!SceneChanger.Instance)
            SceneManager.LoadScene(ConstValues.TitleScene); 

        if (GameManager.Instance)
            GameManager.Instance.InitCamera(mainCamera);
    }

    public void Start()
    {
        BgmManager.Instance.Stop();
        
        GameManager.Instance.SetPlayerOrder(ConstValues.Berserker, default); // default

        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.SetGroundVector();
        GameManager.Instance.InitPlayerStat();
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer);

        fadeUI = GameManager.Instance.SpawnToUIPool(ConstValues.FadeUI, Vector3.zero).GetComponent<FadeSystem>();
        fadeUI.gameObject.SetActive(false);
        
        foreach (var room in roomArray)
        {
            room.EntranceSetting();
            room.gameObject.SetActive(false);
        }
        
        currentRoom = roomArray[0];
        currentRoom.gameObject.SetActive(true);
        currentRoom.FirstStart();
        CashingSpeechFrame();
    }
    
    protected virtual void Update()
    {
        GameManager.Instance.ReduceSkillPlayer();
    }
    
    // 페이드 아웃
    public async UniTask EntranceFadeOut()
    {
        fadeUI.gameObject.SetActive(true);
        fadeUI.SetParameter(0, 1, 0.25f, false);
        await fadeUI.Fade();
    }
    
    // 페이드 인
    public async UniTask EntranceFadeIn()
    {
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

    protected void SpawnGuide(PopupGuideModel model)
    {
        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Guide, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is Popup_Guide guideView)
        {
            var guideInterface = guideView.GuideView.ConvertTo<IUIGuideView>();
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
}
