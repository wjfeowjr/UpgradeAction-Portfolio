using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UISkillPanel : MonoBehaviour
{
    [SerializeField] private List<UISkillView> skillViews;
    private UISkillPresenter uiSkillPresenter;

    private void Start()
    {
        // IUISkillView로 캐스팅
        var viewInterfaces = skillViews.ConvertAll(v => (IUISkillView)v);
        var model = new UICommonModel
        {
            settingSkillList = GameManager.Instance.GetSettingSkillList(),
        };
        uiSkillPresenter = new UISkillPresenter(viewInterfaces, model);
        
        // 처음 한 번 갱신
        uiSkillPresenter.SetSkillInfo();
    }

    private void Update()
    {
        uiSkillPresenter?.UpdateSkillCoolTime();
    }
}