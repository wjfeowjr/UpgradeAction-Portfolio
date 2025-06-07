using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Stage2 : Stage
{
    [SerializeField] private Transform[] npcPos;
    [SerializeField] private Player anotherPlayer;
    [SerializeField] private List<string> monsterWave1 = new List<string>();
    private Npc citizen;
    private Npc system;
    private List<Monster> traceMonsters = new List<Monster>();
    private Monster chargeMonster;

    private float monsterInterval = 1.0f;

    protected override void SetEpisodeName()
    {
        base.SetEpisodeName();
        episodeName = ConstValues.Episode2;
        episodeTitle = "에피소드2: 선행";
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
                Product2();
                break;
            case 2:
                Product3();
                break;
            case 3:
                await Product4();
                break;
        }
    }
    protected override void StageClearButtonAction() 
    {
        Application.Quit();
    }
    
    private void Start()
    {
        StepCharacterSetting();

        episodeStep = new EpisodeStep()
        {
            episodeTitle = 1,
            dialogStep = 1,
            playerStep = 1,
            customMoveStep = 0,
            eventStep = 2,
        };
        GameManager.Instance.ControlStart = true;
        
        LoadEpisode();

        dialogSwitch = true;
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[episodeStep.playerStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.SetGroundVector();

        StartSetting();
        
        SpawnEpisode(episodeTitle);
        SpawnStageClear();
        GameOverCycle();
        ProductEpisode();
        AccumulatedStep();
        StartMonsterList();
    }
    
    private void Update()
    {
        DialogCycle();
        Test();
    }

    private void StartMonsterList()
    {
        monsterWave1.Add(ConstValues.MonsterSpinach);
        monsterWave1.Add(ConstValues.MonsterSpinach);
        monsterWave1.Add(ConstValues.MonsterSpinach);
        monsterWave1.Add(ConstValues.MonsterPurple);
    }

    private void StartSetting()
    {
        citizen = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcCitizen, Vector2.zero).GetComponent<Npc>();
        system = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcSystem, Vector2.zero).GetComponent<Npc>();
        traceMonsters.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSpinach, Vector2.zero).GetComponent<Monster>());
        traceMonsters.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterPurple, Vector2.zero).GetComponent<Monster>());
        chargeMonster = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterCharge, Vector2.zero).GetComponent<Monster>();
        
        citizen.gameObject.SetActive(false);
        system.gameObject.SetActive(false);
        foreach (var traceMonster in traceMonsters)
            traceMonster.gameObject.SetActive(false);
        chargeMonster.gameObject.SetActive(false);
    }
    
    private async UniTask Product1()
    {
        if (episodeStep.dialogStep == 0)
        {
            dialogSwitch = false;
            string dialog1 = "어헝! 정말 좋은 날씨야!";
            string dialog2 = "사람살려!";
            string dialog3 = "저 사람을 구해줘야겠다! 흐헝";
            
            dialogCancellation = new CancellationTokenSource();
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var gunnerSpeechPos = curPlayer.FontPos.position;
            speechFrame1[0].SetPos(gunnerSpeechPos);
            speechFrame1[0].Speech(dialog1);
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            CameraShake(0.1f, 1.5f); 
            
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
            
            PlaySound(ConstValues.GunnerLaugh);
            speechFrame1[0].SetPos(new Vector2(gunnerSpeechPos.x, gunnerSpeechPos.y + 0.5f));
            speechFrame1[0].Speech(dialog3);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            
                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            
            speechFrame1[0].gameObject.SetActive(false);
            
            // 게임 시작
            GameManager.Instance.ControlStart = true;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
            DialogStepUp();
            SaveEpisode();
            dialogSwitch = true;
            
            // var citizenPos = npcPos[0].position;
            // var citizen1 = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcCitizen, citizenPos).GetComponent<Npc>();
            // citizen1.Airborne(-10,10);
            //GameManager.Instance.SpawnMonster(ConstValues.MonsterCharge, monsterPos[0].position);
        }
        MyEventStepUp();
    }
    
    // 대화가 없는 연출은 UniTask형태가 아님
    private async void Product2()
    {
        AccumulatedStep();
        MyEventStepUp();
        SetEventStep();
        //SaveEpisode();
        
        dialogCancellation = new CancellationTokenSource();
        monsterSpawning = true;
        foreach (var wave in monsterWave1)
        {
            var randX = Random.Range(-monsterInterval, monsterInterval);
            var randPos = new Vector2(monsterPos[0].position.x + randX, monsterPos[0].position.y);
            GameManager.Instance.SpawnMonster(wave, randPos);
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
        }
        monsterSpawning = false;
    }
    
    // 대화가 없는 연출은 UniTask형태가 아님
    private void Product3()
    {
        AccumulatedStep();
        MyEventStepUp();
        SaveEpisode();
    }
    
    private async UniTask Product4()
    {
        if (episodeStep.dialogStep == 1)
        {
            dialogSwitch = false;
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
            
            GameManager.Instance.ControlStart = false;
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
            GameManager.Instance.MainCamera.MinXAndY = new Vector2(46.5f, GameManager.Instance.MainCamera.MinXAndY.y);
            
            dialogCancellation = new CancellationTokenSource();
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            await curPlayer.EpisodeMove(customMovePos[episodeStep.customMoveStep].position, curPlayer.BasicStat.moveSpeed, 1);
            
            var citizenSpeechPosition = citizen.FontPos.position;
            speechFrame1[0].SetPos(citizenSpeechPosition);
            speechFrame1[0].Speech(dialog1);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;

            PlaySound(ConstValues.GunnerLaugh);
            var gunnerSpeechPos = curPlayer.FontPos.position;
            speechFrame1[0].SetPos(new Vector2(gunnerSpeechPos.x, gunnerSpeechPos.y + 0.5f));
            speechFrame1[0].Speech(dialog2);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            
                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }

            var monsterSpeechPos = traceMonsters[0].FontPos.position;
            speechFrame1[0].SetPos(monsterSpeechPos);
            speechFrame1[0].Speech(dialog3);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame1[0].gameObject.SetActive(false);
            
            curPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.DialogShot);
            curPlayer.SpawnObject(ConstValues.GunnerFlash, curPlayer.CenterPos.position);

            var length = 0.6f;
            traceMonsters[0].EpisodeMove_X(new Vector2(curPlayer.transform.position.x + length, curPlayer.transform.position.y), 6, -1).Forget();
            traceMonsters[1].EpisodeMove_X(new Vector2(curPlayer.transform.position.x + length, curPlayer.transform.position.y), 6, -1).Forget();
            citizen.EpisodeMove_X(new Vector2(curPlayer.transform.position.x + length, curPlayer.transform.position.y), 6, -1).Forget();

            await UniTask.WaitUntil(() => citizen.transform.position.x < curPlayer.transform.position.x + length + 1.0f); 
            curPlayer.GetComponent<Player_Gunner>().KnockBackShot(false).Forget();
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            citizen.Airborne(4, 7);
            citizen.HitMaterial();
            citizen.SpawnHitEffect(ConstValues.GunnerAttackHitCrit, 1.0f, 1.5f);
            
            traceMonsters[0].Airborne(6, 8);
            traceMonsters[0].HitMaterial();
            traceMonsters[0].SpawnHitEffect(ConstValues.GunnerAttackHitCrit, 1.0f, 1.5f);
            
            traceMonsters[1].Airborne(8, 9);
            traceMonsters[1].HitMaterial();
            traceMonsters[1].SpawnHitEffect(ConstValues.GunnerAttackHitCrit, 1.0f, 1.5f);
            
            CameraShake(0.1f, 0.1f);
            
            BgmManager.Instance.Stop();
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            curPlayer.CustomAnimTrigger(ENormalState.Normal, ConstValues.Idle);
            
            citizenSpeechPosition = citizen.FontPos.position;
            speechFrame1[0].SetPos(new Vector2(citizenSpeechPosition.x, citizenSpeechPosition.y - 1.0f));
            speechFrame1[0].Speech(dialog4);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            gunnerSpeechPos = curPlayer.FontPos.position;
            speechFrame1[0].SetPos(gunnerSpeechPos);
            speechFrame1[0].Speech(dialog5);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            system.gameObject.SetActive(true);
            system.transform.position = npcPos[3].transform.position;
            system.SpawnObject(ConstValues.BangEffect, system.CenterPos.position);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].SetPos(gunnerSpeechPos);
            speechFrame1[0].Speech(dialog6);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            var systemSpeechPos = system.FontPos.position;
            speechFrame1[0].SetPos(systemSpeechPos);
            speechFrame1[0].Speech(dialog7);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].SetPos(gunnerSpeechPos);
            speechFrame1[0].Speech(dialog8);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].SetPos(systemSpeechPos);
            speechFrame1[0].Speech(dialog9);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].SetPos(gunnerSpeechPos);
            speechFrame1[0].Speech(dialog10);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame1[0].gameObject.SetActive(false);
            
            // 검은색 페이드 등장
            var fadeUI = GameManager.Instance.SpawnToUIPool(ConstValues.FadeUI, Vector3.zero);
            var titleSpeechPos = Vector3.zero;
            speechFrameTitle.SetPos(titleSpeechPos);
            speechFrameTitle.Speech(dialog11);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrameTitle.gameObject.SetActive(false);
            fadeUI.SetActive(false);
            
            system.gameObject.SetActive(false);
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Arrive);
            
            speechFrame1[0].gameObject.SetActive(true);
            speechFrame1[0].SetPos(citizenSpeechPosition);
            speechFrame1[0].Speech(dialog12);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Thumbs);
            speechFrame1[0].gameObject.SetActive(true);
            speechFrame1[0].SetPos(citizenSpeechPosition);
            speechFrame1[0].Speech(dialog13);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            PlaySound(ConstValues.GunnerLaugh);
            speechFrame1[0].SetPos(new Vector2(gunnerSpeechPos.x, gunnerSpeechPos.y + 0.5f));
            speechFrame1[0].Speech(dialog14);
            for (int i = 0; i < 2; i++)
            {
                curPlayer.CustomJump(new Vector2(0, 6.0f));
                curPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.Jump);
            
                if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                    return;
            }
            speechFrame1[0].gameObject.SetActive(false);
            
            // 야!!!!!!!!!
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Arrive);
            citizen.Flip(1);
            BgmManager.Instance.PlayBgm(ConstValues.BGMEpisode2Battle);
            CameraShake(0.5f, 0.5f); 
            PlaySound(ConstValues.FighterStrongPunch);
            speechFrameStrong.gameObject.SetActive(true);
            speechFrameStrong.SetPos(strongSpeechPos[0].position);
            speechFrameStrong.Speech(dialog15);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrameStrong.gameObject.SetActive(false);

            chargeMonster.gameObject.SetActive(true);
            chargeMonster.IsDie = true;
            chargeMonster.transform.position = bossPos[0].position;
            chargeMonster.Flip(-1);
            var stopPos = new Vector2(citizen.transform.position.x + 4.0f, citizen.transform.position.y);
            await chargeMonster.EpisodeMove_X(stopPos, chargeMonster.BasicStat.moveSpeed, -1);
            
            chargeMonster.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            var chargeSpeechPosition = chargeMonster.FontPos.position;
            speechFrame1[0].gameObject.SetActive(true);
            speechFrame1[0].SetPos(chargeSpeechPosition);
            speechFrame1[0].Speech(dialog16);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            citizen.Flip(-1);
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Point);
            speechFrame1[0].gameObject.SetActive(true);
            speechFrame1[0].SetPos(citizenSpeechPosition);
            speechFrame1[0].Speech(dialog17);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].gameObject.SetActive(true);
            speechFrame1[0].SetPos(chargeSpeechPosition);
            speechFrame1[0].Speech(dialog18);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            // 시민이 있던위치로 이동
            stopPos = new Vector2(citizen.transform.position.x, citizen.transform.position.y);
            chargeMonster.EpisodeMove_X(stopPos, chargeMonster.BasicStat.moveSpeed, -1).Forget();
            
            citizen.CustomAnimTrigger(ENormalState.Idle, ConstValues.Thumbs);
            speechFrame1[0].SetPos(citizenSpeechPosition);
            speechFrame1[0].Speech(dialog19);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].SetPos(citizenSpeechPosition);
            speechFrame1[0].Speech(dialog20);
            speechFrame1[0].Trace(citizen.FontPos);
            
            var runPos = new Vector2(GameManager.Instance.CurPlayer.transform.position.x - 6.0f, citizen.transform.position.y);
            await citizen.EpisodeMove_X(runPos, 10, -1);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;
            citizen.gameObject.SetActive(false);
            
            speechFrame1[0].SetPos(gunnerSpeechPos);
            speechFrame1[0].Speech(dialog21);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame1[0].gameObject.SetActive(false);

            curPlayer.GetComponent<Player_Gunner>().KnockBackShot(false).Forget();
            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;
            chargeMonster.HitMaterial();
            chargeMonster.SpawnHitEffect(ConstValues.GunnerAttackHitCrit, 1.0f, 1.5f);
            if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
                return;

            curPlayer.BasicStat.bodyType = EBodyType.Normal;
            chargeSpeechPosition = chargeMonster.FontPos.position;
            speechFrame1[0].SetPos(chargeSpeechPosition);
            speechFrame1[0].Speech(dialog22);
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            speechFrame1[0].gameObject.SetActive(false);

            await chargeMonster.GetComponent<Monster_Charge>().EventCharge();
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
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

    private void Test()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(GameManager.Instance.FirstPlayer == ConstValues.Berserker)
                GameManager.Instance.SetCharacterOrder(ConstValues.Berserker, ConstValues.Gunner);
            else if(GameManager.Instance.FirstPlayer == ConstValues.Gunner)
                GameManager.Instance.SetCharacterOrder(ConstValues.Gunner, ConstValues.Berserker);
            
            //GameManager.Instance.CharacterChange(false);
        }
    }

    private void StepCharacterSetting()
    {
        switch (myEventStep)
        {
            case 0:
                GameManager.Instance.SetPlayerOrder(ConstValues.Gunner, default);
                break;
            
            // case 1:
            //     GameManager.Instance.InitPlayer(ConstValues.Gunner, ConstValues.Berserker);
            //     break;
        }
        
        curPlayer = GameManager.Instance.CurPlayer;
    }
    
    private void AccumulatedStep()
    {
        PlayBGM(ConstValues.BGMEpisode2);
        //StopBGM();

        switch (myEventStep)
        {
            case 0:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(0, GameManager.Instance.MainCamera.MinXAndY.y);
                break;
            case 1:
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(36.5f, GameManager.Instance.MainCamera.MinXAndY.y);
                // 벽 설치
                stageWalls.Add(GameManager.Instance.SpawnToObjectPool(ConstValues.StageWallRight, stageWallPos[0]));
                break;
            case 2:
                GameManager.Instance.MainCamera.MaxXAndY = new Vector2(46.5f, GameManager.Instance.MainCamera.MinXAndY.y);
                // 벽 제거
                foreach (var stageWall in stageWalls)
                    stageWall.SetActive(false);
                
                // 시민과 몬스터 스폰시키기
                var citizenPos = npcPos[2].position;
                var traceMonsterPos1 = new Vector2(npcPos[2].position.x + 1.0f, npcPos[2].position.y);
                var traceMonsterPos2 = new Vector2(npcPos[2].position.x + 2.0f, npcPos[2].position.y);
                
                citizen.transform.position = citizenPos;
                citizen.gameObject.SetActive(true);
                
                traceMonsters[0].transform.position = traceMonsterPos1;
                traceMonsters[0].gameObject.SetActive(true);
                
                traceMonsters[1].transform.position = traceMonsterPos2;
                traceMonsters[1].gameObject.SetActive(true);

                traceMonsters[0].IsDie = true;
                traceMonsters[0].Flip(-1);
                
                traceMonsters[1].IsDie = true;
                traceMonsters[1].Flip(-1);
                break;
        }
    }
}
