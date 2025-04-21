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
    void SetSkillInfo(KeyCode keyCode, string skillId);
    void UpdateCoolTimeText(float coolTime);
}

public class UICommonModel
{
    public List<SettingSkill> settingSkillList = new List<SettingSkill>();
}

public class UISkillPresenter
{
    private readonly List<IUISkillView> _views;
    private readonly UICommonModel _model;

    public UISkillPresenter(List<IUISkillView> views, UICommonModel model)
    {
        _views = views;
        _model = model;
    }

    public void SetSkillInfo()
    {
        for (int i = 0; i < _model.settingSkillList.Count; i++)
        {
            _views[i].SetSkillInfo(_model.settingSkillList[i].keyCode, _model.settingSkillList[i].skillId);
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
    [SerializeField] private Image skillImage;
    [SerializeField] private TMP_Text skillKey;
    [SerializeField] private TMP_Text coolTimeText;

    public void SetSkillInfo(KeyCode keyCode, string skillId)
    {
        skillKey.text = keyCode.ToString();
        
        if (string.IsNullOrEmpty(skillId))
        {
            skillImage.gameObject.SetActive(false);
            coolTimeText.gameObject.SetActive(false);
            return;
        }

        skillImage.sprite = GameManager.Instance.GetUISprite(skillId);
    }

    public void UpdateCoolTimeText(float coolTime)
    {
        coolTimeText.gameObject.SetActive(coolTime > 0);
        coolTimeText.text = coolTime.ToString("F1");
    }
}
