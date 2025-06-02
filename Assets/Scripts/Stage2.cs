using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Stage2 : Stage
{
    [SerializeField] private Transform[] npcPos;
    [SerializeField] private Player anotherPlayer;
    
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
        }
    }
    protected override void StageClearButtonAction() 
    {
        Application.Quit();
    }
    
    private void Start()
    {
        // episodeStep = new EpisodeStep()
        // {
        //     episodeTitle = 1,
        //     dialogStep = 1,
        //     playerStep = 0,
        //     customMoveStep = 0,
        //     eventStep = 0,
        // };
        //GameManager.Instance.ControlStart = true;
        
        LoadEpisode();
        StepCharacterSetting();
        
        dialogSwitch = true;
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[episodeStep.playerStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.SetGroundVector();
        
        SpawnEpisode(episodeTitle);
        SpawnStageClear();
        GameOverCycle();
        ProductEpisode();
        AccumulatedStep();
    }
    
    private void Update()
    {
        DialogCycle();
        Test();
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
            
            var citizenPos = npcPos[0].position;
            var traceMonsterPos1 = new Vector2(npcPos[0].position.x - 1.5f, npcPos[0].position.y);
            var traceMonsterPos2 = new Vector2(npcPos[0].position.x - 2.0f, npcPos[0].position.y);
            var traceMonsterPos3 = new Vector2(npcPos[0].position.x - 2.5f, npcPos[0].position.y);
            
            var arrivePos = new Vector2(npcPos[1].position.x, npcPos[1].position.y);
            var citizen1 = GameManager.Instance.SpawnToObjectPool(ConstValues.NpcCitizen, citizenPos).GetComponent<Npc>();
            var traceMonster1 = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSpinach, traceMonsterPos1).GetComponent<Monster>();
            var traceMonster2 = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSpinach, traceMonsterPos2).GetComponent<Monster>();
            var traceMonster3 = GameManager.Instance.SpawnToObjectPool(ConstValues.MonsterSpinach, traceMonsterPos3).GetComponent<Monster>();
            
            var npcSpeechPos = citizen1.FontPos;
            PlaySound(ConstValues.PlayerScream);
            speechFrame1[0].SetPos(npcSpeechPos.position);
            speechFrame1[0].Speech(dialog2);
            speechFrame1[0].Trace(npcSpeechPos);
            citizen1.EpisodeMove_X(arrivePos, 7.0f, 1).Forget();
            traceMonster1.EpisodeMove_X(arrivePos, 7.0f, 1).Forget();
            traceMonster2.EpisodeMove_X(arrivePos, 7.0f, 1).Forget();
            await traceMonster3.EpisodeMove_X(arrivePos, 7.0f, 1);
            
            // 마무리
            citizen1.gameObject.SetActive(false);
            traceMonster1.gameObject.SetActive(false);
            traceMonster2.gameObject.SetActive(false);
            traceMonster3.gameObject.SetActive(false);
            speechFrame1[0].gameObject.SetActive(false);

            if (await NormalDelay(0.5f, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].SetPos(gunnerSpeechPos);
            speechFrame1[0].Speech(dialog3);
            
            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame1[0].gameObject.SetActive(false);
            
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
                // anotherPlayer = GameManager.Instance.Players[0];
                // var curPlayerPos = curPlayer.transform.position;
                // anotherPlayer.transform.position = new Vector2(curPlayerPos.x - 7, curPlayerPos.y);
                // anotherPlayer.gameObject.SetActive(true);
                // GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[0].position);
                // GameManager.Instance.SpawnMonster(ConstValues.MonsterPurple, monsterPos[0].position);
                // GameManager.Instance.SpawnMonster(ConstValues.MonsterCharge, monsterPos[0].position);
                break;
        }
    }
}
