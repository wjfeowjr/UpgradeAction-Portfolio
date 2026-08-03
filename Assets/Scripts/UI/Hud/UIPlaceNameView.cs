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
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    // 호출부가 인터페이스 변환 -> Model 생성 -> Presenter 생성 -> 역주입을
    // 매번 반복하던 것을 한 줄로 줄인다.
    private UIPlaceNamePresenter presenter;
    public UIPlaceNamePresenter Presenter => presenter;

    public UIPlaceNamePresenter Bind(UIPlaceNameModel model)
    {
        presenter = new UIPlaceNamePresenter(this, model);
        return presenter;
    }

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
