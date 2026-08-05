using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class UI_Interface : UIBase
{
    // Presenter 는 각 View 가 직접 들고 있다. 여기서는 그걸 그대로 넘겨준다.
    // 이전에는 UI_Interface 가 Presenter 를 따로 보관하고, 호출부가 만들어서
    // SetXxxPresenter 로 되돌려주는 구조였다. 같은 참조를 두 곳에서 들고 있었다.

    // 콤보
    public UIComboView ComboView => comboView;
    [SerializeField] private UIComboView comboView;

    // 캐릭터 얼굴
    public UICharacterFaceView CharacterFaceView => characterFaceView;
    [SerializeField] private UICharacterFaceView characterFaceView;

    // 체력
    public UIHpView HpView => hpView;
    [SerializeField] private UIHpView hpView;

    // 획득 아이템
    public UIObjectInfoView ObjectInfoView => objectInfoView;
    [SerializeField] private UIObjectInfoView objectInfoView;

    // 재화
    public UIGoodsView GoodsView => goodsView;
    [SerializeField] private UIGoodsView goodsView;

    // 보스체력
    public UIBossHpView BossHpView => bossHpView;
    [SerializeField] private UIBossHpView bossHpView;

    // 지역명
    public UIPlaceNameView PlaceNameView => placeNameView;
    [SerializeField] private UIPlaceNameView placeNameView;

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
    
    // 스킬 Presenter 는 교체/포션/일반 스킬 View 를 한꺼번에 다룬다.
    // View 하나가 자기 것만 조립하는 방식으로는 만들 수 없어,
    // 세 View 를 모두 들고 있는 UI_Interface 가 조립한다.
    // 모델 자체가 아니라 "모델을 만드는 방법"을 넘긴다.
    // 스킬 교체·키 설정 변경 시 Presenter 가 스스로 다시 읽어야 하는데,
    // 그 데이터를 어디서 가져오는지는 Presenter 가 알 필요가 없다.
    public UISkillPresenter BindSkill(Func<UISkillModel> modelSource)
    {
        var skillInterfaces = skillViews.ConvertAll(v => (IUISkillView)v);
        uiSkillPresenter = new UISkillPresenter(changeSkillView, potionSkillView, skillInterfaces, modelSource);
        return uiSkillPresenter;
    }

    private void Update()
    {
        uiSkillPresenter?.UpdateSkillCoolTime();
    }

    private void OnDisable()
    {
    }
}