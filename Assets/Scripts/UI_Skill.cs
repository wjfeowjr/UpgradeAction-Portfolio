using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UI_Skill : UIBase
{
    // SkillViews를 외부에서 접근할 수 있게 public으로 변경
    public UISkillView ChangeCharacter => changeCharacter;
    public List<UISkillView> SkillViews => skillViews;

    [SerializeField] private UISkillView changeCharacter;
    [SerializeField] private List<UISkillView> skillViews;

    private UISkillPresenter uiSkillPresenter;

    // UIManager.BindPresenter에서 호출됨
    public void SetPresenter(UISkillPresenter presenter)
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