using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoManager : MonoBehaviour
{
    private CancellationTokenSource startCancellation;
    [SerializeField] private TMP_Text logoText;

    private void Start()
    {
        GameStart();
    }

    private async void GameStart()
    {
        startCancellation = new CancellationTokenSource();
        logoText.alpha = 0;
        
        float fadeTime = 2.5f;
        logoText.DOFade(1, fadeTime);
        if (await NormalDelay(fadeTime, startCancellation).SuppressCancellationThrow())
            return;
        
        SceneManager.LoadScene(ConstValues.Title);
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
