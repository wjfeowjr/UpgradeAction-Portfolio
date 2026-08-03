using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IPopupAttributeView
{
    void SetModel(string playerId, List<SkillData> skillData, List<PlayerInfo> playerInfo);
    void SetPlayerInfo(); // 정보 갱신용 추가
    void SetAction(PopupCommonActions commonActions, Action<string, Sprite, int, Action, Action, bool> popupAction, Action closeAction);
}

public class PopupAttributeModel
{
    public string playerId;
    public List<SkillData> skillDataList = new List<SkillData>();
    public List<PlayerInfo> playerInfoList;
    public PopupCommonActions commonActions; 
    public Action<string, Sprite, int, Action, Action, bool> popupAction;
    public Action closeAction;
}

public class PopupAttributePresenter
{
    private readonly IPopupAttributeView _view;
    private readonly PopupAttributeModel _model;

    public PopupAttributePresenter(IPopupAttributeView view, PopupAttributeModel model)
    {
        _view = view;
        _model = model;
    }

    public void UpdatePlayerInfo(string newId)
    {
        _model.playerId = newId;
        _view.SetModel(newId, _model.skillDataList, _model.playerInfoList);
        _view.SetAction(_model.commonActions, _model.popupAction, _model.closeAction);
        _view.SetPlayerInfo();
    }
}

public class PopupAttributeView : MonoBehaviour, IPopupAttributeView
{
    private enum eStep
    {
        SkillSelect,
        AttributeSelect,
        PointAdjust
    }

    private enum eAdjust
    {
        Buy = 0,
        Sell = 1,
    }

    [Header("Texts")]
    [SerializeField] private TMP_Text skillText;
    [SerializeField] private TMP_Text leftPoint;
    [SerializeField] private TMP_Text attributeNameText;        // 특성 이름
    [SerializeField] private TMP_Text attributeExplainText;     // 특성 설명
    [SerializeField] private TMP_Text noHaveSkillText;          // 특성 스킬없음
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text activeText;
    [SerializeField] private TMP_Text disActiveText;

    [Header("Objects")]
    [SerializeField] private GameObject explainObject;
    [SerializeField] private GameObject activeObject;
    [SerializeField] private GameObject disActiveObject;
    [SerializeField] private GameObject attributeInfoObject;
    [SerializeField] private GameObject attributeLockObject;
    [SerializeField] private GameObject[] infoObjects;
    
    [SerializeField] private GameObject buyButton;
    [SerializeField] private GameObject sellButton;
    [SerializeField] private TMP_Text buyText;
    [SerializeField] private TMP_Text sellText;
    [SerializeField] private TMP_Text[] enterTexts;

    [Header("Frames")]
    [SerializeField] private AttributeFrame_Skill[] skillArray;
    [SerializeField] private AttributeFrame_Attribute[] attributeArray;
    
    private PopupCommonActions _actions;
    private Action<string, Sprite, int, Action, Action, bool> _popupAction;
    private Action _closeAction;

    private List<SkillData> skillTableList = new List<SkillData>();
    private List<SkillData> skillDataList = new List<SkillData>();
    private List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

    private List<SkillAttributeCopy> attributeList;

    // 현재 스킬에서 사용 가능한 "특성 id"(중복 레벨 행 제거)
    private readonly List<string> attributeIdList = new List<string>();

    private string curPlayerId;
    private string curSkillId;
    private string curAttributeId;

    // 단계/선택 인덱스
    private bool isPointChange;
    private eStep curStep = eStep.SkillSelect;
    private int curSkillIndex;
    private int skillCount;

    private int curAttributeIndex;
    private int attributeSlotCount;

    private eAdjust curAdjust = eAdjust.Buy;

    private const int AttributeCols = 4;
    private const int AttributeRows = 2;
    
    private void OnEnable()
    {
        isPointChange = false;
        noHaveSkillText.text = GameManager.Instance.GetTalk(91001);
    }

    private void Update()
    {
        switch (curStep)
        {
            case eStep.SkillSelect:
                UpdateSkillSelect();
                break;
            case eStep.AttributeSelect:
                UpdateAttributeSelect();
                break;
        }
    }

    #region Step1 - SkillSelect

