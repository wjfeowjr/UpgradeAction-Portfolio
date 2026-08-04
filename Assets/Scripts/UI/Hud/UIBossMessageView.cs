using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IUIBossMessageView
{
    void SetBossMessage(string bossName, EMonsterType monsterType);
    void BossMessageProduct(Action soundAction);
}

public class UIBossMessageModel
{
    public string bossName;
    public EMonsterType monsterType;
}

public class UIBossMessagePresenter
{
    private readonly IUIBossMessageView _bossMessageview;
    private UIBossMessageModel _model;

    public UIBossMessagePresenter(IUIBossMessageView episodeView, UIBossMessageModel model)
    {
        _bossMessageview = episodeView;
        _model = model;
    }
    
    public void SetBossMessage()
    {
        _bossMessageview.SetBossMessage(_model.bossName, _model.monsterType);
    }

    public void BossMessageProduct(Action soundAction)
    {
        _bossMessageview.BossMessageProduct(soundAction);
    }
}

public class UIBossMessageView : MonoBehaviour, IUIBossMessageView
{
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    private UIBossMessagePresenter presenter;
    public UIBossMessagePresenter Presenter => presenter;

    public UIBossMessagePresenter Bind(UIBossMessageModel model)
    {
        presenter = new UIBossMessagePresenter(this, model);
        return presenter;
    }

    private CancellationTokenSource bossMessageCancellation;
    
    private float fadeTime = 0.7f;
    private float moveSecond = 0.5f;
    private float delay = 2.0f;

    [SerializeField] private Transform bossMessageTransform;
    [SerializeField] private TMP_Text bossTypeText;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform stopTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Image fadeImage;
    
    // 딜레이
    private async UniTask BossMessageDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), ignoreTimeScale: true, cancellationToken: bossMessageCancellation.Token);
    }
    
    public void SetBossMessage(string bossName, EMonsterType monsterType)
    {
        bossMessageTransform.position = startTransform.transform.position;
        switch (monsterType)
        {
            case EMonsterType.MiniBoss:
                bossTypeText.text = "Mini";
                break;
            case EMonsterType.HiddenBoss:
                bossTypeText.text = "Hidden";
                break;
        }
        bossTypeText.gameObject.SetActive(monsterType is EMonsterType.MiniBoss or EMonsterType.HiddenBoss);
        bossNameText.text = bossName;
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
    }

    public async void BossMessageProduct(Action soundAction)
    {
        GameManager.Instance.BossProduct = true;
        // 보스 연출 중 팝업이 겹쳐도 서로 어긋나지 않도록 요청 방식으로 멈춘다
        GameManager.Instance.Flow.StopTime(this);
        soundAction?.Invoke();
        bossMessageCancellation = new CancellationTokenSource();

        // 페이드 인
        fadeImage.DOFade(0.7f, fadeTime).SetEase(Ease.Linear).SetUpdate(true);
        
        // 텍스트 이동 후 정지
        soundAction?.Invoke();
        bossMessageTransform.DOMove(stopTransform.position, moveSecond).SetUpdate(true);
        if (await BossMessageDelay(moveSecond).SuppressCancellationThrow())
            return;
        
        if (await BossMessageDelay(delay).SuppressCancellationThrow())
            return;
        
        // 텍스트 화면 바깥으로 이동
        bossMessageTransform.DOMove(endTransform.position, moveSecond).SetUpdate(true);
        
        fadeImage.DOFade(0, fadeTime).SetEase(Ease.Linear).SetUpdate(true);
        if (await BossMessageDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        
        GameManager.Instance.Flow.ResumeTime(this);
        gameObject.SetActive(false);
        GameManager.Instance.BossProduct = false;
    }
}
