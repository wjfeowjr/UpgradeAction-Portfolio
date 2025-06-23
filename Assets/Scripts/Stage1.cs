using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Stage1 : Stage
{
    [SerializeField] private Monster sunObject;
    [SerializeField] private Monster moonObject;
    [SerializeField] private GameObject[] guideObjects;
    private float monsterInterval1 = 1.0f;
    
    protected override void SetEpisodeName()
    {
        episodeName = ConstValues.Episode1;
        episodeTitle = "에피소드1: 날씨 좋은 날";
        base.SetEpisodeName();
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
                await Product4();
                break;

            case 4:
                Product5();
                break;

            case 5:
                await Product6();
                break;

            case 6:
                Product7();
                break;
            
            case 7:
                await Product8();
                break;
            
            case 8:
                await Product9();
                break;
            
            case 9:
                await Product10();
                break;
        }
    }
    protected override void StageClearButtonAction()
    {
        Application.Quit();
    }
    
    protected override void Start()
    {
        base.Start();
        StepCharacterSetting();
        
        // 초반
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 2,
        //     playerStep = 1,
        //     customMoveStep = 1,
        //     eventStep = 1,
        // };
        // GameManager.Instance.ControlStart = true;
        // 잡초맨 전투
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 4,
        //     playerStep = 2,
        //     customMoveStep = 1,
        //     eventStep = 2,
        // };
        // GameManager.Instance.ControlStart = true;
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 4,
        //     playerStep = 3,
        //     customMoveStep = 2,
        //     eventStep = 4,
        // };
        // GameManager.Instance.ControlStart = true;
        // 태양 전투
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 7,
        //     playerStep = 4,
        //     customMoveStep = 3,
        //     eventStep = 7,
        // };
        // GameManager.Instance.ControlStart = true;
        // 임시
        //GameManager.Instance.SpawnMonster(ConstValues.MonsterCoal, new Vector2(curPlayer.transform.position.x + 5.0f, curPlayer.transform.position.y));
        
        LoadEpisode();

        dialogSwitch = true;
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[episodeStep.playerStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.SetGroundVector();

        CashingSunObject();
        SpawnEpisode(episodeTitle);
        SpawnStageClear();
        GameOverCycle();
        ProductEpisode();
        AccumulatedStep();
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
            string dialog2 = "저 거지같은 태양만\n빼고말이야!";
            string dialog3 = "뿌셔버릴거야!!!";
            string dialog4 = "나 잡아봐라~";
            
            dialogCancellation = new CancellationTokenSource();
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

            var berserkerPos = curPlayer.FontPos.position;
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
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);
            
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
            guideObjects[0].SetActive(true);
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
            string dialog1 = "이거나 먹어랏~!";
            string dialog2 = "닿으면 죽겠지?";
            string dialog3 = "회피를 사용하자!";

            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);

            PlaySound(ConstValues.RewardPage);
            
            sunObject.gameObject.transform.position = new Vector2(bossPos[1].transform.position.x + 3.5f, bossPos[1].transform.position.y);
            sunObject.gameObject.SetActive(true);
            await sunObject.EpisodeMove_X(bossPos[1].transform.position, sunObject.BasicStat.moveSpeed, -1);

            var sunMoveVector = new Vector2(sunObject.transform.position.x + 7.5f, sunObject.transform.position.y);
            var sunSpeechPos = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            
            dialogCancellation = new CancellationTokenSource();
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog1);

            sunObject.SpawnObject(ConstValues.FireFlash, sunObject.CenterPos.position);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;

            var pillarVector = new Vector2(trapPos[0].position.x, GameManager.Instance.GroundPosY);
            sunObject.SpawnObject(ConstValues.MonsterSunPillar, pillarVector);

            AccumulatedStep();

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            await NextDialog(speechFrame2[0]);
            
            PlaySound(ConstValues.MonsterSunLaugh);
            sunObject.transform.DOMove(sunMoveVector, 2.0f);
            if (await NormalDelay(2.0f, dialogCancellation).SuppressCancellationThrow())
                return;
            sunObject.gameObject.SetActive(false);
            
            var berserkerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
            await NextDialog(speechFrame1[0]);
            
            // 게임 시작
            guideObjects[1].SetActive(true);
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
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
        }

        dialogCancellation = new CancellationTokenSource();
        monsterSpawning = true;
        GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[0].position);
        if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            return;
        GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[2].position);
        if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            return;
        monsterSpawning = false;
        
        if (episodeStep.dialogStep == 2)
        {
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            string dialog1 = "뭐야 이 잔디들은!!";
            string dialog2 = "넌 게임을 너무 빨리 끝내려 한다!";
            string dialog3 = "우린 그걸 막으러 온 적이다!!";
            string dialog4 = "악!!!!!!!!!";

            var berserkerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
            await NextDialog(speechFrame1[0]);

            var monster1Pos = GameManager.Instance.MonsterList[0].FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], new Vector2(monster1Pos.x, monster1Pos.y), dialog2);
            await NextDialog(speechFrame1[0]);

            var monster2Pos = GameManager.Instance.MonsterList[1].FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], new Vector2(monster2Pos.x, monster2Pos.y), dialog3);
            await NextDialog(speechFrame1[0]);
            
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], new Vector2(berserkerSpeechPos.x, berserkerSpeechPos.y + 0.5f), dialog4);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);
            
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            
            DialogStepUp();
            PlayerStepUp();
            SaveEpisode();
            dialogSwitch = true;
            //Guide2();
        }
        MyEventStepUp();
    }
    
    private async UniTask Product4()
    {
        dialogCancellation = new CancellationTokenSource();
        if (episodeStep.dialogStep == 3)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var berserkerSpeechPos = curPlayer.FontPos.position;
            string dialog1 = "고작 잡몹 두 마리로\n게임시간을 늘릴 수 있을거같냐!";
            string dialog2 = "그래서 더 많이 왔다!!!";
            
            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
            await NextDialog(speechFrame1[0]);
            
            PlaySound(ConstValues.MonsterBigTreeLog);
            CameraShake(0.1f, 0.2f);
            
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[0].position, dialog2);
        }
        
        monsterSpawning = true;
        // 미리 몹 소환하고 잠재워두기
        var monsterList = new List<Monster>();
        for (int i = 0; i < 30; i++)
        {
            var randX = Random.Range(-monsterInterval1, monsterInterval1);
            int idx = 0;
            if (i >= 5)
                idx = 1;
            if (i >= 10)
                idx = 2;
            if (i >= 15)
                idx = 3;
            if (i >= 20)
                idx = 4;
            if (i >= 25)
                idx = 5;
            
            var randPos = new Vector2(monsterPos[idx].position.x + randX, monsterPos[idx].position.y);
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(ConstValues.MonsterSpinach, randPos));
        }
        foreach (var monster in monsterList)
        {
            GameManager.Instance.ActiveMonster(monster, false);
            // if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            //     return;
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
        }
        monsterSpawning = false;
        
        if (episodeStep.dialogStep == 3)
        {
            await NextDialog(speechFrameStrong);

            string dialog3 = "다 뿌셔주마!!!!!";

            var berserkerSpeechPos = curPlayer.FontPos.position;
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);
            
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            
            DialogStepUp();
            SaveEpisode();
            dialogSwitch = true;
        }
        CustomMoveStepUp();
        MyEventStepUp();
    }
    // 대화가 없는 연출은 UniTask형태가 아님
    private void Product5()
    {
        AccumulatedStep();
        MyEventStepUp();
        SetEventStep();
        SaveEpisode();
    }
    
    private async UniTask Product6()
    {
        AccumulatedStep();
        dialogCancellation = new CancellationTokenSource();
        if (episodeStep.dialogStep == 4)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
        }
        
        monsterSpawning = true;
        var monsterList = new List<Monster>();
        for (int i = 0; i < 3; i++)
        {
            var xPos = -monsterInterval1;
            if (i == 1)
                xPos = 0;
            else if (i == 2)
                xPos = monsterInterval1;
            
            var randPos = new Vector2(monsterPos[6].position.x + xPos, monsterPos[6].position.y);
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(ConstValues.MonsterCoal, randPos));
        }
        for (int i = 0; i < 3; i++)
        {
            var xPos = -monsterInterval1;
            if (i == 1)
                xPos = 0;
            else if (i == 2)
                xPos = monsterInterval1;
            
            var randPos = new Vector2(monsterPos[7].position.x + xPos, monsterPos[7].position.y);
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(ConstValues.MonsterPurple, randPos));
        }
        foreach (var monster in monsterList)
        {
            GameManager.Instance.ActiveMonster(monster, false);
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            // if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            //     return;
        }
        var coalMonster = monsterList[0];
        var purpleMonster = monsterList[3];
        monsterSpawning = false;
        
        if (episodeStep.dialogStep == 4)
        {
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            string dialog1 = "하하! 우린 더 강한 적이다!";
            string dialog2 = "원거리 맛 좀 봐라!";
            string dialog3 = "첫 스테이지부터\n난이도가 왜 이런거야!";
            
            var coalPos = coalMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], coalPos, dialog1);
            await NextDialog(speechFrame1[0]);
            
            var purplePos = purpleMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], purplePos, dialog2);
            await NextDialog(speechFrame1[0]);

            var berserkerPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog3);
            await NextDialog(speechFrame1[0]);

            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            
            DialogStepUp();
            PlayerStepUp();
            SaveEpisode();
            dialogSwitch = true;
        }
        CustomMoveStepUp();
        MyEventStepUp();
    }
    // 대화가 없는 연출은 UniTask형태가 아님
    private void Product7()
    {
        AccumulatedStep();
        MyEventStepUp();
        SetEventStep();
        SaveEpisode();
    }

    private async UniTask Product8()
    {
        AccumulatedStep();

        if (episodeStep.dialogStep == 5)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
        }

        var sunPos = new Vector2(bossPos[2].transform.position.x, bossPos[2].transform.position.y + 3.5f);
        sunObject = GameManager.Instance.SpawnMonster(ConstValues.MonsterSun, sunPos, true, () => { SpawnBossMessage(sunObject.BasicStat.name); });

        if (episodeStep.dialogStep == 5)
        {
            string dialog1 = "넌 표정이 마음에 안 들었어!!";
            string dialog2 = "이제 뿌셔주마!";
            string dialog3 = "덤벼보던가!";
            
            dialogCancellation = new CancellationTokenSource();
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            var speechPosition = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], speechPosition, dialog1);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], speechPosition, dialog2);
            await NextDialog(speechFrame1[0]);

            var sunSpeechPos = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog3); 
            await NextDialog(speechFrame2[0]);

            // 게임 시작
            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            PlayerStepUp();
            SaveEpisode();
            dialogSwitch = true;
        }
        MyEventStepUp();
    }

    private async UniTask Product9()
    {
        MyEventStepUp();
        dialogCancellation = new CancellationTokenSource();
        monsterSpawning = true;
        if (episodeStep.dialogStep == 6)
        {
            dialogSwitch = false;
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            GameManager.Instance.CurPlayer.Immortal = true;

            sunObject.transform.DOMove(bossPos[2].position, 0.5f);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            sunObject.Flip(-1);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            string dialog1 = "어허헝!! 태양은\n죽지 않아!!!";
            string dialog2 = "ㅋ";

            var sunSpeechPos = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog1); 
            await NextDialog(speechFrame2[0]);

            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog2); 
            await NextDialog(speechFrame2[0]);
        }

        // BGM 끄기
        StopBGM();

        if (episodeStep.dialogStep == 6)
        {
            string dialog3 = "어!?";
            string dialog4 = "오오???!";

            var sunSpeechPos = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog3); 
            await sunObject.GetComponent<Monster_Sun>().DieBomb(1, 0);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            await NextDialog(speechFrame2[0]);
            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog4); 
            await sunObject.GetComponent<Monster_Sun>().DieBomb(2, 0.3f);
            await sunObject.GetComponent<Monster_Sun>().DieBomb(2, 0.2f);
            sunObject.DieShake();
            await sunObject.GetComponent<Monster_Sun>().DieBomb(10, 0.1f);
            await NextDialog(speechFrame2[0]);
            sunObject.DieExplosion();
        }
        await UniTask.WaitUntil(() => !sunObject.gameObject.activeSelf);

        // 태양 죽음
        if (episodeStep.dialogStep == 6)
        {
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            string dialog5 = "무식하긴 ㅋ";
            string dialog6 = "이 세상에 영원한 건 없다.";
            string dialog7 = "흙으로 돌아가라 태양...";

            var berserkerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog5); 
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog6); 
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog7); 
            await NextDialog(speechFrame1[0]);
        }

        var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, GameManager.Instance.MainCamera.transform.position).GetComponent<FadeSystem>();
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade();
        BgSpriteChange(ConstValues.BgTutorial2);
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        if (episodeStep.dialogStep == 6)
        {
            string dialog8 = "어둠이 찾아왔다..";
            string dialog9 = "?";
            
            var berserkerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog8); 
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog9); 
        }
        
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        fadeBg.gameObject.SetActive(false);

        BgmManager.Instance.Play();
        var moonPos = new Vector2(bossPos[2].transform.position.x, bossPos[2].transform.position.y + 3.5f);
        moonObject = GameManager.Instance.SpawnMonster(ConstValues.MonsterMoon, moonPos, true, () => { SpawnBossMessage(sunObject.BasicStat.name); });
        monsterSpawning = false;
        
        if (episodeStep.dialogStep == 6)
        {
            string dialog10 = "넌 뭐냐??";
            string dialog11 = "으아아악!\n내 친구 태양을 뿌셔버리다니!";
            string dialog12 = "태양의 복수를 하러\n내가 찾아왔다!";
            string dialog13 = "저것도 거지같이 생겼네!?\n너도 태양 곁으로 보내주마!";
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            await NextDialog(speechFrame1[0]);
            
            var berserkerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog10); 
            await NextDialog(speechFrame1[0]);

            var moonSpeech = new Vector2(moonObject.CenterPos.position.x - 2.0f, moonObject.CenterPos.position.y); 
            SpawnSpeechFrame(speechFrame2[0], moonSpeech, dialog11); 
            await NextDialog(speechFrame2[0]);
            
            SpawnSpeechFrame(speechFrame2[0], moonSpeech, dialog12); 
            await NextDialog(speechFrame2[0]);
            
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog13);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);

                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);
            
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            GameManager.Instance.CurPlayer.Immortal = false;
            DialogStepUp();
            SaveEpisode();
            dialogSwitch = true;
        }
    }

    private async UniTask Product10()
    {
        if (episodeStep.dialogStep == 7)
        {
            dialogSwitch = false;
            string dialog14 = "으아아아아악!!!!";
            string dialog15 = "난 돌아올 것이다!!!";
            string dialog16 = "진짜 어둠이 찾아왔다..";
            string dialog17 = "이제 가야지";
            string dialog18 = "9시간 뒤..";
            string dialog19 = "바보 같은 놈";
            string dialog20 = "밤이라서 잠깐\n없어진 거야";
            string dialog21 = "ㅋ";
            
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            dialogCancellation = new CancellationTokenSource();
            GameManager.Instance.CurPlayer.Immortal = true;

            moonObject.transform.DOMove(bossPos[2].position, 0.5f);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            moonObject.Flip(-1);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);

            moonObject.DieShake();
            moonObject.GetComponent<Monster_Moon>().DieBomb();
            var moonSpeech = new Vector2(moonObject.CenterPos.position.x - 2.0f, moonObject.CenterPos.position.y);
            SpawnSpeechFrame(speechFrame2[0], moonSpeech, dialog14); 
            await NextDialog(speechFrame2[0]);

            SpawnSpeechFrame(speechFrame2[0], moonSpeech, dialog15); 
            await NextDialog(speechFrame2[0]);
            
            moonObject.DieExplosion();
            BgmManager.Instance.Stop();
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, GameManager.Instance.MainCamera.transform.position).GetComponent<FadeSystem>();
            fadeBg.SetParameter(0, 1.0f, 1.5f, false);
            await fadeBg.Fade();
            BgSpriteChange(ConstValues.BgTutorial);
            foreach (var stageWall in stageWalls)
                stageWall.SetActive(false);
            GameManager.Instance.MainCamera.SetTarget(null);
            
            var berserkerPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog16); 
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog17); 
            await NextDialog(speechFrame1[0]);
            
            var movePos = new Vector2(curPlayer.transform.position.x + 15.0f, curPlayer.transform.position.y);
            await curPlayer.EpisodeMove(movePos, curPlayer.BasicStat.moveSpeed, 1);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var titleSpeechPos = Vector3.zero;
            SpawnSpeechFrame(speechFrameTitle, titleSpeechPos, dialog18); 
            await NextDialog(speechFrameTitle);

            BgmManager.Instance.Play();
            PlaySound(ConstValues.ChickenCock);
            fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
            await fadeBg.Fade();

            PlaySound(ConstValues.RewardPage);
            sunObject.gameObject.transform.position = new Vector2(bossPos[2].transform.position.x + 3.5f, bossPos[2].transform.position.y);
            sunObject.gameObject.SetActive(true);
            await sunObject.EpisodeMove_X(bossPos[2].transform.position, sunObject.BasicStat.moveSpeed, -1);

            var sunSpeechPos = new Vector2(sunObject.CenterPos.position.x - 2.0f, sunObject.CenterPos.position.y);
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog19); 
            await NextDialog(speechFrame2[0]);

            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog20); 
            await NextDialog(speechFrame2[0]);
            
            SpawnSpeechFrame(speechFrame2[0], sunSpeechPos, dialog21); 
            await NextDialog(speechFrame2[0]);

            // 엔딩 연출
            DialogStepUp();
            SaveEpisode();
            ProductStageClear();
        }
    }

    private void StepCharacterSetting()
    {
        GameManager.Instance.SetPlayerOrder(ConstValues.Berserker, default); // default
        curPlayer = GameManager.Instance.CurPlayer;
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

                foreach (var guideObject in guideObjects)
                    guideObject.SetActive(false);
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
            case 4:
                foreach (var stageWall in stageWalls)
                    stageWall.SetActive(false);
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(117.4f, GameManager.Instance.MainCamera.MinXAndY.y);
                break;
            
            case 5:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(110.5f, GameManager.Instance.MainCamera.MinXAndY.y);
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(117.5f, GameManager.Instance.MainCamera.MinXAndY.y);

                // 벽 설치
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallLeft, stageWallPos[2]));
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallRight, stageWallPos[3]));
                break;
            
            case 6:
                foreach (var stageWall in stageWalls)
                    stageWall.SetActive(false);
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(138.5f, GameManager.Instance.MainCamera.MinXAndY.y);
                break;
            
            case 7:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(133.7f, GameManager.Instance.MainCamera.MinXAndY.y);
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(138.5f, GameManager.Instance.MainCamera.MinXAndY.y);
                
                // 벽 설치
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallLeft, stageWallPos[4]));
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallRight, stageWallPos[5]));
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

    // private void Guide2()
    // {
    //     var guideModel = new PopupGuideModel()
    //     {
    //         guideMessage = "<color=#F36B6B>'X'</color>키와 우측 하단의 스킬들을 활용하여 전투를 해보세요!",
    //         imgName = "Guide2",
    //     };
    //     SpawnGuide(guideModel);
    // }
    
    // private void Guide3()
    // {
    //     var guideModel = new PopupGuideModel()
    //     {
    //         guideMessage = "<color=#F36B6B>'보스'</color>는 일반 몬스터와 달리 강력한 패턴으로 무장하고 있습니다.\n공격과 스킬을 잘 활용하여 상대하세요!",
    //         imgName = "Guide3",
    //     };
    //     SpawnGuide(guideModel);
    // }
}
