using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startText; 
    private CancellationTokenSource fadeCancellation;
    
    private void Start()
    {
        StartBGM();
        SetText();
        TextFade();
        ButtonSetting();
        if (SceneChanger.Instance)
            SceneChanger.Instance.TitleScene = true;
    }

    private void StartBGM()
    {
        BgmManager.Instance.PlayBgm(ConstValues.BGMTitle);
    }

    private void SetText()
    {
        startText.text = "화면을 클릭하세요";
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

    private void ButtonSetting()
    {
        startButton.onClick.AddListener(()=> {GameManager.Instance.GoScene(ConstValues.BattleScene);});
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
