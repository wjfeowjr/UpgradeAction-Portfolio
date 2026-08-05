using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIEpisodeModel
{
    public string episodeName;
}

public class UIEpisodeView : MonoBehaviour
{
    private CancellationTokenSource episodeCancellation;
    
    private float fadeTime = 0.5f;
    private float moveSecond = 0.5f;
    private float delay = 2.0f;
    private float finish = 1.0f;
    
    [SerializeField] private TMP_Text episodeText;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform stopTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Image fadeImage;
    
    // 딜레이
    private async UniTask EpisodeDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: episodeCancellation.Token);
    }
    
    public void SetEpisode(UIEpisodeModel model)
    {
        SetEpisode(model.episodeName);
    }

    private void SetEpisode(string episodeName)
    {
        episodeText.text = episodeName;
        episodeText.transform.position = startTransform.transform.position;
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
    }
    
    public async UniTask EpisodeProduct(Action soundAction)
    {
        gameObject.SetActive(true);
        episodeCancellation = new CancellationTokenSource();
        
        // 페이드 인
        fadeImage.DOFade(0.7f, fadeTime).SetEase(Ease.Linear);
        if (await EpisodeDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        // 텍스트 이동 후 정지
        soundAction?.Invoke();
        episodeText.transform.DOMove(stopTransform.position, moveSecond);
        if (await EpisodeDelay(moveSecond).SuppressCancellationThrow())
            return;
        
        if (await EpisodeDelay(delay).SuppressCancellationThrow())
            return;
        
        // 텍스트 화면 바깥으로 이동
        episodeText.transform.DOMove(endTransform.position, moveSecond);
        if (await EpisodeDelay(moveSecond).SuppressCancellationThrow())
            return;
        
        fadeImage.DOFade(0, fadeTime).SetEase(Ease.Linear);
        if (await EpisodeDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        if (await EpisodeDelay(finish).SuppressCancellationThrow())
            return;
        
        gameObject.SetActive(false);
    }
}
