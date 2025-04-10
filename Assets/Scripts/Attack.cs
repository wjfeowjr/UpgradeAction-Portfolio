using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public enum EEffectType
{
    Damaged,
    Airborne,
    Stun
}
public enum EDirectionType
{
    Fixed,
    Relative
}

[Serializable]
public class AttackInfo
{
    public string id;
    public EEffectType effectType;
    public float effectTime;
    public EDirectionType directionType;
    public int coefficient;
    public float knockBack;
    public Vector2 upperPower;
    public float colliderTime;
    public string hitEffectId;
}
public class Attack : MonoBehaviour
{
    [SerializeField] private AttackInfo attackInfo;
    private Player castChar;
    private Transform traceTransform;
    private Collider2D myCollider;
    
    private float dir;
    private float leftColliderTime;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }
    private void Update()
    {
        ColliderTimer();
    }

    public void SetupData(Player player, AttackData attackData)
    {
        castChar = player;
        dir = player.transform.localScale.x;
        
        attackInfo = new AttackInfo();
        attackInfo.id = attackData.id;
        attackInfo.effectType = (EEffectType)Enum.Parse(typeof(EEffectType), attackData.effectType);
        attackInfo.effectTime = attackData.effectTime;
        attackInfo.directionType = (EDirectionType)Enum.Parse(typeof(EDirectionType), attackData.directionType);
        attackInfo.coefficient = attackData.coefficient;
        attackInfo.knockBack = attackData.knockBack;
        
        if (string.IsNullOrEmpty(attackData.upperPower))
        {
            attackInfo.upperPower = Vector2.zero;
        }
        else
        {
            var upperPowerSplit = attackData.upperPower.Split(';');
            attackInfo.upperPower = new Vector2(float.Parse(upperPowerSplit[0]), float.Parse(upperPowerSplit[1]));
        }

        attackInfo.colliderTime = attackData.colliderTime;
        attackInfo.hitEffectId = attackData.hitEffectId;
    }

    public void EnableSetting()
    {
        myCollider.enabled = true;
        leftColliderTime = 0;
    }
    
    private void ColliderTimer()
    {
        if (attackInfo.colliderTime == 0)
            return;
        
        leftColliderTime += Time.deltaTime;

        if (leftColliderTime >= attackInfo.colliderTime)
            myCollider.enabled = false;
    }
}
