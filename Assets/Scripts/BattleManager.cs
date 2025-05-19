using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private FollowCamera mainCamera;
    
    [SerializeField] private Transform[] playerPos;
    [SerializeField] private Transform[] chapterPos;
    [SerializeField] private Transform[] producePos;
    [SerializeField] private Transform[] stageWallPos;
    [SerializeField] private Transform[] trapPos;
    
    [SerializeField] private List<Collider2D> platformColliderList;
    private CancellationTokenSource dialogCancellation;
    private CancellationTokenSource dieCancellation;

    [SerializeField] private int episodeTitle = 0;
    [SerializeField] private int step = 0;
    [SerializeField] private int dialog = 0;
    [SerializeField] private int curStep = 0;
    
    private float dialogDelay1 = 2.5f;
    private float dialogDelay2 = 1.0f;

    private GameObject stageWall;
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
        episodeTitle = GetKey(ConstValues.Episode);
        step = GetKey(ConstValues.Step);
        dialog = GetKey(ConstValues.Dialog);

        curStep = step - 1;
        if (curStep < 0)
            curStep = 0;
        
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[curStep].position);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Episode, Vector2.zero);
        GameManager.Instance.PlatformColliderList = platformColliderList;
        GameOverCycle();
        ProductEpisode();
        // 누적된 연출 진행
        AccumulatedStep();
    }

    private void Update()
    {
        ChapterCycle();
    }

    private void ProductEpisode()
    {
        if (episodeTitle != 0)
            return;
        
        var uiEpisodeObj = GameManager.Instance.GetUI(eUIType.UI_Episode);
        if (uiEpisodeObj == null)
            return;
        
        var uiInterface = uiEpisodeObj.GetComponent<UI_Episode>();
        uiInterface.EpisodePresenter.HandelEpisodeEnd(EpisodeEnd);
        uiInterface.EpisodePresenter.EpisodeProduct();
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
        await NormalDelay(1.0f, dieCancellation);
        GameManager.Instance.SpawnToPopupPool(eUIType.Popup_GameOver, Vector2.zero);
        Time.timeScale = 0;
    }
    
    private void ChapterCycle()
    {
        if (episodeTitle == 0)
            return;
        
        if (step > chapterPos.Length - 1)
            return;
        
        if (curPlayer.transform.position.x >= chapterPos[step].transform.position.x && GameManager.Instance.MonsterList.Count == 0)
        {
            // 대화 진행
            DialogStep();

            dialog++;
            SetKey(ConstValues.Dialog, dialog);

            step++;
            SetKey(ConstValues.Step, step);
        }
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

    private async void DialogStep()
    {
        // 대화 진행
        switch (dialog) 
        {
            case 0:
                await Product1();
                break;
            
            case 1:
                await Product2();
                break;
        }
    }

    private async UniTask Product1()
    {
        string dialog1 = "날씨 참 좋다...";
        string dialog2 = "저 거지같은\n태양만 빼고\n말이야!";
        string dialog3 = "뿌셔버릴거야!!!";

        dialogCancellation = new CancellationTokenSource();
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);

        if (await NormalDelay(0.1f, dialogCancellation).SuppressCancellationThrow())
            return;

        var speechPosition = curPlayer.FontPos.position;
        var speechFrame = GameManager.Instance.SpawnSpeechFrame(speechPosition, dialog1);

        if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
            return;

        speechFrame.Speech(dialog2);

        if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
            return;

        speechFrame.transform.position = new Vector2(speechPosition.x, speechPosition.y + 0.5f);
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

        // 게임 시작
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
    }

    private async UniTask Product2()
    {
        string dialog1 = "저기에 닿으면 쌔까맣게 타 죽을거야";
        string dialog2 = "Z키로 저 함정을 돌파해 보자!";

        GameManager.Instance.ControlStart = false;
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
        
        // 카메라 제한
        GameManager.Instance.MainCamera.MinXAndY = new Vector2(40.5f, GameManager.Instance.MainCamera.MinXAndY.y);

        await curPlayer.CustomMove(producePos[0].position, 1);
        
        // 함정 설치
        GameManager.Instance.SpawnTrap(ConstValues.TrapPillar, trapPos[0].position);
        
        dialogCancellation = new CancellationTokenSource();
        var speechPosition = curPlayer.FontPos.position;
        var speechFrame = GameManager.Instance.SpawnSpeechFrame(speechPosition, dialog1);
                
        if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
            return;
        
        speechFrame.Speech(dialog2);
                
        if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
            return;
                
        speechFrame.gameObject.SetActive(false);
        // 게임 시작
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
    }
    
    // GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, chapterPos[1].position);
    //stageWall = GameManager.Instance.SpawnToObjectPool(ConstValues.StageWall, stageWallPos[0]);

    private void AccumulatedStep()
    {
        if (step >= 1)
        {
            // 카메라 제한
            GameManager.Instance.MainCamera.MinXAndY = new Vector2(40.5f, GameManager.Instance.MainCamera.MinXAndY.y);
            // 함정 설치
            GameManager.Instance.SpawnTrap(ConstValues.TrapPillar, trapPos[0].position);
        }
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    
}
