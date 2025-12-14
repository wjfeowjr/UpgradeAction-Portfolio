using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IPopupAttributeView
{
    void SetModel(SkillCollection playerSkill);
    void SetAction(Action closeAction);
}

public class PopupAttributeModel
{
    public Action closeAction;
}

public class PopupAttributePresenter
{
    private readonly IPopupAttributeView _attributeView;
    private readonly PopupAttributeModel _model;

    public PopupAttributePresenter(IPopupAttributeView attributeView, PopupAttributeModel model)
    {
        _attributeView = attributeView;
        _model = model;
    }

    public void Expansion(Action action)
    {
        action?.Invoke();
    }

    public void SetModel(SkillCollection playerSkill)
    {
        _attributeView.SetModel(playerSkill);
    }

    public void SetAction(Action action)
    {
        _model.closeAction = action;
        _attributeView.SetAction(_model.closeAction);
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
        Plus = 0,
        Minus = 1,
    }

    [Header("Texts")]
    [SerializeField] private TMP_Text skillText;
    [SerializeField] private TMP_Text attributeText;
    [SerializeField] private TMP_Text leftPointText;
    [SerializeField] private TMP_Text leftPoint;
    [SerializeField] private TMP_Text attributeNameText;        // 특성 이름
    [SerializeField] private TMP_Text attributeExplainText;     // 특성 설명
    [SerializeField] private TMP_Text costText;

    [Header("Objects")]
    [SerializeField] private GameObject explainObject;

    [Header("Buttons")]
    [SerializeField] private AttributeButton plusButton;
    [SerializeField] private AttributeButton minusButton;

    [Header("Frames")]
    [SerializeField] private AttributeFrame_Skill[] skillArray;
    [SerializeField] private AttributeFrame_Attribute[] attributeArray;

    private Action _closeAction;

    private SkillCollection skillInfo;
    private SkillSetting skillSetting;
    private readonly List<SkillData> skillTableList = new List<SkillData>();
    private List<SkillAttributeData> attributeTableList;

    // 현재 스킬에서 사용 가능한 "특성 id"(중복 레벨 행 제거)
    private readonly List<string> attributeIdList = new List<string>();
    private readonly HashSet<string> attributeIdSet = new HashSet<string>();

    private string curSkillId;
    private string curAttributeId;

    // 단계/선택 인덱스
    private eStep curStep = eStep.SkillSelect;
    private int curSkillIndex;
    private int skillCount;

    private int curAttributeIndex;
    private int attributeSlotCount;

    private eAdjust curAdjust = eAdjust.Plus;

    private const int AttributeCols = 3;
    private const int AttributeRows = 2;

