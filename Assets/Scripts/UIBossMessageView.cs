using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IUIBossMessageView
{
    void SetBossMessage(string bossName);
    void BossMessageProduct(Action soundAction);
}

public class UIBossMessageModel
{
    public string bossName;
}

public class UIBossMessagePresenter
{
    private readonly IUIBossMessageView _bossMessageview;
    private UIBossMessageModel _model;

    public UIBossMessagePresenter(IUIBossMessageView episodeView, UIBossMessageModel model)
    {
        _bossMessageview = episodeView;
        _model = model;
    }
    
    public void SetBossMessage()
    {
        _bossMessageview.SetBossMessage(_model.bossName);
    }

    public void BossMessageProduct(Action soundAction)
    {
        _bossMessageview.BossMessageProduct(soundAction);
    }
}

public class UIBossMessageView : MonoBehaviour, IUIBossMessageView
{
    private CancellationTokenSource bossMessageCancellation;
    
    private float fadeTime = 0.7f;
    private float moveSecond = 0.5f;
    private float delay = 2.0f;
    private float finish = 1.0f;

    [SerializeField] private Transform bossMessageTransform;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform stopTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Image fadeImage;
    
    // 딜레이
    private async UniTask BossMessageDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), ignoreTimeScale: true, cancellationToken: bossMessageCancellation.Token);
    }
    
    public void SetBossMessage(string bossName)
    {
        bossMessageTransform.position = startTransform.transform.position;
        bossNameText.text = bossName;
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
    }

    public async void BossMessageProduct(Action soundAction)
    {
        Time.timeScale = 0;
        soundAction?.Invoke();
        bossMessageCancellation = new CancellationTokenSource();

        // 페이드 인
        fadeImage.DOFade(0.7f, fadeTime).SetEase(Ease.Linear).SetUpdate(true);
        // if (await BossMessageDelay(fadeTime).SuppressCancellationThrow())
        //     return;
        
        // 텍스트 이동 후 정지
        soundAction?.Invoke();
        bossMessageTransform.DOMove(stopTransform.position, moveSecond).SetUpdate(true);
        if (await BossMessageDelay(moveSecond).SuppressCancellationThrow())
            return;
        
        if (await BossMessageDelay(delay).SuppressCancellationThrow())
            return;
        
        // 텍스트 화면 바깥으로 이동
        bossMessageTransform.DOMove(endTransform.position, moveSecond).SetUpdate(true);
        // if (await BossMessageDelay(moveSecond).SuppressCancellationThrow())
        //     return;
        
        fadeImage.DOFade(0, fadeTime).SetEase(Ease.Linear).SetUpdate(true);
        if (await BossMessageDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        // if (await BossMessageDelay(finish).SuppressCancellationThrow())
        //     return;
        
        Time.timeScale = 1;
        gameObject.SetActive(false);
    }
}
