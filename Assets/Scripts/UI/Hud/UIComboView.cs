using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 표시할 값 묶음. 로직은 없다.
public class UIComboModel
{
    public int comboCount;
}

// 받은 값을 그리기만 한다.
// 무엇을 그릴지 판단하는 부분이 없어 Presenter 를 두지 않았다.
public class UIComboView : MonoBehaviour
{
    private CancellationTokenSource comboCancellation;
    private float expansionTime = 0.02f;
    private float reduceTime = 0.4f;
    private float delay = 2.0f;
    private float fadeTime = 0.1f;

    [SerializeField] private TMP_Text comboText;

    private void OnDisable()
    {
        comboText.gameObject.SetActive(false);
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

    public void ComboProduct()
    {
        ComboProduct(GameManager.Instance.ComboCount);
    }

    private async void ComboProduct(int comboCount)
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

        // 연출이 끝나면 콤보 카운트를 초기화한다.
        // 이전에는 View 가 이벤트를 쏘고 Presenter 가 받아서 처리했다.
        GameManager.Instance.ComboCount = 0;
    }
}
