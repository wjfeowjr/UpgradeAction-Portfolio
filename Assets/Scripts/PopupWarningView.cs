using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IPopupWarningView
{
    UniTask SetMessage(string message);
}

public class PopupWarningModel
{
    public string message;
}

public class PopupWarningPresenter
{
    private readonly IPopupWarningView _warningView;
    private PopupWarningModel _model;

    public PopupWarningPresenter(IPopupWarningView minimapView, PopupWarningModel model)
    {
        _warningView = minimapView;
        _model = model;
    }
    
    public UniTask SetMessage()
    {
        return _warningView.SetMessage(_model.message);
    }
}

public class PopupWarningView : MonoBehaviour, IPopupWarningView
{
    private CancellationTokenSource warningCancellation;
    private Tween scaleTween;
    private Vector3 expansionScale = new Vector3(1, 1, 1);
    private Vector3 reduceScale = new Vector3(1, 0, 1);
    private float delay = 1.2f;
    private float duration = 0.3f;
    
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject warningLineObject;

    public async UniTask SetMessage(string message)
    {
        warningCancellation?.Cancel();
        if(scaleTween != null)
            scaleTween.Kill();
        
        messageText.text = message;
        warningLineObject.transform.localScale = reduceScale;
        scaleTween = warningLineObject.transform.DOScale(expansionScale, duration).SetEase(Ease.Linear).SetUpdate(true);
        
        warningCancellation = new CancellationTokenSource();
        if (await NormalDelay(delay, warningCancellation).SuppressCancellationThrow())
            return;
        
        scaleTween = warningLineObject.transform.DOScale(reduceScale, duration).SetEase(Ease.Linear).SetUpdate(true);
        if (await NormalDelay(duration, warningCancellation).SuppressCancellationThrow())
            return;
        
        gameObject.SetActive(false);
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), ignoreTimeScale: true, cancellationToken: tokenSource.Token);
    }
}
