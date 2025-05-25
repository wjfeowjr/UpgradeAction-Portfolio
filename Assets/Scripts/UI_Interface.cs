using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UI_Interface : UIBase
{
    // 콤보
    public UIComboView ComboView => comboView;
    [SerializeField] private UIComboView comboView;
    private UIComboPresenter uiComboPresenter;
    public UIComboPresenter ComboPresenter => uiComboPresenter;
    
    // 체력
    public UIHpView HpView => hpView;
    [SerializeField] private UIHpView hpView;
    private UIHpPresenter uiHpPresenter;
    public UIHpPresenter HpPresenter => uiHpPresenter;
    
    // 보스체력
    public UIBossHpView BossHpView => bossHpView;
    [SerializeField] private UIBossHpView bossHpView;
    private UIBossHpPresenter uiBossHpPresenter;
    public UIBossHpPresenter BossHpPresenter => uiBossHpPresenter;
    
    // 스킬
    public UISkillView ChangeCharacter => changeCharacter;
    public List<UISkillView> SkillViews => skillViews;
    [SerializeField] private UISkillView changeCharacter;
    [SerializeField] private List<UISkillView> skillViews;
    private UISkillPresenter uiSkillPresenter;

    public void SetComboPresenter(UIComboPresenter presenter)
    {
        uiComboPresenter = presenter;
    }
    
    public void SetHpPresenter(UIHpPresenter presenter)
    {
        uiHpPresenter = presenter;
    }
    
    public void SetBossHpPresenter(UIBossHpPresenter presenter)
    {
        uiBossHpPresenter = presenter;
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
        //uiSkillPresenter?.OnSkillDroppedCleanUp();
    }
}