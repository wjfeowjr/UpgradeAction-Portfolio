using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IUIEpisodeView
{
    void SetEpisode(string episodeName);
    void EpisodeProduct();
    event Action EpisodeEnd;
}

public class UIEpisodeModel
{
    public string episodeName;
}

public class UIEpisodePresenter
{
    private readonly IUIEpisodeView _episodeview;
    private UIEpisodeModel _model;

    public UIEpisodePresenter(IUIEpisodeView episodeView, UIEpisodeModel model)
    {
        _episodeview = episodeView;
        _model = model;
    }
    
    public void SetEpisode()
    {
        _episodeview.SetEpisode(_model.episodeName);
    }

    public void EpisodeProduct()
    {
        _episodeview.EpisodeProduct();
    }

    public void HandelEpisodeEnd(Action action)
    {
        _episodeview.EpisodeEnd += action;
    }
}

public class UIEpisodeView : MonoBehaviour, IUIEpisodeView
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
    
    public event Action EpisodeEnd;

    // 딜레이
    private async UniTask EpisodeDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: episodeCancellation.Token);
    }
    
    public void SetEpisode(string episodeName)
    {
        episodeText.text = episodeName;
        episodeText.transform.position = startTransform.transform.position;
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
    }
    
    public async void EpisodeProduct()
    {
        episodeCancellation = new CancellationTokenSource();
        
        // 페이드 인
        fadeImage.DOFade(0.7f, fadeTime).SetEase(Ease.Linear);
        Debug.Log("이야1");
        if (await EpisodeDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        // 텍스트 이동 후 정지
        episodeText.transform.DOMove(stopTransform.position, moveSecond);
        Debug.Log("이야2");
        if (await EpisodeDelay(moveSecond).SuppressCancellationThrow())
            return;
        
        if (await EpisodeDelay(delay).SuppressCancellationThrow())
            return;
        
        // 텍스트 화면 바깥으로 이동
        episodeText.transform.DOMove(endTransform.position, moveSecond);
        Debug.Log("이야3");
        if (await EpisodeDelay(moveSecond).SuppressCancellationThrow())
            return;
        
        fadeImage.DOFade(0, fadeTime).SetEase(Ease.Linear);
        Debug.Log("이야4");
        if (await EpisodeDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        if (await EpisodeDelay(finish).SuppressCancellationThrow())
            return;
        
        EpisodeEnd?.Invoke();
        gameObject.SetActive(false);
    }
}
