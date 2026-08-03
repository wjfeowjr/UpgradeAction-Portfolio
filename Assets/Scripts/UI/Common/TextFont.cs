using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

public enum EFontType
{
    MyDamage,
    EnemyDamage,
    MyCritical,
    EnemyCritical,
    AdditionalDamage,
    Heal,
    Dot,
}

public class TextFont : MonoBehaviour
{
    private CancellationTokenSource delayCancellation;
    private TextMeshProUGUI myText;
    private Tween expansionTween;
    private Tween fadeTween;
    private Tween moveTween;
    private string textValue;

    private EFontType fontType;

    public float arrivePosY;
    public float delay;
    public float fadeSecond;
    public float upSecond;
    public float stopSecond;
    public float downSecond;
    public Vector3 startScale;
    public Vector3 expansionScale;

    private void Awake()
    {
        myText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        transform.SetAsLastSibling();
        FontProduction();
    }

    private void OnDisable()
    {
        delayCancellation?.Cancel();
    }

    // 폰트 보여주기(단위, 사이즈, 값, 타입)
    public void DisplayFont(int fontSize, string value)
    {
        myText.fontSize = fontSize;
        textValue = value;
        
        Color existingColor = myText.color;
        existingColor.a = 0f;
        myText.color = existingColor;
        myText.text = textValue;
        
        if(fontType == EFontType.EnemyCritical)
            myText.text = $"{textValue}!";
    }

    public void ColorSetting(EFontType type)
    {
        fontType = type;
        switch (fontType)
        {
            case EFontType.MyDamage:
                myText.color = ConstValues.WhiteColor;
                break;
            case EFontType.EnemyDamage:
                myText.color = ConstValues.RedColor;
                break;
            case EFontType.MyCritical:
                myText.color = ConstValues.YellowColor;
                break;
            case EFontType.EnemyCritical:
                myText.color = ConstValues.OrangeColor;
                break;
            case EFontType.AdditionalDamage:
                myText.color = ConstValues.CyanColor;
                break;
            case EFontType.Heal:
                myText.color = ConstValues.GreenColor;
                break;
            case EFontType.Dot:
                myText.color = ConstValues.OrangeColor;
                break;
        }
    }

    // 폰트 연출
    private async void FontProduction()
    {
        delayCancellation = new CancellationTokenSource();
        if (await YieldDelay(delayCancellation).SuppressCancellationThrow())
            return;
        
        Vector2 startVector = transform.position;
        Vector2 secondVector = new Vector2(startVector.x, startVector.y + arrivePosY - 0.1f);
        Vector2 arriveVector = new Vector2(startVector.x, startVector.y + arrivePosY);
        
        if(expansionTween == null)
            expansionTween = transform.DOScale(expansionScale, upSecond).SetAutoKill(false).SetRecyclable(true);
        else
            expansionTween.Restart();

        if(fadeTween == null)
            fadeTween = myText.DOFade(1, upSecond).SetAutoKill(false).SetRecyclable(true);
        else
            fadeTween.Restart();
        
        if(moveTween == null)
            moveTween = transform.DOMove(arriveVector, upSecond);
        else
            moveTween.Restart();
        
        if(await NormalDelay(upSecond, delayCancellation).SuppressCancellationThrow())
            return;

        transform.DOScale(startScale, stopSecond);
        transform.DOMove(secondVector, stopSecond);
        if(await NormalDelay(stopSecond, delayCancellation).SuppressCancellationThrow())
            return;
        
        if(await NormalDelay(delay, delayCancellation).SuppressCancellationThrow())
            return;
        
        myText.DOFade(0, fadeSecond);
        transform.DOMove(startVector, downSecond);
        if(await NormalDelay(downSecond, delayCancellation).SuppressCancellationThrow())
            return;
        
        gameObject.SetActive(false);
    }
    
    // 1프레임 딜레이
    private async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }
    // 일반 딜레이
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
