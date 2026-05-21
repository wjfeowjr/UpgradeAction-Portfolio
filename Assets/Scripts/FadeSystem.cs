using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class FadeSystem : MonoBehaviour
{
    private CancellationTokenSource fadeCancellation;
    [SerializeField] private Ease myEase;
    [SerializeField] private Image[] myImages;
    [SerializeField] private TextMeshProUGUI[] myTexts;
    [SerializeField] private SpriteRenderer[] mySpriteRenderers;
    [SerializeField] private float duration;
    [SerializeField] private float startDelay;
    [SerializeField] private float endDelay;
    [SerializeField] private float startAlpha;
    [SerializeField] private float endAlpha;
    [SerializeField] private int loopCount;
    [SerializeField] private bool endDelete;

    private void Awake()
    {
        fadeCancellation = new CancellationTokenSource();
    }

    public void SetParameter(float setStart, float setEnd, float setDuration, bool delete, int setLoopCount = 0)
    {
        startAlpha = setStart;
        endAlpha = setEnd;
        duration = setDuration;
        endDelete = delete;
        loopCount = setLoopCount;
    }

    public void ColorInput(Color color)
    {
        foreach (var myImage in myImages)
            myImage.color = color;
        foreach (var myText in myTexts)
            myText.color = color;
        foreach (var mySpriteRenderer in mySpriteRenderers)
            mySpriteRenderer.color = color;
    }

    public async UniTask Fade(bool ignoreTime)
    {
        foreach (var myImage in myImages)
        {
            Color imageColor = myImage.color;
            imageColor.a = startAlpha;
            myImage.color = imageColor;
        }
        foreach (var myText in myTexts)
        {
            Color textColor = myText.color;
            textColor.a = startAlpha;
            myText.color = textColor;
        }
        foreach (var mySpriteRenderer in mySpriteRenderers)
        {
            Color spriteRendererColor = mySpriteRenderer.color;
            spriteRendererColor.a = startAlpha;
            mySpriteRenderer.color = spriteRendererColor;
        }
        
        await UniTask.Delay(TimeSpan.FromSeconds(startDelay), ignoreTimeScale: ignoreTime, cancellationToken: fadeCancellation.Token);

        if (loopCount == -1)
        {
            foreach (var myImage in myImages)
                myImage.DOFade(endAlpha, duration).SetUpdate(ignoreTime).SetLoops(loopCount, LoopType.Yoyo).SetEase(myEase);
            foreach (var myText in myTexts)
                myText.DOFade(endAlpha, duration).SetUpdate(ignoreTime).SetLoops(loopCount, LoopType.Yoyo).SetEase(myEase);
            foreach (var mySpriteRenderer in mySpriteRenderers)
                mySpriteRenderer.DOFade(endAlpha, duration).SetUpdate(ignoreTime).SetLoops(loopCount, LoopType.Yoyo).SetEase(myEase);
        }
        else
        {
            int currentLoop = 0;
            while (true)
            {
                foreach (var myImage in myImages)
                    myImage.DOFade(endAlpha, duration).SetUpdate(ignoreTime).SetEase(myEase);
                foreach (var myText in myTexts)
                    myText.DOFade(endAlpha, duration).SetUpdate(ignoreTime).SetEase(myEase);
                foreach (var mySpriteRenderer in mySpriteRenderers)
                    mySpriteRenderer.DOFade(endAlpha, duration).SetUpdate(ignoreTime).SetEase(myEase);
                
                await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: ignoreTime, cancellationToken: fadeCancellation.Token);
                await UniTask.Delay(TimeSpan.FromSeconds(endDelay), ignoreTimeScale: ignoreTime, cancellationToken: fadeCancellation.Token);
                // 반복횟수를 넘어가면 즉시 빠져나옴
                currentLoop += 1;
                if (currentLoop >= loopCount)
                    break;
                
                foreach (var myImage in myImages)
                    myImage.DOFade(startAlpha, duration).SetUpdate(ignoreTime).SetEase(myEase);
                foreach (var myText in myTexts)
                    myText.DOFade(startAlpha, duration).SetUpdate(ignoreTime).SetEase(myEase);
                foreach (var mySpriteRenderer in mySpriteRenderers)
                    mySpriteRenderer.DOFade(startAlpha, duration).SetUpdate(ignoreTime).SetEase(myEase);
                
                await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: ignoreTime, cancellationToken: fadeCancellation.Token);
                await UniTask.Delay(TimeSpan.FromSeconds(endDelay), ignoreTimeScale: ignoreTime, cancellationToken: fadeCancellation.Token);
                // 반복횟수를 넘어가면 즉시 빠져나옴
                currentLoop += 1;
                if (currentLoop >= loopCount)
                    break;
            }
        }

        await UniTask.WaitUntil(() => loopCount > -1, cancellationToken: fadeCancellation.Token);
        if (endDelete)
            gameObject.SetActive(false);
    }
}
