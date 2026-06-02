using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupSkillView
{
    void SetModel(string playerId, List<SkillData> skillData, List<PlayerInfo> playerInfo);
    void SetPlayerInfo();
    void SetAction(PopupCommonActions commonActions, Action closeAction);
}

public class PopupSkillModel
{
    public string playerId;
    public List<SkillData> skillTableList = new List<SkillData>();
    public List<PlayerInfo> playerInfoList = new List<PlayerInfo>();
    public PopupCommonActions commonActions;
    public Action closeAction;
}

public class PopupSkillPresenter
{
    private readonly IPopupSkillView _view;
    private readonly PopupSkillModel _model;

    public PopupSkillPresenter(IPopupSkillView view, PopupSkillModel model)
    {
        _view = view;
        _model = model;
    }

    public void UpdatePlayerInfo(string newId)
    {
        _model.playerId = newId;
        _view.SetModel(newId, _model.skillTableList, _model.playerInfoList);
        _view.SetAction(_model.commonActions, _model.closeAction);
        _view.SetPlayerInfo();
    }
}

public class PopupSkillView : MonoBehaviour, IPopupSkillView
{
    [Header("Texts")]
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private TMP_Text skillExplain;
    [SerializeField] private TMP_Text skillCoolTime;
    [SerializeField] private TMP_Text skillStack;
    [SerializeField] private TMP_Text skillArmor;

    [SerializeField] private TMP_Text passiveName;
    [SerializeField] private TMP_Text passiveExplain;

    [Header("Image")]
    [SerializeField] private Image currentSkillImage;
    [SerializeField] private Image passiveImage;
    
    [Header("Object")]
    [SerializeField] private GameObject skillObject;
    [SerializeField] private GameObject lockObject;

    [Header("Frames")]
    [SerializeField] private AttributeFrame_Skill[] skillArray;

    private PopupCommonActions _actions;
    private Action _closeAction;
    
    private List<SkillData> skillTableList = new List<SkillData>();
    private List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

    // 현재 캐릭터의 표시 대상 스킬(SkillData, type == Skill || Dash, caster == curPlayerId)
    private List<SkillData> curSkillList = new List<SkillData>();
    private PlayerInfo curPlayerInfo;

    private string curPlayerId;

    private int curSkillIndex;
    private int skillCount;

