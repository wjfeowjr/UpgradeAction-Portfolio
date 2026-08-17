using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PopupCharacterModel
{
    public string playerId;
    public PopupCommonActions commonActions;
}

public class PopupCharacterView : MonoBehaviour
{
    private PopupCharacterModel _model;

    public void SetData(PopupCharacterModel model) => _model = model;

    public void UpdatePlayerInfo(string newId)
    {
        _model.playerId = newId;
        SetModel(newId);
        SetAction(_model.commonActions);
        SetPlayerInfo();
    }

    private PopupCommonActions commonActions;
    private string curPlayerId;
 
    [SerializeField] private TMP_Text selectKeyUpText;
    [SerializeField] private TMP_Text selectKeyDownText;
    [SerializeField] private TMP_Text curPlayerText;
    [SerializeField] private TMP_Text[] statNameTexts;
    [SerializeField] private TMP_Text[] statTexts;
    
    [SerializeField] private ChoiceFrameUI[] choiceFrameObjects;
 
    // 팝업 선택 순서: SkillInfo → Attribute → Relic → Item
    private readonly ePopupState[] _popupStateOrder = { ePopupState.SkillInfo, ePopupState.Attribute, ePopupState.Relic, ePopupState.Item };
    private int _selectedIndex = 0;
 
    // 엔터 입력 시 Popup_Character로 선택된 상태를 전달하는 콜백
    private Action<ePopupState> _onStateSelected;
 
    private void SetModel(string playerId)
    {
        curPlayerId = playerId;
    }

    private void SetAction(PopupCommonActions common)
    {
        commonActions = common;
    }
 
    private void SetPlayerInfo()
    {
        selectKeyUpText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.upKey);
        selectKeyDownText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.downKey);
        curPlayerText.text = GameManager.Instance.GetCharacterTalk(curPlayerId);
        
        // 순서: SkillInfo → Attribute → Relic → Item
        if(choiceFrameObjects.Length > 0)
            choiceFrameObjects[0].SetText(GameManager.Instance.GetTalk(30065)); // TODO: Talk.json id 추가 후 GetTalk으로 교체
        if(choiceFrameObjects.Length > 1)
            choiceFrameObjects[1].SetText(GameManager.Instance.GetTalk(30058));
        if(choiceFrameObjects.Length > 2)
            choiceFrameObjects[2].SetText(GameManager.Instance.GetTalk(30059));
        if(choiceFrameObjects.Length > 3)
            choiceFrameObjects[3].SetText(GameManager.Instance.GetTalk(30064));
        
        var curPlayer = GameManager.Instance.GetPlayer(curPlayerId);
        int txtIdx = 50100;
        for (int i = 0; i < statNameTexts.Length; i++)
        {
            statNameTexts[i].text = GameManager.Instance.GetTalk(txtIdx + i);
            
            switch (i)
            {
                case 0:
                    statTexts[i].SetText("{0} / {1}", curPlayer.BasicStat.hp, curPlayer.BasicStat.maxHp);
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
        if (choiceFrameObjects == null || choiceFrameObjects.Length == 0)
            return;
 
        // 위 방향키: 이전 항목 선택 (사이클)
        if (Input.GetKeyDown(GameManager.Instance.upKey))
        {
            _selectedIndex = (_selectedIndex - 1 + choiceFrameObjects.Length) % choiceFrameObjects.Length;
            RefreshExpansionSelection();
            commonActions?.PlayMoveSound?.Invoke();
        }
 
        // 아래 방향키: 다음 항목 선택 (사이클)
        if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            _selectedIndex = (_selectedIndex + 1) % choiceFrameObjects.Length;
            RefreshExpansionSelection();
            commonActions?.PlayMoveSound?.Invoke();
        }
 
        // 엔터: 현재 선택된 항목의 팝업 상태를 콜백으로 전달
        if (InputHelper.GetEnterDown())
        {
            if (_selectedIndex >= 0 && _selectedIndex < _popupStateOrder.Length)
                _onStateSelected?.Invoke(_popupStateOrder[_selectedIndex]);
            commonActions?.PlayCancelSound?.Invoke();
        }
    }
 
    // 선택 인덱스 기준으로 전체 expansionObjects 선택/비선택 상태 갱신
    private void RefreshExpansionSelection()
    {
        for (int i = 0; i < choiceFrameObjects.Length; i++)
        {
            if (i == _selectedIndex)
            {
                choiceFrameObjects[i].Expansion(1.1f);
                choiceFrameObjects[i].SelectObjectActive(true);
            }
            else
            {
                choiceFrameObjects[i].Reduction();
                choiceFrameObjects[i].SelectObjectActive(false);
            }
        }
    }
}