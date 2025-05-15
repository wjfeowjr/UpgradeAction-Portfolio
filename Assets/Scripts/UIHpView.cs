using System;
using System.Collections.Generic;
using UnityEngine;

public interface IUIHpView
{
    void SetHp(Character character);
    void SetHpText(Character character);
    void HpReduce(Character character, float speed);
}

public class UIHpModel
{
    public Character character;
}

public class UIHpPresenter
{
    private readonly IUIHpView _hpview;
    private UIHpModel _model;

    public UIHpPresenter(IUIHpView hpView, UIHpModel model)
    {
        _hpview = hpView;
        _model = model;
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
}

public class UIHpView : MonoBehaviour, IUIHpView
{
    [SerializeField] private Gauge hpGauge;

    public void SetHp(Character character)
    {
        hpGauge.GaugeSetting(character.BasicStat.hp, character.BasicStat.maxHp);
        SetHpText(character);
    }
    
    public void SetHpText(Character character)
    {
        hpGauge.DisplayValue(character);
    }
    
    public void HpReduce(Character character, float speed)
    {
        hpGauge.GaugeReduce(character.BasicStat.hp, character.BasicStat.maxHp, speed);
    }
}
