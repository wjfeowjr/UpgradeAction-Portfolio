using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IUISkillView
{
    void SetSkillInfo(KeyCode keyCode, string skillId, List<float> coolTime = null);
    void UpdateCoolTimeText(List<float> coolTime);
    event Action OnSkillDropped;
}

public class UISkillModel
{
    public List<SettingSkill> settingSkillList = new List<SettingSkill>();
}

public class UISkillPresenter
{
    private readonly List<IUISkillView> _views;
    private UISkillModel _model;

    public UISkillPresenter(List<IUISkillView> views, UISkillModel model)
    {
        _views = views;
        _model = model;
        
        for (int i = 0; i < _views.Count; i++)
            _views[i].OnSkillDropped += OnSkillDropped;
    }
    public void OnSkillDroppedCleanUp()
    {
        for (int i = 0; i < _views.Count; i++)
            _views[i].OnSkillDropped -= OnSkillDropped;
    }
    
    private void OnSkillDropped()
    {
        RefreshModel();
        // UI 전체 갱신
        SetSkillInfo();
    }
    
    private async void RefreshModel()
    {
        _model = new UISkillModel
        {
            settingSkillList = GameManager.Instance.GetSettingSkillList()
        };
    }

    public void SetSkillInfo()
    {
        for (int i = 0; i < _model.settingSkillList.Count; i++)
        {
            var playerSkill = _model.settingSkillList[i].playerSkill;
            if (playerSkill == null)
            {
                _views[i].SetSkillInfo(_model.settingSkillList[i].keyCode, default);
            }
            else
            {
                var settingSkill = _model.settingSkillList[i];
                _views[i].SetSkillInfo(settingSkill.keyCode, settingSkill.skillId, settingSkill.playerSkill.coolTime);
            }
        }
    }
    
    /// <summary>
    /// 모델의 스킬 리스트(최대 9개)만큼 순회하며 각 뷰를 업데이트
    /// </summary>
    public void UpdateSkillCoolTime()
    {
        for (int i = 0; i < _model.settingSkillList.Count; i++)
        {
            var skill = _model.settingSkillList[i].playerSkill;
            if(skill == null)
                continue;
            
            _views[i].UpdateCoolTimeText(skill.GetRemainingCooldown());
        }
    }
}

public class UISkillView : MonoBehaviour, IUISkillView
{
    private List<float> maxCoolTime = new List<float>();
    private string mySkillId;
    private KeyCode myKeyCode;
    
    [SerializeField] private Image skillImage;
    [SerializeField] private TMP_Text skillKey;
    [SerializeField] private TMP_Text coolTimeText;
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private GameObject coolTimeObject;
    [SerializeField] private Image coolTimeImage;
    [SerializeField] private Image stackCoolTimeImage;
    
    public event Action OnSkillDropped;

    public bool IsDash()
    {
        return myKeyCode == GameManager.Instance.dashKey;
    }
    
    public string GetSkillId()
    {
        return mySkillId;
    }

    public Sprite GetSprite()
    {
        return skillImage.sprite;
    }
    
    public void SetSkillInfo(KeyCode keyCode, string skillId, List<float> coolTime = null)
    {
        myKeyCode = keyCode;
        mySkillId = skillId;
        maxCoolTime = coolTime;
        
        skillKey.text = keyCode.ToString();
        skillImage.gameObject.SetActive(!string.IsNullOrEmpty(skillId));
        coolTimeText.gameObject.SetActive(!string.IsNullOrEmpty(skillId));
        coolTimeObject.SetActive(!string.IsNullOrEmpty(skillId));
        
        stackText.gameObject.SetActive(maxCoolTime != null && maxCoolTime.Count > 1);
        stackCoolTimeImage.gameObject.SetActive(maxCoolTime != null && maxCoolTime.Count > 1);

        if (string.IsNullOrEmpty(skillId))
            return;

        skillImage.sprite = GameManager.Instance.GetUISprite(skillId);
    }

    public void UpdateCoolTimeText(List<float> coolTime)
    {
        // 스택형 쿨타임 표시
        if (coolTime.Count > 1)
        {
            // 모든 스택을 소모하지 않았다면, 기본 쿨타임을 보여준다
            if (coolTime[2] > 0)
            {
                coolTimeText.gameObject.SetActive(coolTime[0] > 0);
                coolTimeObject.SetActive(coolTime[0] > 0);
                coolTimeText.text = coolTime[0].ToString("F1");
                coolTimeImage.fillAmount = coolTime[0] / maxCoolTime[0];
            }
            // 모든 스택을 소모하였다면, 스택 쿨타임을 기본 쿨타임으로 보여준다
            else
            {
                coolTimeText.gameObject.SetActive(coolTime[1] > 0);
                coolTimeObject.SetActive(coolTime[1] > 0);
                coolTimeText.text = coolTime[1].ToString("F1");
                coolTimeImage.fillAmount = coolTime[1] / maxCoolTime[1];
            }
            
            // 기본 쿨타임이 돌아가는동안은 스택 쿨타임이 보이지 않는다.
            stackText.gameObject.SetActive((int)coolTime[2] > 0);
            stackCoolTimeImage.gameObject.SetActive(coolTime[0] <= 0 && coolTime[2] > 0);
            
            stackText.text = ((int)coolTime[2]).ToString();
            stackCoolTimeImage.fillAmount = coolTime[1] / maxCoolTime[1];
        }
        // 일반형 쿨타임 표시, 기본 쿨타임을 보여준다
        else
        {
            coolTimeText.gameObject.SetActive(coolTime[0] > 0);
            coolTimeObject.SetActive(coolTime[0] > 0);
            coolTimeText.text = coolTime[0].ToString("F1");
            coolTimeImage.fillAmount = coolTime[0] / maxCoolTime[0];
        }
    }
    
    public void ExecuteSkillAction(string skillId)
    {
        GameManager.Instance.SetSkillId(myKeyCode, skillId);
        OnSkillDropped?.Invoke();  // Presenter에게 알림
    }
}
