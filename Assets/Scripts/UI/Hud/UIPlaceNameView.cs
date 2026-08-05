using DG.Tweening;
using TMPro;
using UnityEngine;

// 표시할 값 묶음. 로직은 없다.
public class UIPlaceNameModel
{
    public string placeName;
}

// 받은 값을 그리기만 한다.
// 무엇을 그릴지 판단하는 부분이 없어 Presenter 를 두지 않았다.
public class UIPlaceNameView : MonoBehaviour
{
    private const float FadeDuration = 0.5f;
    private const float StayDuration = 1.5f;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text    placeText;

    private Sequence _sequence;

    public void SetPlaceText(UIPlaceNameModel model)
    {
        SetPlaceText(model.placeName);
    }

    private void SetPlaceText(string placeName)
    {
        _sequence?.Kill();

        gameObject.SetActive(true);
        placeText.text    = placeName;
        canvasGroup.alpha = 0f;

        _sequence = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, FadeDuration))
            .AppendInterval(StayDuration)
            .Append(canvasGroup.DOFade(0f, FadeDuration))
            .SetUpdate(true);
        SoundManager.Instance.PlaySound(ConstValues.Upgrade);
    }

    public void HideImmediate()
    {
        _sequence?.Kill();
        gameObject.SetActive(false);
    }
}