    private void UpdateSkillSelect()
    {
        if (skillCount <= 0)
            return;

        if (Input.GetKeyDown(GameManager.Instance.upKey))
        {
            SetSkillIndex(curSkillIndex - 1);
            _actions?.PlayMoveSound?.Invoke();
        }
        else if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            SetSkillIndex(curSkillIndex + 1);
            _actions?.PlayMoveSound?.Invoke();
        }
        else if (InputHelper.GetEnterDown() || InputHelper.GetKeypadEnterDown())
        {
            // 스킬 선택 확정 → 특성 선택 단계 진입
            EnterAttributeSelect();
            _actions?.PlaySelectSound?.Invoke();
        }
        else if (Input.GetKeyDown(GameManager.Instance.escKey))
        {
            CloseAction();
            _actions?.PlayMoveSound?.Invoke();
        }
    }

    private void SetSkillList()
    {
        List<SkillData> playerSkillList = new List<SkillData>();
        foreach (var skillTable in skillTableList)
        {
            if (skillTable.caster == curPlayerId && skillTable.type != ConstValues.Dash)
            {
                playerSkillList.Add(skillTable);
            }
        }

        for (var i = 0; i < skillArray.Length; i++)
        {
            if (i < playerSkillList.Count)
            {
                skillArray[i].gameObject.SetActive(true);
                var playerInfo = playerInfoList.Find(x => x.playerId == curPlayerId);
                skillArray[i].SetData(playerInfo.skillList.Find(x => x.skillId == playerSkillList[i].id) != null, playerSkillList[i]);
            }
            else
            {
                skillArray[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetupSkillNavigation()
    {
        skillCount = Mathf.Min(skillTableList.Count, skillArray.Length);
        if (skillCount <= 0)
        {
            curSkillIndex = 0;
            return;
        }

        if (curSkillIndex < 0 || curSkillIndex >= skillCount)
            curSkillIndex = 0;
    }

    private void SetSkillIndex(int newIndex, bool force = false)
    {
        if (skillCount <= 0)
            return;

        // 순환
        newIndex %= skillCount;
        if (newIndex < 0)
            newIndex += skillCount;

        if (!force && curSkillIndex == newIndex)
            return;

        curSkillIndex = newIndex;

        for (int i = 0; i < skillCount; i++)
        {
            if (i == curSkillIndex)
            {
                skillArray[i].SelectObjectActive(true);
                skillArray[i].Expansion(1.05f);
            }
            else
            {
                skillArray[i].SelectObjectActive(false);
                skillArray[i].Reduction();
            }
        }

        // 스킬이 바뀌면 특성 리스트 갱신 + 선택 리셋
        skillDataList.Clear();
        foreach (var skillTable in skillTableList)
        {
            if (skillTable.caster == curPlayerId && skillTable.type != ConstValues.Dash)
            {
                skillDataList.Add(skillTable);
            }
        }
        BuildAttributeListForSkill(skillDataList[curSkillIndex].id, resetSelection: true);
    }

    #endregion

    #region Step2 - AttributeSelect

    private void UpdateAttributeSelect()
    {
        if (attributeSlotCount <= 0)
            return;

        if (Input.GetKeyDown(GameManager.Instance.leftKey))
        {
            MoveAttributeHorizontal(-1);
        }
        else if (Input.GetKeyDown(GameManager.Instance.rightKey))
        {
            MoveAttributeHorizontal(1);
        }
        else if (Input.GetKeyDown(GameManager.Instance.upKey))
        {
            MoveAttributeVertical(-1);
        }
        else if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            MoveAttributeVertical(1);
        }
        else if (InputHelper.GetEnterDown() || InputHelper.GetKeypadEnterDown())
        {
            EnterPointAdjust();
        }
        else if (Input.GetKeyDown(GameManager.Instance.escKey))
        {
            BackToSkillSelect();
        }
    }

    private void EnterAttributeSelect()
    {
        SetupAttributeNavigation();
        if (attributeSlotCount <= 0)
            return;

        curStep = eStep.AttributeSelect;
        SetAttributeIndex(FindFirstSelectableAttributeIndex(), true);
        skillArray[curSkillIndex].Reduction();
    }

    private void BackToSkillSelect()
    {
        curStep = eStep.SkillSelect;
        explainObject.SetActive(false);

        for (int i = 0; i < attributeArray.Length; i++)
        {
            if (attributeArray[i] == null)
                continue;
            
            attributeArray[i].SelectObjectActive(false);
            attributeArray[i].Reduction();
        }
        skillArray[curSkillIndex].SelectObjectActive(true);
        skillArray[curSkillIndex].Expansion(1.05f);
        _actions?.PlayCancelSound?.Invoke();
    }

    private void SetupAttributeNavigation()
    {
        attributeSlotCount = 0;
        for (int i = 0; i < attributeArray.Length; i++)
        {
            if (attributeArray[i] != null && attributeArray[i].gameObject.activeSelf)
                attributeSlotCount++;
        }

        if (attributeSlotCount <= 0)
        {
            curAttributeIndex = 0;
            return;
        }

        if (curAttributeIndex < 0 || curAttributeIndex >= attributeArray.Length)
            curAttributeIndex = 0;
    }

    private int FindFirstSelectableAttributeIndex()
    {
        for (int i = 0; i < attributeArray.Length; i++)
        {
            if (IsAttributeSelectable(i))
                return i;
        }
        return 0;
    }

    private bool IsAttributeSelectable(int idx)
    {
        if (idx < 0 || idx >= attributeArray.Length)
            return false;

        if (attributeArray[idx] == null || !attributeArray[idx].gameObject.activeSelf)
            return false;

        // id 리스트와 매핑 깨짐 방지
        if (idx >= attributeIdList.Count)
            return false;

        return true;
    }

    private void SetAttributeIndex(int newIndex, bool force = false)
    {
        if (attributeSlotCount <= 0)
            return;

        if (!IsAttributeSelectable(newIndex))
            newIndex = FindFirstSelectableAttributeIndex();

        if (!force && curAttributeIndex == newIndex)
            return;

        curAttributeIndex = newIndex;

        for (int i = 0; i < attributeArray.Length; i++)
        {
            if (attributeArray[i] == null)
                continue;

            if (i == curAttributeIndex && attributeArray[i].gameObject.activeSelf)
            {
                attributeArray[i].SelectObjectActive(true);
                attributeArray[i].Expansion(1.1f);
            }
            else
            {
                attributeArray[i].SelectObjectActive(false);
                attributeArray[i].Reduction();
            }
        }

        curAttributeId = attributeIdList[curAttributeIndex];
        GetAttributeInfo(curAttributeId);
    }

    private void MoveAttributeHorizontal(int dir)
    {
        int row = curAttributeIndex / AttributeCols;
        int col = curAttributeIndex % AttributeCols;

        // 같은 row 내에서 순환 탐색
        for (int step = 1; step <= AttributeCols; step++)
        {
            int newCol = (col + (dir * step)) % AttributeCols;
            if (newCol < 0) newCol += AttributeCols;

            int idx = row * AttributeCols + newCol;
            if (IsAttributeSelectable(idx))
            {
                SetAttributeIndex(idx);
                _actions?.PlayMoveSound?.Invoke();
                return;
            }
        }

        SetAttributeIndex(FindFirstSelectableAttributeIndex(), true);
    }

    private void MoveAttributeVertical(int dir)
    {
        int row = curAttributeIndex / AttributeCols;
        int col = curAttributeIndex % AttributeCols;

        int targetRow = (row + dir) % AttributeRows;
        if (targetRow < 0)
            targetRow += AttributeRows;

        int idx = targetRow * AttributeCols + col;
        if (IsAttributeSelectable(idx))
        {
            SetAttributeIndex(idx);
            return;
        }

        for (int step = 1; step < AttributeCols; step++)
        {
            int newCol = (col + step) % AttributeCols;
            idx = targetRow * AttributeCols + newCol;
            if (IsAttributeSelectable(idx))
            {
                SetAttributeIndex(idx);
                _actions?.PlayMoveSound?.Invoke();
                return;
            }
        }

        SetAttributeIndex(FindFirstSelectableAttributeIndex(), true);
    }

    #endregion

    #region Step3 - PointAdjust

    private void EnterPointAdjust()
    {
        if (string.IsNullOrEmpty(curSkillId) || string.IsNullOrEmpty(curAttributeId))
            return;

        curStep = eStep.PointAdjust;
        attributeArray[curAttributeIndex].SelectObjectActive(false);

        // 즉시 구매
        GameManager.Instance.HideHighestObjects();
        YesAction();

    }

    private void YesAction()
    {
        curStep = eStep.AttributeSelect;
        attributeArray[curAttributeIndex].SelectObjectActive(true);
        ApplyPointAdjust();
    }
    
    private void NoAction()
    {
        curStep = eStep.AttributeSelect;
        attributeArray[curAttributeIndex].SelectObjectActive(true);
    }
    
    private void CheckAdjust()
    {
        var playerInfo = playerInfoList.Find(x => x.playerId == curPlayerId);
        Skill skill = playerInfo.skillList.Find(x => x.skillId == curSkillId);
        bool isHaveAttribute = skill.attributeList.Contains(curAttributeId);
        if(isHaveAttribute)
            curAdjust = eAdjust.Sell;
        else
            curAdjust = eAdjust.Buy;
        
        ButtonActive();
    }

    private void ButtonActive()
    {
        if (curAdjust == eAdjust.Buy)
        {
            activeObject.SetActive(false);
            disActiveObject.SetActive(true);
            buyButton.SetActive(true);
            sellButton.SetActive(false);
        }
        else
        {
            activeObject.SetActive(true);
            disActiveObject.SetActive(false);
            buyButton.SetActive(false);
            sellButton.SetActive(true);
        }
    }

    private void ApplyPointAdjust()
    {
        Vector2 effectVector = Vector2.zero;
        foreach (var attribute in attributeArray)
        {
            if (attribute.attributeId == curAttributeId)
            {
                effectVector = attribute.EffectPos;
                break;
            }
        }
        if (curAdjust == eAdjust.Buy)
            GameManager.Instance.BuyAttribute(curSkillId, curAttributeId, effectVector);
        else
            GameManager.Instance.SellAttribute(curSkillId, curAttributeId, effectVector);

        // async void 내부에서 저장/경고가 발생할 수 있으니, 다음 프레임에 UI를 한 번 더 갱신
        RefreshAfterAdjust();
        isPointChange = true;
    }

    private void RefreshAfterAdjust()
    {
        RefreshLeftPoint();
        RefreshAttributeActiveStates(keepSelection: true);
        SetAttributeIndex(curAttributeIndex, true);
        CheckAdjust();
    }

    #endregion

    #region Data/Refresh

    public void SetModel(string playerId, List<SkillData> skillData, List<PlayerInfo> playerInfo)
    {
        curPlayerId = playerId;
        
        skillTableList = skillData;
        playerInfoList = playerInfo;

        SetPlayerInfo();

        skillText.text = GameManager.Instance.GetTalk(30004);
        activeText.text = GameManager.Instance.GetTalk(30006);
        disActiveText.text = GameManager.Instance.GetTalk(30007);
        
        buyText.text = GameManager.Instance.GetTalk(30008);
        sellText.text = GameManager.Instance.GetTalk(30009);

        foreach (var enterText in enterTexts)
            enterText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.enterKey); 
    }
    
    public void SetAction(PopupCommonActions commonActions, Action<string, Sprite, int, Action, Action, bool> popupAction, Action closeAction)
    {
        _actions = commonActions;
        _popupAction = popupAction;
        _closeAction = closeAction;
    }

    public void SetPlayerInfo()
    {
        SetSkillList();
        SetupSkillNavigation();
        RefreshLeftPoint();
        

        // 초기 상태: 스킬 선택 단계, 0번 선택
        curStep = eStep.SkillSelect;
        SetSkillIndex(0, true);
        explainObject.SetActive(false);
    }

    private void RefreshLeftPoint()
    {
        var playerInfo = playerInfoList.Find(x => x.playerId == curPlayerId);
        if (leftPoint != null && playerInfo != null)
            leftPoint.text = playerInfo.attributePoint.ToString();
    }

    private void BuildAttributeListForSkill(string skillId, bool resetSelection)
    {
        curSkillId = skillId;
        explainObject.SetActive(false);
        var playerInfo = playerInfoList.Find(x => x.playerId == curPlayerId);
        
        // 배우지 않은 스킬이면 슬롯 숨김
        bool isNotSkill = playerInfo.skillList.Count == 0 || playerInfo.skillList.Find(x => x.skillId == curSkillId) == null;
        attributeLockObject.SetActive(isNotSkill);
        attributeInfoObject.SetActive(!isNotSkill);
        
        if (isNotSkill)
        {
            for (int i = 0; i < attributeArray.Length; i++)
            {
                if (attributeArray[i] == null)
                    continue;
                
                attributeArray[i].gameObject.SetActive(false);
                attributeArray[i].SelectObjectActive(false);
                attributeArray[i].Reduction();
            }

            attributeList = null;
            attributeIdList.Clear();
            attributeSlotCount = 0;
            if (resetSelection)
                curAttributeIndex = 0;
            
            return;
        }

        attributeList = GameManager.Instance.GetAttributesBySkill(curSkillId);
        BuildAttributeIdList();

        var skill = playerInfo.skillList.Find(x => x.skillId == curSkillId);

        for (int i = 0; i < attributeArray.Length; i++)
        {
            if (attributeArray[i] == null)
                continue;
            
            attributeArray[i].gameObject.SetActive(false);
            attributeArray[i].SelectObjectActive(false);
            attributeArray[i].Reduction();
        }

        int showCount = Mathf.Min(attributeIdList.Count, attributeArray.Length);
        for (int i = 0; i < showCount; i++)
        {
            attributeArray[i].gameObject.SetActive(true);
            string attrId = attributeIdList[i];
            bool isActive = skill.attributeList.Contains(attrId);
            bool isLocked = GameManager.Instance.LockAttributeList.Find(x => x.id == attributeIdList[i]).isLock;
            attributeArray[i].SetData(attrId, isActive, isLocked);
        }

        attributeSlotCount = showCount;
        if (resetSelection)
            curAttributeIndex = 0;
    }

    private void RefreshAttributeActiveStates(bool keepSelection)
    {
        if (string.IsNullOrEmpty(curSkillId))
            return;

        // 리스트/프레임만 다시 갱신 (선택 리셋 여부 제어)
        int prevIndex = curAttributeIndex;
        BuildAttributeListForSkill(curSkillId, resetSelection: !keepSelection);
        if (keepSelection)
            curAttributeIndex = prevIndex;
    }

    private void BuildAttributeIdList()
    {
        attributeIdList.Clear();

        if (attributeList == null)
            return;

        for (int i = 0; i < attributeList.Count; i++)
        {
            if (attributeList[i] == null)
                continue;
            
            attributeIdList.Add(attributeList[i].id);
            if (attributeIdList.Count >= attributeArray.Length)
                break;
        }
    }

    // 특성 정보 확인
    private void GetAttributeInfo(string id)
    {
        if (attributeList == null)
            return;
        
        bool isLocked = GameManager.Instance.LockAttributeList.Find(x => x.id == id).isLock;
        if (isLocked)
        {
            attributeNameText.text =  GameManager.Instance.GetTalk(81000);
            attributeExplainText.text = GameManager.Instance.GetTalk(91000);
            
            foreach (var infoObject in infoObjects)
                infoObject.SetActive(false);
            
            explainObject.SetActive(true);
            return;
        }
        
        // 이름/설명
        SkillAttributeCopy skillAttributeCopy = attributeList.Find(x => x.id == id);
        if (skillAttributeCopy == null)
            return;
        
        attributeNameText.text = GameManager.Instance.GetTalk(skillAttributeCopy.talk);
        foreach (var infoObject in infoObjects)
            infoObject.SetActive(true);
        
        if (skillAttributeCopy.upgradeValue.Count > 0 || skillAttributeCopy.buffTime > 0 || skillAttributeCopy.buffValue > 0 || skillAttributeCopy.objectCount > 1)
        {
            List<object> timeAndValue = new List<object>();
            if (skillAttributeCopy.upgradeValue.Count > 0)
            {
                foreach (var value in skillAttributeCopy.upgradeValue)
                {
                    timeAndValue.Add(value);
                }
            }
            if(skillAttributeCopy.buffTime > 0)
                timeAndValue.Add(skillAttributeCopy.buffTime);
            if(skillAttributeCopy.buffValue > 0)
                timeAndValue.Add(skillAttributeCopy.buffValue);
            if(skillAttributeCopy.objectCount > 1)
                timeAndValue.Add(skillAttributeCopy.objectCount);
            
            attributeExplainText.text = string.Format(GameManager.Instance.GetTalk(skillAttributeCopy.explainTalk), timeAndValue.ToArray());
        }
        else
        {
            attributeExplainText.text = GameManager.Instance.GetTalk(skillAttributeCopy.explainTalk);
        }
        
        costText.text = skillAttributeCopy.cost.ToString();
        explainObject.SetActive(true);
        CheckAdjust();
    }
    #endregion

    #region Close

    public void CloseAction()
    {
        if (_closeAction == null)
            return;
        
        _closeAction.Invoke();
        if (isPointChange)
        {
            GameManager.Instance.SaveGame();
            GameManager.Instance.SetPlayerAttribute();
        }
    }

    #endregion
}
