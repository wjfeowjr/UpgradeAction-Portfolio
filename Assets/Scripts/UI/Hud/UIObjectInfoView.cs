using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IUIObjectInfoView
{
    void SetObjectText(string id, string objectName, int count);
    void HideImmediate();
}

public class UIObjectInfoModel
{
    public string id;
    public string objectName;
    public int count;
}

public class UIObjectInfoPresenter
{
    private readonly IUIObjectInfoView _getObjectInfoView;
    private UIObjectInfoModel _model;

    public UIObjectInfoPresenter(IUIObjectInfoView getObjectInfoView, UIObjectInfoModel model)
    {
        _getObjectInfoView = getObjectInfoView;
        _model         = model;
    }

    public void SetObjectText()
    {
        _getObjectInfoView.SetObjectText(_model.id, _model.objectName, _model.count);
    }

    public void HideImmediate()
    {
        _getObjectInfoView.HideImmediate();
    }
}

public class UIObjectInfoView : MonoBehaviour, IUIObjectInfoView
{
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    // 호출부가 인터페이스 변환 -> Model 생성 -> Presenter 생성 -> 역주입을
    // 매번 반복하던 것을 한 줄로 줄인다.
    private UIObjectInfoPresenter presenter;
    public UIObjectInfoPresenter Presenter => presenter;

    public UIObjectInfoPresenter Bind(UIObjectInfoModel model)
    {
        presenter = new UIObjectInfoPresenter(this, model);
        return presenter;
    }

    private const float FadeDuration = 0.5f;
    private const float StayDuration = 1.5f;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text objectText;

    private Sequence _sequence;

    public void SetObjectText(string id, string objectName, int count)
    {
        _sequence?.Kill();

        gameObject.SetActive(true);

        image.sprite = GameManager.Instance.GetAtlasSprite(id);
        
        if(count <= 1)
            objectText.text = objectName;
        else
            objectText.text = $"{objectName} x {count}";
        
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
