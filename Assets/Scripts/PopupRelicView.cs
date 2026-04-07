using System;
using UnityEngine;

public interface IPopupRelicView
{
    void SetModel(string playerId);
    void SetPlayerInfo();
    void SetAction(PopupCommonActions commonActions, Action closeAction);
}
 
public class PopupRelicModel
{
    public string playerId;
    public PopupCommonActions commonActions;
    public Action closeAction;
}
 
public class PopupRelicPresenter
{
    private readonly IPopupRelicView _view;
    private readonly PopupRelicModel _model;
 
    public PopupRelicPresenter(IPopupRelicView view, PopupRelicModel model)
    {
        _view = view;
        _model = model;
    }
    
    public void UpdatePlayerInfo(string newId)
    {
        _model.playerId = newId;
        _view.SetModel(newId);
        _view.SetAction(_model.commonActions, _model.closeAction);
        _view.SetPlayerInfo();
    }
}

public class PopupRelicView : MonoBehaviour, IPopupRelicView
{
    private PopupCommonActions _actions;
    private Action _closeAction;
    
    private string curPlayerId;

    private void Update()
    {
        UpdateRelicSelect();
    }

    public void SetModel(string playerId)
    {
        
    }
 
    public void SetAction(PopupCommonActions commonActions, Action closeAction)
    {
        _actions = commonActions;
        _closeAction = closeAction;
    }
 
    public void SetPlayerInfo()
    {
        
    }
    
    private void UpdateRelicSelect()
    { 
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAction();
            _actions?.PlayMoveSound?.Invoke();
        }
    }
    
    public void CloseAction()
    {
        if (_closeAction == null)
            return;
        
        _closeAction.Invoke();
    }
}
