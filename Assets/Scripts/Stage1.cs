using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class Stage1 : Stage
{
    [SerializeField] private Monster sunObject;
    
    private void Start()
    {
        // 잡초맨 전투
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 3,
        //     playerStep = 2,
        //     customMoveStep = 2,
        //     eventStep = 2,
        // };
        // GameManager.Instance.ControlStart = true;
        
        // 태양 전투
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 3,
        //     playerStep = 2,
        //     customMoveStep = 2,
        //     eventStep = 4,
        // };
        // GameManager.Instance.ControlStart = true;

        LoadEpisode();

        dialogSwitch = true;
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[episodeStep.playerStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.SetGroundVector();

        CashingSunObject();
        SpawnEpisode("에피소드1: 날씨 좋은 날");
        SpawnStageClear();
        GameOverCycle();
        ProductEpisode();
        AccumulatedStep();
    }

    private void Update()
    {
        DialogCycle();
    }

    protected override void SetEpisodeName()
    {
        episodeName = ConstValues.Episode1;
    }
    protected override async void DialogStep()
    {
        // 대화 진행
        switch (myEventStep)
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
                Product4();
                break;

            case 4:
                await Product5();
                break;

            case 5:
                await Product6();
                break;
        }
    }
    protected override void StageClearButtonAction()
    {
        Application.Quit();
    }
    
    private void CashingSunObject()
    {
        if (!sunObject)
        {
            sunObject = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSun, bossPos[0]).GetComponent<Monster>();
            sunObject.gameObject.SetActive(false);
        }
    }

    private async UniTask Product1()
    {
        if (episodeStep.dialogStep == 0)
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
            var speechFrame2 =
                GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame2, speechPosition2, dialog1);
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
            SaveEpisode();
            dialogSwitch = true;
        }
        MyEventStepUp();
    }

    private async UniTask Product2()
    {
        // 카메라 제한
        GameManager.Instance.MainCamera.MinXAndY = new Vector2(40.5f, GameManager.Instance.MainCamera.MinXAndY.y);
        if (episodeStep.dialogStep == 1)
        {
            dialogSwitch = false;
            string dialog1 = "ㅋㅋㅋㅋㅋㅋㅋ";
            string dialog2 = "이거나\n먹어랏~!";
            string dialog3 = "닿으면 죽겠지?";
            string dialog4 = "회피를 사용하자!";

            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.CustomMove(customMovePos[episodeStep.customMoveStep].position, 1);

            PlaySound(ConstValues.RewardPage);
            
            sunObject.gameObject.transform.position = new Vector2(bossPos[1].transform.position.x + 3.5f, bossPos[1].transform.position.y);
            sunObject.gameObject.SetActive(true);
            await sunObject.CustomMove(bossPos[1].transform.position, -1, true);

            var speechPosition2 = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            var speechFrame2 =
                GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame2, speechPosition2, dialog1);
            var sunMoveVector = new Vector2(sunObject.transform.position.x + 7.5f, sunObject.transform.position.y);

            speechFrame2.Speech(dialog1);
            
            dialogCancellation = new CancellationTokenSource();
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
            
            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1, speechPosition, dialog3);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.Speech(dialog4);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.gameObject.SetActive(false);
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            
            SetEventStep();
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            SaveEpisode();
            dialogSwitch = true;
            Guide1();
        }
        MyEventStepUp();
    }

    private async UniTask Product3()
    {
        AccumulatedStep();

        if (episodeStep.dialogStep == 2)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.CustomMove(customMovePos[episodeStep.customMoveStep].position, 1);
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

        if (episodeStep.dialogStep == 2)
        {
            string dialog1 = "뭐야 이 시금치들은!!";
            string dialog2 = "여긴 우리\n구역이다.";
            string dialog3 = "그래\n당장 꺼져!";
            string dialog4 = "난 주인공이다.\n아무도 막을 수 없다!";

            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1, speechPosition, dialog1);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.SetPos(new Vector2(GameManager.Instance.MonsterList[0].FontPos.position.x,
                GameManager.Instance.MonsterList[0].FontPos.position.y - 0.5f));
            speechFrame.Speech(dialog2);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.SetPos(new Vector2(GameManager.Instance.MonsterList[1].FontPos.position.x, GameManager.Instance.MonsterList[1].FontPos.position.y - 0.5f));
            speechFrame.Speech(dialog3);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame.SetPos(speechPosition);
            speechFrame.Speech(dialog4);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame.gameObject.SetActive(false);
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            SaveEpisode();
            dialogSwitch = true;
            Guide2();
        }
        MyEventStepUp();
    }

    // 대화가 없는 연출은 UniTask형태가 아님
    private void Product4()
    {
        AccumulatedStep();
        MyEventStepUp();
        SetEventStep();
        SaveEpisode();
    }

    private async UniTask Product5()
    {
        AccumulatedStep();

        if (episodeStep.dialogStep == 3)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.CustomMove(customMovePos[episodeStep.customMoveStep].position, 1);
        }

        var sunPos = new Vector2(bossPos[2].transform.position.x, bossPos[2].transform.position.y + 3.5f);
        sunObject = GameManager.Instance.SpawnMonster(ConstValues.MonsterSun, sunPos, true, () => { SpawnBossMessage(sunObject.BasicStat.name); });

        if (episodeStep.dialogStep == 3)
        {
            string dialog1 = "넌 표정이 마음에 안 들었어!!";
            string dialog2 = "이제 뿌셔주마!";
            string dialog3 = "덤벼보던가ㅋㅋ";
            
            dialogCancellation = new CancellationTokenSource();
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1, speechPosition, dialog1);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame.Speech(dialog2);
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame.gameObject.SetActive(false);
            
            var speechPosition2 = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            var speechFrame2 = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame2, speechPosition2, dialog3);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame2.gameObject.SetActive(false);
            Guide3();
            
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            SaveEpisode();
            dialogSwitch = true;
        }
        MyEventStepUp();
    }

    private async UniTask Product6()
    {
        if (episodeStep.dialogStep == 4)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            dialogCancellation = new CancellationTokenSource();

            GameManager.Instance.CurPlayer.Immortal = true;

            sunObject.transform.DOMove(bossPos[2].position, 0.5f);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            sunObject.Flip(-1);
            await curPlayer.CustomMove(customMovePos[episodeStep.customMoveStep].position, 1);

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            string dialog1 = "어허헝!! 태양은\n죽지 않아!!!";
            string dialog2 = "ㅋ";
            string dialog3 = "어!?";
            string dialog4 = "오오???!";
            string dialog5 = "무식하긴 ㅋ";
            string dialog6 = "이 세상에 영원한 건 없다.";
            string dialog7 = "흙으로 돌아가라 태양...";
            string dialog8 = "어둠이 찾아왔다..";
            string dialog9 = "9시간 뒤..";
            string dialog10 = "?";
            string dialog11 = "ㅋㅋㅋㅋㅋㅋㅋ";
            string dialog12 = "밤이라서 잠깐\n없어진 거야";
            string dialog13 = "ㅋ";
            
            var speechPosition2 = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            var speechFrame2 = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame2, speechPosition2, dialog1);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            speechFrame2.Speech(dialog2);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            // BGM 끄기
            BgmManager.Instance.Stop();
            
            speechFrame2.Speech(dialog3);
            await SunBomb(1, 0);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame2.Speech(dialog4);
            await SunBomb(2, 0.3f);
            await SunBomb(2, 0.2f);
            sunObject.DieShake();
            await SunBomb(10, 0.1f);
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            sunObject.DieExplosion();
            speechFrame2.gameObject.SetActive(false);
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1, speechPosition, dialog5);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame.Speech(dialog6);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame.Speech(dialog7);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame.gameObject.SetActive(false);
            
            var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, GameManager.Instance.MainCamera.transform.position).GetComponent<FadeSystem>();
            fadeBg.SetParameter(0, 1.0f, 1.5f, false);
            await fadeBg.Fade();
            
            speechFrame.Speech(dialog8);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame.gameObject.SetActive(false);
            
            var speechPosition3 = new Vector3(GameManager.Instance.MainCamera.transform.position.x, 0, 0);
            var speechFrame3 = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrameTitle, speechPosition3, dialog9);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame3.gameObject.SetActive(false);
            
            BgmManager.Instance.Play();
            
            PlaySound(ConstValues.ChickenCock);
            speechFrame.Speech(dialog10);
            fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
            await fadeBg.Fade();
            speechFrame.gameObject.SetActive(false);
            
            PlaySound(ConstValues.RewardPage);
            sunObject.gameObject.transform.position = new Vector2(bossPos[2].transform.position.x + 3.5f, bossPos[2].transform.position.y);
            sunObject.gameObject.SetActive(true);
            await sunObject.CustomMove(bossPos[2].transform.position, -1, true);
            
            speechFrame2.Speech(dialog11);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame2.Speech(dialog12);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            // await GameManager.Instance.MainCamera.SetTarget(sunObject.CenterPos.transform, true);
            // await GameManager.Instance.MainCamera.SetZoom(3, 1.0f);
            speechFrame2.Speech(dialog13);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame2.gameObject.SetActive(false);
            
            // 엔딩 연출
            ProductStageClear();
        }
        SaveEpisode();
        MyEventStepUp();
    }
    private async UniTask SunBomb(int slashCount, float slashInterval)
    {
        for (int i = 0; i < slashCount; i++)
        {
            sunObject.HitMaterial();
            //sunObject.SpawnHitEffect(ConstValues.BerserkerAttackHitCrit, 0.5f);
            sunObject.SpawnHitEffect(ConstValues.MonsterSunAttackHit, 1.0f, 1.5f);
            if (await NormalDelay(slashInterval, dialogCancellation).SuppressCancellationThrow())
                return;
        }
    }

    private void AccumulatedStep()
    {
        if(episodeStep.dialogStep > 0)
            PlayBGM(ConstValues.BGMEpisode1);
        else
            PlayBGM(ConstValues.BGMEpisodeStart);
        
        switch (myEventStep)
        {
            case 0:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(0, GameManager.Instance.MainCamera.MinXAndY.y);
                if (episodeStep.dialogStep == 0)
                {
                    sunObject.gameObject.SetActive(true);
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
    
    private void Guide1()
    {
        var guideModel = new PopupGuideModel()
        {
            guideMessage =
                "<color=#F36B6B>'Z'</color>키를 입력하여 회피 할 수 있습니다.\n회피 도중에는 <color=#F36B6B>'무적'</color>입니다.\n<color=#F36B6B>피격, 넘어짐 상태에서도 사용할 수 있습니다.</color>",
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
    
    private void Guide3()
    {
        var guideModel = new PopupGuideModel()
        {
            guideMessage = "<color=#F36B6B>'보스'</color>는 일반 몬스터와 달리 강력한 패턴으로 무장하고 있습니다.\n공격과 스킬을 잘 활용하여 상대하세요!",
            imgName = "Guide3",
        };
        SpawnGuide(guideModel);
    }
}
