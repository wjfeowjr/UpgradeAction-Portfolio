using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SaveObject : MonoBehaviour
{
    [SerializeField] private GameObject uiObject;
    [SerializeField] private Transform savePointPos;
    [SerializeField] private TextMeshPro[] myTexts;
    [SerializeField] private SpriteRenderer[] mySpriteRenderers;
    
    private Vector3 reduceScale = new Vector3(1, 0, 0);
    private Vector3 expansionScale = new Vector3(1, 1, 0);
    private Action saveAction;
    
    private Tween fadeTween1;
    private Tween fadeTween2;
    private Tween expansionTween;
    
    private float onAlpha = 1.0f;
    private float offAlpha = 0;
    private float duration = 0.3f;
    
    private CancellationTokenSource waitCancellation;

    private bool isFading;
    private bool isExpansion;

    public Transform SavePointPos => savePointPos;
    
    private void OnEnable()
    {
        StartSetting();
    }

    private void Update()
    {
        if (!isFading && isExpansion && Input.GetKeyDown(KeyCode.UpArrow))
        {
            FadeOut();
            saveAction();
        }
    }

    public void SetSaveAction(Action action)
    {
        saveAction = action;
    }

    private async void FadeOut()
    {
        isFading = true;
        await RoomManager.Instance.FadeIn(ConstValues.WhiteColor);
        isFading = false;
    }

    private void StartSetting()
    {
        foreach (var myText in myTexts)
            myText.color = ConstValues.WhiteColorAlpha0;
        foreach (var mySpriteRenderer in mySpriteRenderers)
            mySpriteRenderer.color = ConstValues.WhiteColorAlpha0;
        uiObject.transform.localScale = reduceScale;
        uiObject.SetActive(false);
    }

    public void Expansion()
    {
        isExpansion = true;
        waitCancellation?.Cancel();
        uiObject.SetActive(true);
        
        if(fadeTween1 != null)
            fadeTween1.Kill();
        if(fadeTween2 != null)
            fadeTween2.Kill();
        if(expansionTween != null)
            expansionTween.Kill();

        foreach (var myText in myTexts)
            fadeTween1 = myText.DOFade(onAlpha, duration).SetEase(Ease.Linear);
        foreach (var mySpriteRenderer in mySpriteRenderers)
            fadeTween2 = mySpriteRenderer.DOFade(onAlpha, duration).SetEase(Ease.Linear);
        
        expansionTween = uiObject.transform.DOScale(expansionScale, duration).SetEase(Ease.Linear);
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
        foreach (var mySpriteRenderer in mySpriteRenderers)
            fadeTween2 = mySpriteRenderer.DOFade(offAlpha, duration).SetEase(Ease.Linear);
        
        expansionTween = uiObject.transform.DOScale(reduceScale, duration).SetEase(Ease.Linear);
        
        waitCancellation = new CancellationTokenSource();
        if (await NormalDelay(duration, waitCancellation).SuppressCancellationThrow())
            return;
        
        StartSetting();
    }

    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
