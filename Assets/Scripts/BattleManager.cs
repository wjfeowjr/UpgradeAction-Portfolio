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
    
    [SerializeField] private List<Collider2D> platformColliderList;
    private CancellationTokenSource dialogCancellation;
    private CancellationTokenSource dieCancellation;
    
    [SerializeField] private int step = 0;
    [SerializeField] private int dialog = 0;

    [SerializeField] private int curStep = 0;
    
    private Player curPlayer;
    
    private void Awake()
    {
        if (SceneChanger.Instance)
            SceneChanger.Instance.SceneControl();

        if (GameManager.Instance)
        {
            GameManager.Instance.InitCamera(mainCamera);
            GameManager.Instance.ClearMonsterList();
            curPlayer = GameManager.Instance.CurPlayer;
        }
    }

    private void Start()
    {
        step = GetKey(ConstValues.Step);
        dialog = GetKey(ConstValues.Dialog);

        curStep = step - 1;
        if (curStep < 0)
            curStep = 0;
        
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos[curStep]);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.PlatformColliderList = platformColliderList;
        GameOverCycle();
    }

    private void Update()
    {
        ChapterCycle();
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
        if (step > chapterPos.Length - 1)
            return;
        
        if (curPlayer.transform.position.x >= chapterPos[step].transform.position.x && GameManager.Instance.MonsterList.Count == 0)
        {
            // 대화 진행
            DialogStep();
            // 연출 진행
            ProduceStep();
            
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
                string dialog1 = "날씨 참 좋다...";
                string dialog2 = "저 거지같은\n태양만 빼고\n말이야!";
                string dialog3 = "뿌셔버릴거야!!!";
        
                float dialogDelay1 = 2.0f;
                float dialogDelay2 = 1.0f;
        
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
                break;
            
            case 1:
                GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
                GameManager.Instance.ControlStart = false;
                await curPlayer.CustomMove(producePos[0].position, 1);
                
                // 몹 소환
                GameManager.Instance.SpawnMonster(ConstValues.MonsterSpinach, playerPos[1].position);
                
                string dialog4 = "이건 테스트 글이다";
                float dialogDelay3 = 2.0f;
                dialogCancellation = new CancellationTokenSource();

                var speechPosition2 = curPlayer.FontPos.position;
                var speechFrame2 = GameManager.Instance.SpawnSpeechFrame(speechPosition2, dialog4);
                
                if (await NormalDelay(dialogDelay3, dialogCancellation).SuppressCancellationThrow())
                    return;
                
                speechFrame2.gameObject.SetActive(false);
                // 게임 시작
                GameManager.Instance.ControlStart = true;
                GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
                break;
        }
    }

    private void ProduceStep()
    {
        // 단계 연출 진행
        switch (step)
        {
            case 1:
                GameManager.Instance.MainCamera.MinXAndY = new Vector2(40.5f, GameManager.Instance.MainCamera.MinXAndY.y);
                break;
        }
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
