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
}

public class Popup_Character : UIBase
{
    [SerializeField] private ePopupState popupState;
    [SerializeField] private PopupCharacterView characterView;
    [SerializeField] private PopupAttributeView attributeView;
    
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private GameObject berserkerObject;
    [SerializeField] private GameObject gunnerObject;
    [SerializeField] private GameObject fighterObject;
    
    // 인터페이스로 외부에 노출
    public IPopupCharacterView CharacterView => characterView;
    public IPopupAttributeView AttributeView => attributeView;

    private PopupCharacterPresenter _characterPresenter;
    private PopupAttributePresenter _attributePresenter;
    
    private string curPlayerId;
    
    public void InitPresenters(PopupCharacterPresenter charPresenter, PopupAttributePresenter attrPresenter)
    {
        _characterPresenter = charPresenter;
        _attributePresenter = attrPresenter;

        // 초기 상태 설정 (1번 규칙: Character 상태로 시작)
        SetState(ePopupState.Character);
        RefreshAll();
    }
    
    private void Update()
    {
        // 2번 규칙: 'n'은 특성창, 'm'은 캐릭터창
        if (Input.GetKeyDown(KeyCode.N))
            SetState(ePopupState.Attribute);
        if (Input.GetKeyDown(KeyCode.M))
            SetState(ePopupState.Character);

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

    public void SetState(ePopupState state)
    {
        popupState = state;
        characterView.gameObject.SetActive(popupState == ePopupState.Character);
        attributeView.gameObject.SetActive(popupState == ePopupState.Attribute);
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
    }

    public void UpdateCommonUI(string playerId)
    {
        curPlayerId = playerId;
        popupText.text = GameManager.Instance.GetTalk(30005);
        berserkerObject.SetActive(curPlayerId == ConstValues.Berserker);
        gunnerObject.SetActive(curPlayerId == ConstValues.Gunner);
        fighterObject.SetActive(curPlayerId == ConstValues.Fighter);
    }
}
