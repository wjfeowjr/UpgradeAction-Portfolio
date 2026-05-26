using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupCommonActions
{
    public Action PlayMoveSound;
    public Action PlaySelectSound;
    public Action PlayCancelSound;
}

[Serializable]
public enum ePopupState
{
    Character = 0,
    Attribute = 1,
    Relic = 2,
    Item = 3,
}

public class Popup_Character : UIBase
{
    [SerializeField] private ePopupState popupState;
    [SerializeField] private PopupCharacterView characterView;
    [SerializeField] private PopupAttributeView attributeView;
    [SerializeField] private PopupRelicView relicView;
    [SerializeField] private PopupItemView itemView;
    
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private TMP_Text selectText;
    [SerializeField] private TMP_Text backText;
    [SerializeField] private TMP_Text leftKeyText;
    [SerializeField] private TMP_Text rightKeyText;
    
    [SerializeField] private GameObject[] playerObjects;
    [SerializeField] private GameObject berserkerObject;
    [SerializeField] private GameObject gunnerObject;
    [SerializeField] private GameObject fighterObject;
    
    [SerializeField] private ExpansionUiObject berserkerFrame;
    [SerializeField] private ExpansionUiObject gunnerFrame;
    [SerializeField] private ExpansionUiObject fighterFrame;

    protected List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

    private PopupCharacterPresenter _characterPresenter;
    private PopupAttributePresenter _attributePresenter;
    private PopupRelicPresenter     _relicPresenter;
    private PopupItemPresenter      _itemPresenter;

    private string curPlayerId;
    private Action closeAction;
    private PopupCommonActions commonActions;

