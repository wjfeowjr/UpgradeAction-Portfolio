using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
 
public interface IPopupCharacterView
{
    void SetModel(string playerId);
    void SetPlayerInfo();
    void SetAction(PopupCommonActions commonActions);
}
 
public class PopupCharacterModel
{
    public string playerId;
    public PopupCommonActions commonActions;
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
    private PopupCommonActions commonActions;
    private string curPlayerId;
 
    [SerializeField] private TMP_Text curPlayerText;
    [SerializeField] private TMP_Text[] statNameTexts;
    [SerializeField] private TMP_Text[] statTexts;
    
    [SerializeField] private ExpansionUiObject[] expansionObjects;
 
    // 팝업 선택 순서: Attribute → Relic
    private readonly ePopupState[] _popupStateOrder = { ePopupState.Attribute, ePopupState.Relic };
    private int _selectedIndex = 0;
 
    // 엔터 입력 시 Popup_Character로 선택된 상태를 전달하는 콜백
    private Action<ePopupState> _onStateSelected;
 
    public void SetModel(string playerId)
    {
        curPlayerId = playerId;
    }

    public void SetAction(PopupCommonActions common)
    {
        commonActions = common;
    }
 
    public void SetPlayerInfo()
    {
        curPlayerText.text = curPlayerId;
        
        var curPlayer = GameManager.Instance.GetPlayer(curPlayerId);
        int txtIdx = 50100;
        for (int i = 0; i < statNameTexts.Length; i++)
        {
            statNameTexts[i].text = GameManager.Instance.GetTalk(txtIdx + i);
            
            switch (i)
            {
                case 0:
                    statTexts[i].text = $"{curPlayer.BasicStat.hp} / {curPlayer.BasicStat.maxHp}";
                    break;
                case 1:
                    statTexts[i].text = curPlayer.BasicStat.power.ToString();
                    break;
                case 2:
                    statTexts[i].text = curPlayer.BasicStat.defence.ToString();
                    break;
                case 3:
                    statTexts[i].text = curPlayer.BasicStat.moveSpeed.ToString(CultureInfo.InvariantCulture);
                    break;
                case 4:
                    statTexts[i].text = curPlayer.BasicStat.attackSpeed.ToString(CultureInfo.InvariantCulture);
                    break;
                case 5:
                    statTexts[i].text = $"{curPlayer.BasicStat.criticalChance}%";
                    break;
                case 6:
                    statTexts[i].text = $"{curPlayer.BasicStat.criticalDamage}%";
                    break;
            }
        }
    }
 
    // Popup_Character에서 초기화 시 호출, 콜백 등록 및 첫 번째 항목 선택 상태로 초기화
    public void InitExpansionSelection(Action<ePopupState> onStateSelected)
    {
        _onStateSelected = onStateSelected;
        _selectedIndex = 0;
        RefreshExpansionSelection();
    }
 
    private void Update()
    {
        if (expansionObjects == null || expansionObjects.Length == 0)
            return;
 
        // 위 방향키: 이전 항목 선택 (사이클)
        if (Input.GetKeyDown(GameManager.Instance.upKey))
        {
            _selectedIndex = (_selectedIndex - 1 + expansionObjects.Length) % expansionObjects.Length;
            RefreshExpansionSelection();
            commonActions?.PlayMoveSound?.Invoke();
        }
 
        // 아래 방향키: 다음 항목 선택 (사이클)
        if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            _selectedIndex = (_selectedIndex + 1) % expansionObjects.Length;
            RefreshExpansionSelection();
            commonActions?.PlayMoveSound?.Invoke();
        }
 
        // 엔터: 현재 선택된 항목의 팝업 상태를 콜백으로 전달
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (_selectedIndex >= 0 && _selectedIndex < _popupStateOrder.Length)
                _onStateSelected?.Invoke(_popupStateOrder[_selectedIndex]);
            commonActions?.PlayCancelSound?.Invoke();
        }
    }
 
    // 선택 인덱스 기준으로 전체 expansionObjects 선택/비선택 상태 갱신
    private void RefreshExpansionSelection()
    {
        for (int i = 0; i < expansionObjects.Length; i++)
        {
            if (i == _selectedIndex)
            {
                expansionObjects[i].Expansion(1.1f);
                expansionObjects[i].SelectObjectActive(true);
            }
            else
            {
                expansionObjects[i].Reduction();
                expansionObjects[i].SelectObjectActive(false);
            }
        }
    }
}