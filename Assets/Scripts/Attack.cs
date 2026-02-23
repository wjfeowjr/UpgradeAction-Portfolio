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
    public bool ignoreSuperArmor;
    public bool ignoreImmortal;
    public bool continuous;
    public float continuousDelay;
    public bool duplicate;
    public EDirectionType directionType;
    public int coefficient;
    public int stagger;
    public float knockBack;
    public Vector2 upperPower;
    public int customDir;
    public float colliderTime;
    public Vector2 hitShake;
    public float shakeTime;
    public string hitEffectId;
}
public class Attack : MonoBehaviour
{
    [SerializeField] private AttackInfo attackInfo;
    private Character castChar;
    private Transform traceTransform;
    private Collider2D myCollider;
    private List<Collider2D> targetColliders = new List<Collider2D>();
    
    [SerializeField] private int dir;
    private float leftColliderTime;

    public AttackInfo AttackInfo => attackInfo;
    public Character CastChar => castChar;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        ColliderTimer();
    }

    private void OnDisable()
    {
        TargetColReset();
    }

    public void SetupCastChar(Character character)
    {
        castChar = character;
    }
    public void SetupData(AttackData attackData)
    {
        if (attackInfo != null)
            return;
        
        attackInfo = new AttackInfo();
        attackInfo.id = attackData.id;
        attackInfo.effectType = (EEffectType)Enum.Parse(typeof(EEffectType), attackData.effectType);
        attackInfo.effectTime = attackData.effectTime;
        attackInfo.ignoreSuperArmor = attackData.ignoreSuperArmor;
        attackInfo.ignoreImmortal = attackData.ignoreImmortal;
        attackInfo.continuous = attackData.continuous;
        attackInfo.continuousDelay = attackData.continuousDelay;
        attackInfo.duplicate = attackData.duplicate;
        attackInfo.directionType = (EDirectionType)Enum.Parse(typeof(EDirectionType), attackData.directionType);
        attackInfo.coefficient = attackData.coefficient;
        attackInfo.stagger = attackData.stagger;
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

        attackInfo.customDir = attackData.customDir;
        attackInfo.colliderTime = attackData.colliderTime;

        if (string.IsNullOrEmpty(attackData.hitShake))
        {
            attackInfo.hitShake = Vector2.zero;
        }
        else
        {
            var hitShakeSplit = attackData.hitShake.Split(';');
            attackInfo.hitShake = new Vector2(float.Parse(hitShakeSplit[0]), float.Parse(hitShakeSplit[1]));
        }

        attackInfo.shakeTime = attackData.shakeTime;
        attackInfo.hitEffectId = attackData.hitEffectId;
    }

    public void EnableSetting()
    {
        myCollider.enabled = true;
        leftColliderTime = 0;
        TargetColReset();

        if (castChar)
        {
            dir = castChar.transform.localScale.x > 0 ? 1 : -1;
            if(attackInfo.upperPower.x < 0)
                dir = castChar.transform.localScale.x > 0 ? -1 : 1;
        }
        else
        {
            if (attackInfo.customDir != 0)
                dir = attackInfo.customDir;
        }
        
        ContinuousCollider();
    }

    public void DisActiveCollider()
    {
        myCollider.enabled = false;
    }
    
    private void ColliderTimer()
    {
        if (attackInfo.colliderTime == 0)
            return;
        
        leftColliderTime += Time.deltaTime;

        if (leftColliderTime >= attackInfo.colliderTime)
            myCollider.enabled = false;
    }

    private async void ContinuousCollider()
    {
        if (!attackInfo.continuous)
            return;
        
        while (gameObject.activeSelf)
        {
            myCollider.enabled = true;
            await UniTask.Delay(TimeSpan.FromSeconds(attackInfo.continuousDelay));
            myCollider.enabled = false;
            TargetColReset();
            await UniTask.Yield();
        }
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
        if(castChar)
            return critPercent < castChar.BasicStat.criticalChance;
        
        return false;
    }
    
    private int GetDamage(bool isCrit)
    {
        // 캐스터가 없다면(함정 등) 표기된 대미지 그대로
        if (castChar == null)
            return attackInfo.coefficient;
        
        // 원래 주는 피해량
        float originDamage = castChar.BasicStat.power * attackInfo.coefficient * 0.01f;
        // 룬, 특성에 따라서 최종적인 대미지의 피해량이 높아짐
        int finalDamage = (int)originDamage;
        if (isCrit)
        {
            float critDamage = originDamage * castChar.BasicStat.criticalDamage * 0.01f;
            finalDamage = (int)critDamage;
        }

        return finalDamage;
    }

    public void SetUpperPower(Vector2 upperPower)
    {
        attackInfo.upperPower = upperPower;
    }
    
    // 맞는 대상의 반격 여부 판별
    public bool IsCanCounter(Character hitObject)
    {
        bool canCounter = false;
        var targetScale = hitObject.transform.localScale.x;
        
        // 근거리 공격
        if (GetComponent<Missile>() == null)
        {
            switch (attackInfo.directionType)
            {
                case EDirectionType.Fixed:
                    // 오른쪽
                    if (targetScale > 0)
                        canCounter = dir == -1;
                    // 왼쪽
                    else
                        canCounter = dir == 1;
                    break;
                    
                case EDirectionType.Relative:
                    // 오른쪽
                    if(targetScale > 0)
                        canCounter = transform.position.x > hitObject.transform.position.x;
                    // 왼쪽
                    else
                        canCounter = transform.position.x < hitObject.transform.position.x;
                    break;
            }
            
        }
        // 원거리 공격
        else
        {
            // 오른쪽
            if(targetScale > 0)
                canCounter = transform.position.x > hitObject.transform.position.x;
            // 왼쪽
            else
                canCounter = transform.position.x < hitObject.transform.position.x;
        }
        
        return canCounter;
    }

    // 공격판정 적용
    // if (col.GetComponent<Monster>() != null)
    // hitTarget.LookAt(transform.position.x);
    private void OnTriggerEnter2D(Collider2D col)
    {
        var hitTarget = col.GetComponent<Character>();
        if (hitTarget != null)
        {
            if((hitTarget.Immortal && !attackInfo.ignoreImmortal) || hitTarget.IsDie || hitTarget.BasicStat.hp <= 0)
                return;

            bool isTrapAttack = false;
            
            // 플레이어의 공격
            if (castChar)
            {
                if (castChar.GetComponent<Player>())
                {
                    if (col.GetComponent<Monster>() == null && col.GetComponent<Npc>() == null)
                        return;
                
                    // 스프라이트가 점멸한다
                    hitTarget.HitMaterial();
                }
                // 몬스터의 공격
                if (castChar.GetComponent<Monster>())
                {
                    if (col.GetComponent<Player>() == null && col.GetComponent<Npc>() == null)
                        return;
                }
            }
            // 트랩
            else
            {
                if (col.GetComponent<Player>() == null && col.GetComponent<Monster>() == null)
                    return;

                isTrapAttack = true;
            }
            
            GameManager.Instance.CameraShake(attackInfo.hitShake[0], attackInfo.hitShake[1], attackInfo.shakeTime);
            
            // 반격이 가능한지 확인!
            if (hitTarget.BasicStat.bodyType == EBodyType.Counter)
            {
                bool isCanCounter = IsCanCounter(hitTarget);
                if (isCanCounter)
                {
                    hitTarget.IsCounterAttack = true;
                    return;
                }
                
                // 반격 실패 시 다시 기본 아머타입으로 돌아옴
                hitTarget.BasicStat.bodyType = hitTarget.OriginStat.bodyType;
            }

            // 피격이팩트 생성
            if(attackInfo.hitEffectId != ConstValues.None)
                hitTarget.SpawnHitEffect(attackInfo.hitEffectId, 0.5f);
            
            // 대상이 피해를 입는다(치명타 피해인지 확인)
            bool critical = GetCritical();
            int damage = GetDamage(critical);
            float randDmg = Random.Range(0.95f, 1.05f);
            damage = (int)(damage * randDmg);

            // 피해입기
            hitTarget.TakeDamage(damage, isTrapAttack);
            // 폰트소환
            hitTarget.SpawnDamageFont(damage, critical);
            
            // 피해를 입고, 체력이 0으로 떨어지면 죽는다
            if (hitTarget.BasicStat.hp <= 0)
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

            if (!attackInfo.duplicate)
                IgnoreCol(col);

            hitTarget.TakeStagger(attackInfo.stagger);
            if (!hitTarget.ImmuneStagger && hitTarget.BasicStat.stagger <= 0 && hitTarget.OriginStat.bodyType is EBodyType.StrongArmor or EBodyType.HyperArmor)
            {
                // 무력화 효과 넣기
                hitTarget.Stagger();
                return;
            }
            
            if (hitTarget.GetAirborneState() || hitTarget.GetJumpState())
            {
                if(hitTarget.BasicStat.bodyType == EBodyType.Normal || (hitTarget.BasicStat.bodyType == EBodyType.SuperArmor && attackInfo.ignoreSuperArmor))
                    hitTarget.Airborne(upperPowerX, attackInfo.upperPower.y);
                
                switch (attackInfo.effectType)
                {
                    case EEffectType.Stun:
                        if(hitTarget.OriginStat.bodyType is EBodyType.Normal or EBodyType.SuperArmor or EBodyType.HeavyArmor)
                            hitTarget.Stun(attackInfo.effectTime);
                        break;
                }
            }
            else
            {
                switch (attackInfo.effectType)
                {
                    case EEffectType.Airborne:
                        if(hitTarget.BasicStat.bodyType == EBodyType.Normal || (hitTarget.BasicStat.bodyType == EBodyType.SuperArmor && attackInfo.ignoreSuperArmor))
                            hitTarget.Airborne(upperPowerX, attackInfo.upperPower.y);
                        break;
            
                    case EEffectType.Stun:
                        if(hitTarget.OriginStat.bodyType is EBodyType.Normal or EBodyType.SuperArmor or EBodyType.HeavyArmor)
                            hitTarget.Stun(attackInfo.effectTime);
                        break;
            
                    case EEffectType.Damaged:
                        if (hitTarget.BasicStat.bodyType == EBodyType.Normal || (hitTarget.BasicStat.bodyType == EBodyType.SuperArmor && attackInfo.ignoreSuperArmor))
                        {
                            hitTarget.Damaged(attackInfo.effectTime);
                            hitTarget.KnockBack(knockBackX);
                        }
                        break;
                }
            }
        }
    }
}