    public void InitPresenters(string initialPlayerId, Action close)
    {
        curPlayerId = initialPlayerId;
        selectText.text = string.Format(GameManager.Instance.GetTalk(30103), GameManager.Instance.GetKeyCode(GameManager.Instance.confirmKey));
        backText.text = string.Format(GameManager.Instance.GetTalk(30104), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey));
        leftKeyText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.changeCharacterLeftKey);
        rightKeyText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.changeCharacterRightKey);

        berserkerFrame.SetText(GameManager.Instance.GetTalk(50000));
        gunnerFrame.SetText(GameManager.Instance.GetTalk(50001));
        fighterFrame.SetText(GameManager.Instance.GetTalk(50002));

        berserkerFrame.gameObject.SetActive(GameManager.Instance.PlayerList.Contains(ConstValues.Berserker));
        gunnerFrame.gameObject.SetActive(GameManager.Instance.PlayerList.Contains(ConstValues.Gunner));
        fighterFrame.gameObject.SetActive(GameManager.Instance.PlayerList.Contains(ConstValues.Fighter));

        playerInfoList = GameManager.Instance.PlayerInfoList;

        closeAction = close;

        var common = new PopupCommonActions
        {
            PlayMoveSound   = () => SoundManager.Instance.PlaySound(ConstValues.Jump1,         true),
            PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2,  true),
            PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton,   true),
        };
        commonActions = common;

        var charModel = new PopupCharacterModel
        {
            playerId      = curPlayerId,
            commonActions = common
        };
        _characterPresenter = new PopupCharacterPresenter(characterView, charModel);

        var attrModel = new PopupAttributeModel
        {
            playerId       = curPlayerId,
            skillDataList  = TableManager.Instance.skillTable.Skill,
            playerInfoList = playerInfoList,
            commonActions  = common,
            popupAction    = GameManager.Instance.SpawnSelect,
            closeAction    = () => SetState(ePopupState.Character),
        };
        _attributePresenter = new PopupAttributePresenter(attributeView, attrModel);

        var relicModel = new PopupRelicModel
        {
            playerId       = curPlayerId,
            playerInfoList = playerInfoList,
            commonActions  = common,
            closeAction    = () => SetState(ePopupState.Character),
            // Q/E 캐릭터 변경: dir +1(E) / -1(Q)
            changeCharAction = (dir) =>
            {
                ChangeModel(dir);
                RefreshAll();
            },
        };
        _relicPresenter = new PopupRelicPresenter(relicView, relicModel);

        var itemModel = new PopupItemModel
        {
            itemList      = GameManager.Instance.ItemList,
            commonActions = common,
            closeAction   = () => SetState(ePopupState.Character),
        };
        _itemPresenter = new PopupItemPresenter(itemView, itemModel);

        SetState(ePopupState.Character);
        RefreshAll();

        characterView.InitExpansionSelection(OnExpansionStateSelected);
    }

    private async void Update()
    {
        // Relic 상태가 아닐 때만 Q/E로 캐릭터 변경 처리
        // (Relic 상태의 Q/E는 PopupRelicView에서 직접 처리)
        if (popupState != ePopupState.Relic && popupState != ePopupState.Item)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ChangeModel(-1);
                RefreshAll();
                commonActions?.PlayMoveSound?.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                ChangeModel(+1);
                RefreshAll();
                commonActions?.PlayMoveSound?.Invoke();
            }
        }

        if (popupState == ePopupState.Character && Input.GetKeyDown(KeyCode.Escape))
        {
            await ReductionClose(true, true);
            closeAction?.Invoke();
        }
    }

    private void OnExpansionStateSelected(ePopupState selectedState)
    {
        switch (selectedState)
        {
            case ePopupState.Attribute:
                SetState(ePopupState.Attribute);
                break;
            case ePopupState.Relic:
                SetState(ePopupState.Relic);
                break;
            case ePopupState.Item:
                SetState(ePopupState.Item);
                break;
        }
    }

    public void SetState(ePopupState state)
    {
        popupState = state;

        switch (popupState)
        {
            case ePopupState.Character:
                popupText.text = GameManager.Instance.GetTalk(30020);
                //_characterPresenter.UpdatePlayerInfo(curPlayerId);
                break;
            case ePopupState.Attribute:
                popupText.text = GameManager.Instance.GetTalk(30021);
                break;
            case ePopupState.Relic:
                popupText.text = GameManager.Instance.GetTalk(30022);
                break;
            case ePopupState.Item:
                popupText.text = GameManager.Instance.GetTalk(30063);
                break;
        }

        characterView.gameObject.SetActive(popupState == ePopupState.Character);
        attributeView.gameObject.SetActive(popupState == ePopupState.Attribute);
        relicView.gameObject.SetActive(popupState == ePopupState.Relic);
        itemView.gameObject.SetActive(popupState == ePopupState.Item);

        foreach (var playerObject in playerObjects)
            playerObject.SetActive(popupState != ePopupState.Item);
        
        RefreshAll();
    }

    // dir: +1 = 다음(E), -1 = 이전(Q)
    public void ChangeModel(int dir = 1)
    {
        int curIdx  = GameManager.Instance.PlayerList.IndexOf(curPlayerId);
        int nextIdx = (curIdx + dir + GameManager.Instance.PlayerList.Count) % GameManager.Instance.PlayerList.Count;
        curPlayerId = GameManager.Instance.PlayerList[nextIdx];
    }

    private void RefreshAll()
    {
        UpdateCommonUI(curPlayerId);
        _characterPresenter.UpdatePlayerInfo(curPlayerId);
        _attributePresenter.UpdatePlayerInfo(curPlayerId);
        _relicPresenter.UpdatePlayerInfo(curPlayerId);
        _itemPresenter.UpdateItemInfo();
    }

    public void UpdateCommonUI(string playerId)
    {
        curPlayerId = playerId;

        berserkerFrame.Reduction();
        gunnerFrame.Reduction();
        fighterFrame.Reduction();

        float expansionScale = 1.1f;
        switch (curPlayerId)
        {
            case ConstValues.Berserker:
                berserkerFrame.Expansion(expansionScale);
                break;
            case ConstValues.Gunner:
                gunnerFrame.Expansion(expansionScale);
                break;
            case ConstValues.Fighter:
                fighterFrame.Expansion(expansionScale);
                break;
        }

        berserkerFrame.SelectObjectActive(curPlayerId == ConstValues.Berserker);
        gunnerFrame.SelectObjectActive(curPlayerId == ConstValues.Gunner);
        fighterFrame.SelectObjectActive(curPlayerId == ConstValues.Fighter);

        berserkerObject.SetActive(curPlayerId == ConstValues.Berserker);
        gunnerObject.SetActive(curPlayerId == ConstValues.Gunner);
        fighterObject.SetActive(curPlayerId == ConstValues.Fighter);
    }
}