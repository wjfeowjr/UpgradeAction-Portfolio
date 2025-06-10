using TMPro;
using UnityEngine;

public interface IUIBossHpView
{
    void HideHp();
    
    void SetName(Character character);
    
    void SetHp(Character character);
    void SetHpText(Character character);
    void HpReduce(Character character, float speed);
    
    void SetStagger(Character character);
    void SetStaggerText(Character character);
    void StaggerReduce(Character character, float speed);
    Transform StaggerGaugeTransform();
}

public class UIBossHpModel
{
    public Character character;
}

public class UIBossHpPresenter
{
    private readonly IUIBossHpView _hpview;
    private UIBossHpModel _model;

    public UIBossHpPresenter(IUIBossHpView hpView)
    {
        _hpview = hpView;
    }

    public void SetModel(UIBossHpModel model)
    {
        _model = model;
        _hpview.SetName(_model.character);
    }
    
    public void HideHp()
    {
        _hpview.HideHp();
    }
    public void SetHp()
    {
        _hpview.SetHp(_model.character);
    }
    public void SetHpText()
    {
        _hpview.SetHpText(_model.character);
    }
    public void HpReduce()
    {
        _hpview.HpReduce(_model.character, 1.5f);
    }
    
    public void SetStagger()
    {
        _hpview.SetStagger(_model.character);
    }
    public void SetStaggerText()
    {
        _hpview.SetStaggerText(_model.character);
    }
    public void StaggerReduce()
    {
        _hpview.StaggerReduce(_model.character, 1.5f);
    }
    public Transform StaggerGaugeTransform()
    {
        return _hpview.StaggerGaugeTransform();
    }
}

public class UIBossHpView : MonoBehaviour, IUIBossHpView
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Gauge hpGauge;
    [SerializeField] private Gauge staggerGauge;

    public void HideHp()
    {
        nameText.gameObject.SetActive(false);
        hpGauge.gameObject.SetActive(false);
        staggerGauge.gameObject.SetActive(false);
    }
    
    // 이름
    public void SetName(Character character)
    {
        if(!nameText.gameObject.activeSelf)
            nameText.gameObject.SetActive(true);

        nameText.text = character.BasicStat.name;
    }

    // 체력
    public void SetHp(Character character)
    {
        if(character.BasicStat.hp > 0 && !hpGauge.gameObject.activeSelf)
            hpGauge.gameObject.SetActive(true);

        hpGauge.GaugeSetting(character.BasicStat.hp, character.BasicStat.maxHp);
        SetHpText(character);
        
        if(character.BasicStat.hp <= 0)
            hpGauge.gameObject.SetActive(false);
    }
    public void SetHpText(Character character)
    {
        hpGauge.DisplayValue(character);
    }
    public void HpReduce(Character character, float speed)
    {
        hpGauge.GaugeReduce(character.BasicStat.hp, character.BasicStat.maxHp, speed);
    }
    
    // 무력화
    public void SetStagger(Character character)
    {
        if (character.BasicStat.bodyType != EBodyType.StrongArmor && character.BasicStat.bodyType != EBodyType.HyperArmor)
            return;
        
        if(!staggerGauge.gameObject.activeSelf)
            staggerGauge.gameObject.SetActive(true);
        
        staggerGauge.GaugeSetting(character.BasicStat.stagger, character.BasicStat.maxStagger);
        SetStaggerText(character);

        if (character.BasicStat.stagger <= 0)
        {
            // 터지는 연출 넣기
            staggerGauge.gameObject.SetActive(false);
        }
    }
    public void SetStaggerText(Character character)
    {
        if (character.BasicStat.bodyType != EBodyType.StrongArmor && character.BasicStat.bodyType != EBodyType.HyperArmor)
            return;
        
        staggerGauge.DisplayValue(character);
    }
    public void StaggerReduce(Character character, float speed)
    {
        if (character.BasicStat.bodyType != EBodyType.StrongArmor && character.BasicStat.bodyType != EBodyType.HyperArmor)
            return;
        
        staggerGauge.GaugeReduce(character.BasicStat.stagger, character.BasicStat.maxStagger, speed);
    }

    public Transform StaggerGaugeTransform()
    {
        return staggerGauge.transform;
    }
}
