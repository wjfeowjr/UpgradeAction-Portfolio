using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private TMP_Text startText; 
    private CancellationTokenSource fadeCancellation;
    
    private void Start()
    {
        StartBGM();
        SetText();
        TextFade();
        if (SceneChanger.Instance)
            SceneChanger.Instance.TitleScene = true;
    }

    private void Update()
    {
        AnyKeyStart();
    }

    private void StartBGM()
    {
        BgmManager.Instance.PlayBgm(ConstValues.BGMTitle, true);
    }

    private void SetText()
    {
        startText.text = "Press Any Key";
    }

    private async void TextFade()
    {
        fadeCancellation = new CancellationTokenSource();
        float fadeTime = 1.0f;
        while (true)
        {
            startText.DOFade(0, fadeTime);
            if (await NormalDelay(fadeTime, fadeCancellation).SuppressCancellationThrow())
                return;
            
            startText.DOFade(1, fadeTime);
            if (await NormalDelay(fadeTime, fadeCancellation).SuppressCancellationThrow())
                return;
        }
    }

    private void AnyKeyStart()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            //PlayerPrefs.DeleteAll();
            GameManager.Instance.DefaultSkillKeySetting();
            GameManager.Instance.FirstStart();
        }

        // 아무 키 누르기
        if (!Input.anyKeyDown)
            return;
        
        // 마우스 클릭은 제외
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return;
            
        GameManager.Instance.GoScene(ConstValues.BattleScene);
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
