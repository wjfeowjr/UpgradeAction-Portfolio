using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IPopupAttributeView
{
    void SetModel(string playerId, List<SkillData> berserkerList, List<SkillData> gunnerList, List<SkillAttributeData> attributeList, SkillCollection playerSkill);
    void SetAction(Action playMoveSound, Action playSelectSound, Action playCancelSound, Action closeAction, Action<string, Sprite, int, Action, Action> popupAction);
}

public class PopupAttributeModel
{
    public string playerId;
    public List<SkillData> berserkerSkillList = new List<SkillData>();
    public List<SkillData> gunnerSkillList = new List<SkillData>();
    public List<SkillAttributeData> attributeList = new List<SkillAttributeData>(); 
    public SkillCollection playerSkill;
    public Action playMoveSound;
    public Action playSelectSound;
    public Action playCancelSound;
    public Action closeAction;
    public Action<string, Sprite, int, Action, Action> popupAction;
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

    public void SetModel()
    {
        _attributeView.SetModel(_model.playerId, _model.berserkerSkillList, _model.gunnerSkillList, _model.attributeList, _model.playerSkill);
    }

    public void SetAction()
    {
        _attributeView.SetAction(_model.playMoveSound, _model.playSelectSound, _model.playCancelSound, _model.closeAction, _model.popupAction);
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
    [SerializeField] private TMP_Text attributeText;
    [SerializeField] private TMP_Text leftPoint;
    [SerializeField] private TMP_Text attributeNameText;        // 특성 이름
    [SerializeField] private TMP_Text attributeExplainText;     // 특성 설명
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text activeText;
    [SerializeField] private TMP_Text disActiveText;
    
    [Header("Objects")]
    [SerializeField] private GameObject explainObject;
    [SerializeField] private GameObject activeObject;
    [SerializeField] private GameObject disActiveObject;

    [SerializeField] private GameObject buyButton;
    [SerializeField] private GameObject sellButton;
    [SerializeField] private TMP_Text buyText;
    [SerializeField] private TMP_Text sellText;
    [SerializeField] private TMP_Text[] enterTexts;
    [SerializeField] private TMP_Text selectText;
    [SerializeField] private TMP_Text backText;
    [SerializeField] private TMP_Text changeText;
    
    [SerializeField] private GameObject berserkerObject;
    [SerializeField] private GameObject gunnerObject;
    
    [Header("Frames")]
    [SerializeField] private AttributeFrame_Skill[] skillArray;
    [SerializeField] private AttributeFrame_Attribute[] attributeArray;

    private Action playMoveSound;
    private Action playSelectSound;
    private Action playCancelSound;
    private Action closeAction;
    private Action<string, Sprite, int, Action, Action> popupAction;

    private SkillCollection skillInfo;
    private SkillSetting skillSetting;
    private readonly List<SkillData> skillTableList = new List<SkillData>();
    private List<SkillData> berserkerSkillList = new List<SkillData>();
    private List<SkillData> gunnerSkillList = new List<SkillData>();
    
    private List<SkillAttributeData> allAttributeList;
    private List<SkillAttributeData> attributeTableList;

    // 현재 스킬에서 사용 가능한 "특성 id"(중복 레벨 행 제거)
    private readonly List<string> attributeIdList = new List<string>();
    private readonly HashSet<string> attributeIdSet = new HashSet<string>();

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

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SetSkillIndex(curSkillIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetSkillIndex(curSkillIndex + 1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ChangeModel();
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

        List<SkillData> playerSkillList = new List<SkillData>();
        foreach (var skillTable in skillTableList)
        {
            if (skillTable.caster == curPlayerId)
            {
                playerSkillList.Add(skillTable);
            }
        }

        for (var i = 0; i < skillArray.Length; i++)
        {
            if (i < showCount)
            {
                skillArray[i].gameObject.SetActive(true);
                skillArray[i].SetData(skillSetting.skillList.Find(x => x.skillId == playerSkillList[i].id) != null, playerSkillList[i]);
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
        BuildAttributeListForSkill(skillTableList[curSkillIndex].id, resetSelection: true);
        playMoveSound();
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
        //skillArray[curSkillIndex].SelectObjectActive(false);
        skillArray[curSkillIndex].Reduction();
        playSelectSound();
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
        playCancelSound();
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
                playMoveSound();
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
                playMoveSound();
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

        // 팝업으로 구매
        // playSelectSound();
        // string message = $"구매 하시겠습니까?";
        // if(curAdjust == eAdjust.Sell)
        //     message = $"판매 하시겠습니까?";
        // Sprite goodsSprite = GameManager.Instance.GetAtlasSprite(ConstValues.IconAttributePoint);
        // popupAction(message, goodsSprite, curCost, YesAction, NoAction);
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
        Skill skill = skillSetting.skillList.Find(x => x.skillId == curSkillId);
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
        if (curAdjust == eAdjust.Buy)
            skillInfo.BuyAttribute(curSkillId, curAttributeId, effectVector);
        else
            skillInfo.SellAttribute(curSkillId, curAttributeId, effectVector);

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

    public void SetModel(string playerId, List<SkillData> berserkerList, List<SkillData> gunnerList, List<SkillAttributeData> attributeList, SkillCollection playerSkill)
    {
        skillInfo = playerSkill;
        
        curPlayerId = playerId;
        allAttributeList = attributeList;

        berserkerSkillList = berserkerList;
        gunnerSkillList = gunnerList;

        SetPlayerInfo();

        skillText.text = GameManager.Instance.GetTalk(30004);
        attributeText.text = GameManager.Instance.GetTalk(30005);
        activeText.text = GameManager.Instance.GetTalk(30006);
        disActiveText.text = GameManager.Instance.GetTalk(30007);
        
        buyText.text = GameManager.Instance.GetTalk(30008);
        sellText.text = GameManager.Instance.GetTalk(30009);

        foreach (var enterText in enterTexts)
            enterText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.confirmKey);
        
        selectText.text = string.Format(GameManager.Instance.GetTalk(30103), GameManager.Instance.GetKeyCode(GameManager.Instance.confirmKey));
        backText.text = string.Format(GameManager.Instance.GetTalk(30104), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey));
        changeText.text = string.Format(GameManager.Instance.GetTalk(30105), GameManager.Instance.GetKeyCode(GameManager.Instance.changeCharacterKey));
    }

    private void ChangeModel()
    {
        if (curPlayerId == GameManager.Instance.FirstPlayer)
            curPlayerId = GameManager.Instance.SecondPlayer;
        else if (curPlayerId == GameManager.Instance.SecondPlayer)
            curPlayerId = GameManager.Instance.FirstPlayer;

        SetPlayerInfo();
    }

    private void SetPlayerInfo()
    {
        skillTableList.Clear();
        
        switch (curPlayerId)
        {
            case ConstValues.Berserker:
                skillSetting = skillInfo.berserkerSkillSetting;
                skillTableList.AddRange(berserkerSkillList);
                berserkerObject.SetActive(true);
                gunnerObject.SetActive(false);
                break;

            case ConstValues.Gunner:
                skillSetting = skillInfo.gunnerSkillSetting;
                skillTableList.AddRange(gunnerSkillList);
                berserkerObject.SetActive(false);
                gunnerObject.SetActive(true);
                break;
        }

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
        if (leftPoint != null && skillSetting != null)
            leftPoint.text = skillSetting.attributePoint.ToString();
    }

    private void BuildAttributeListForSkill(string skillId, bool resetSelection)
    {
        curSkillId = skillId;
        explainObject.SetActive(false);
        //attributeText.gameObject.SetActive(false);
        
        // 배우지 않은 스킬이면 슬롯 숨김
        if (skillSetting == null || skillSetting.skillList.Find(x => x.skillId == curSkillId) == null)
        {
            for (int i = 0; i < attributeArray.Length; i++)
            {
                if (attributeArray[i] == null)
                    continue;
                
                attributeArray[i].gameObject.SetActive(false);
                attributeArray[i].SelectObjectActive(false);
                attributeArray[i].Reduction();
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
        attributeTableList = allAttributeList.FindAll(x => x.skill == curSkillId);
        BuildAttributeIdList();

        var skill = skillSetting.skillList.Find(x => x.skillId == curSkillId);

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

        // 이름/설명
        SkillAttributeData attributeData = attributeTableList.Find(x => x.id == id);
        if (attributeData == null)
            return;
        
        attributeNameText.text = GameManager.Instance.GetTalk(attributeData.talk);

        if (attributeData.cost < 100 && (attributeData.buffTime > 0 || attributeData.buffValue > 0))
        {
            List<object> timeAndValue = new List<object>();
            if(attributeData.buffTime > 0)
                timeAndValue.Add(attributeData.buffTime);
            if(attributeData.buffValue > 0)
                timeAndValue.Add(attributeData.buffValue);
            
            attributeExplainText.text = string.Format(GameManager.Instance.GetTalk(attributeData.explainTalk), timeAndValue.ToArray());
        }
        else
        {
            attributeExplainText.text = GameManager.Instance.GetTalk(attributeData.explainTalk);
        }
        
        costText.text = attributeData.cost.ToString();
        explainObject.SetActive(true);
        CheckAdjust();
    }
    #endregion

    #region Close

    public void SetAction(Action moveSound, Action selectSound, Action cancelSound, Action close, Action<string, Sprite, int, Action, Action> popup)
    {
        playMoveSound = moveSound;
        playSelectSound = selectSound;
        playCancelSound = cancelSound;
        closeAction = close;
        popupAction = popup;
    }

    public void CloseAction()
    {
        if (closeAction != null)
        {
            closeAction.Invoke();
            if(isPointChange)
                GameManager.Instance.SaveGame();
            return;
        }

        // 안전장치
        gameObject.SetActive(false);
    }

    #endregion
}
