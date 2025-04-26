using System;
using System.Collections;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TotalBar : MonoBehaviour
{
    [SerializeField] private Character castCharacter;

    [SerializeField] private Gauge hpBar;
    [SerializeField] private Gauge staggerBar;
    //[SerializeField] private Gauge passiveBar;
    
    private void OnEnable()
    {
        if(hpBar) 
            hpBar.GaugeMax();
        if(staggerBar) 
            staggerBar.GaugeMax();
    }

    public void SetCastCharacter(Character character)
    {
        castCharacter = character;
        SetGauge(castCharacter);
    }

    // 패시브 게이지 세팅 (객체화 시킬컷)
    public void SettingPassiveBar()
    {
        if(!castCharacter)
            return;

        // if(passiveBar) 
        //     passiveBar.SettingColorAndObject(castPlayer.UniqueId);
    }

    // 체력 게이지 감소
    public void ReduceHpBar(float currentValue, float maxValue, float speed)
    {
        if (!hpBar)
            return;
        
        DisplayHp();
        hpBar.GaugeReduce(currentValue, maxValue, speed);
    }

    // Hp글씨 표시해서 보여주기
    private void DisplayHp()
    {
        if (castCharacter.GetComponent<Player>())
        {
            hpBar.DisplayPercent(castCharacter);
            return;
        }
        if (castCharacter.GetComponent<Monster>())
        {
            hpBar.DisplayValue(castCharacter);
            return;
        }
    }

    // 무력화 게이지 감소
    public void StaggerBarReduce(float currentValue, float maxValue, float speed)
    {
        if (castCharacter.ImmuneStagger || !staggerBar.gameObject.activeSelf)
            return;

        if (currentValue > 0)
            staggerBar.GaugeReduce(currentValue, maxValue, speed);
        // else
        //     staggerBar.CrashStaggerBar();
    }
    
    // 무력화 게이지 재생성
    public void RespawnStaggerBar()
    {
        if(!staggerBar)
            return;
        
        staggerBar.GaugeMax();
        staggerBar.gameObject.SetActive(true);
        staggerBar.transform.localScale = new Vector3(0, 1, 1);
        staggerBar.transform.DOScale(Vector3.one, 0.5f);
    }

    // 게이지 최신화
    public void SetGauge(Character character)
    {
        if (hpBar)
        {
            DisplayHp();
            hpBar.GaugeSetting(character.BasicStat.hp, character.BasicStat.maxHp);
        }

        // 무력화 게이지가 존재한다면 무력화 게이지에 값을 넣어준다
        if (staggerBar)
        {
            staggerBar.GaugeSetting(character.BasicStat.stagger, character.BasicStat.maxStagger);
            // 스트롱 아머, 하이퍼 아머가 아닐 경우
            staggerBar.gameObject.SetActive(castCharacter.BasicStat.bodyType is EBodyType.StrongArmor or EBodyType.HyperArmor);
        }
    }
    
    // public void SetGauge(float currentHp, float maxHp, float currentStagger = 0, float maxStagger = 0)
    // {
    //     if (hpBar)
    //     {
    //         DisplayHp();
    //         hpBar.GaugeSetting(currentHp, maxHp);
    //     }
    //
    //     // 무력화 게이지가 존재한다면 무력화 게이지에 값을 넣어준다
    //     if (staggerBar)
    //     {
    //         staggerBar.GaugeSetting(currentStagger, maxStagger);
    //         // 스트롱 아머, 하이퍼 아머가 아닐 경우
    //         staggerBar.gameObject.SetActive(castCharacter.GetBodyType() is EBodyType.StrongArmor or EBodyType.HyperArmor);
    //     }
    // }
    
    // 비활성화
    public void ActiveObject(bool active)
    {
        gameObject.SetActive(active);
    }
}