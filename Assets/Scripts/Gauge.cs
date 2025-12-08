using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gauge : MonoBehaviour
{
    private CancellationTokenSource cancellationToken;
    [SerializeField] protected Slider mainGauge;
    [SerializeField] protected Slider reduceGauge;
    [SerializeField] protected TextMeshProUGUI gaugeText;

    // 딜레이
    private async UniTask GaugeDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: cancellationToken.Token);
    }
    private async UniTask YieldDelay()
    {
        await UniTask.Yield(cancellationToken: cancellationToken.Token);
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
        // if(mainGauge) 
        //     mainGauge.value = 1;
        if(reduceGauge) 
            reduceGauge.value = 1;
    }
    // 게이지 해당 비율로 즉시 고정
    public void GaugeSetting(float currentValue, float maxValue, string text = default)
    {
        if (mainGauge)
        {
            if (maxValue > 0)
            {
                mainGauge.value = currentValue / maxValue;
            }
        }

        if (reduceGauge)
            reduceGauge.value = mainGauge.value;
        
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
        reduceGauge.value = 0;

        if (mainGauge)
        {
            CancelDelay();
            cancellationToken = new CancellationTokenSource();
            while (mainGauge.value < fillArrive)
            {
                if(mainGauge) 
                    mainGauge.value += speed * Time.deltaTime;
                if (await YieldDelay().SuppressCancellationThrow())
                    return;
            }
            mainGauge.value = fillArrive;
        }
    }
    
    public async void GaugeReduce(float currentValue, float maxValue, float speed)
    {
        if(!mainGauge)
            return;
        
        mainGauge.value = currentValue / maxValue;
        
        if (!(currentValue > 0))
            return;
        
        CancelDelay();
        cancellationToken = new CancellationTokenSource();
        
        if (await GaugeDelay(ConstValues.GaugeReduce).SuppressCancellationThrow())
            return;

        while (mainGauge.value < reduceGauge.value)
        {
            if(reduceGauge) 
                reduceGauge.value -=speed * Time.deltaTime;
            if (await YieldDelay().SuppressCancellationThrow())
                return;
        }
        reduceGauge.value = mainGauge.value;
    }
}
