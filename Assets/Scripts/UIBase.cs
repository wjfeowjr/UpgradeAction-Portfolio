using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UIBase : MonoBehaviour
{
    [SerializeField] private eUIType uiType;
    [SerializeField] private GameObject uiObject;
    
    public eUIType GetUIType()
    {
        return uiType;
    }
    public void Setup(eUIType type)
    {
        uiType = type;
    }

    public async void Close()
    {
        gameObject.SetActive(false);
    }
    public async void ExpansionOpen(bool timeStop, bool controlStop)
    {
        if (timeStop)
            Time.timeScale = 0;

        PlaySound(ConstValues.Popup);
        uiObject.transform.localScale = Vector3.zero;
        var endVector = Vector3.one;
        var time = 0.2f;
        uiObject.transform.DOScale(endVector, time).SetUpdate(true);
        
        if(controlStop)
            GameManager.Instance.ControlStart = false;
    }
    public async void ReductionClose(bool timeReset, bool controlStart)
    {
        PlaySound(ConstValues.NormalButton2);
        var endVector = Vector3.zero;
        var time = 0.2f;
        uiObject.transform.DOScale(endVector, time).SetUpdate(true);
        await UniTask.Delay(TimeSpan.FromSeconds(time), ignoreTimeScale: true);
        if (timeReset)
            Time.timeScale = 1;
        gameObject.SetActive(false);
        
        if(controlStart)
            GameManager.Instance.ControlStart = true;
    }

    private void PlaySound(string soundId)
    {
        SoundManager.Instance.PlaySound(soundId);
    }
}
