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
        BgmManager.Instance.PlayBgm(ConstValues.BGMTitle);
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
            PlayerPrefs.DeleteAll();
            GameManager.Instance.DefaultSkillSetting();
            GameManager.Instance.DefaultSkillKeySetting();
            GameManager.Instance.DefaultGoodsSetting();
        }
        
        if (Input.anyKeyDown)
            GameManager.Instance.GoScene(ConstValues.BattleScene);
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
