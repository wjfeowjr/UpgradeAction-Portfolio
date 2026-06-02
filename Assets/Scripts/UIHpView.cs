using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IUIHpView
{
    void SetHp(Player player);
    void SetHpText(Player player);
    void HpReduce(Player player, float speed);
    
    void SetResource(Player player);
    void SetResourceText(Player player);
}

public class UIHpModel
{
    public Player player;
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
        _hpview.SetHp(_model.player);
    }
    public void SetHpText()
    {
        _hpview.SetHpText(_model.player);
    }
    public void HpReduce()
    {
        _hpview.HpReduce(_model.player, 1.5f);
    }
    
    public void SetResource()
    {
        _hpview.SetResource(_model.player);
    }
    
    public void SetResourceText()
    {
        _hpview.SetResourceText(_model.player);
    }
}

public class UIHpView : MonoBehaviour, IUIHpView
{
    [SerializeField] private Gauge hpGauge;
    [SerializeField] private Gauge resourceGauge;
    [SerializeField] private Sprite[] resourceSprite;
    
    public void SetHp(Player player)
    {
        hpGauge.GaugeSetting(player.BasicStat.hp, player.BasicStat.maxHp);
        SetHpText(player);
    }
    
    public void SetHpText(Player player)
    {
        hpGauge.DisplayHp(player);
    }
    
    public void HpReduce(Player player, float speed)
    {
        hpGauge.GaugeReduce(player.BasicStat.hp, player.BasicStat.maxHp, speed);
    }
    
    public void SetResource(Player player)
    {
        resourceGauge.GaugeSetting(player.PlayerStat.resource, player.PlayerStat.maxResource);
        switch (player.BasicStat.id)
        {
            case ConstValues.Berserker:
                resourceGauge.SetGaugeSprite(resourceSprite[0]);
                break;
            
            case ConstValues.Gunner:
                resourceGauge.SetGaugeSprite(resourceSprite[1]);
                break;
            
            case ConstValues.Fighter:
                resourceGauge.SetGaugeSprite(resourceSprite[2]);
                break;
        }
        SetResourceText(player);
    }
    
    public void SetResourceText(Player player)
    {
        resourceGauge.DisplayResource(player);
    }
}
