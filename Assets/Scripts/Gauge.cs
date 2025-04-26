using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gauge : MonoBehaviour
{
    private CancellationTokenSource cancellationToken;
    [SerializeField] protected Image emptyGauge;
    [SerializeField] protected Image mainGauge;
    [SerializeField] protected Image reduceGauge;
    [SerializeField] protected TextMeshProUGUI gaugeText;

    // private void Start()
    // {
    //     if(emptyGauge)
    //         emptyGauge.color = new Color(emptyGauge.color.r, emptyGauge.color.g, emptyGauge.color.b, 0);
    //     if(mainGauge)
    //         mainGauge.color = new Color(mainGauge.color.r, mainGauge.color.g, mainGauge.color.b, 0);
    //     if(reduceGauge)
    //         reduceGauge.color = new Color(mainGauge.color.r, mainGauge.color.g, mainGauge.color.b, 0);
    // }
    
    // 딜레이
    private async UniTask GaugeDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: cancellationToken.Token);
    }
    private void CancelDelay()
    {
        cancellationToken?.Cancel();
    }
    
    public void DisplayPercent(Character character)
    {
        if (!gaugeText)
            return;
        
        if (character)
        {
            // 나눗셈을 할때 분모가 0이면 안된다
            float hpPercent = default;
            if(character.BasicStat.maxHp != 0)
                hpPercent = (float)character.BasicStat.hp / character.BasicStat.maxHp * 100;
            
            GaugeTextInput($"{(int)hpPercent}%");
            // 살아있을 때 최솟값은 항상 1%로
            if (hpPercent is > 0 and < 1)
                GaugeTextInput("1%");
        }
    }
    public void DisplayValue(Character character)
    {
        if (!gaugeText)
            return;
        
        if (character)
            GaugeTextInput($"{character.BasicStat.hp}/{character.BasicStat.maxHp}");
    }

    // 게이치 풀로 채우기
    public void GaugeMax()
    {
        if(mainGauge) 
            mainGauge.fillAmount = 1;
        if(reduceGauge) 
            reduceGauge.fillAmount = 1;
    }
    // 게이치 해당 비율로 즉시 고정
    public void GaugeSetting(float currentValue, float maxValue, string text = default)
    {
        if (mainGauge)
        {
            if (maxValue > 0)
            {
                mainGauge.fillAmount = currentValue / maxValue;
            }
        }

        if (reduceGauge)
            reduceGauge.fillAmount = mainGauge.fillAmount;
        
        if (text != default)
            GaugeTextInput(text);
    }
    protected void GaugeTextInput(string text)
    {
        if (gaugeText)
            gaugeText.text = text;
    }
    
    public async UniTaskVoid GaugeFill(float currentValue, float maxValue, float speed)
    {
        float fillArrive = currentValue / maxValue;
        reduceGauge.fillAmount = 0;

        if (mainGauge)
        {
            CancelDelay();
            cancellationToken = new CancellationTokenSource();
            while (mainGauge.fillAmount < fillArrive)
            {
                if(mainGauge) 
                    mainGauge.fillAmount += ConstValues.GaugeFillSpeed * speed;
                if (await GaugeDelay(ConstValues.GaugeFillSpeed).SuppressCancellationThrow())
                    return;
            }
            mainGauge.fillAmount = fillArrive;
        }
    }
    
    public async void GaugeReduce(float currentValue, float maxValue, float speed)
    {
        if(!mainGauge)
            return;
        
        mainGauge.fillAmount = currentValue / maxValue;
        
        if (!(currentValue > 0))
            return;
        
        CancelDelay();
        cancellationToken = new CancellationTokenSource();
        
        if (await GaugeDelay(ConstValues.GaugeReduce).SuppressCancellationThrow())
            return;

        while (mainGauge.fillAmount < reduceGauge.fillAmount)
        {
            if(reduceGauge) 
                reduceGauge.fillAmount -= ConstValues.GaugeFillSpeed * speed;
            if (await GaugeDelay(ConstValues.GaugeFillSpeed).SuppressCancellationThrow())
                return;
        }
        reduceGauge.fillAmount = mainGauge.fillAmount;
    }
}
