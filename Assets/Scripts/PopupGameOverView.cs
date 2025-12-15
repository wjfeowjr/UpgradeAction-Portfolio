using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupGameOverView
{
    void SetModel(string title, string message, Action confirmAction);
}

public class PopupGameOverModel
{
    public string title;
    public string message;
    public Action replayAction;
}

public class PopupGameOverPresenter
{
    private IPopupGameOverView _gameOverView;
    private PopupGameOverModel _model;

    public PopupGameOverPresenter(IPopupGameOverView gameOverView,  PopupGameOverModel model)
    {
        _gameOverView = gameOverView;
        _model = model;
    }

    public void SetModel()
    {
        _gameOverView.SetModel(_model.title, _model.message, _model.replayAction);
    }
    
    public void Restart()
    {
        if (Input.GetKeyDown(GameManager.Instance.spaceKey))
            _model.replayAction?.Invoke();
    }
}

public class PopupGameOverView : MonoBehaviour, IPopupGameOverView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    private Action action;
    
    public void SetModel(string title, string message, Action confirmAction)
    {
        titleText.text = title;
        messageText.text = message;
        
        action = confirmAction;
        
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            action();
        });
    }
}
