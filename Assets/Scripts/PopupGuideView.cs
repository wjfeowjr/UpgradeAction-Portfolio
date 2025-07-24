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

    public PopupGuidePresenter(IPopupGuideView guideView, PopupGuideModel model)
    {
        _guideView = guideView;
        _model = model;
    }

    public void Expansion(Action action)
    {
        action?.Invoke();
    }

    public void SetModel(string guideMessage, string imgName)
    {
        _model.guideMessage = guideMessage;
        _model.imgName = imgName;
        _guideView.SetModel(_model.guideMessage, _model.imgName);
    }
    
    public void SetAction(Action action)
    {
        _model.closeAction = action;
        _guideView.SetAction(_model.closeAction);
    }
    
    public void CloseGuide()
    {
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
        titleText.text = "가이드";
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
