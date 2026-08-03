using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupGameOverView
{
    void SetModel(string title, string message);
    void BgActive(bool active);
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
    private bool _isRestarted;

    public PopupGameOverPresenter(IPopupGameOverView gameOverView,  PopupGameOverModel model)
    {
        _gameOverView = gameOverView;
        _model = model;
    }
    
    public void Open(Action action)
    {
        action?.Invoke();
    }

    public void SetModel()
    {
        _gameOverView.SetModel(_model.title, _model.message);
    }

    public void BgActive(bool active)
    {
        _gameOverView.BgActive(active);
    }
    
    public void Restart()
    {
        if (_isRestarted)
            return;

        if (Input.GetKeyDown(GameManager.Instance.enterKey))
        {
            _isRestarted = true;
            _model.replayAction?.Invoke();
        }
    }
}

public class PopupGameOverView : MonoBehaviour, IPopupGameOverView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject bgObject;
    [SerializeField] private GameObject[] playerObjects;

    public void SetModel(string title, string message)
    {
        titleText.text = title;
        messageText.text = message;
        
        for (int i = 0; i < playerObjects.Length; i++)
            playerObjects[i].SetActive(i < GameManager.Instance.PlayerList.Count);
    }

    public void BgActive(bool active)
    {
        bgObject.SetActive(active);
    }
}
