using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IUIComboView
{
    void SetCombo();
    void ComboProduct(int comboCount);
    event Action OnComboEnd;
}

public class UIComboModel
{
    public int comboCount;
}

public class UIComboPresenter
{
    private readonly IUIComboView _comboview;
    private UIComboModel _model;

    public UIComboPresenter(IUIComboView hpView, UIComboModel model)
    {
        _comboview = hpView;
        _model = model;
        _comboview.OnComboEnd += OnComboEndHandler;
    }
    
    public void SetCombo()
    {
        _comboview.SetCombo();
    }

    public void ComboProduct()
    {
        _model.comboCount = GameManager.Instance.ComboCount;
        _comboview.ComboProduct(_model.comboCount);
    }
    
    private void OnComboEndHandler()
    {
        GameManager.Instance.ComboCount = 0;
    }
}

public class UIComboView : MonoBehaviour, IUIComboView
{
    private CancellationTokenSource comboCancellation;
    private float expansionTime = 0.02f;
    private float reduceTime = 0.4f;
    private float delay = 2.0f;
    private float fadeTime = 0.1f;
    
    public event Action OnComboEnd;
    [SerializeField] private TMP_Text comboText;

    private void OnDisable()
    {
        comboCancellation?.Cancel();
    }

    // 딜레이
    private async UniTask ComboDelay(float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: comboCancellation.Token);
    }

    public void SetCombo()
    {
        comboText.gameObject.SetActive(false);
    }
    
    public async void ComboProduct(int comboCount)
    {
        comboCancellation?.Cancel();
        comboCancellation = new CancellationTokenSource();
        comboText.gameObject.SetActive(true);
        
        comboText.text = $"{comboCount} {ConstValues.Combo}!";
        comboText.DOPause();
        comboText.transform.DOPause();
        
        comboText.color = ConstValues.WhiteColor;
        comboText.transform.localScale = Vector3.one;
        comboText.gameObject.SetActive(true);
        
        comboText.transform.DOScale(new Vector3(2.0f, 2.0f, 2.0f), expansionTime).SetEase(Ease.Linear);
        if (await ComboDelay(expansionTime).SuppressCancellationThrow())
            return;
        
        comboText.transform.DOScale(new Vector3(1, 1, 1), reduceTime);
        if (await ComboDelay(reduceTime).SuppressCancellationThrow())
            return;
        
        if (await ComboDelay(delay).SuppressCancellationThrow())
            return;
        
        comboText.DOFade(0, fadeTime).SetEase(Ease.Linear);
        if (await ComboDelay(fadeTime).SuppressCancellationThrow())
            return;
        
        comboText.gameObject.SetActive(false);
        OnComboEnd?.Invoke();
    }
}
