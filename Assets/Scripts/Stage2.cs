using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Stage2 : Stage
{
    protected override void SetEpisodeName()
    {
        base.SetEpisodeName();
        episodeName = ConstValues.Episode2;
        episodeTitle = "에피소드2: 협동";
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
        episodeStep = new EpisodeStep()
        {
            episodeTitle = 1,
            dialogStep = 1,
            playerStep = 0,
            customMoveStep = 0,
            eventStep = 0,
        };
        GameManager.Instance.ControlStart = true;
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
            string dialog1 = "어헝!";

            dialogCancellation = new CancellationTokenSource();
            GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

            if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
                return;

            var speechPosition = curPlayer.FontPos.position;
            var speechFrame = GameManager.Instance.SpawnSpeechFrame(ConstValues.SpeechFrame1, speechPosition, dialog1);

            if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
                return;
            
            speechFrame.gameObject.SetActive(false);

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
        //PlayBGM(ConstValues.BGMEpisode2);
        StopBGM();

        switch (myEventStep)
        {
            case 0:
                // 카메라 제한
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(0, GameManager.Instance.MainCamera.MinXAndY.y);
                GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, monsterPos[0].position);
                GameManager.Instance.SpawnMonster(ConstValues.MonsterPurple, monsterPos[0].position);
                GameManager.Instance.SpawnMonster(ConstValues.MonsterCharge, monsterPos[0].position);
                break;
        }
    }
}
