using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UI_Interface : UIBase
{
    // 체력
    public UIHpView HpView => hpView;
    
    [SerializeField] private UIHpView hpView;
    private UIHpPresenter uiHpPresenter;
    public UIHpPresenter HpPresenter => uiHpPresenter;
    
    // 스킬
    public UISkillView ChangeCharacter => changeCharacter;
    public List<UISkillView> SkillViews => skillViews;

    [SerializeField] private UISkillView changeCharacter;
    [SerializeField] private List<UISkillView> skillViews;
    
    private UISkillPresenter uiSkillPresenter;
    
    public void SetHpPresenter(UIHpPresenter presenter)
    {
        uiHpPresenter = presenter;
    }
    
    public void SetSkillPresenter(UISkillPresenter presenter)
    {
        uiSkillPresenter = presenter;
    }

    private void Update()
    {
        uiSkillPresenter?.UpdateSkillCoolTime();
    }

    private void OnDisable()
    {
        uiSkillPresenter?.OnSkillDroppedCleanUp();
    }
}