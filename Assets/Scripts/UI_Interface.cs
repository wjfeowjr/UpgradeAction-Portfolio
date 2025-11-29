using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class UI_Interface : UIBase
{
    // 콤보
    public UIComboView ComboView => comboView;
    [SerializeField] private UIComboView comboView;
    private UIComboPresenter uiComboPresenter;
    public UIComboPresenter ComboPresenter => uiComboPresenter;
    
    // 캐릭터 얼굴
    public UICharacterFaceView CharacterFaceView => characterFaceView;
    [SerializeField] private UICharacterFaceView characterFaceView;
    private UICharacterFacePresenter uiCharacterFacePresenter;

    // 체력
    public UIHpView HpView => hpView;
    [SerializeField] private UIHpView hpView;
    private UIHpPresenter uiHpPresenter;
    public UIHpPresenter HpPresenter => uiHpPresenter;
    
    // 재화
    public UIGoodsView GoodsView => goodsView;
    [SerializeField] private UIGoodsView goodsView;
    private UIGoodsPresenter uiGoodsPresenter;
    public UIGoodsPresenter GoodsPresenter => uiGoodsPresenter;
    
    // 보스체력
    public UIBossHpView BossHpView => bossHpView;
    [SerializeField] private UIBossHpView bossHpView;
    private UIBossHpPresenter uiBossHpPresenter;
    public UIBossHpPresenter BossHpPresenter => uiBossHpPresenter;
    
    // 스킬
    public UISkillView SkillView => skillView;
    public List<UISkillView> SkillViews => skillViews;
    [SerializeField] private RectTransform skillLayout;
    [SerializeField] private UISkillView skillView;
    [SerializeField] private List<UISkillView> skillViews;
    private UISkillPresenter uiSkillPresenter;

    public Vector3 GetTooltipPos()
    {
        return new Vector2(skillLayout.position.x + 1.9f, skillLayout.position.y + 0.8f);
    }
    
    public Vector3 GetDashSkillPos()
    {
        return new Vector2(skillViews[0].transform.position.x, skillViews[0].transform.position.y - 0.06f);
    }
    
    public void SetComboPresenter(UIComboPresenter presenter)
    {
        uiComboPresenter = presenter;
    }
    
    public void SetCharacterFacePresenter(UICharacterFacePresenter presenter)
    {
        uiCharacterFacePresenter = presenter;
    }
    
    public void SetHpPresenter(UIHpPresenter presenter)
    {
        uiHpPresenter = presenter;
    }
    
    public void SetGoodsPresenter(UIGoodsPresenter presenter)
    {
        uiGoodsPresenter = presenter;
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