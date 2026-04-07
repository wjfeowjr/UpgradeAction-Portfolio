using System;
using TMPro;
using UnityEngine;

// 공통 UI 사운드/액션을 관리하는 모델
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
}

public class Popup_Character : UIBase
{
    [SerializeField] private ePopupState popupState;
    [SerializeField] private PopupCharacterView characterView;
    [SerializeField] private PopupAttributeView attributeView;
    [SerializeField] private PopupRelicView relicView;
    
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private GameObject berserkerObject;
    [SerializeField] private GameObject gunnerObject;
    [SerializeField] private GameObject fighterObject;
    
    private PopupCharacterPresenter _characterPresenter;
    private PopupAttributePresenter _attributePresenter;
    private PopupRelicPresenter _relicPresenter;
    
    private string curPlayerId;
    
    public void InitPresenters(string initialPlayerId)
    {
        curPlayerId = initialPlayerId;
        
        // 1. 공통 액션 정의 (중복 방지)
        var common = new PopupCommonActions
        {
            PlayMoveSound = () => SoundManager.Instance.PlaySound(ConstValues.Jump1, true),
            PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true),
            PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton, true),
        };
        
        // 2. Character MVP 생성
        var charModel = new PopupCharacterModel
        {
            playerId = curPlayerId, 
            commonActions = common
        };
        _characterPresenter = new PopupCharacterPresenter(characterView, charModel);

        // 3. Attribute MVP 생성
        var attrModel = new PopupAttributeModel 
        {
            playerId = curPlayerId,
            skillDataList = TableManager.Instance.skillTable.Skill,
            playerInfoList = GameManager.Instance.PlayerInfoList,
            commonActions = common,
            popupAction = GameManager.Instance.SpawnSelect,
            closeAction = () => SetState(ePopupState.Character),
        };
        _attributePresenter = new PopupAttributePresenter(attributeView, attrModel);
        
        // 4. Relic MVP 생성
        var relicModel = new PopupRelicModel
        {
            playerId = curPlayerId, 
            commonActions = common,
            closeAction = () => SetState(ePopupState.Character),
        };
        _relicPresenter = new PopupRelicPresenter(relicView, relicModel);

        // 초기 상태 설정 (1번 규칙: Character 상태로 시작)
        SetState(ePopupState.Character);
        RefreshAll();

        // CharacterView에 expansionObject 선택 초기화 및 콜백 등록
        characterView.InitExpansionSelection(OnExpansionStateSelected);
    }
    
    private void Update()
    {
        // 3번 규칙: LeftShift 누르면 캐릭터 변경 및 정보 갱신
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ChangeModel();
            RefreshAll();
        }

        // 5번 규칙: Character 상태에서 Esc 누르면 닫기
        if (popupState == ePopupState.Character && Input.GetKeyDown(KeyCode.Escape))
        {
            ReductionClose(true, true);
        }
    }

    // CharacterView에서 엔터 입력 시 호출되는 콜백
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
        }
    }

    public void SetState(ePopupState state)
    {
        popupState = state;

        switch (popupState)
        {
            case ePopupState.Character:
                popupText.text = GameManager.Instance.GetTalk(30020);
                break;
            case ePopupState.Attribute:
                popupText.text = GameManager.Instance.GetTalk(30021);
                break;
            case ePopupState.Relic:
                popupText.text = GameManager.Instance.GetTalk(30022);
                break;
        }
        
        characterView.gameObject.SetActive(popupState == ePopupState.Character);
        attributeView.gameObject.SetActive(popupState == ePopupState.Attribute);
        relicView.gameObject.SetActive(popupState == ePopupState.Relic);
    }

    public void ChangeModel()
    {
        // GameManager의 현재 플레이어를 다음 플레이어로 교체하는 로직
        // 예: 리스트 순환 혹은 토글
        int curIdx = GameManager.Instance.PlayerList.IndexOf(curPlayerId);
        int nextIdx = (curIdx + 1) % GameManager.Instance.PlayerList.Count;
        curPlayerId = GameManager.Instance.PlayerList[nextIdx];
    }

    private void RefreshAll()
    {
        // 최상위 UI(텍스트, 오브젝트) 갱신
        UpdateCommonUI(curPlayerId);
        
        // 각 View의 데이터 갱신 (Presenter를 거쳐 실행)
        _characterPresenter.UpdatePlayerInfo(curPlayerId);
        _attributePresenter.UpdatePlayerInfo(curPlayerId);
        _relicPresenter.UpdatePlayerInfo(curPlayerId);
    }

    public void UpdateCommonUI(string playerId)
    {
        curPlayerId = playerId;
        berserkerObject.SetActive(curPlayerId == ConstValues.Berserker);
        gunnerObject.SetActive(curPlayerId == ConstValues.Gunner);
        fighterObject.SetActive(curPlayerId == ConstValues.Fighter);
    }
}