using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupGuideView
{
    void SetModel(string guideMessage, string imgName);
    void SetAction(Action closeAction);
}

public class PopupGuideModel
{
    public string guideMessage;
    public string imgName;
    public Action closeAction;
}

public class PopupGuidePresenter
{
    private IPopupGuideView _guideView;
    private PopupGuideModel _model;
    // 가이드 팝업이 스폰된 프레임의 잔여 입력(GetKeyDown)을 무시하기 위한 플래그
    private bool _inputReady;

    public PopupGuidePresenter(IPopupGuideView guideView, PopupGuideModel model)
    {
        _guideView = guideView;
        _model = model;
        _inputReady = false;
    }

    public void Expansion(Action action)
    {
        action?.Invoke();
    }

    public void SetModel()
    {
        _guideView.SetModel(_model.guideMessage, _model.imgName);
    }

    public void SetAction(Action action)
    {
        _model.closeAction = action;
        _guideView.SetAction(_model.closeAction);
    }

    public void CloseGuide()
    {
        // 스폰된 프레임에서는 입력을 받지 않고 한 프레임 뒤부터 감지
        if (!_inputReady)
        {
            _inputReady = true;
            return;
        }

        if (Input.GetKeyDown(GameManager.Instance.escKey))
            _model.closeAction?.Invoke();
    }
}

public class PopupGuideView : MonoBehaviour, IPopupGuideView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text explainText;
    [SerializeField] private Image guideImage;
    [SerializeField] private Button closeButton;

    public void SetModel(string guideMessage, string imgName)
    {
        titleText.text = GameManager.Instance.GetTalk(30010);
        explainText.text = guideMessage;
        guideImage.sprite = GameManager.Instance.GetAtlasSprite(imgName);
    }
    
    public void SetAction(Action closeAction)
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            closeAction();
        });
    }
}
