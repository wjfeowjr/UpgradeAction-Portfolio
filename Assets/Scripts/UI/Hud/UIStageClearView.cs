using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IUIStageClearView
{
    void SetStageClear(string episodeName, string clearString, string buttonString, Action confirmAction);
    void StageClearProduct(Action soundAction);
}

public class UIStageClearModel
{
    public string episodeName;
    public string clearString;
    public string buttonString;
    public Action confirmAction;
}

public class UIStageClearPresenter
{
    private readonly IUIStageClearView _stageClearview;
    private UIStageClearModel _model;

    public UIStageClearPresenter(IUIStageClearView stageClearView, UIStageClearModel model)
    {
        _stageClearview = stageClearView;
        _model = model;
    }
    
    public void SetStageClear()
    {
        _stageClearview.SetStageClear(_model.episodeName, _model.clearString, _model.buttonString, _model.confirmAction);
    }

    public void StageClearProduct(Action soundAction)
    {
        _stageClearview.StageClearProduct(soundAction);
    }
}

public class UIStageClearView : MonoBehaviour, IUIStageClearView
{
    private CancellationTokenSource stageClearCancellation;
    
    private float fadeTime = 0.5f;
    private float reduceTime = 0.5f;
    private float moveSecond = 0.5f;
    private float finish = 1.0f;
    
    [SerializeField] private TMP_Text episodeText;
    [SerializeField] private TMP_Text clearText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Image fadeImage;
    [SerializeField] private Button nextButton;
    private Action action;
    
    // 딜레이
    private async UniTask EpisodeDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: stageClearCancellation.Token);
    }
    
    public void SetStageClear(string episodeName, string clearString, string buttonString, Action confirmAction)
    {
        episodeText.text = episodeName;
        episodeText.transform.position = startTransform.transform.position;
        
        clearText.text = clearString;
        clearText.gameObject.SetActive(false);
        clearText.transform.localScale = new Vector3(6, 6, 6);

        buttonText.text = buttonString;
       
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
        nextButton.gameObject.SetActive(false);

        action = confirmAction;
        
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(() =>
        {
            action();
        });
        
        gameObject.SetActive(false);
    }
    
    public async void StageClearProduct(Action soundAction)
    {
        stageClearCancellation = new CancellationTokenSource();
        
        // 페이드 인
        fadeImage.DOFade(0.7f, fadeTime).SetEase(Ease.Linear);
        if (await EpisodeDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        // 텍스트 이동 후 정지
        soundAction?.Invoke();
        episodeText.transform.DOMove(endTransform.position, moveSecond);
        if (await EpisodeDelay(moveSecond).SuppressCancellationThrow())
            return;
        
        // 클리어 나오고
        clearText.gameObject.SetActive(true);
        clearText.transform.DOScale(Vector3.one, reduceTime).SetEase(Ease.Linear);
        if (await EpisodeDelay(reduceTime).SuppressCancellationThrow())
            return;
        soundAction?.Invoke();
        
        if (await EpisodeDelay(finish).SuppressCancellationThrow())
            return;
        
        nextButton.gameObject.SetActive(true);
    }
    
    public void InvokeAction()
    {
        action.Invoke();
    }

    public bool IsNextButtonActive()
    {
        return nextButton.gameObject.activeSelf;
    }
}
