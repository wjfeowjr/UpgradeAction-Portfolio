using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IPopupCommonView
{
    void SetTitle(string title);
    void SetDesc(string desc);
    void SetButton(UIButtonData data);
    void SetClose(Action onClose);
}

public class PopupCommonModel
{
    public string Title;
    public string Desc;
    public UIButtonData ButtonData;
}

public class PopupCommonPresenter
{
    private readonly IPopupCommonView _view;
    private readonly PopupCommonModel _model;

    public PopupCommonPresenter(IPopupCommonView view, PopupCommonModel model)
    {
        _view = view;
        _model = model;

        _view.SetTitle(_model.Title);
        _view.SetDesc(_model.Desc);
        _view.SetButton(_model.ButtonData);
        _view.SetClose(OnClose);
    }

    private void OnClose()
    {
        //UIManager.Instance.Close(eUIType.Popup_Common);
    }
}

public class PopupCommonView : UIBase, IPopupCommonView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private UIButtonView confirmButton;
    [SerializeField] private Button closeButton;

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetDesc(string desc)
    {
        descText.text = desc;
    }

    public void SetButton(UIButtonData data)
    {
        confirmButton.SetData(data);
    }

    public void SetClose(Action onClose)
    {
        Utils.AddClickAction(closeButton, onClose);
    }
}
