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

    // 체력
    public UIHpView HpView => hpView;
    [SerializeField] private UIHpView hpView;
    private UIHpPresenter uiHpPresenter;
    public UIHpPresenter HpPresenter => uiHpPresenter;
    
    // 획득 아이템
    public UIObjectInfoView ObjectInfoView => objectInfoView;
    [SerializeField] private UIObjectInfoView objectInfoView;
    private UIObjectInfoPresenter uiObjectInfoPresenter;
    public UIObjectInfoPresenter ObjectInfoPresenter => uiObjectInfoPresenter;
    
    // 재화
    public UIGoodsView GoodsView => goodsView;
    [SerializeField] private UIGoodsView goodsView;
    private UIGoodsPresenter uiGoodsPresenter;

    // 보스체력
    public UIBossHpView BossHpView => bossHpView;
    [SerializeField] private UIBossHpView bossHpView;
    private UIBossHpPresenter uiBossHpPresenter;
    public UIBossHpPresenter BossHpPresenter => uiBossHpPresenter;

    // 지역명
    public UIPlaceNameView PlaceNameView => placeNameView;
    [SerializeField] private UIPlaceNameView placeNameView;
    private UIPlaceNamePresenter uiPlaceNamePresenter;
    public UIPlaceNamePresenter PlaceNamePresenter => uiPlaceNamePresenter;
    
    // 스킬
    public UISkillView ChangeSkillView => changeSkillView;
    public UISkillView PotionSkillView => potionSkillView;
    
    public List<UISkillView> SkillViews => skillViews;
    [SerializeField] private RectTransform skillLayout;
    [SerializeField] private RectTransform waitingCharacterPos;
    [SerializeField] private UISkillView changeSkillView;
    [SerializeField] private UISkillView potionSkillView;
    [SerializeField] private List<UISkillView> skillViews;
    private UISkillPresenter uiSkillPresenter;
    public UISkillPresenter SkillPresenter => uiSkillPresenter;
    
    public Vector3 GetTooltipPos()
    {
        return new Vector2(skillLayout.position.x + 1.9f, skillLayout.position.y + 0.8f);
    }
    
    public Vector3 GetDashSkillPos()
    {
        return new Vector2(skillViews[0].transform.position.x, skillViews[0].transform.position.y - 0.1f);
    }

    public Vector3 GetWaitingCharacterPos()
    {
        return waitingCharacterPos.position;
    }
    
    public void SetComboPresenter(UIComboPresenter presenter)
    {
        uiComboPresenter = presenter;
    }

    public void SetHpPresenter(UIHpPresenter presenter)
    {
        uiHpPresenter = presenter;
    }
    
    public void SetObjectInfoPresenter(UIObjectInfoPresenter presenter)
    {
        uiObjectInfoPresenter = presenter;
    }
    
    public void SetGoodsPresenter(UIGoodsPresenter presenter)
    {
        uiGoodsPresenter = presenter;
    }
    
    public void SetBossHpPresenter(UIBossHpPresenter presenter)
    {
        uiBossHpPresenter = presenter;
    }

    public void SetPlaceNamePresenter(UIPlaceNamePresenter presenter)
    {
        uiPlaceNamePresenter = presenter;
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