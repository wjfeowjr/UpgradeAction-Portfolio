using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public enum EEffectType
{
    Damaged,
    Airborne
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
    public bool flipScale;
    public bool tracePos;
    public float colliderTime;
    public float objectTime;
    public string hitEffectId;
    public string sound;
}
public class Attack : MonoBehaviour
{
    [SerializeField] private AttackInfo attackInfo;
    private Player castChar;

    private Transform traceTransform;
    private Vector3 defaultScale;
    private Vector3 reverseScale;
    private Collider2D myCollider;
    
    private float leftColliderTime;
    private float leftObjectTime;
    
    private void Awake()
    {
        defaultScale = transform.localScale;
        reverseScale = new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z);
        myCollider = GetComponent<Collider2D>();
    }
    private void Update()
    {
        Timer();
        Trace();
    }

    public float GetObjectTime()
    {
        return attackInfo.objectTime;
    }

    public void SetupData(Player player, AttackData attackData, Transform attackTransform)
    {
        castChar = player;
        
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
        
        attackInfo.flipScale = attackData.flipScale;
        attackInfo.tracePos = attackData.tracePos;
        attackInfo.colliderTime = attackData.colliderTime;
        attackInfo.objectTime = attackData.objectTime;
        attackInfo.hitEffectId = attackData.hitEffectId;
        attackInfo.sound = attackData.sound;

        if (attackInfo.tracePos)
            traceTransform = attackTransform;
    }

    public void EnableSetting()
    {
        myCollider.enabled = true;
        leftColliderTime = 0;
        leftObjectTime = 0;

        if (!attackInfo.flipScale)
            return;
        
        transform.localScale = castChar.transform.localScale.x > 0 ? defaultScale : reverseScale;
    }
    
    private void Timer()
    {
        if (attackInfo.colliderTime == 0 && attackInfo.objectTime == 0)
            return;
        
        leftColliderTime += Time.deltaTime;
        leftObjectTime += Time.deltaTime;
        
        if (leftColliderTime >= attackInfo.colliderTime)
            myCollider.enabled = false;
        if (leftObjectTime >= attackInfo.objectTime)
            gameObject.SetActive(false);
    }
    private void Trace()
    {
        if (traceTransform)
            transform.position = traceTransform.position;
    }
}
