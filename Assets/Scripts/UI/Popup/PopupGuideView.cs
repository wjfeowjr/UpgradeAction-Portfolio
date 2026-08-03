using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupGuideView
{
    void SetModel(string guideTitle, string guideMessage, List<string> imgNameList);
}

public class PopupGuideModel
{
    public string guideTitle;
    public string guideMessage;
    public List<string> imgNameList = new List<string>();
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

    public void Open(Action action)
    {
        action?.Invoke();
    }

    public void SetModel()
    {
        _guideView.SetModel(_model.guideTitle, _model.guideMessage, _model.imgNameList);
    }

    public void SetAction(Action action)
    {
        _model.closeAction = action;
    }

    public void Close()
    {
        if (Input.GetKeyDown(GameManager.Instance.escKey))
            _model.closeAction?.Invoke();
    }
}

public class PopupGuideView : MonoBehaviour, IPopupGuideView
{
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    private PopupGuidePresenter presenter;

    public PopupGuidePresenter Bind(PopupGuideModel model)
    {
        presenter = new PopupGuidePresenter(this, model);
        return presenter;
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text explainText;
    [SerializeField] private TMP_Text closeText;
    [SerializeField] private Image[] guideImages;

    public void SetModel(string guideTitle, string guideMessage, List<string> imgNameList)
    {
        titleText.text = guideTitle;
        explainText.text = guideMessage;
        closeText.text = string.Format(GameManager.Instance.GetTalk(30102), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey));
        
        for (int i = 0; i < guideImages.Length; i++)
        {
            if (i < imgNameList.Count)
            {
                guideImages[i].transform.parent.parent.gameObject.SetActive(true);
                guideImages[i].sprite = GameManager.Instance.GetAtlasSprite(imgNameList[i]);
            }
            else
            {
                guideImages[i].transform.parent.parent.gameObject.SetActive(false);
            }
        }
    }
}
