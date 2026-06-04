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
    [SerializeField] private Gauge shieldGauge;
    [SerializeField] private Gauge resourceGauge;
    [SerializeField] private Sprite[] resourceSprite;
    
    public void SetHp(Player player)
    {
        var hp = player.BasicStat.hp;
        var shield = player.BasicStat.shield;
        // 총량이 최대체력을 넘을 때만 게이지가 확장된다
        var finalHp = Mathf.Max(player.BasicStat.maxHp, hp + shield);

        // shieldGauge(흰색)는 hpGauge 뒤에 두고 (hp+shield)까지 채워, 체력 오른쪽 구간만 보이게 함
        shieldGauge.GaugeSetting(hp + shield, finalHp);
        hpGauge.GaugeSetting(hp, finalHp);
        SetHpText(player);
    }
    
    public void SetHpText(Player player)
    {
        hpGauge.DisplayHp(player);
    }
    
    public void HpReduce(Player player, float speed)
    {
        var hp = player.BasicStat.hp;
        var shield = player.BasicStat.shield;
        var finalHp = Mathf.Max(player.BasicStat.maxHp, hp + shield);

        shieldGauge.GaugeSetting(hp + shield, finalHp);
        hpGauge.GaugeReduce(hp, finalHp, speed);
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