    private void Update()
    {
        if (skillCount <= 0)
            return;

        // 위/아래 방향키로 스킬 선택 이동 (순환)
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
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAction();
            _actions?.PlayCancelSound?.Invoke();
        }
    }

    #region Data / Refresh

    public void SetModel(string playerId, List<SkillData> skillData, List<PlayerInfo> playerInfo)
    {
        curPlayerId = playerId;
        skillTableList = skillData;
        playerInfoList = playerInfo;
    }

    public void SetAction(PopupCommonActions commonActions, Action closeAction)
    {
        _actions = commonActions;
        _closeAction = closeAction;
    }

    public void SetPlayerInfo()
    {
        SetSkillList();
        SetupSkillNavigation();

        // 초기 상태: 0번 스킬 선택
        SetSkillIndex(0, true);
        RefreshPassive();
    }

    // SkillData 테이블을 돌며 caster/type 필터를 적용해 표시 대상 스킬을 모으고 skillArray 에 채운다
    private void SetSkillList()
    {
        curSkillList.Clear();
        List<SkillData> playerSkillList = new List<SkillData>();
        foreach (var skillTable in skillTableList)
        {
            if (skillTable.caster == curPlayerId && skillTable.type is ConstValues.Dash or ConstValues.Skill)
            {
                playerSkillList.Add(skillTable);
            }
        }
        curSkillList.AddRange(playerSkillList);

        for (var i = 0; i < skillArray.Length; i++)
        {
            if (i < playerSkillList.Count)
            {
                skillArray[i].gameObject.SetActive(true);
                curPlayerInfo = playerInfoList.Find(x => x.playerId == curPlayerId);
                skillArray[i].SetData(curPlayerInfo.skillList.Find(x => x.skillId == playerSkillList[i].id) != null, playerSkillList[i]);
            }
            else
            {
                skillArray[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetupSkillNavigation()
    {
        skillCount = Mathf.Min(curSkillList.Count, skillArray.Length);
        if (skillCount <= 0)
        {
            curSkillIndex = 0;
            return;
        }

        if (curSkillIndex < 0 || curSkillIndex >= skillCount)
            curSkillIndex = 0;
    }

    // 스킬 선택 인덱스 갱신 + 프레임 선택/해제 + 우측 상세 갱신
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
            if (!skillArray[i])
                continue;

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

        ShowSkillDetail(curSkillList[curSkillIndex]);
    }

    // 선택된 스킬의 상세 정보 표시 (보유 시 PlayerSkill 소스, 미보유 시 잠금 처리)
    private void ShowSkillDetail(SkillData skill)
    {
        if (skill == null)
            return;

        var playerSkill = curPlayerInfo.skillList.Find(x=> x.skillId == skill.id);
        bool isLocked = playerSkill == null;   // 아직 획득하지 않은 스킬 = 잠금

        // 잠금 시 상세 정보 숨김 — PopupAttributeView 의 lock 처리와 동일
        if (skillCoolTime)
            skillCoolTime.gameObject.SetActive(!isLocked);
        if (skillStack)
            skillStack.gameObject.SetActive(!isLocked);
        if(skillObject)
            skillObject.gameObject.SetActive(!isLocked);
        if(lockObject)
            lockObject.SetActive(isLocked);
        if (skillArmor)
            skillArmor.text = default;

        if (isLocked)
        {
            if (skillName)
                skillName.text = GameManager.Instance.GetTalk(60000);
            if (skillExplain)
                skillExplain.text = GameManager.Instance.GetTalk(70000);
            return;
        }

        var skillInfo = skillTableList.Find(x => x.id == skill.id);
        if (skillInfo == null)
            return;

        currentSkillImage.sprite = GameManager.Instance.GetAtlasSprite(skillInfo.id);
        
        if (skillName)
            skillName.text = GameManager.Instance.GetTalk(skillInfo.talk);
        
        if (skillExplain)
        {
            skillExplain.text = GameManager.Instance.GetTalk(skillInfo.explainTalk);
            
            var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == skillInfo.id);
            if (attackData != null)
            {
                if (!string.IsNullOrWhiteSpace(attackData.deBuffTime))
                {
                    var deBuffTimeSplit = attackData.deBuffTime.Split(';');
                    skillExplain.text = string.Format(GameManager.Instance.GetTalk(skillInfo.explainTalk), deBuffTimeSplit[0]);
                }
            }
            
            if (!string.IsNullOrWhiteSpace(skillInfo.buffName))
            {
                List<object> valueList = new List<object>();
                valueList.Add(skillInfo.buffTime.ToString(CultureInfo.InvariantCulture));
                valueList.Add(skillInfo.buffCount.ToString(CultureInfo.InvariantCulture));
                var buffValueSplit = skillInfo.buffValue.Split(';');
                foreach (var buffValue in buffValueSplit)
                    valueList.Add(buffValue);

                object[] valueArray = valueList.ToArray();
                skillExplain.text = string.Format(GameManager.Instance.GetTalk(skillInfo.explainTalk), valueArray);
            }
        }

        if (skillCoolTime && skillStack)
        {
            var coolTimeString = skill.coolTime.Split(';');
            List<float> coolTimeList = new List<float>();
            foreach (var coolTime in coolTimeString)
                coolTimeList.Add(float.Parse(coolTime));
            
            skillCoolTime.text = string.Format(GameManager.Instance.GetTalk(30107), coolTimeList[0].ToString(CultureInfo.InvariantCulture));
            
            skillStack.gameObject.SetActive(coolTimeList.Count > 1);
            if (coolTimeList.Count > 1)
            {
                skillCoolTime.text = string.Format(GameManager.Instance.GetTalk(30107), coolTimeList[1].ToString(CultureInfo.InvariantCulture));
                skillStack.text = string.Format(GameManager.Instance.GetTalk(30108), coolTimeList[2].ToString(CultureInfo.InvariantCulture));
            }
        }

        if (skillArmor && skillInfo.skillArmor == ConstValues.SuperArmor)
            skillArmor.text = GameManager.Instance.GetTalk(30109);
    }

    // 클래스 고유 패시브 표시 (Player 테이블의 passive / passiveComment)
    private void RefreshPassive()
    {
        var playerData = TableManager.Instance.playerTable.Player.Find(x => x.id == curPlayerId);
        if (playerData == null)
            return;

        var passiveData = GameManager.Instance.passiveCopyList.Find(x => x.id == playerData.passive);
        if(passiveData == null)
            return;

        if (passiveImage)
            passiveImage.sprite = GameManager.Instance.GetAtlasSprite(passiveData.id);
        
        if (passiveName)
            passiveName.text = string.Format(GameManager.Instance.GetTalk(30113), GameManager.Instance.GetTalk(passiveData.passiveName));
        
        if (passiveExplain)
        {
            List<object> valueList = new List<object>();
            
            if(passiveData.buffTime > 0)
                valueList.Add(passiveData.buffTime);

            foreach (var buffValue in passiveData.buffValue)
                valueList.Add(buffValue);

            if(passiveData.penaltyValue > 0)
                valueList.Add(passiveData.penaltyValue);

            object[] valueArray = valueList.ToArray();
            passiveExplain.text = string.Format(GameManager.Instance.GetTalk(passiveData.passiveExplain), valueArray);
        }
    }

    #endregion

    #region Close

    private void CloseAction()
    {
        _closeAction?.Invoke();
    }

    #endregion
}
