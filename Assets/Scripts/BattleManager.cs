using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private FollowCamera mainCamera;
    [SerializeField] private Transform playerPos;
    [SerializeField] private List<Collider2D> platformColliderList;
    private CancellationTokenSource dialogCancellation;

    private void Awake()
    {
        if (SceneChanger.Instance)
            SceneChanger.Instance.SceneControl();
        
        if(GameManager.Instance)
            GameManager.Instance.InitCamera(mainCamera); 
    }

    private void Start()
    {
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.PlatformColliderList = platformColliderList;
        Test();
    }

    private async void Test()
    {
        // string dialog1 = "날씨 참 좋다...";
        // string dialog2 = "저 거지같은\n태양만 빼고\n말이야!";
        // string dialog3 = "뿌셔버릴거야!!!";
        //
        // float dialogDelay1 = 2.0f;
        // float dialogDelay2 = 1.0f;
        //
        // dialogCancellation = new CancellationTokenSource();
        // GameManager.Instance.GetUI(eUIType.UI_Skill).SetActive(false);
        //
        // var speechPosition = GameManager.Instance.CurPlayer.FontPos.position;
        // var speechFrame = GameManager.Instance.SpawnSpeechFrame(speechPosition, dialog1);
        //
        // if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
        //     return;
        //
        // speechFrame.Speech(dialog2);
        //
        // if (await NormalDelay(dialogDelay1, dialogCancellation).SuppressCancellationThrow())
        //     return;
        //
        // speechFrame.transform.position = new Vector2(speechPosition.x, speechPosition.y + 0.5f);
        // speechFrame.Speech(dialog3);
        //
        // for (int i = 0; i < 2; i++)
        // {
        //     GameManager.Instance.CurPlayer.CustomJump(new Vector2(0, 6.0f));
        //     GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Jump, ConstValues.DialogJump);
        //
        //     if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
        //         return;
        // }
        //
        // if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
        //     return;
        //
        // speechFrame.gameObject.SetActive(false);

        // 게임 시작
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(true);
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
