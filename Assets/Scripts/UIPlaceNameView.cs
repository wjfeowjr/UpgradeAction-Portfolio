using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IUIPlaceNameView
{
    void SetPlaceText(string placeName);
    void HideImmediate();
}

public class UIPlaceNameModel
{
    public string placeName;
}

public class UIPlaceNamePresenter
{
    private readonly IUIPlaceNameView _placeNameView;
    private UIPlaceNameModel _model;

    public UIPlaceNamePresenter(IUIPlaceNameView placeNameView, UIPlaceNameModel model)
    {
        _placeNameView = placeNameView;
        _model         = model;
    }

    public void SetPlaceText()
    {
        _placeNameView.SetPlaceText(_model.placeName);
    }

    public void HideImmediate()
    {
        _placeNameView.HideImmediate();
    }
}

public class UIPlaceNameView : MonoBehaviour, IUIPlaceNameView
{
    private const float FadeDuration = 0.5f;
    private const float StayDuration = 1.5f;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text    placeText;

    private Sequence _sequence;

    public void SetPlaceText(string placeName)
    {
        _sequence?.Kill();

        gameObject.SetActive(true);
        placeText.text    = placeName;
        canvasGroup.alpha = 0f;

        _sequence = DOTween.Sequence().Append(canvasGroup.DOFade(1f, FadeDuration)).AppendInterval(StayDuration).Append(canvasGroup.DOFade(0f, FadeDuration));
        SoundManager.Instance.PlaySound(ConstValues.Upgrade);
    }

    public void HideImmediate()
    {
        _sequence?.Kill();
        gameObject.SetActive(false);
    }
}
