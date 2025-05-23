using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IUIGameOverView
{
    void SetMessage(string title, string message, Action confirmAction);
}

public class PopupGameOverModel
{
    public string title;
    public string message;
    public Action confirmAction;
}

public class PopupGameOverPresenter
{
    private IUIGameOverView _gameOverView;
    private PopupGameOverModel _model;

    public PopupGameOverPresenter(IUIGameOverView gameOverView,  PopupGameOverModel model)
    {
        _gameOverView = gameOverView;
        _model = model;
    }

    public void SetPopup()
    {
        _gameOverView.SetMessage(_model.title, _model.message, _model.confirmAction);
    }
}

public class PopupGameOverView : MonoBehaviour, IUIGameOverView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    
    public void SetMessage(string title, string message, Action confirmAction)
    {
        titleText.text = title;
        messageText.text = message;
        
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            confirmAction();
        });
    }
}