    private void OnDisable()
    {
        // 안전장치: 꺼질 때 버튼 선택 상태 초기화
        plusButton.UnSelect();
        minusButton.UnSelect();
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
            case eStep.PointAdjust:
                UpdatePointAdjust();
                break;
        }
    }

    #region Step1 - SkillSelect

    private void UpdateSkillSelect()
    {
        if (skillCount <= 0)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SetSkillIndex(curSkillIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetSkillIndex(curSkillIndex + 1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // 스킬 선택 확정 → 특성 선택 단계 진입
            EnterAttributeSelect();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAction();
        }
    }

    private void SetSkillList()
    {
        var showCount = Mathf.Min(skillTableList.Count, skillArray.Length);

        for (var i = 0; i < skillArray.Length; i++)
        {
            if (i < showCount)
            {
                skillArray[i].gameObject.SetActive(true);
                skillArray[i].SetData(skillSetting.skillList.Find(x => x.skillId == skillTableList[i].id) != null, skillTableList[i]);
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
                skillArray[i].Select();
            else
                skillArray[i].UnSelect();
        }

        // 스킬이 바뀌면 특성 리스트 갱신 + 선택 리셋
        BuildAttributeListForSkill(skillTableList[curSkillIndex].id, resetSelection: true);
        SoundManager.Instance.PlaySound(ConstValues.Jump1, true);
    }

    #endregion

    #region Step2 - AttributeSelect

    private void UpdateAttributeSelect()
    {
        if (attributeSlotCount <= 0)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveAttributeHorizontal(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveAttributeHorizontal(1);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveAttributeVertical(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveAttributeVertical(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            EnterPointAdjust();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
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
        skillArray[curSkillIndex].SelectObjectActive(false);
        SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
    }

    private void BackToSkillSelect()
    {
        curStep = eStep.SkillSelect;
        explainObject.SetActive(false);

        for (int i = 0; i < attributeArray.Length; i++)
        {
            if (attributeArray[i] == null)
                continue;
            
            attributeArray[i].UnSelect();
        }
        skillArray[curSkillIndex].SelectObjectActive(true);
        SoundManager.Instance.PlaySound(ConstValues.NormalButton, true);
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
                attributeArray[i].Select();
            else
                attributeArray[i].UnSelect();
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
                SoundManager.Instance.PlaySound(ConstValues.Jump1, true);
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
        if (targetRow < 0) targetRow += AttributeRows;

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
                SoundManager.Instance.PlaySound(ConstValues.Jump1, true);
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
        plusButton.Select();
        minusButton.Select();
        attributeArray[curAttributeIndex].SelectObjectActive(false);
        SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
    }

    private void BackToAttributeSelect()
    {
        curStep = eStep.AttributeSelect;
        plusButton.UnSelect();
        minusButton.UnSelect();
        attributeArray[curAttributeIndex].SelectObjectActive(true);
        SoundManager.Instance.PlaySound(ConstValues.NormalButton, true);
    }

    private void UpdatePointAdjust()
    { 
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ApplyPointAdjust();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToAttributeSelect();
        }
    }

    private void CheckAdjust()
    {
        Skill skill = skillSetting.skillList.Find(x => x.skillId == curSkillId);
        bool isHaveAttribute = skill.attributeList.Find(x => x.attributeId == curAttributeId) != null;
        if(isHaveAttribute)
            curAdjust = eAdjust.Minus;
        else
            curAdjust = eAdjust.Plus;
        
        ButtonActive();
    }

    private void ButtonActive()
    {
        if (curAdjust == eAdjust.Plus)
        {
            plusButton.gameObject.SetActive(true);
            minusButton.gameObject.SetActive(false);
        }
        else
        {
            plusButton.gameObject.SetActive(false);
            minusButton.gameObject.SetActive(true);
        }
    }

    private void ApplyPointAdjust()
    {
        if (skillInfo == null || skillSetting == null)
            return;

        Vector2 effectVector = Vector2.zero;
        foreach (var attribute in attributeArray)
        {
            if (attribute.attributeId == curAttributeId)
            {
                effectVector = attribute.EffectPos;
                break;
            }
        }
        if (curAdjust == eAdjust.Plus)
            skillInfo.AttributeLvUp(curSkillId, curAttributeId, effectVector);
        else
            skillInfo.AttributeLvDown(curSkillId, curAttributeId, effectVector);

        // async void 내부에서 저장/경고가 발생할 수 있으니, 다음 프레임에 UI를 한 번 더 갱신
        RefreshAfterAdjust();
    }

    private void RefreshAfterAdjust()
    {
        RefreshLeftPoint();
        RefreshAttributeActiveStates(keepSelection: true);
        SetAttributeIndex(curAttributeIndex, true);
        
        CheckAdjust();
        plusButton.Select();
        minusButton.Select();
    }

    #endregion

    #region Data/Refresh

    public void SetModel(SkillCollection playerSkill)
    {
        skillInfo = playerSkill;
        skillTableList.Clear();

        switch (GameManager.Instance.CurPlayer.BasicStat.id)
        {
            case ConstValues.Berserker:
                skillSetting = skillInfo.berserkerSkillSetting;
                skillTableList.AddRange(TableManager.Instance.skillTable.Skill.FindAll(
                    x => x.caster == ConstValues.Berserker && x.type != ConstValues.Dash));
                break;

            case ConstValues.Gunner:
                skillSetting = skillInfo.gunnerSkillSetting;
                skillTableList.AddRange(TableManager.Instance.skillTable.Skill.FindAll(
                    x => x.caster == ConstValues.Gunner && x.type != ConstValues.Dash));
                break;
        }

        SetSkillList();
        SetupSkillNavigation();
        RefreshLeftPoint();

        // 초기 상태: 스킬 선택 단계, 0번 선택
        curStep = eStep.SkillSelect;
        SetSkillIndex(0, true);
        explainObject.SetActive(false);
        plusButton.UnSelect();
        minusButton.UnSelect();

        skillText.text = "스킬";
        attributeText.text = "특성";
        leftPointText.text = "남은 포인트";
    }

    private void RefreshLeftPoint()
    {
        if (leftPoint != null && skillSetting != null)
            leftPoint.text = skillSetting.attributePoint.ToString();
    }

    private void BuildAttributeListForSkill(string skillId, bool resetSelection)
    {
        curSkillId = skillId;
        explainObject.SetActive(false);
        attributeText.gameObject.SetActive(false);
        
        // 배우지 않은 스킬이면 슬롯 숨김
        if (skillSetting == null || skillSetting.skillList.Find(x => x.skillId == curSkillId) == null)
        {
            
            for (int i = 0; i < attributeArray.Length; i++)
            {
                if (attributeArray[i] == null)
                    continue;
                
                attributeArray[i].gameObject.SetActive(false);
                attributeArray[i].UnSelect();
            }

            attributeTableList = null;
            attributeIdList.Clear();
            attributeIdSet.Clear();
            attributeSlotCount = 0;
            if (resetSelection)
                curAttributeIndex = 0;
            
            return;
        }

        attributeText.gameObject.SetActive(true);
        attributeTableList = TableManager.Instance.skillAttributeTable.SkillAttribute.FindAll(x => x.skill == curSkillId);
        BuildAttributeIdList();

        var skill = skillSetting.skillList.Find(x => x.skillId == curSkillId);

        for (int i = 0; i < attributeArray.Length; i++)
        {
            if (attributeArray[i] == null)
                continue;
            
            attributeArray[i].UnSelect();
            attributeArray[i].gameObject.SetActive(false);
        }

        int showCount = Mathf.Min(attributeIdList.Count, attributeArray.Length);
        for (int i = 0; i < showCount; i++)
        {
            attributeArray[i].gameObject.SetActive(true);
            string attrId = attributeIdList[i];
            bool isActive = skill.attributeList.Find(x => x.attributeId == attrId) != null;
            attributeArray[i].SetData(attrId, isActive);
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
        attributeIdSet.Clear();

        if (attributeTableList == null)
            return;

        for (int i = 0; i < attributeTableList.Count; i++)
        {
            var row = attributeTableList[i];
            if (row == null) continue;
            if (attributeIdSet.Add(row.id))
            {
                attributeIdList.Add(row.id);
                if (attributeIdList.Count >= attributeArray.Length)
                    break;
            }
        }
    }

    // 특성 정보 확인
    private void GetAttributeInfo(string id)
    {
        if (attributeTableList == null)
            return;

        // 이름/설명은 보통 레벨마다 동일하다고 가정하고, level==1 우선
        SkillAttributeData baseRow = null;
        for (int i = 0; i < attributeTableList.Count; i++)
        {
            var row = attributeTableList[i];
            if (row == null)
                continue;
            if (row.id != id)
                continue;
            
            if (row.level == 1)
            {
                baseRow = row;
                break;
            }
            if (baseRow == null)
                baseRow = row;
        }

        if (baseRow == null)
            return;
        
        attributeNameText.text = baseRow.name;
        attributeExplainText.text = baseRow.talk;
        costText.text = $"비용: {baseRow.cost} 포인트";

        explainObject.SetActive(true);
        CheckAdjust();
        plusButton.UnSelect();
        minusButton.UnSelect();
    }
    #endregion

    #region Close

    public void SetAction(Action action)
    {
        _closeAction = action;
    }

    public void CloseAction()
    {
        if (_closeAction != null)
        {
            _closeAction.Invoke();
            return;
        }

        // 안전장치
        gameObject.SetActive(false);
    }

    #endregion
}
