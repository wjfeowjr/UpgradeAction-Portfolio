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
    
    // 스킬
    public UISkillView ChangeCharacter => changeCharacter;
    public List<UISkillView> SkillViews => skillViews;
    [SerializeField] private UISkillView changeCharacter;
    [SerializeField] private List<UISkillView> skillViews;
    private UISkillPresenter uiSkillPresenter;
    
    // 에피소드
    public UIEpisodeView EpisodeView => episodeView;
    [SerializeField] private UIEpisodeView episodeView;
    private UIEpisodePresenter uiEpisodePresenter;
    public UIEpisodePresenter EpisodePresenter => uiEpisodePresenter;
    
    public void SetComboPresenter(UIComboPresenter presenter)
    {
        uiComboPresenter = presenter;
    }
    
    public void SetHpPresenter(UIHpPresenter presenter)
    {
        uiHpPresenter = presenter;
    }
    
    public void SetSkillPresenter(UISkillPresenter presenter)
    {
        uiSkillPresenter = presenter;
    }

    public void SetEpisodePresenter(UIEpisodePresenter presenter)
    {
        uiEpisodePresenter = presenter;
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