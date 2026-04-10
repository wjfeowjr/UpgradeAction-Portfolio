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
    private Monster dialogMonster1;
    private Monster dialogMonster2;
    private float monsterInterval1 = 1.0f;

    protected override async void Start()
    {
        base.Start();
        //stepTrigger[0].SetAction(() => Product1(0));
        //stepTrigger[1].SetAction(() => Product2(1));
        //stepTrigger[2].SetAction(() => Product3(2));
        //stepTrigger[3].SetAction(() => Product6(3));
        //stepTrigger[4].SetAction(() => Product8(4));

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
        //     dialogStep = 2, // 3
        //     playerStep = 1,
        //     customMoveStep = 1,
        //     eventStep = 2,
        // };
        // GameManager.Instance.ControlStart = true;
        // 석탄맨 전투
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 5,
        //     playerStep = 3,
        //     customMoveStep = 2,
        //     eventStep = 3,
        // };
        // GameManager.Instance.ControlStart = true;
        // 태양 전투
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 6,
        //     playerStep = 4,
        //     customMoveStep = 3,
        //     eventStep = 4,
        // };
        // GameManager.Instance.ControlStart = true;

        LoadEpisode();
        StepCharacterSetting();
        
        //GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[episodeStep.playerStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        //GameManager.Instance.SetGroundVector();

        CashingSunObject();
        SpawnEpisode(episodeTitle);
        SpawnStageClear();
        GameOverCycle();
        ProductEpisode();
        AccumulatedStep();
    }
    
    protected override void SetEpisodeName()
    {
        episodeName = ConstValues.Episode1;
        episodeTitle = "에피소드1: 날씨 좋은 날";
        base.SetEpisodeName();
    }
    
    protected override void StageClearButtonAction()
    {
        GameManager.Instance.GoScene(ConstValues.BattleScene);
    }

    private void CashingSunObject()
    {
        if (!sunObject)
        {
            sunObject = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSun, bossPos[0]).GetComponent<Monster>();
            sunObject.gameObject.SetActive(false);
        }
    }

    private async UniTask Product1(int idx)
    {
        await UniTask.WaitUntil(()=> episodeStep.episodeTitle > 0);
        SetEventStep(idx);
        
        if (episodeStep.dialogStep == 0)
        {
            string dialog1 = "날씨 참 좋다...";
            string dialog2 = "저 거지같은 태양만\n빼고말이야!";
            string dialog3 = "뿌셔버릴거야!!!";
            string dialog4 = "나 잡아봐라~";
            
            dialogCancellation = new CancellationTokenSource();
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

            var berserkerPos = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog1);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog2);
            await NextDialog(speechFrame1[0]);
            
            PlayBGM(ConstValues.BGMSunHill);
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 0.4f, 1.0f);
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
        }
    }

    private async UniTask Product2(int idx)
    {
        // 카메라 제한
        SetEventStep(idx);
        //GameManager.Instance.MainCamera.MinXAndY = new Vector2(40.5f, GameManager.Instance.MainCamera.MinXAndY.y);
        if (episodeStep.dialogStep == 1)
        {
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

            //var pillarVector = new Vector2(trapPos[0].position.x, GameManager.Instance.GroundPosY);
            //sunObject.SpawnObject(ConstValues.MonsterSunPillar, pillarVector);

            AccumulatedStep();

            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            await NextDialog(speechFrame2[0]);
            
            PlaySound(ConstValues.MonsterSunLaugh);
            sunObject.transform.DOMove(sunMoveVector, 2.0f);
            if (await NormalDelay(2.0f, dialogCancellation).SuppressCancellationThrow())
                return;
            sunObject.gameObject.SetActive(false);
            
            var berserkerSpeechPos = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
            await NextDialog(speechFrame1[0]);
            
            // 게임 시작
            guideObjects[1].SetActive(true);
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            SaveEpisode();
            Guide1();
        }
    }

    private async UniTask Product3(int idx)
    {
        SetEventStep(idx);
        if (episodeStep.dialogStep == 2)
        {
            AccumulatedStep();
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
        }

        dialogCancellation = new CancellationTokenSource();
        
        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product4);
        
        monsterSpawning = true;
        //var isExplosion = episodeStep.dialogStep > 2;
        dialogMonster1 = GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[0].position, false);
        if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            return;
        dialogMonster2 = GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[2].position, false);
        if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            return;
        monsterSpawning = false;
        
        if (episodeStep.dialogStep == 2)
        {
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            string dialog1 = "뭐야 이 잔디들은!!";
            string dialog2 = "거기 멈춰라! 칼든 놈!";
            string dialog3 = "태양을 뿌셔버리기 전에\n우리랑 놀아줘야겠다 ㅋ";
            string dialog4 = "그래야 게임 환불을 못해!";
            string dialog5 = "니네는 대체 적이냐?\n자본주의의 노예냐?";
            string dialog6 = "둘 다지! 요즘 게임은 재미보다\n환불 회피가 핵심이거든! ㅋㅋ";
            string dialog7 = "우리가 이렇게 한 줄씩 말하는것도\n시간 끌기 전략이다!";
            string dialog8 = "악!!!!!!!!!";

            var berserkerSpeechPos = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog1);
            await NextDialog(speechFrame1[0]);
            
            var monster1Pos = dialogMonster1.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], new Vector2(monster1Pos.x, monster1Pos.y), dialog2);
            await NextDialog(speechFrame1[0]);

            var monster2Pos = dialogMonster2.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], new Vector2(monster2Pos.x, monster2Pos.y), dialog3);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], new Vector2(monster2Pos.x, monster2Pos.y), dialog4);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog5);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], new Vector2(monster1Pos.x, monster1Pos.y), dialog6);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], new Vector2(monster1Pos.x, monster1Pos.y), dialog7);
            await NextDialog(speechFrame1[0]);
            
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], new Vector2(berserkerSpeechPos.x, berserkerSpeechPos.y), dialog8);
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
            PlayerStepUp();
            DialogStepUp();
            SaveEpisode();
            //Guide2();
        }
    }
    
    private async UniTask Product4()
    {
        dialogCancellation = new CancellationTokenSource();
        if (episodeStep.dialogStep == 3)
        {
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var berserkerSpeechPos = curPlayer.SpeechPos.position;
            string dialog1 = "어차피.. 환불은..\n유저 맘이야.. 끄윽";
            string dialog2 = "잡몹 두 마리로\n플레이 타임을 늘릴 수 있을 거 같았냐?";
            string dialog3 = "레벨디자인 꼬라지 봐라!!";
            string dialog4 = "환불은 안 돼!!!";
            
            var monsterSpeech = dialogMonster1.SpeechPos.position;
            var monsterTransform = dialogMonster1.transform;
            if (!dialogMonster1.gameObject.activeSelf)
            {
                monsterSpeech = dialogMonster2.SpeechPos.position;
                monsterTransform = dialogMonster2.transform;
            }
            
            GameManager.Instance.SetCameraTarget(monsterTransform);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame1[0], new Vector2(monsterSpeech.x, monsterSpeech.y - 1.0f), dialog1);
            await NextDialog(speechFrame1[0]);
            
            GameManager.Instance.SetCameraTarget(GameManager.Instance.CurPlayer.transform);
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
            await NextDialog(speechFrame1[0]);
            
            PlaySound(ConstValues.MonsterBigTreeLog);
            CameraShake(0.1f, 0.1f, 0.2f);
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[0].position, dialog4);
        }
        
        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product5);
        
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
            GameManager.Instance.ActiveMonster(monster);
            // if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            //     return;
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
        }
        monsterSpawning = false;
        
        if (episodeStep.dialogStep == 3)
        {
            string dialog5 = "등장으로\n3초 잡아먹기!";
            string dialog6 = "다 뿌셔주마!!!!!";
            
            await NextDialog(speechFrameStrong);
            
            PlaySound(ConstValues.MonsterBigTreeLog);
            CameraShake(0.1f, 0.1f, 0.2f);
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[0].position, dialog5);
            await NextDialog(speechFrameStrong);

            var berserkerSpeechPos = curPlayer.SpeechPos.position;
            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog6);
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
        }
        CustomMoveStepUp();
    }
    // 대화가 없는 연출은 UniTask형태가 아님
    private void Product5()
    {
        foreach (var stageWall in stageWalls)
            stageWall.SetActive(false);
        //GameManager.Instance.MainCamera.MaxXAndY = new Vector2(117.4f, GameManager.Instance.MainCamera.MinXAndY.y);
        SaveEpisode();
    }
    
    private async UniTask Product6(int idx)
    {
        SetEventStep(idx);
        dialogCancellation = new CancellationTokenSource();
        if (episodeStep.dialogStep == 4)
        {
            AccumulatedStep();
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
        }
        
        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product7);
        
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
            GameManager.Instance.ActiveMonster(monster);
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            // if (await YieldDelay(dialogCancellation).SuppressCancellationThrow())
            //     return;
        }
        var coalMonster = monsterList[0];
        var purpleMonster = monsterList[3];

        if (episodeStep.dialogStep == 4)
        {
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            string dialog1 = "하하! 우린 더 강한 적이다!";
            string dialog2 = "너 하나때문에 이 게임의\n레벨디자인이 바꼈다";
            string dialog3 = "자랑스러워 해라";
            string dialog4 = "진짜 미친 게임이네 ㅋㅋ";
            
            var coalPos = coalMonster.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], coalPos, dialog1);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], coalPos, dialog2);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], coalPos, dialog3);
            await NextDialog(speechFrame1[0]);

            var berserkerPos = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerPos, dialog4);
            await NextDialog(speechFrame1[0]);

            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            
            DialogStepUp();
            PlayerStepUp();
            SaveEpisode();
        }
        CustomMoveStepUp();

        if(await WaitUntil(() => GameManager.Instance.MonsterList.Count == 0, dialogCancellation).SuppressCancellationThrow())
            return;
        
        monsterList.Clear();
        var randX = Random.Range(-monsterInterval1, monsterInterval1);
        var randPos1 = new Vector2(monsterPos[7].position.x + randX, monsterPos[7].position.y);
        for (int i = 0; i < 2; i++)
        {
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(ConstValues.MonsterCoal, randPos1));
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(ConstValues.MonsterPurple, randPos1));
        }
        foreach (var monster in monsterList)
        {
            GameManager.Instance.ActiveMonster(monster);
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
        }
        
        monsterList.Clear();
        var randPos2 = new Vector2(monsterPos[8].position.x + randX, monsterPos[8].position.y);
        for (int i = 0; i < 2; i++)
        {
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(ConstValues.MonsterCoal, randPos2));
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(ConstValues.MonsterPurple, randPos2));
        }
        foreach (var monster in monsterList)
        {
            GameManager.Instance.ActiveMonster(monster);
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
        }
        monsterSpawning = false;
    }
    // 대화가 없는 연출은 UniTask형태가 아님
    private void Product7()
    {
        foreach (var stageWall in stageWalls)
            stageWall.SetActive(false);
        //GameManager.Instance.MainCamera.MaxXAndY = new Vector2(138.5f, GameManager.Instance.MainCamera.MinXAndY.y);
        SaveEpisode();
    }

    private async UniTask Product8(int idx)
    {
        SetEventStep(idx);
        if (episodeStep.dialogStep == 5)
        {
            AccumulatedStep();
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
        }

        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product9);
        
        var sunPos = new Vector2(bossPos[2].transform.position.x, bossPos[2].transform.position.y + 3.5f);
        sunObject = GameManager.Instance.SpawnMonster(ConstValues.MonsterSun, sunPos, false, true, SpawnBossMessage);

        if (episodeStep.dialogStep == 5)
        {
            string dialog1 = "니 표정은 진작부터 마음에 안 들었어";
            string dialog2 = "지금 당장 뿌셔버리겠다!";
            string dialog3 = "덤벼보던가!";

            dialogCancellation = new CancellationTokenSource();
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogPose);
            var berserkerSpeech = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeech, dialog1);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeech, dialog2);
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
        }
    }

    private async UniTask Product9()
    {
        dialogCancellation = new CancellationTokenSource();
        
        if (episodeStep.dialogStep == 6)
        {
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
            string dialog7 = "흙으로 돌아가라 태양..";

            var berserkerSpeechPos = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog5); 
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog6); 
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog7); 
            await NextDialog(speechFrame1[0]);
        }

        var cameraPos = GameManager.Instance.MainCamera.transform.position;
        var fadePos = new Vector3(cameraPos.x, cameraPos.y, 0);
        var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, fadePos).GetComponent<FadeSystem>();
        fadeBg.SetParameter(0, 1.0f, 1.5f, false);
        await fadeBg.Fade();
        BgSpriteChange(ConstValues.BgSunHillNight);
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        if (episodeStep.dialogStep == 6)
        {
            string dialog8 = "어둠이 찾아왔다..";
            string dialog9 = "?";
            
            var berserkerSpeechPos = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog8); 
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog9); 
        }
        
        fadeBg.SetParameter(1.0f, 0.0f, 1.5f, true);
        await fadeBg.Fade();
        fadeBg.gameObject.SetActive(false);

        BgmManager.Instance.Play();

        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product10);
        
        var moonPos = new Vector2(bossPos[2].transform.position.x, bossPos[2].transform.position.y + 3.5f);
        moonObject = GameManager.Instance.SpawnMonster(ConstValues.MonsterMoon, moonPos, false, true, SpawnBossMessage);

        if (episodeStep.dialogStep == 6)
        {
            string dialog10 = "뭐야 또?";
            string dialog11 = "으아아악!\n내 친구 태양을 뿌셔버리다니!";
            string dialog12 = "태양의 복수를 하러\n내가 찾아왔다!";
            string dialog13 = "이건 단순한 복수가 아니다!!";
            string dialog14 = "그래!!!!!!!!!\n태양이든 달이든, 오늘 다 때려뿌순다!!!";
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            await NextDialog(speechFrame1[0]);
            
            var berserkerSpeechPos = curPlayer.SpeechPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog10); 
            await NextDialog(speechFrame1[0]);

            var moonSpeech = new Vector2(moonObject.CenterPos.position.x - 2.0f, moonObject.CenterPos.position.y); 
            SpawnSpeechFrame(speechFrame2[0], moonSpeech, dialog11); 
            await NextDialog(speechFrame2[0]);
            
            SpawnSpeechFrame(speechFrame2[0], moonSpeech, dialog12); 
            await NextDialog(speechFrame2[0]);

            SpawnSpeechFrame(speechFrame2[0], moonSpeech, dialog13); 
            await NextDialog(speechFrame2[0]);

            PlaySound(ConstValues.PlayerScream);
            CameraShake(0.4f, 0.4f, 1.0f);
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog14);
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
        }
    }

    private async UniTask Product10()
    {
        if (episodeStep.dialogStep == 7)
        {
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

            var cameraPos = GameManager.Instance.MainCamera.transform.position;
            var fadePos = new Vector3(cameraPos.x, cameraPos.y, 0);
            var fadeBg = GameManager.Instance.SpawnToObjectPool(ConstValues.FadeBg, fadePos).GetComponent<FadeSystem>();
            fadeBg.SetParameter(0, 1.0f, 1.5f, false);
            await fadeBg.Fade();
            BgSpriteChange(ConstValues.BgSunHill);
            foreach (var stageWall in stageWalls)
                stageWall.SetActive(false);
            GameManager.Instance.SetCameraTarget(null);
            
            var berserkerPos = curPlayer.SpeechPos.position;
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
            ProductStageClear(1);
        }
    }

    private void StepCharacterSetting()
    {
        //GameManager.Instance.AddPlayer(ConstValues.Berserker, default); // default
        curPlayer = GameManager.Instance.CurPlayer;
    }

    private void AccumulatedStep()
    {
        
    }
    
    private void Guide1()
    {
        var guideModel = new PopupGuideModel()
        {
            guideMessage = "<color=#F36B6B>'Z'</color>키를 입력하여 회피 할 수 있습니다.\n회피 도중에는 <color=#F36B6B>'무적'</color>입니다.\n<color=#F36B6B>피격, 넘어짐 상태에서도 사용할 수 있습니다.</color>",
            imgName = "Guide1",
        };
        SpawnGuide(guideModel);
    }
}
