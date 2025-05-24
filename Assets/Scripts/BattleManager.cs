using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private FollowCamera mainCamera;
    [SerializeField] private Monster sunObject;
    
    [SerializeField] private Transform[] playerPos;
    [SerializeField] private Transform[] stepPos;
    [SerializeField] private Transform[] customMovePos;
    [SerializeField] private Transform[] monsterPos;
    [SerializeField] private Transform[] stageWallPos;
    [SerializeField] private Transform[] trapPos;
    [SerializeField] private Transform[] bossPos;
    
    [SerializeField] private List<Collider2D> platformColliderList;
    private CancellationTokenSource dialogCancellation;
    private CancellationTokenSource productCancellation;
    private CancellationTokenSource dieCancellation;

    [SerializeField] private int episodeTitle = 0;
    [SerializeField] private bool dialogSwitch;
    
    // 대화 스탭
    [SerializeField] private int dialogStep = 0;
    // 플레이어의 시작위치
    [SerializeField] private int playerStep = 0;
    // 플레이어가 연출 상 이동하는 위치의 x값
    [SerializeField] private int customMoveStep = 0;
    // 현재 스탭
    [SerializeField] private int curStep = 0;

    private float dialogDelay1 = 2.5f;
    private float dialogDelay2 = 1.0f;

    [SerializeField] private List<GameObject> stageWalls = new List<GameObject>();
    private Player curPlayer;
    
    private void Awake()
    {
        if (SceneChanger.Instance)
            SceneChanger.Instance.SceneControl();

        if (GameManager.Instance)
        {
            GameManager.Instance.InitCamera(mainCamera);
            GameManager.Instance.ClearMonsterList();
            GameManager.Instance.DisActiveObjectList();
            curPlayer = GameManager.Instance.CurPlayer;
        }
    }

    private void Start()
    {
        // episodeTitle = GetKey(ConstValues.Episode);
        // dialogStep = GetKey(ConstValues.DialogStep);
        // customMoveStep = GetKey(ConstValues.CustomMoveStep);
        // playerStep = GetKey(ConstValues.PlayerStep);
        // curStep = GetKey(ConstValues.CurStep);
        
        episodeTitle = 1;
        dialogStep = 3;
        customMoveStep = 2;
        playerStep = 3;
        curStep = 3;
        GameManager.Instance.ControlStart = true;

        dialogSwitch = true;
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[playerStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.PlatformColliderList = platformColliderList;
        GameManager.Instance.SetGroundVector();
        
        SpawnEpisode();
        GameOverCycle();
        ProductEpisode();
        AccumulatedStep();
    }

    private void Update()
    {
        DialogCycle();
    }

    private void SpawnEpisode()
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_Episode, Vector2.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_Episode episodeView)
        {
            var episodeInterface = episodeView.EpisodeView.ConvertTo<IUIEpisodeView>();
            var episodeModel = new UIEpisodeModel()
            {
                episodeName = "에피소드1: 날씨 좋은 날",
            };
            var episodePresenter = new UIEpisodePresenter(episodeInterface, episodeModel);
            episodeView.SetEpisodePresenter(episodePresenter);
            episodePresenter.SetEpisode();
        }
    }
    
    private void SpawnGuide(PopupGuideModel model)
    {
        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Guide, Vector2.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is Popup_Guide guideView)
        {
            var guideInterface = guideView.GuideView.ConvertTo<IUIGuideView>();
            var guideModel = new PopupGuideModel()
            {
                closeAction = ()=>{ uiBase.ReductionClose(true);}
            };
            var guidePresenter = new PopupGuidePresenter(guideInterface, guideModel);

            guidePresenter.Expansion(() => { uiBase.ExpansionOpen(true);});
            guidePresenter.SetModel(model.guideMessage, model.imgName);
            guidePresenter.SetAction(guideModel.closeAction);
        }
    }
    private void Guide1()
    {
        var guideModel = new PopupGuideModel()
        {
            guideMessage = "<color=#F36B6B>'Z'</color>키를 입력하여 회피 할 수 있습니다.\n회피 도중에는 <color=#F36B6B>'무적'</color>입니다.\n피격, 넘어짐 상태에서도 사용할 수 있습니다.",
            imgName = "Guide1",
        };
        SpawnGuide(guideModel);
    }
    private void Guide2()
    {
        var guideModel = new PopupGuideModel()
        {
            guideMessage = "<color=#F36B6B>'X'</color>키와 우측 하단의 스킬들을 활용하여 전투를 해보세요!\n몬스터한테 맞아 체력이 다 깎이면 죽습니다.",
            imgName = "Guide2",
        };
        SpawnGuide(guideModel);
    }

    private int GetKey(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            Debug.Log($"저장된 {key}가 존재 = {PlayerPrefs.GetInt(key)}");
            return PlayerPrefs.GetInt(key);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"{key} = 0");
            PlayerPrefs.SetInt(key, 0);
            return PlayerPrefs.GetInt(key);
        }
    }

    private void SetKey(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        //return PlayerPrefs.GetInt(key);
    }

    private void ProductEpisode()
    {
        if (episodeTitle != 0)
            return;
        
        PlayBGM(ConstValues.BGMEpisodeStart);
        
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
        SetKey(ConstValues.Episode, 1);
        episodeTitle = GetKey(ConstValues.Episode);
    }

    private async void GameOverCycle()
    {
        await UniTask.WaitUntil(() => curPlayer.IsDie);
        GameManager.Instance.ControlStart = false;
        dieCancellation = new CancellationTokenSource();
        if (await NormalDelay(1.0f, dieCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.SpawnToPopupPool(eUIType.Popup_GameOver, Vector2.zero);
        Time.timeScale = 0;
    }
    
    private void DialogCycle()
    {
        if (episodeTitle == 0)
            return;
        
        if (curStep > stepPos.Length - 1)
            return;

        if (dialogSwitch && curPlayer.transform.position.x >= stepPos[curStep].transform.position.x && GameManager.Instance.MonsterList.Count == 0)
        {
            // 대화 진행
            DialogStep();
        }
    }
    
    private async void DialogStep()
    {
        // 대화 진행
        switch (curStep) 
        {
            case 0:
                await Product1();
                break;
            
            case 1:
                await Product2();
                break;
            
            case 2:
                await Product3();
                break;
            
            case 3:
                await Product4();
                break;
            
            case 4:
                await Product5();
                break;
        }
    }

    private async UniTask Product1()
    {
        curStep++;
        
        if (dialogStep == 0)
        {
            dialogSwitch = false;
            string dialog1 = "날씨 참 좋다...";
            string dialog2 = "저 거지같은\n태양만 빼고\n말이야!";
            string dialog3 = "뿌셔버릴거야!!!";
            string dialog4 = "ㅋㅋㅋㅋㅋㅋ";
            string dialog5 = "나 잡아봐라~";

            dialogCancellation = new CancellationTokenSource();
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;

            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1, speechPosition, dialog1);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.Speech(dialog2);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            PlayBGM(ConstValues.BGMEpisode1);
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 1.0f);

            speechFrame.SetPos(new Vector2(speechPosition.x, speechPosition.y + 0.5f));
            speechFrame.Speech(dialog3);

            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame.gameObject.SetActive(false);
            
            var speechPosition2 = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            var speechFrame2 = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame2, speechPosition2, dialog1);
            var sunMoveVector = new Vector2(sunObject.transform.position.x + 7.5f, sunObject.transform.position.y);
            
            speechFrame2.Speech(dialog4);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame2.Speech(dialog5);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame2.gameObject.SetActive(false);

            PlaySound(ConstValues.MonsterSunLaugh);
            sunObject.transform.DOMove(sunMoveVector, 2.0f);
            if (await NormalDelay(2.0f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            sunObject.gameObject.SetActive(false);
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            //GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, playerPos[0].position);
            dialogSwitch = true;
        }
    }

    private async UniTask Product2()
    {
        // 카메라 제한
        GameManager.Instance.MainCamera.MinXAndY = new Vector2(40.5f, GameManager.Instance.MainCamera.MinXAndY.y);
        if (dialogStep == 1)
        {
            dialogSwitch = false;
            string dialog1 = "ㅋㅋㅋㅋㅋㅋㅋ";
            string dialog2 = "이거나\n먹어랏~!";
            
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.CustomMove(customMovePos[customMoveStep].position, 1);

            PlaySound(ConstValues.RewardPage);
            sunObject.gameObject.transform.position = new Vector2(bossPos[1].transform.position.x + 3.5f, bossPos[1].transform.position.y);
            sunObject.gameObject.SetActive(true);
            await sunObject.CustomMove(bossPos[1].transform.position, -1, true);

            var speechPosition2 = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            var speechFrame2 = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame2, speechPosition2, dialog1);
            var sunMoveVector = new Vector2(sunObject.transform.position.x + 7.5f, sunObject.transform.position.y);
            
            speechFrame2.Speech(dialog1);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame2.Speech(dialog2);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            sunObject.SpawnObject(ConstValues.FireFlash, sunObject.CenterPos.position);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var pillarVector = new Vector2(trapPos[0].position.x, GameManager.Instance.GroundPosY);
            sunObject.SpawnObject(ConstValues.MonsterSunPillar, pillarVector);

            AccumulatedStep();
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame2.gameObject.SetActive(false);
            
            PlaySound(ConstValues.MonsterSunLaugh);
            sunObject.transform.DOMove(sunMoveVector, 2.0f);
            if (await NormalDelay(2.0f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            sunObject.gameObject.SetActive(false);
        }
        SetKey(ConstValues.CurStep, curStep);
        curStep++;

        if (dialogStep == 1)
        {
            string dialog1 = "불기둥이 너무 뜨거워!";
            string dialog2 = "회피를 사용해야겠어!";
            
            dialogCancellation = new CancellationTokenSource();
            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1,speechPosition, dialog1);
                
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
        
            speechFrame.Speech(dialog2);
                
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
                
            speechFrame.gameObject.SetActive(false);
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            dialogSwitch = true;
            Guide1();
        }
    }
    
    private async UniTask Product3()
    {
        AccumulatedStep();
        SetKey(ConstValues.CurStep, curStep);
        curStep++;

        if (dialogStep == 2)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.CustomMove(customMovePos[customMoveStep].position, 1);
        }

        dialogCancellation = new CancellationTokenSource();
        GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[0].position);
        if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[1].position);
        if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
            return;
        
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;

        if (dialogStep == 2)
        {
            string dialog1 = "뭐야 이 시금치들은!!";
            string dialog2 = "여긴 우리\n구역이다.";
            string dialog3 = "그래\n당장 꺼져!";
            string dialog4 = "악!!!!!!!!";

            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1,speechPosition, dialog1);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.SetPos(new Vector2(GameManager.Instance.MonsterList[0].FontPos.position.x, GameManager.Instance.MonsterList[0].FontPos.position.y - 0.5f));
            speechFrame.Speech(dialog2);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.SetPos(new Vector2(GameManager.Instance.MonsterList[1].FontPos.position.x, GameManager.Instance.MonsterList[1].FontPos.position.y - 0.5f));
            speechFrame.Speech(dialog3);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 1.0f);
            speechFrame.SetPos(new Vector2(speechPosition.x, speechPosition.y + 0.5f));
            speechFrame.Speech(dialog4);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.gameObject.SetActive(false);
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            dialogSwitch = true;
            Guide2();
        }
    }
    
    private async UniTask Product4()
    {
        AccumulatedStep();
        curStep++;
        SetKey(ConstValues.CurStep, curStep);
        
        if (dialogStep == 3)
            DialogStepUp();
    }
    
    private async UniTask Product5()
    {
        AccumulatedStep();
        SetKey(ConstValues.CurStep, curStep);
        curStep++;
        
        if (dialogStep == 4)
        {
            dialogSwitch = false;
            string dialog1 = "널 뿌셔버리려고\n여기까지 왔다!";

            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.CustomMove(customMovePos[customMoveStep].position, 1);
            
            var sunPos = new Vector2(bossPos[2].transform.position.x, bossPos[2].transform.position.y + 3.5f);
            sunObject = GameManager.Instance.SpawnMonster(ConstValues.MonsterSun, sunPos, true);
            
            dialogCancellation = new CancellationTokenSource();
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1,speechPosition, dialog1);
                
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.gameObject.SetActive(false);
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            dialogSwitch = true;
        }

        Debug.Log("보스 등장!");
    }

    // 대화 단계 증가
    private void DialogStepUp()
    {
        dialogStep++;
        SetKey(ConstValues.DialogStep, dialogStep);
    }
    // 플레이어 시작위치 다음 위치로 변경
    private void PlayerStepUp()
    {
        playerStep++;
        SetKey(ConstValues.PlayerStep, playerStep);
    }
    // 연출 단계 증가
    private void CustomMoveStepUp()
    {
        customMoveStep++;
        SetKey(ConstValues.CustomMoveStep, customMoveStep);
    }

    private void AccumulatedStep()
    {
        if(dialogStep > 0)
            PlayBGM(ConstValues.BGMEpisode1);
        
        switch (curStep)
        {
            case 0:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(0, GameManager.Instance.MainCamera.MinXAndY.y);
                if (dialogStep == 0)
                {
                    sunObject = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSun, bossPos[0]).GetComponent<Monster>();
                    sunObject.Flip(-1);
                }
                break;
            case 1:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(40.5f, GameManager.Instance.MainCamera.MinXAndY.y);
                // 함정 설치
                GameManager.Instance.SpawnTrap(ConstValues.TrapPillar, trapPos[0].position);
                break;
            case 2:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(76, GameManager.Instance.MainCamera.MinXAndY.y);
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(92, GameManager.Instance.MainCamera.MinXAndY.y);

                // 벽 설치
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallLeft, stageWallPos[0]));
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallRight, stageWallPos[1]));
                break;
            case 3:
                foreach (var stageWall in stageWalls)
                    stageWall.SetActive(false);
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(112.4f, GameManager.Instance.MainCamera.MinXAndY.y);
                break;
            case 4:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(110, GameManager.Instance.MainCamera.MinXAndY.y);
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(114.8f, GameManager.Instance.MainCamera.MinXAndY.y);

                // 벽 설치
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallLeft, stageWallPos[2]));
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallRight, stageWallPos[3]));
                break;
        }
    }

    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
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
}
