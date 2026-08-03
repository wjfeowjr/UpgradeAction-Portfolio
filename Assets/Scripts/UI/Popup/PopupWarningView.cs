using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IPopupWarningView
{
    UniTask SetMessage(string message, float delay);
}

public class PopupWarningModel
{
    public string message;
    // 경고 문구가 유지되는 시간 (기본 1.2초)
    public float delay;
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
        return _warningView.SetMessage(_model.message, _model.delay);
    }
}

public class PopupWarningView : MonoBehaviour, IPopupWarningView
{
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    private PopupWarningPresenter presenter;

    public PopupWarningPresenter Bind(PopupWarningModel model)
    {
        presenter = new PopupWarningPresenter(this, model);
        return presenter;
    }

    private CancellationTokenSource warningCancellation;
    private Tween scaleTween;
    private Vector3 expansionScale = new Vector3(1, 1, 1);
    private Vector3 reduceScale = new Vector3(1, 0, 1);
    private float duration = 0.3f;

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject warningLineObject;

    public async UniTask SetMessage(string message, float delay)
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
