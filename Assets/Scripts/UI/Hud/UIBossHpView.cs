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
    void StaggerExplosion(Character character, Transform targetTransform);
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

    public void StaggerExplosion(Character character, Transform targetTransform)
    {
        _hpview.StaggerExplosion(character, targetTransform);
    }
}

public class UIBossHpView : MonoBehaviour, IUIBossHpView
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Gauge hpGauge;
    [SerializeField] private Gauge staggerGauge;
    private Character staggerCharacter;   // 무력화 게이지가 현재 표시 중인 보스

    public void HideHp()
    {
        staggerGauge.gameObject.SetActive(false);
        gameObject.SetActive(false);
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
        if (character.BasicStat.hp > 0 && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            hpGauge.GaugeSetting(character.BasicStat.hp, character.BasicStat.maxHp);
        }
        
        SetHpText(character);
        if(character.BasicStat.hp <= 0)
            gameObject.SetActive(false);
    }
    public void SetHpText(Character character)
    {
        hpGauge.DisplayHp(character);
    }
    public void HpReduce(Character character, float speed)
    {
        hpGauge.GaugeReduce(character.BasicStat.hp, character.BasicStat.maxHp, speed);
        if (character.BasicStat.hp <= 0)
        {
            gameObject.SetActive(false);
            staggerGauge.gameObject.SetActive(false);
        }
    }
    
    // 무력화
    public void SetStagger(Character character)
    {
        if (character.BasicStat.bodyType != EBodyType.StrongArmor && character.BasicStat.bodyType != EBodyType.HyperArmor)
            return;

        // 게이지가 새로 켜지거나, 다른 보스로 바뀌거나, 수치가 가득 찼을 때(무력화 회복)만 fill을 즉시 고정.
        // 전투 중 매 타격마다 고정하면 reduce 트레일 연출이 지워진다
        bool gaugeOff = !staggerGauge.gameObject.activeSelf;
        bool changedBoss = staggerCharacter != character;
        bool fullStagger = character.BasicStat.stagger >= character.BasicStat.maxStagger;
        if (gaugeOff || changedBoss || fullStagger)
        {
            staggerGauge.gameObject.SetActive(true);
            staggerGauge.GaugeSetting(character.BasicStat.stagger, character.BasicStat.maxStagger);
            staggerCharacter = character;
        }
        
        character.ImmuneStagger = false;
        
        SetStaggerText(character);
        if (character.BasicStat.stagger <= 0)
            staggerGauge.gameObject.SetActive(false);
    }
    public void SetStaggerText(Character character)
    {
        if (character.BasicStat.bodyType != EBodyType.StrongArmor && character.BasicStat.bodyType != EBodyType.HyperArmor)
            return;
        
        staggerGauge.DisplayHp(character);
    }
    public void StaggerReduce(Character character, float speed)
    {
        if (character.BasicStat.bodyType != EBodyType.StrongArmor && character.BasicStat.bodyType != EBodyType.HyperArmor)
            return;
        
        staggerGauge.GaugeReduce(character.BasicStat.stagger, character.BasicStat.maxStagger, speed);
    }

    public void StaggerExplosion(Character character, Transform targetTransform)
    {
        if (character.BasicStat.bodyType != EBodyType.StrongArmor && character.BasicStat.bodyType != EBodyType.HyperArmor) 
            return;
        
        GameManager.Instance.SpawnToUIPool(ConstValues.StaggerExplosionUI, targetTransform.position);
    }

    public Transform StaggerGaugeTransform()
    {
        return staggerGauge.transform;
    }
}
