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
    void SetSkillInfo(KeyCode keyCode, string skillId, float coolTime);
    void UpdateCoolTimeText(float coolTime);
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
    
    private void RefreshModel()
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
                _views[i].SetSkillInfo(_model.settingSkillList[i].keyCode, default, 0);
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
    private float maxCoolTime;
    private KeyCode myKeyCode;
    
    [SerializeField] private Image skillImage;
    [SerializeField] private TMP_Text skillKey;
    [SerializeField] private TMP_Text coolTimeText;
    [SerializeField] private GameObject coolTimeObject;
    [SerializeField] private Image coolTimeImage;
    public event Action OnSkillDropped;

    public void SetSkillInfo(KeyCode keyCode, string skillId, float coolTime)
    {
        myKeyCode = keyCode;
        maxCoolTime = coolTime;
        
        skillKey.text = keyCode.ToString();
        skillImage.gameObject.SetActive(!string.IsNullOrEmpty(skillId));
        coolTimeText.gameObject.SetActive(!string.IsNullOrEmpty(skillId));
        coolTimeObject.SetActive(!string.IsNullOrEmpty(skillId));
        
        if (string.IsNullOrEmpty(skillId))
            return;

        skillImage.sprite = GameManager.Instance.GetUISprite(skillId);
    }

    public void UpdateCoolTimeText(float coolTime)
    {
        coolTimeText.gameObject.SetActive(coolTime > 0);
        coolTimeObject.SetActive(coolTime > 0);
            
        coolTimeText.text = coolTime.ToString("F1");
        coolTimeImage.fillAmount = coolTime / maxCoolTime;
    }
    
    public void ExecuteSkillAction(string skillId)
    {
        GameManager.Instance.SetBerserkerSkillId(myKeyCode, skillId);
        OnSkillDropped?.Invoke();  // Presenter에게 알림
    }
}
