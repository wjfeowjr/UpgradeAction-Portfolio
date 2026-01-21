using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionObject : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] myTexts;
    [SerializeField] private Image[] myImages;
    
    private Vector3 reduceScale = new Vector3(1, 0, 0);
    private Vector3 expansionScale = new Vector3(1, 1, 0);
    private Action interactionAction;
    
    private Tween fadeTween1;
    private Tween fadeTween2;
    private Tween expansionTween;
    
    private float onAlpha = 1.0f;
    private float offAlpha = 0;
    private float duration = 0.3f;
    
    private CancellationTokenSource waitCancellation;

    private bool isFading;
    private bool isExpansion;

    private void OnEnable()
    {
        StartSetting();
    }

    // 모든 상호작용은 플레이어가 서있는 상태에서만 할 수 있다
    private void Update()
    {
        if (GameManager.Instance.ControlStart && GameManager.Instance.CurPlayer.NormalState == ENormalState.Idle && !isFading && isExpansion && Input.GetKeyDown(KeyCode.UpArrow))
        {
            if(interactionAction != null)
                interactionAction();
        }
    }

    public void SetText(string name, string key)
    {
        myTexts[0].text = name;
        myTexts[1].text = key;
    }

    public void SetInteractionAction(Action action)
    {
        interactionAction = action;
    }

    public async void FadeOut()
    {
        isFading = true;
        await RoomManager.Instance.FadeIn(ConstValues.WhiteColor);
        isFading = false;
    }

    private void StartSetting()
    {
        foreach (var myText in myTexts)
            myText.color = ConstValues.WhiteColorAlpha0;
        foreach (var mySpriteRenderer in myImages)
            mySpriteRenderer.color = ConstValues.WhiteColorAlpha0;
        transform.localScale = reduceScale;
    }

    public void Expansion()
    {
        isExpansion = true;
        waitCancellation?.Cancel();

        if(fadeTween1 != null)
            fadeTween1.Kill();
        if(fadeTween2 != null)
            fadeTween2.Kill();
        if(expansionTween != null)
            expansionTween.Kill();

        foreach (var myText in myTexts)
            fadeTween1 = myText.DOFade(onAlpha, duration).SetEase(Ease.Linear);
        foreach (var mySpriteRenderer in myImages)
            fadeTween2 = mySpriteRenderer.DOFade(onAlpha, duration).SetEase(Ease.Linear);
        
        expansionTween = transform.DOScale(expansionScale, duration).SetEase(Ease.Linear);
    }

    public async void Reduce()
    {
        isExpansion = false;
        waitCancellation?.Cancel();
        if(fadeTween1 != null)
            fadeTween1.Kill();
        if(fadeTween2 != null)
            fadeTween2.Kill();
        if(expansionTween != null)
            expansionTween.Kill();
        
        foreach (var myText in myTexts)
            fadeTween1 = myText.DOFade(offAlpha, duration).SetEase(Ease.Linear);
        foreach (var mySpriteRenderer in myImages)
            fadeTween2 = mySpriteRenderer.DOFade(offAlpha, duration).SetEase(Ease.Linear);
        
        expansionTween = transform.DOScale(reduceScale, duration).SetEase(Ease.Linear);
        
        waitCancellation = new CancellationTokenSource();
        if (await NormalDelay(duration, waitCancellation).SuppressCancellationThrow())
            return;
        
        StartSetting();
        gameObject.SetActive(false);
    }

    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
