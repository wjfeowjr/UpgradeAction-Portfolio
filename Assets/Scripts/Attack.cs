using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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
    private Character castChar;
    private Transform traceTransform;
    private Collider2D myCollider;
    protected List<Collider2D> targetColliders = new List<Collider2D>();
    
    private int dir;
    private float leftColliderTime;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }
    private void Update()
    {
        ColliderTimer();
    }

    public void SetupData(Character character, AttackData attackData)
    {
        castChar = character;

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
        TargetColReset();
        
        dir = castChar.transform.localScale.x > 0 ? 1 : -1;
    }
    
    private void ColliderTimer()
    {
        if (attackInfo.colliderTime == 0)
            return;
        
        leftColliderTime += Time.deltaTime;

        if (leftColliderTime >= attackInfo.colliderTime)
            myCollider.enabled = false;
    }
    
    // 충돌한 콜라이더 무시
    private void IgnoreCol(Collider2D col)
    {
        Physics2D.IgnoreCollision(myCollider, col, true);
        targetColliders.Add(col);
    }
    // 무시한 콜라이더 리셋
    private void TargetColReset()
    {
        foreach (var targetCollider in targetColliders)
            Physics2D.IgnoreCollision(myCollider, targetCollider, false);

        targetColliders.Clear();
    }

    private bool GetCritical()
    {
        var critPercent = Random.Range(0, 100);
        return critPercent < castChar.GetBasicStat().criticalChance;
    }
    
    private int GetDamage(bool isCrit)
    {
        // 원래 주는 피해량
        float originDamage = castChar.GetBasicStat().power * attackInfo.coefficient * 0.01f;
        // 룬, 특성에 따라서 최종적인 대미지의 피해량이 높아짐
        int finalDamage = (int)originDamage;
        if (isCrit)
        {
            float critDamage = originDamage * castChar.GetBasicStat().criticalDamage * 0.01f;
            finalDamage = (int)critDamage;
        }

        return finalDamage;
    }

    // 공격판정 적용
    // if (col.GetComponent<Monster>() != null)
    // hitTarget.LookAt(transform.position.x);
    private void OnTriggerEnter2D(Collider2D col)
    {
        var hitTarget = col.GetComponent<Character>();
        if (hitTarget != null)
        {
            if(hitTarget.GetImmortal())
                return;
            
            // 플레이어의 공격
            if (castChar.GetComponent<Player>())
            {
                if (col.GetComponent<Monster>() == null)
                    return;
                
                // 스프라이트가 점멸한다
                hitTarget.HitMaterial();
            }
            // 몬스터의 공격
            if (castChar.GetComponent<Monster>())
            {
                if (col.GetComponent<Player>() == null)
                    return;
            }

            // 피격이팩트 생성
            hitTarget.SpawnHitEffect(attackInfo.hitEffectId, 0.5f);
            
            // 대상이 피해를 입는다(치명타 피해인지 확인)
            bool critical = GetCritical();
            int damage = GetDamage(critical);
            
            // 피해입기
            hitTarget.TakeDamage(damage);
            // 폰트소환
            hitTarget.SpawnDamageFont(damage, critical);

            // 피해를 입고, 체력이 0으로 떨어지면 죽는다
            if (hitTarget.GetBasicStat().hp <= 0)
            {
                hitTarget.Die();
                return;
            }

            var upperPowerX = attackInfo.upperPower.x;
            var knockBackX = attackInfo.knockBack;
            
            switch (attackInfo.directionType)
            {
                // 한 쪽으로만 밀어내는 판정
                case EDirectionType.Fixed:
                    upperPowerX = dir * Math.Abs(upperPowerX);
                    knockBackX = dir * Math.Abs(knockBackX);
                    break;
                
                // 내 위치 기준으로 양 옆으로 밀어내는 판정
                case EDirectionType.Relative:
                    if (transform.position.x > hitTarget.transform.position.x)
                    {
                        upperPowerX = -Math.Abs(upperPowerX);
                        knockBackX = -Math.Abs(knockBackX);
                    }
                    else
                    {
                        upperPowerX = Math.Abs(upperPowerX);
                        knockBackX = Math.Abs(knockBackX);
                    }
                    break;
            }
            
            if (hitTarget.GetAirborneState() || hitTarget.GetJumpState())
            {
                hitTarget.Airborne(upperPowerX, attackInfo.upperPower.y);
                switch (attackInfo.effectType)
                {
                    case EEffectType.Stun:
                        hitTarget.Stun(attackInfo.effectTime);
                        break;
                }
            }
            else
            {
                switch (attackInfo.effectType)
                {
                    case EEffectType.Airborne:
                        hitTarget.Airborne(upperPowerX, attackInfo.upperPower.y);
                        break;
            
                    case EEffectType.Stun:
                        hitTarget.Stun(attackInfo.effectTime);
                        break;
            
                    case EEffectType.Damaged:
                        hitTarget.Damaged(attackInfo.effectTime);
                        hitTarget.KnockBack(knockBackX);
                        break;
                }
            }
            IgnoreCol(col);
        }
    }
}
