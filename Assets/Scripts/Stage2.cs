using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Stage2 : Stage
{
    [SerializeField] private Transform[] npcPos;

    private List<string> monsterWave1 = new List<string>();
    private List<string> monsterWave2 = new List<string>();
    private List<string> monsterWave3 = new List<string>();
    
    private Npc citizen;
    private Npc system;
    private Npc gameSystem;
    private List<Monster> traceMonsters = new List<Monster>();
    private Monster chargeMonster;
    private Monster chargeBoss;
    
    private float monsterInterval1 = 1.0f;
    private float monsterInterval2 = 7.0f;
    private float monsterInterval3 = 5.0f;

    protected override void StageClearButtonAction() 
    {
        Application.Quit();
    }
    
    protected override void Start()
    {
        base.Start();
        //stepTrigger[0].SetAction(() => Product1(0));
        //stepTrigger[1].SetAction(() => Product2(1));
        //stepTrigger[2].SetAction(() => Product4(2));
        //stepTrigger[3].SetAction(Product5);
        //stepTrigger[4].SetAction(() => Product7(3));
        //GameManager.Instance.AddNewSkill();
        
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 2,
        //     playerStep = 3,
        //     customMoveStep = 1,
        //     eventStep = 5,
        // };
        // episodeStep = new EpisodeStep() // 권투맨 전 잡몹전투
        // {
        //     episodeTitle = 1,
        //     dialogStep = 2,
        //     playerStep = 3,
        //     customMoveStep = 2,
        //     eventStep = 3,
        // };
        // GameManager.Instance.ControlStart = true;
        // episodeStep = new EpisodeStep() // 권투맨 전투(대화 포함)
        // {
        //     episodeTitle = 1,
        //     dialogStep = 3,
        //     playerStep = 3,
        //     customMoveStep = 2,
        //     eventStep = 3,
        // };
        // GameManager.Instance.ControlStart = true;
        // episodeStep = new EpisodeStep() // 권투맨 전투(대화생략)
        // {
        //     episodeTitle = 1,
        //     dialogStep = 4,
        //     playerStep = 3,
        //     customMoveStep = 2,
        //     eventStep = 7,
        // };
        // GameManager.Instance.ControlStart = true;
        
        LoadEpisode();
        StepCharacterSetting();

        //GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[episodeStep.playerStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        //GameManager.Instance.SetGroundVector();

        StartSetting();
        SpawnEpisode(episodeTitle);
        SpawnStageClear();
        GameOverCycle();
        ProductEpisode();
        AccumulatedStep();
        StartMonsterList();
    }
    
    protected override void SetEpisodeName()
    {
        episodeName = ConstValues.Episode2;
        episodeTitle = "에피소드2: 선행";
        base.SetEpisodeName();
    }

    private void StartMonsterList()
    {
        monsterWave1.Add(ConstValues.MonsterSpinach);
        monsterWave1.Add(ConstValues.MonsterSpinach);
        monsterWave1.Add(ConstValues.MonsterSpinach);
        monsterWave1.Add(ConstValues.MonsterPurple);
        
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterPurple);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterPurple);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterPurple);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterPurple);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterSpinach);
        monsterWave2.Add(ConstValues.MonsterPurple);
        
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterPurple);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterPurple);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterPurple);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterPurple);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterSpinach);
        monsterWave3.Add(ConstValues.MonsterPurple);
    }

    private void StartSetting()
    {
        citizen = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcCitizen, Vector2.zero).GetComponent<Npc>();
        system = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcSystem, Vector2.zero).GetComponent<Npc>();
        gameSystem = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcGameSystem, Vector2.zero).GetComponent<Npc>();
        
        traceMonsters.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSpinach, Vector2.zero).GetComponent<Monster>());
        traceMonsters.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterPurple, Vector2.zero).GetComponent<Monster>());
        
        chargeMonster = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterCharge, Vector2.zero).GetComponent<Monster>();
        
        citizen.gameObject.SetActive(false);
        system.gameObject.SetActive(false);
        gameSystem.gameObject.SetActive(false);
        foreach (var traceMonster in traceMonsters)
            traceMonster.gameObject.SetActive(false);
        chargeMonster.gameObject.SetActive(false);
    }

    private void CitizenSetting()
    {
        citizen = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcCitizen, Vector2.zero).GetComponent<Npc>();
        
        traceMonsters.Clear();
        traceMonsters.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSpinach, Vector2.zero).GetComponent<Monster>());
        traceMonsters.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterPurple, Vector2.zero).GetComponent<Monster>());
        
        chargeMonster = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterCharge, Vector2.zero).GetComponent<Monster>();
        
        citizen.gameObject.SetActive(false);
        foreach (var traceMonster in traceMonsters)
            traceMonster.gameObject.SetActive(false);
        chargeMonster.gameObject.SetActive(false);
    }
    
    private async UniTask Product1(int idx)
    {
        GameManager.Instance.SetCameraTarget(curPlayer.transform);
        await UniTask.WaitUntil(()=> episodeStep.episodeTitle > 0);
        SetEventStep(idx);
        
        if (episodeStep.dialogStep == 0)
        {
            string dialog1 = "어헝! 정말 좋은 날씨야!";
            string dialog2 = "사람살려!";
            string dialog3 = "저 사람을 구해줘야겠다! 흐헝";
            
            dialogCancellation = new CancellationTokenSource();
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

            var gunnerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog1);
            await NextDialog(speechFrame1[0]);

            var citizenPos = npcPos[0].position;
            var traceMonsterPos1 = new Vector2(npcPos[0].position.x - 1.5f, npcPos[0].position.y);
            var traceMonsterPos2 = new Vector2(npcPos[0].position.x - 2.5f, npcPos[0].position.y);

            citizen.gameObject.SetActive(true);
            traceMonsters[0].gameObject.SetActive(true);
            traceMonsters[1].gameObject.SetActive(true);
            
            citizen.transform.position = citizenPos;
            traceMonsters[0].transform.position = traceMonsterPos1;
            traceMonsters[1].transform.position = traceMonsterPos2;

            var arrivePos = new Vector2(npcPos[1].position.x, npcPos[1].position.y);
            var npcSpeechPos = citizen.FontPos;
            PlaySound(ConstValues.PlayerScream);
            speechFrame1[0].SetPos(npcSpeechPos.position);
            speechFrame1[0].Speech(dialog2);
            speechFrame1[0].Trace(npcSpeechPos);
            citizen.EpisodeMove_X(arrivePos, 7.0f, 1).Forget();
            traceMonsters[0].EpisodeMove_X(arrivePos, 7.0f, 1).Forget();
            await traceMonsters[1].EpisodeMove_X(arrivePos, 7.0f, 1);
            
            // 마무리
            citizen.gameObject.SetActive(false);
            traceMonsters[0].gameObject.SetActive(false);
            traceMonsters[1].gameObject.SetActive(false);
            speechFrame1[0].gameObject.SetActive(false);
            
            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog3);
            // PlaySound(ConstValues.GunnerLaugh);
            // for (int i = 0; i < 2; i++)
            // {
            //     curPlayer.CustomJump(new Vector2(0, 6.0f));
            //     curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            //
            //     if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            //         return;
            // }
            await NextDialog(speechFrame1[0]);
            GameManager.Instance.SetCameraTarget(curPlayer.transform);
            
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            SaveEpisode();
        }
    }
    
    private async void Product2(int idx)
    {
        SetEventStep(idx);
        AccumulatedStep();
        
        dialogCancellation = new CancellationTokenSource();
        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product3);
        
        monsterSpawning = true;
        // 미리 몹 소환하고 잠재워두기
        var monsterList = new List<Monster>();
        foreach (var wave in monsterWave1)
        {
            var randX = Random.Range(-monsterInterval1, monsterInterval1);
            var randPos = new Vector2(monsterPos[0].position.x + randX, monsterPos[0].position.y);
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(wave, randPos));
        }
        foreach (var monster in monsterList)
        {
            GameManager.Instance.ActiveMonster(monster);
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
        }
        monsterSpawning = false;

        if (episodeStep.playerStep == 0)
        {
            PlayerStepUp();
            SaveEpisode();
        }
    }
    
    private void Product3()
    {
        //GameManager.Instance.MainCamera.MaxXAndY = new Vector2(46.5f, GameManager.Instance.MainCamera.MinXAndY.y);
        // 벽 제거
        foreach (var stageWall in stageWalls)
            stageWall.SetActive(false);
        
        CitizenSetting();
        // 시민과 몬스터 스폰시키기
        var citizenPos = npcPos[2].position;
        var traceMonsterPos1 = new Vector2(npcPos[2].position.x + 2.0f, npcPos[2].position.y);
        var traceMonsterPos2 = new Vector2(npcPos[2].position.x + 3.0f, npcPos[2].position.y);
                
        citizen.transform.position = citizenPos;
        citizen.gameObject.SetActive(true);
        citizen.IsDie = true;
                
        traceMonsters[0].transform.position = traceMonsterPos1;
        traceMonsters[0].gameObject.SetActive(true);
        traceMonsters[0].IsDie = true;
        traceMonsters[0].Flip(-1);
        traceMonsters[0].CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
                
        traceMonsters[1].transform.position = traceMonsterPos2;
        traceMonsters[1].gameObject.SetActive(true);
        traceMonsters[1].IsDie = true;
        traceMonsters[1].Flip(-1);
        traceMonsters[1].CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
    }
    
    private async UniTask Product4(int idx)
    {
        SetEventStep(idx);

        if (episodeStep.dialogStep == 1)
        {
            stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallRight, stageWallPos[1]));
            //SetTimeScale(100.0f);
            string dialog1 = "나좀 살려줘!";
            string dialog2 = "어헝! 내쪽으로 어서 달려와!";
            string dialog3 = "어딜 도망치려고!";
            string dialog4 = "??";
            string dialog5 = "총을 잘못 쏴서 셋 다 죽어 버렸네 크헝!";
            string dialog6 = "??넌 뭐냐?";
            string dialog7 = "광고를 보면 쓰러진 시민을 살릴 수 있어!";
            string dialog8 = "어헝! 난데없이 나타나서 광고를 보라고?";
            string dialog9 = "안 보면 못 살려 ㅋ";
            string dialog10 = "속는 셈 치고 봐야겠네 ㅎ";
            string dialog11 = "광고 시청 후";
            string dialog12 = "오잉? 나 살아났네?"; 
            string dialog13 = "너가 날 살렸어!";
            string dialog14 = "어헝! 내가 시민을 구했다!";
            string dialog15 = "야!!!";
            string dialog16 = "내 부하들 누가 이렇게 만들었냐?";
            string dialog17 = "너잖아!";
            string dialog18 = "필요없어\n둘 다 부숴주마";
            string dialog19 = "그 동안 고마웠다";
            string dialog20 = "ㅌㅌ";
            string dialog21 = "으으허헝허헝허헝허헝헝헝헝허헝허헝허";
            string dialog22 = "난 슈퍼아머다";
            string dialog23 = "어이 잡초맨!\n죽었냐??";
            string dialog24 = "짜잔!";
            string dialog25 = "광고를 보고 부활시키겠...";
            string dialog26 = "집어 치워!";
            string dialog27 = "무슨 조무래기 일반 몬스터\n살리려고 광고를 봐!";
            string dialog28 = "우리 죽어요...";
            string dialog29 = "게임 시스템 나와!";
            string dialog30 = "왜요?";
            string dialog31 = "리스폰 시스템 작동!";
            string dialog32 = "형님!!\n저희 되살아났습니다!";
            string dialog33 = "광고를 왜 봐?\n게임시스템 부수면 되는걸";
            string dialog34 = "내 주먹이 가성비 짱이다!";
            string dialog35 = "근데 저희가 너무 많아요!";
            string dialog36 = "모든 스테이지로 퍼져라!\n플레이 타임을 늘려야 한다!";
            string dialog37 = "한편 총잡이는...";
            string dialog38 = "어헝!\n땅에 박혀버렸어";
            string dialog39 = "뭐여 이건?";
            string dialog40 = "어헝! 사람이다!";
            string dialog41 = "우리가 만난 건 운명이야!\n그니까 나좀 살려줘!";
            string dialog42 = "살려달라고?\n나랑은 상관없는 일이다";
            string dialog43 = "근데 내가 주인공이라서\n살려줘야겠다";
            string dialog44 = "기다려봐";
            string dialog45 = "으허허헝허헝\n고마워~!";
            string dialog46 = "땅에는 왜 박혀있었던 거야?";
            string dialog47 = "잠시 후";
            string dialog48 = "이 세상은 정말 혼란스럽다";
            string dialog49 = "나랑 상관 없는 일이다";
            string dialog50 = "근데 내가 주인공이라서\n저놈들을 단죄하러 가야겠다";
            string dialog51 = "어헝!\n나도 합류할게!";

            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            //GameManager.Instance.MainCamera.MinXAndY = new Vector2(46.5f, GameManager.Instance.MainCamera.MinXAndY.y);
            
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
            
            dialogCancellation = new CancellationTokenSource();
            var citizenSpeechPosition = citizen.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], citizenSpeechPosition, dialog1);
            await NextDialog(speechFrame1[0]);
            
            // PlaySound(ConstValues.GunnerLaugh);
            // for (int i = 0; i < 2; i++)
            // {
            //     curPlayer.CustomJump(new Vector2(0, 6.0f));
            //     curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            //
            //     if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            //         return;
            // }
            var gunnerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);

            var monsterSpeechPos = traceMonsters[0].FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], monsterSpeechPos, dialog3);
            await NextDialog(speechFrame1[0]);

            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogShot);
            curPlayer.SpawnObject(ConstValues.GunnerFlash, curPlayer.CenterPos.position);

            var length = 0.6f;
            
            citizen.IsDie = false;
            traceMonsters[0].IsDie = false;
            traceMonsters[1].IsDie = false;
            traceMonsters[0].EpisodeMove_X(new Vector2(curPlayer.transform.position.x + length, curPlayer.transform.position.y), 6, -1).Forget();
            traceMonsters[1].EpisodeMove_X(new Vector2(curPlayer.transform.position.x + length, curPlayer.transform.position.y), 6, -1).Forget();
            citizen.EpisodeMove_X(new Vector2(curPlayer.transform.position.x + length, curPlayer.transform.position.y), 6, -1).Forget();

            await UniTask.WaitUntil(() => citizen.transform.position.x < curPlayer.transform.position.x + length + 2.0f); 
            curPlayer.GetComponent<Player_Gunner>().EventKnockBackShot().Forget();
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            BgmManager.Instance.Stop();
            
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            traceMonsters[0].IsDie = true;
            traceMonsters[1].IsDie = true;
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            curPlayer.CustomAnimTrigger(ENormalState.Normal, ConstValues.Idle);
            
            citizenSpeechPosition = citizen.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], new Vector2(citizenSpeechPosition.x, citizenSpeechPosition.y - 1.0f), dialog4);
            await NextDialog(speechFrame1[0]);

            gunnerSpeechPos = curPlayer.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog5);
            await NextDialog(speechFrame1[0]);

            system.gameObject.SetActive(true);
            system.transform.position = new Vector2(citizen.transform.position.x, citizen.transform.position.y + 2.0f);
            system.SpawnObject(ConstValues.BangEffect, system.CenterPos.position);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog6);
            await NextDialog(speechFrame1[0]);
            
            var systemSpeechPos = system.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], systemSpeechPos, dialog7);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog8);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], systemSpeechPos, dialog9);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog10);
            await NextDialog(speechFrame1[0]);

            // 검은색 페이드 등장
            var fadeUI = GameManager.Instance.SpawnToUIPool(ConstValues.FadeUI, Vector3.zero);
            var titleSpeechPos = Vector3.zero;
            SpawnSpeechFrame(speechFrameTitle, titleSpeechPos, dialog11); 
            await NextDialog(speechFrameTitle);
            fadeUI.SetActive(false);
            
            BgmManager.Instance.Play();
            system.gameObject.SetActive(false);
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Arrive);
            
            SpawnSpeechFrame(speechFrame1[0], citizenSpeechPosition, dialog12);
            await NextDialog(speechFrame1[0]);

            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Thumbs);
            SpawnSpeechFrame(speechFrame1[0], citizenSpeechPosition, dialog13);
            await NextDialog(speechFrame1[0]);

            PlaySound(ConstValues.GunnerLaugh);
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog14);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            
                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);
            
            // 야!!!!!!!!!
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Arrive);
            citizen.Flip(1);
            BgmManager.Instance.PlayBgm(ConstValues.BGMEpisode2Battle);
            CameraShake(0.5f, 0.5f, 0.5f); 
            PlaySound(ConstValues.FighterStrongPunch);
            
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[0].position, dialog15);
            await NextDialog(speechFrameStrong);

            chargeMonster.gameObject.SetActive(true);
            chargeMonster.transform.position = bossPos[0].position;
            chargeMonster.Flip(-1);
            var stopPos = new Vector2(citizen.transform.position.x + 2.0f, citizen.transform.position.y);
            await chargeMonster.EpisodeMove_X(stopPos, chargeMonster.BasicStat.moveSpeed, -1);
            
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            var chargeSpeechPosition = chargeMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog16);
            await NextDialog(speechFrame1[0]);

            citizen.Flip(-1);
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Point);
            
            SpawnSpeechFrame(speechFrame1[0], citizenSpeechPosition, dialog17);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog18);
            await NextDialog(speechFrame1[0]);

            // 시민이 있던위치로 이동
            stopPos = new Vector2(citizen.transform.position.x, citizen.transform.position.y);
            chargeMonster.EpisodeMove_X(stopPos, chargeMonster.BasicStat.moveSpeed, -1).Forget();
            
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Thumbs);
            SpawnSpeechFrame(speechFrame1[0], citizenSpeechPosition, dialog19);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].SetPos(citizenSpeechPosition);
            speechFrame1[0].Speech(dialog20);
            speechFrame1[0].Trace(citizen.FontPos);
            
            var runPos = new Vector2(GameManager.Instance.CurPlayer.transform.position.x - 12.0f, citizen.transform.position.y);
            await citizen.EpisodeMove_X(runPos, 10, -1);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            citizen.gameObject.SetActive(false);
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog21);
            await NextDialog(speechFrame1[0]);

            curPlayer.GetComponent<Player_Gunner>().EventKnockBackShot().Forget();
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            curPlayer.BasicStat.bodyType = EBodyType.Normal;
            chargeSpeechPosition = chargeMonster.FontPos.position;
            
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog22);
            await NextDialog(speechFrame1[0]);

            await chargeMonster.GetComponent<Monster_Charge>().EventCharge(3.0f, 20, 20);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            
            // 어이 잡초맨! 죽었냐??
            chargeSpeechPosition = chargeMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog23);
            speechFrame1[0].Trace(chargeMonster.FontPos);
            
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            chargeMonster.Flip(1);
            var movePos = new Vector2(traceMonsters[0].transform.position.x - 2.0f, traceMonsters[0].transform.position.y);
            await chargeMonster.EpisodeMove_X(movePos, chargeMonster.BasicStat.moveSpeed, 1);
            await NextDialog(speechFrame1[0]);
            
            system.gameObject.SetActive(true);
            system.transform.position = new Vector2(traceMonsters[0].transform.position.x, traceMonsters[0].transform.position.y + 2.0f);
            system.SpawnObject(ConstValues.BangEffect, system.CenterPos.position);
            // 짜잔!
            systemSpeechPos = system.FontPos.position;
            
            SpawnSpeechFrame(speechFrame1[0], systemSpeechPos, dialog24);
            await NextDialog(speechFrame1[0]);

            // 광고를 보고 부활시키겠...
            SpawnSpeechFrame(speechFrame1[0], systemSpeechPos, dialog25);
            await NextDialog(speechFrame1[0]);
            
            // 집어치워!
            chargeSpeechPosition = chargeMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog26);
            speechFrame1[0].Trace(chargeMonster.FontPos);
            await chargeMonster.GetComponent<Monster_Charge>().EventCharge(0.1f, 20, 20);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            system.gameObject.SetActive(false);
            await NextDialog(speechFrame1[0]);
            
            // 무슨 조무래기 일반몬스터
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog27);
            await NextDialog(speechFrame1[0]);

            var traceMonsterPos1 = new Vector2(traceMonsters[0].FontPos.position.x, traceMonsters[0].FontPos.position.y - 1.0f);
            SpawnSpeechFrame(speechFrame1[0], traceMonsterPos1, dialog28);
            traceMonsters[0].BlinkDelete();
            traceMonsters[1].BlinkDelete();
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            await NextDialog(speechFrame1[0]);
            
            // 게임시스템 나와!
            chargeSpeechPosition = chargeMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog29);
            await NextDialog(speechFrame1[0]);
            speechFrame1[0].Trace(chargeMonster.FontPos);

            gameSystem.gameObject.SetActive(true);
            gameSystem.transform.position = new Vector2(chargeMonster.transform.position.x + 5.0f, chargeMonster.FontPos.position.y);
            gameSystem.SpawnObject(ConstValues.BangEffect, gameSystem.CenterPos.position);
            GameManager.Instance.SetCameraTarget(gameSystem.transform);
            //GameManager.Instance.MainCamera.MaxXAndY = new Vector2(59.0f, GameManager.Instance.MainCamera.MinXAndY.y);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var gameSystemPosition = gameSystem.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], gameSystemPosition, dialog30);
            await NextDialog(speechFrame1[0]);

            await chargeMonster.GetComponent<Monster_Charge>().EventCharge(1.0f, 20, 1);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);

            for (int i = 0; i < 25; i++)
            {
                var textFont = GameManager.Instance.SpawnToUIObjectPool(ConstValues.TextFont, gameSystem.transform).GetComponent<TextFont>();
                textFont.ColorSetting(EFontType.MyDamage);
                textFont.DisplayFont(50, dialog31);
                SoundManager.Instance.PlaySound(ConstValues.MonsterMouseCursorWarning);
                if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            chargeMonster.Flip(-1);
            GameManager.Instance.SetCameraTarget(chargeMonster.transform);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            // 저희 되살아났습니다!
            float zPos = 0.0f;
            for (int i = 0; i < 35; i++)
            {
                var randX = Random.Range(-monsterInterval2, monsterInterval2);
                var randPos = new Vector3(monsterPos[1].position.x + randX, monsterPos[0].position.y, zPos);
                zPos += 0.1f;

                var rand = Random.Range(0, 2);
                var randMonster = ConstValues.MonsterSpinach;
                if (rand == 1)
                    randMonster = ConstValues.MonsterPurple;
                
                var monster = GameManager.Instance.SpawnMonster(randMonster, randPos);
                monster.LookAt(chargeMonster.transform.position.x);
                if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            PlaySound(ConstValues.MonsterBigTreeLog);
            CameraShake(0.1f, 0.1f, 0.2f);
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[1].position, dialog32);
            await NextDialog(speechFrameStrong);

            // 광고를 왜 봐? 게임시스템 부수면 되는걸
            chargeSpeechPosition = chargeMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog33);
            await NextDialog(speechFrame1[0]);

            // 내 주먹이 가성비 짱이다!
            PlaySound(ConstValues.PlayerFlash);
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.PunchPose);
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog34);
            await NextDialog(speechFrame1[0]);
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            
            // 근데 제가 너무 많아요!
            PlaySound(ConstValues.MonsterBigTreeLog);
            CameraShake(0.1f, 0.1f, 0.2f);
            
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[1].position, dialog35);
            await NextDialog(speechFrameStrong);

            // 빨리 모든 스테이지로 퍼져
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPosition, dialog36);
            await NextDialog(speechFrame1[0]);

            // 몹들 정리
            GameManager.Instance.ClearMonsterList();
            chargeMonster.gameObject.SetActive(false);
            
            // 한편 총잡이는
            fadeUI.SetActive(true);
            SpawnSpeechFrame(speechFrameTitle, titleSpeechPos, dialog37);
            
            // 벽 제거
            foreach (var stageWall in stageWalls)
                stageWall.SetActive(false);
            
            // 벽 설치
            stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallLeft, stageWallPos[2]));

            // 카메라 위치 변경 및 캐릭터 세팅
            //GameManager.Instance.MainCamera.MinXAndY = new Vector2(82f, GameManager.Instance.MainCamera.MinXAndY.y);
            //GameManager.Instance.MainCamera.MaxXAndY = new Vector2(82f, GameManager.Instance.MainCamera.MinXAndY.y);
            GameManager.Instance.SetCameraTarget(curPlayer.transform);
            // 이곳에서 광전사와 거너의 주도권이 바낌
            //GameManager.Instance.SetCharacterOrder(ConstValues.Berserker, ConstValues.Gunner);
            curPlayer = GameManager.Instance.CurPlayer;
            
            var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
            gunner.gameObject.SetActive(true);
            gunner.transform.position = playerPos[2].transform.position;
            gunner.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogGround);
            
            var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
            var startPos = new Vector2(gunner.transform.position.x - 7.0f, gunner.transform.position.y);
            var endPos = new Vector2(gunner.transform.position.x - 2.5f, gunner.transform.position.y);
            berserker.transform.position = startPos;
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            await NextDialog(speechFrameTitle);
            fadeUI.SetActive(false);

            // 땅에 박혀버렸어
            speechFrame1[0].gameObject.SetActive(true);
            gunnerSpeechPos = new Vector2(gunner.FontPos.position.x, gunner.FontPos.position.y - 0.5f);
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog38);
            await NextDialog(speechFrame1[0]);

            // 뭐여 이건?
            berserker.gameObject.SetActive(true);
            await berserker.EpisodeMove(endPos, berserker.BasicStat.moveSpeed, 1);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            var berserkerSpeechPos = berserker.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog39);
            await NextDialog(speechFrame1[0]);

            // 우리가 만난 건 운명이야!
            gunner.Flip(-1);
            gunner.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogGroundLaugh);
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog40);
            await NextDialog(speechFrame1[0]);

            // 그러니까 나 좀 살려줘..
            gunner.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogGround);
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog41);
            await NextDialog(speechFrame1[0]);

            // 살려달라고? 나랑은 상관 없는 일이다
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog42);
            await NextDialog(speechFrame1[0]);
            
            // 근데 내가 주인공이라서 살려줘야겠다
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog43);
            await NextDialog(speechFrame1[0]);
            
            // 기다려봐
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog44);
            await NextDialog(speechFrame1[0]);
            
            await berserker.EventCrash();
            PlaySound(ConstValues.PlayerScream);
            gunner.SpawnObject(ConstValues.BerserkerCrashHitEffect, gunner.transform.position);
            gunner.Airborne(0f, 12f);
            if (await NormalDelay(3.0f, dialogCancellation).SuppressCancellationThrow())
                return;
            berserker.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
            
            // 어헝 고마워
            gunnerSpeechPos = gunner.FontPos.position;
            PlaySound(ConstValues.GunnerLaugh);
            SpawnSpeechFrame(speechFrame1[0], new Vector2(gunnerSpeechPos.x, gunnerSpeechPos.y + 0.5f), dialog45);
            for (int i = 0; i < 2; i++)
            {
                gunner.CustomJump(new Vector2(0, 6.0f));
                gunner.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            
                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);
            
            // 땅에는 왜 박혀있던거야?
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog46);
            await NextDialog(speechFrame1[0]);

            // 잠시 후
            fadeUI.SetActive(true);
            SpawnSpeechFrame(speechFrameTitle, titleSpeechPos, dialog47); 
            await NextDialog(speechFrameTitle);
            fadeUI.SetActive(false);
            
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            // 이 세상은 정말 혼란스럽다
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog48);
            await NextDialog(speechFrame1[0]);

            // 나와는 상관 없는 일이다
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog49);
            await NextDialog(speechFrame1[0]);
            
            // 근데 내가 주인공 뭐시기
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog50);
            await NextDialog(speechFrame1[0]);
            
            // 어헝 나도 합류할게!
            // PlaySound(ConstValues.GunnerLaugh);
            // for (int i = 0; i < 2; i++)
            // {
            //     gunner.CustomJump(new Vector2(0, 6.0f));
            //     gunner.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            //
            //     if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            //         return;
            // }
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog51);
            await NextDialog(speechFrame1[0]);

            // 가이드 띄우기
            Guide3();
            
            gunner.SpawnObject(ConstValues.BangEffect, gunner.CenterPos.position);
            gunner.gameObject.SetActive(false);
            
            //GameManager.Instance.MainCamera.MinXAndY = new Vector2(77.5f, GameManager.Instance.MainCamera.MinXAndY.y);
            //GameManager.Instance.MainCamera.MaxXAndY = new Vector2(89.5f, GameManager.Instance.MainCamera.MinXAndY.y);

            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            PlayerStepUp();
            CustomMoveStepUp();
            SaveEpisode();
        }
    }
    
    private async void Product5()
    {
        stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallRight, stageWallPos[3]));
        
        dialogCancellation = new CancellationTokenSource();
        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product6);
        monsterSpawning = true;

        // 미리 몹 소환하고 잠재워두기
        var monsterList = new List<Monster>();
        foreach (var wave in monsterWave2)
        {
            var randX = Random.Range(-monsterInterval2, monsterInterval2);
            var randPos = new Vector2(monsterPos[2].position.x + randX, monsterPos[2].position.y);
            monsterList.Add(GameManager.Instance.ActiveAndHideMonster(wave, randPos));
        }
        foreach (var monster in monsterList)
        {
            GameManager.Instance.ActiveMonster(monster);
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
        }
        monsterSpawning = false;
    }
    
    // 벽 사라짐
    private void Product6()
    {
        // 벽 제거
        foreach (var stageWall in stageWalls)
            stageWall.SetActive(false);
        //GameManager.Instance.MainCamera.MaxXAndY = new Vector2(120f, GameManager.Instance.MainCamera.MinXAndY.y);
        PlayerStepUp();
    }

    private async void Product7(int idx)
    {
        SetEventStep(idx);
        SaveEpisode();
        AccumulatedStep();

        dialogCancellation = new CancellationTokenSource();
        waitCancellation = new CancellationTokenSource();
        
        MonsterClearAction(Product8);
        if (episodeStep.dialogStep == 2)
        {
            monsterSpawning = true;
            var monsterList = new List<Monster>();
            foreach (var wave in monsterWave3)
            {
                var randX = Random.Range(-monsterInterval3, monsterInterval3);
                int randY = Random.Range(0, 3);
                var randPos = new Vector2(monsterPos[3 + randY].position.x + randX, monsterPos[3 + randY].position.y);
                monsterList.Add(GameManager.Instance.ActiveAndHideMonster(wave, randPos));
            }
            foreach (var monster in monsterList)
            {
                GameManager.Instance.ActiveMonster(monster);
                if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            monsterSpawning = false;
        }
    }
    
    private async void Product8()
    {
        if (episodeStep.dialogStep == 2)
        {
            string dialog1 = "야!!!";
            string dialog2 = "???";
            string dialog3 = "넌 뭐냐?";
            string dialog4 = "그게 중요하냐?";
            string dialog5 = "니 얼굴 보니까 어차피\n내가 누구든 나를 때릴 거 같은데?";
            string dialog6 = "잘 아네?";
            string dialog7 = "저 녀석 슈퍼아머야! 조심해!";
            string dialog8 = "슈퍼아머를 <color=#F36B6B>박살</color>내버릴 기술이 필요하겠군";
            
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

            await curPlayer.WaitIdle();
            

            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position,
                curPlayer.BasicStat.moveSpeed, 1);
            
            // 항상 광전사가 플레이어의 위치에 있어야함
            var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
            var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();

            var berserkerPos = curPlayer.transform.position;
            var gunnerPos = new Vector2(curPlayer.transform.position.x - 1.5f, curPlayer.transform.position.y);

            berserker.gameObject.SetActive(true);
            berserker.transform.position = berserkerPos;
            berserker.Flip(1);

            gunner.gameObject.SetActive(true);
            gunner.transform.position = berserkerPos;
            await gunner.EpisodeMove(gunnerPos, gunner.BasicStat.moveSpeed, 1);
            
            GameManager.Instance.SetCameraTarget(gunner.transform);

            dialogCancellation = new CancellationTokenSource();
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            CameraShake(0.5f,0.5f, 0.5f); 
            PlaySound(ConstValues.FighterStrongPunch);
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[2].position, dialog1);
            await NextDialog(speechFrameStrong);

            chargeMonster.gameObject.SetActive(true);
            chargeMonster.transform.position = bossPos[1].position;
            var movePos = new Vector2(bossPos[2].position.x + 2.0f, bossPos[2].position.y);
            await chargeMonster.EpisodeMove_X(movePos, 4, -1);
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);

            var chargeSpeechPos = chargeMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPos, dialog3);
            await NextDialog(speechFrame1[0]);

            var berserkerSpeechPos = berserker.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog4);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog5);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], chargeSpeechPos, dialog6);
            await NextDialog(speechFrame1[0]);
            
            var gunnerSpeechPos = gunner.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog7);
            PlaySound(ConstValues.GunnerLaugh);
            for (int i = 0; i < 2; i++)
            {
                gunner.CustomJump(new Vector2(0, 6.0f));
                gunner.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            
                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);

            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog8);
            await NextDialog(speechFrame1[0]);
            
            if (curPlayer == berserker)
            {
                gunner.SpawnObject(ConstValues.BangEffect, gunner.CenterPos.position);
                gunner.gameObject.SetActive(false);
            }
            else if (curPlayer == gunner)
            {
                berserker.SpawnObject(ConstValues.BangEffect, berserker.CenterPos.position);
                berserker.gameObject.SetActive(false);
            }
            GameManager.Instance.SetCameraTarget(curPlayer.transform);
            
            // 게임 시작
            GameManager.Instance.SetMonster(chargeMonster, false, false);
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            SaveEpisode();
            chargeMonster.IdleOrMove();
        }
        else if (episodeStep.dialogStep >= 3)
        {
            chargeMonster = GameManager.Instance.SpawnMonster(ConstValues.MonsterCharge, bossPos[2].transform.position, false);
        }
        
        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product9);
    }

    private async void Product9()
    {
        dialogCancellation = new CancellationTokenSource();
        
        waitCancellation = new CancellationTokenSource();
        MonsterClearAction(Product10);
        
        monsterSpawning = true;
        if (episodeStep.dialogStep == 3)
        {
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            GameManager.Instance.SetCameraTarget(chargeMonster.transform);
        }
        
        await UniTask.WaitUntil(() => chargeMonster.NormalState == ENormalState.Down);
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.LandingPose);
        chargeMonster.SpawnObject($"{chargeMonster.BasicStat.id}_{ConstValues.Appear}", chargeMonster.transform.position);
        
        if (episodeStep.dialogStep == 3)
        {
            string dialog1 = "이익!";
            var chargeSpeechPos = chargeMonster.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], new Vector2(chargeSpeechPos.x, chargeSpeechPos.y - 0.6f), dialog1);
            await NextDialog(speechFrame1[0]);
        }
        
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.JumpPose);
        await chargeMonster.GetComponent<Monster_Charge>().EventJump();

        if (episodeStep.dialogStep == 3)
        {
            string dialog2 = "너네도!\n짜증나는 플랫폼도!\n다 부숴버릴 테다!!";
            
            GameManager.Instance.SetCameraTarget(curPlayer.transform);
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
            
            // 항상 광전사가 플레이어의 위치에 있어야함
            var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
            var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();

            var berserkerPos = curPlayer.transform.position;
            var gunnerPos = new Vector2(curPlayer.transform.position.x - 1.5f, curPlayer.transform.position.y);

            berserker.gameObject.SetActive(true);
            berserker.transform.position = berserkerPos;
            berserker.Flip(1);

            gunner.gameObject.SetActive(true);
            gunner.transform.position = berserkerPos;
            await gunner.EpisodeMove(gunnerPos, gunner.BasicStat.moveSpeed, 1);
            
            GameManager.Instance.SetCameraTarget(berserker.transform);
            
            CameraShake(0.5f, 0.5f, 0.5f); 
            PlaySound(ConstValues.FighterStrongPunch);
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[3].position, dialog2);
            await NextDialog(speechFrameStrong);
        }
        
        if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
            return;
        
        Camera cam = GameManager.Instance.MainCamera.MyCamera;
        float startY = cam.ViewportToWorldPoint(new Vector3(1, 1, 0)).y;
        var bossPos = new Vector2(monsterPos[3].transform.position.x, startY + 5);
        chargeBoss = GameManager.Instance.SpawnMonster(ConstValues.MonsterBigCharge, bossPos, false, true, SpawnBossMessage);
        monsterSpawning = false;
        
        if (episodeStep.dialogStep == 3)
        {
            if (await NormalDelay(3.0f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            string dialog3 = "이야!!!!!!!";
            string dialog4 = "와! 거대해요!";
            string dialog5 = "커진건 나랑 상관 없는 일이다";
            string dialog6 = "근데, 저런 놈도 <color=#F36B6B>무력화</color>시킬 방법이 있을거다";
            string dialog7 = "가자!";

            var bossSpeechPos = chargeBoss.FontPos.position;
            
            CameraShake(0.5f, 0.5f, 0.5f); 
            PlaySound(ConstValues.FighterStrongPunch);
            SpawnSpeechFrame(speechFrameStrong, strongSpeechPos[3].position, dialog3);
            await NextDialog(speechFrameStrong);

            // 항상 광전사가 플레이어의 위치에 있어야함
            var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
            var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();

            var gunnerSpeechPos = gunner.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog4);
            PlaySound(ConstValues.GunnerLaugh);
            for (int i = 0; i < 2; i++)
            {
                gunner.CustomJump(new Vector2(0, 6.0f));
                gunner.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            
                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            await NextDialog(speechFrame1[0]);

            var berserkerSpeechPos = berserker.FontPos.position;
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog5);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog6);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog7);
            await NextDialog(speechFrame1[0]);
            
            if (curPlayer == berserker)
            {
                gunner.SpawnObject(ConstValues.BangEffect, gunner.CenterPos.position);
                gunner.gameObject.SetActive(false);
            }
            else if (curPlayer == gunner)
            {
                berserker.SpawnObject(ConstValues.BangEffect, berserker.CenterPos.position);
                berserker.gameObject.SetActive(false);
            }
            GameManager.Instance.SetCameraTarget(curPlayer.transform);
            
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            SaveEpisode();
        }
    }
    
    private async void Product10()
    {
        if (episodeStep.dialogStep == 4)
        {
            string dialog1 = "우리가 이겼어!";
            string dialog2 = "근데 많은 적들이 스테이지 곳곳으로 퍼져버렸어!";
            string dialog3 = "나한테 좋은 방법이 있다";
            string dialog4 = "뭔데?";
            string dialog5 = "날 따라와\n같이 갈 데가 있다";
            
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            GameManager.Instance.SetCameraTarget(chargeBoss.transform);
            
            dialogCancellation = new CancellationTokenSource();
            if (await NormalDelay(3.0f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            chargeBoss.GetComponent<Monster_BigCharge>().DieAirborne(base.bossPos[2].position);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            GameManager.Instance.SetCameraTarget(curPlayer.transform);

            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
            
            // 항상 광전사가 플레이어의 위치에 있어야함
            var berserker = GameManager.Instance.GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
            var gunner = GameManager.Instance.GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();

            var berserkerPos = curPlayer.transform.position;
            var gunnerPos = new Vector2(curPlayer.transform.position.x - 1.5f, curPlayer.transform.position.y);

            berserker.gameObject.SetActive(true);
            berserker.transform.position = berserkerPos;
            berserker.Flip(1);

            gunner.gameObject.SetActive(true);
            gunner.transform.position = berserkerPos;
            await gunner.EpisodeMove(gunnerPos, gunner.BasicStat.moveSpeed, 1);
            GameManager.Instance.SetCameraTarget(berserker.transform);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            var berserkerSpeechPos = berserker.FontPos.position;
            var gunnerSpeechPos = gunner.FontPos.position;
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog1);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog2);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog3);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], gunnerSpeechPos, dialog4);
            await NextDialog(speechFrame1[0]);
            
            SpawnSpeechFrame(speechFrame1[0], berserkerSpeechPos, dialog5);
            await NextDialog(speechFrame1[0]);

            ProductStageClear(2);
        }
    }
    
    private void Test()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // if(GameManager.Instance.PlayerList == ConstValues.Berserker)
            //     GameManager.Instance.SetCharacterOrder(ConstValues.Berserker, ConstValues.Gunner);
            // else if(GameManager.Instance.PlayerList == ConstValues.Gunner)
            //     GameManager.Instance.SetCharacterOrder(ConstValues.Gunner, ConstValues.Berserker);
            
            //GameManager.Instance.CharacterChange(false);
        }
    }

    private void StepCharacterSetting()
    {
        switch (episodeStep.eventStep)
        {
            case 0:
                //GameManager.Instance.AddPlayer(ConstValues.Gunner, default);
                break;
        }
        
        //if(episodeStep.eventStep >= 2)
            //GameManager.Instance.AddPlayer(ConstValues.Berserker, ConstValues.Gunner);
        
        curPlayer = GameManager.Instance.CurPlayer;
    }
    
    private async void AccumulatedStep()
    {
        
    }
    
    private void Guide3()
    {
        var guideModel = new PopupGuideModel()
        {
            guideMessage = "<color=#F36B6B>'Shift'</color>키를 입력하여 캐릭터를 교체 할 수 있습니다.\n<color=#F36B6B>캐릭터의 체력은 서로 공유됩니다.</color>",
            imgName = "Guide3",
        };
        SpawnGuide(guideModel);
    }
}
