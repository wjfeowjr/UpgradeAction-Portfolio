using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UIBase : MonoBehaviour
{
    [SerializeField] private eUIType uiType;
    [SerializeField] private GameObject uiObject;
    [SerializeField] private CanvasGroup canvasGroup;
    protected bool openComplete;

    // 외부(자식 뷰 등)에서 열림 완료 여부를 확인할 때 사용 (마우스 입력 게이트 등)
    public bool OpenComplete => openComplete;

    private void OnEnable()
    {
        openComplete = false;
    }

    public eUIType GetUIType()
    {
        return uiType;
    }
    public void Setup(eUIType type)
    {
        uiType = type;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
    public async UniTask ExpansionOpen(bool timeStop, bool controlStop)
    {
        // 이 팝업이 정지를 '요청'한다. 위에 다른 팝업이 겹쳐도 서로 어긋나지 않는다.
        if (timeStop)
            GameManager.Instance.Flow.StopTime(this);

        PlaySound(ConstValues.Popup, true);
        uiObject.transform.localScale = Vector3.zero;
        var endVector = Vector3.one;
        var time = 0.2f;
        uiObject.transform.DOScale(endVector, time).SetUpdate(true);
        if (controlStop)
            GameManager.Instance.LockControl(this);
        
        await UniTask.Delay(TimeSpan.FromSeconds(time), ignoreTimeScale: true);
        openComplete = true;
    }
    public async UniTask ReductionClose(bool timeReset, bool controlStart)
    {
        PlaySound(ConstValues.NormalButton2, true);
        var endVector = Vector3.zero;
        var time = 0.2f;
        uiObject.transform.DOScale(endVector, time).SetUpdate(true);
        await UniTask.Delay(TimeSpan.FromSeconds(time), ignoreTimeScale: true);
        // 내 요청만 푼다. 아래에 다른 팝업이 남아 있으면 계속 멈춰 있다.
        if (timeReset)
            GameManager.Instance.Flow.ResumeTime(this);
        gameObject.SetActive(false);
        openComplete = false;
        
        if (controlStart)
            GameManager.Instance.UnlockControl(this);
    }
    public virtual async UniTask FadeOpen(bool timeStop, bool controlStop, float time, bool fadeSound = true)
    {
        if (timeStop)
            GameManager.Instance.Flow.StopTime(this);

        if(fadeSound)
            PlaySound(ConstValues.Upgrade, true);

        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, time).SetUpdate(true);
        if (controlStop)
            GameManager.Instance.LockControl(this);
        
        await UniTask.Delay(TimeSpan.FromSeconds(time), ignoreTimeScale: true);
        openComplete = true;
    }
    public async UniTask FadeClose(bool timeReset, bool controlStart, float time, bool fadeSound = false)
    {
        if(fadeSound)
            PlaySound(ConstValues.Upgrade, true);
        
        canvasGroup.DOFade(0, time).SetUpdate(true);
        await UniTask.Delay(TimeSpan.FromSeconds(time), ignoreTimeScale: true);
        if (timeReset)
            GameManager.Instance.Flow.ResumeTime(this);

        gameObject.SetActive(false);
        openComplete = false;

        if (controlStart)
            GameManager.Instance.UnlockControl(this);
    }

    private void PlaySound(string soundId, bool ignoreTime)
    {
        SoundManager.Instance.PlaySound(soundId, ignoreTime);
    }
}
