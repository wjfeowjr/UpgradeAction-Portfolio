using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IPopupCharacterView
{
    void SetModel(string playerId);
    void SetPlayerInfo(); // 정보 갱신용 추가
    void SetAction(PopupCommonActions commonActions);
}

public class PopupCharacterModel
{
    public string playerId;
    public PopupCommonActions commonActions; // 공통 액션 포함
}

public class PopupCharacterPresenter
{
    private readonly IPopupCharacterView _view;
    private readonly PopupCharacterModel _model;

    public PopupCharacterPresenter(IPopupCharacterView view, PopupCharacterModel model)
    {
        _view = view;
        _model = model;
    }

    public void SetModel() => _view.SetModel(_model.playerId);
    public void UpdatePlayerInfo(string newId) 
    {
        _model.playerId = newId;
        _view.SetModel(newId);
        _view.SetAction(_model.commonActions);
        _view.SetPlayerInfo(); 
    }
}

public class PopupCharacterView : MonoBehaviour, IPopupCharacterView
{
    private PopupCommonActions _actions;
    private string curPlayerId;

    [SerializeField] private TMP_Text curPlayerText;

    public void SetModel(string playerId)
    {
        curPlayerId = playerId;
        curPlayerText.text = curPlayerId;
    }
    
    public void SetAction(PopupCommonActions actions) => _actions = actions;

    public void SetPlayerInfo()
    {
        
    }
}
