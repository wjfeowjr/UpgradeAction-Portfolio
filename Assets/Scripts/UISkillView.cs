using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IUICommonView
{
    void UpdateCoolTimeText(string text);
}

public class UICommonModel
{
    public List<PlayerSkill> skillList = new List<PlayerSkill>();
}

public class UISkillPresenter
{
    private readonly IUICommonView _view;
    private readonly UICommonModel _model;

    public UISkillPresenter(IUICommonView view, UICommonModel model)
    {
        _view = view;
        _model = model;
    }
    
    public void StartCoolTimeUpdater()
    {
        if (_model.skillList.Count <= 0)
            return;
        
        var remaining = $"{_model.skillList[0].GetRemainingCooldown():F1}";
        _view.UpdateCoolTimeText(remaining);
    }
}

public class UISkillView : UIBase, IUICommonView
{
    [SerializeField] private TMP_Text coolTimeText;

    public void UpdateCoolTimeText(string text)
    {
        coolTimeText.text = text;
    }
}
