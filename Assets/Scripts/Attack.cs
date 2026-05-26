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
}
public enum EDirectionType
{
    Fixed,
    Relative
}

[Serializable]
public class DeBuffInfo
{
    public EBuffType deBuff;
    public int deBuffPercent;
    public float deBuffTime;
}

[Serializable]
public class AttackInfo
{
    public string id;
    public EEffectType effectType;
    public float effectTime;
    public List<DeBuffInfo> deBuffInfoList = new List<DeBuffInfo>();
    public bool ignoreSuperArmor;
    public bool ignoreImmortal;
    public bool respawnAttack;
    public bool destroyProjectile;
    public bool continuous;
    public float continuousDelay;
    public bool duplicate;
    public EDirectionType directionType;
    public int coefficient;
    public int criticalChance;
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
    private AttackInfo originInfo;
    [SerializeField] private AttackInfo attackInfo;
    private Character castChar;
    private Transform traceTransform;
    private Collider2D myCollider;
    private List<Collider2D> targetColliders = new List<Collider2D>();
    // 지속형(duplicate) 공격에서 무적 상태로 진입한 타겟. 무적이 풀리는 즉시 myCollider를 다시 켜기 위해 추적.
    private Character immortalWaitTarget;

    [SerializeField] private int dir;
    private float leftColliderTime;
    
    public Character CastChar => castChar;
    public AttackInfo AttackInfo => attackInfo;
    
    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        ColliderTimer();
        CheckImmortalWait();
    }

    private void OnDisable()
    {
        TargetColReset();
        immortalWaitTarget = null;
    }

    // 듀플리케이트 공격이 무적 타겟 때문에 콜라이더를 꺼둔 상태에서,
    // 무적이 풀리거나 대상이 파괴되면 콜라이더를 다시 켜서 재판정이 일어나게 한다.
    private void CheckImmortalWait()
    {
        if (!immortalWaitTarget)
        {
            immortalWaitTarget = null;
            return;
        }

        if (!immortalWaitTarget.Immortal && !immortalWaitTarget.Dodge)
        {
            immortalWaitTarget = null;
            if (myCollider)
                myCollider.enabled = true;
        }
    }

    public void SetupCastChar(Character character)
    {
        castChar = character;
    }

    public void SetupData(AttackData attackData)
    {
        if (originInfo == null)
        {
            originInfo = new AttackInfo();
            originInfo.id = attackData.id;
            originInfo.effectType = (EEffectType)Enum.Parse(typeof(EEffectType), attackData.effectType);
            originInfo.effectTime = attackData.effectTime;
            
            var deBuffSplit1 = attackData.deBuff.Split(';');
            var deBuffTimePercent1 = attackData.deBuffPercent.Split(';');
            var deBuffTimeSplit1 = attackData.deBuffTime.Split(';');

            if (!string.IsNullOrWhiteSpace(deBuffSplit1[0]))
            {
                for (var i = 0; i < deBuffSplit1.Length; i++)
                {
                    DeBuffInfo deBuffInfo = new DeBuffInfo();
                    deBuffInfo.deBuff = (EBuffType)Enum.Parse(typeof(EBuffType), deBuffSplit1[i]);
                    deBuffInfo.deBuffPercent = int.Parse(deBuffTimePercent1[i]);
                    deBuffInfo.deBuffTime = float.Parse(deBuffTimeSplit1[i]);
                    originInfo.deBuffInfoList.Add(deBuffInfo);
                }
            }
            originInfo.ignoreSuperArmor = attackData.ignoreSuperArmor;
            originInfo.ignoreImmortal = attackData.ignoreImmortal;
            originInfo.respawnAttack = attackData.respawnAttack;
            originInfo.destroyProjectile = attackData.destroyProjectile;
            originInfo.continuous = attackData.continuous;
            originInfo.continuousDelay = attackData.continuousDelay;
            originInfo.duplicate = attackData.duplicate;
            originInfo.directionType = (EDirectionType)Enum.Parse(typeof(EDirectionType), attackData.directionType);
            originInfo.coefficient = attackData.coefficient;
            originInfo.criticalChance = attackData.criticalChance;
            originInfo.stagger = attackData.stagger;
            originInfo.knockBack = attackData.knockBack;
            if (string.IsNullOrEmpty(attackData.upperPower))
            {
                originInfo.upperPower = Vector2.zero;
            }
            else
            {
                var upperPowerSplit1 = attackData.upperPower.Split(';');
                originInfo.upperPower = new Vector2(float.Parse(upperPowerSplit1[0]), float.Parse(upperPowerSplit1[1]));
            }
            originInfo.customDir = attackData.customDir;
            originInfo.colliderTime = attackData.colliderTime;
            if (string.IsNullOrEmpty(attackData.hitShake))
            {
                originInfo.hitShake = Vector2.zero;
            }
            else
            {
                var hitShakeSplit1 = attackData.hitShake.Split(';');
                originInfo.hitShake = new Vector2(float.Parse(hitShakeSplit1[0]), float.Parse(hitShakeSplit1[1]));
            }
            originInfo.shakeTime = attackData.shakeTime;
            originInfo.hitEffectId = attackData.hitEffectId;
        }

        // 어택데이터는 항상 초기화
        attackInfo = new AttackInfo();
        attackInfo.id = attackData.id;
        attackInfo.effectType = (EEffectType)Enum.Parse(typeof(EEffectType), attackData.effectType);
        attackInfo.effectTime = attackData.effectTime;
        
        var deBuffSplit = attackData.deBuff.Split(';');
        var deBuffTimePercent = attackData.deBuffPercent.Split(';');
        var deBuffTimeSplit = attackData.deBuffTime.Split(';');
        if (!string.IsNullOrWhiteSpace(deBuffSplit[0]))
        {
            for (var i = 0; i < deBuffSplit.Length; i++)
            {
                DeBuffInfo deBuffInfo = new DeBuffInfo();
                deBuffInfo.deBuff = (EBuffType)Enum.Parse(typeof(EBuffType), deBuffSplit[i]);
                deBuffInfo.deBuffPercent = int.Parse(deBuffTimePercent[i]);
                deBuffInfo.deBuffTime = float.Parse(deBuffTimeSplit[i]);
                attackInfo.deBuffInfoList.Add(deBuffInfo);
            }
        }

        attackInfo.ignoreSuperArmor = attackData.ignoreSuperArmor;
        attackInfo.ignoreImmortal = attackData.ignoreImmortal;
        attackInfo.respawnAttack = attackData.respawnAttack;
        attackInfo.destroyProjectile = attackData.destroyProjectile;
        attackInfo.continuous = attackData.continuous;
        attackInfo.continuousDelay = attackData.continuousDelay;
        attackInfo.duplicate = attackData.duplicate;
        attackInfo.directionType = (EDirectionType)Enum.Parse(typeof(EDirectionType), attackData.directionType);
        attackInfo.coefficient = attackData.coefficient;
        attackInfo.criticalChance = attackData.criticalChance;
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

    public void AttributeCheck()
    {
        var passiveList = GameManager.Instance.GetAttributePassive(attackInfo.id);
        foreach (var passive in passiveList)
        {
            switch (passive)
            {
                // 투사체 파괴
                case ConstValues.DestroyProjectile:
                    attackInfo.destroyProjectile = true;
                    break;
                // 슈퍼아머 무시
                case ConstValues.IgnoreSuperArmor:
                    attackInfo.ignoreSuperArmor = true;
                    break;
            }
        }
        
        var upgradeList = GameManager.Instance.GetAttributeUpgrade(attackInfo.id);
        foreach (var upgrade in upgradeList)
        {
            switch (upgrade.upgradeId)
            {
                // 피해 증가
                case ConstValues.DamageUp:
                    var result = originInfo.coefficient * upgrade.upgradeValue * 0.01f;
                    int final = Mathf.RoundToInt(result);
                    attackInfo.coefficient += final;
                    break;
                // 치명타 확률 증가
                case ConstValues.CriticalChanceUp:
                    attackInfo.criticalChance += upgrade.upgradeValue;
                    break;
            }
        }
        // 피해 증폭은 가장 뒤에 계산된다
        foreach (var upgrade in upgradeList)
        {
            switch (upgrade.upgradeId)
            {
                // 피해 증폭
                case ConstValues.DamageMultiplier:
                    attackInfo.coefficient *= upgrade.upgradeValue;
                    break;
            }
        }
        
        // 디버프추가
        var deBuffList = GameManager.Instance.GetAttributeDeBuff(attackInfo.id);
        foreach (var deBuff in deBuffList)
        {
            DeBuffInfo deBuffInfo = new DeBuffInfo
            {
                deBuff = (EBuffType)Enum.Parse(typeof(EBuffType), deBuff.buffId),
                deBuffTime = deBuff.buffTime,
                deBuffPercent = 100
            };
            attackInfo.deBuffInfoList.Add(deBuffInfo);
        }
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
        if (attackInfo == null || attackInfo.colliderTime == 0)
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
        if (myCollider)
        {
            foreach (var targetCollider in targetColliders)
            {
                if (targetCollider)
                    Physics2D.IgnoreCollision(myCollider, targetCollider, false);
            }
        }

        targetColliders.Clear();
    }

    private bool GetCritical()
    {
        var critPercent = Random.Range(0, 100);
        if(castChar)
            return critPercent < castChar.BasicStat.criticalChance + attackInfo.criticalChance;
        
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
    private void OnTriggerEnter2D(Collider2D col)
    {
        // 1. 투사체 파괴 (미사일/수류탄 등) — 파괴됐다면 종료
        if (attackInfo.destroyProjectile && TryDestroyProjectile(col))
            return;

        // 2. Character 컴포넌트가 있어야만 피격 처리
        var hitTarget = col.GetComponent<Character>();
        if (hitTarget == null)
            return;

        // 3. 무적/사망 상태 필터링
        if (!IsHittable(hitTarget))
        {
            // 지속형(duplicate) 공격에서 무적/회피 타겟이 안으로 들어온 경우,
            // 무적/회피가 풀릴 때까지 myCollider를 비활성화해 두고 CheckImmortalWait에서 재활성화한다.
            // (무적 돌진 등으로 레이저 내부에 안착했을 때 무적/회피 해제 후에도 피격 갱신이 가능하도록)
            if (attackInfo.duplicate && !attackInfo.ignoreImmortal && (hitTarget.Immortal || hitTarget.Dodge))
            {
                immortalWaitTarget = hitTarget;
                if (myCollider)
                    myCollider.enabled = false;
            }
            return;
        }

        // 4. 캐스터-타깃 관계 유효성 검증 (트랩 여부 산출)
        if (!IsValidTarget(col, hitTarget, out bool isTrapAttack))
            return;

        // 5. 카메라 셰이크
        GameManager.Instance.CameraShake(attackInfo.hitShake[0], attackInfo.hitShake[1], attackInfo.shakeTime);

        // 6. 카운터 처리 — 성공 시 데미지 들어가지 않고 종료
        if (TryHandleCounter(hitTarget))
            return;

        // 7. 피격 이팩트
        if (!string.IsNullOrWhiteSpace(attackInfo.hitEffectId))
            hitTarget.SpawnHitEffect(attackInfo.hitEffectId, 0.5f);

        // 8. 기본 데미지 적용
        bool critical = GetCritical();
        int finalDamage = ApplyDamage(hitTarget, critical, isTrapAttack);

        // 9. 감전 추가 피해
        TryApplyShockBonus(hitTarget, finalDamage, critical, isTrapAttack);

        // 10. 사망 체크
        if (hitTarget.BasicStat.hp <= 0)
        {
            hitTarget.Die();
            return;
        }

        // 11. 넉백/공중치 방향 계산
        CalculateDirectionalForces(hitTarget, out float upperPowerX, out float knockBackX);

        // 12. 중복 타격 방지 (다시 충돌하지 않도록 콜라이더 무시)
        if (!attackInfo.duplicate)
            IgnoreCol(col);

        // 13. 스태거 누적 / 무력화 — 무력화 발생 시 종료
        if (TryApplyStagger(hitTarget))
            return;

        // 14. 상태이상(디버프) 적용
        ApplyDeBuffs(hitTarget, critical);

        // 15. 피격 리액션 (Airborne / Damaged)
        ApplyHitReaction(hitTarget, upperPowerX, knockBackX);

        // 16. 리스폰 어택 (트랩 등) — 플레이어가 맞았을 때만 안전 위치로 복귀
        TryRespawnPlayer(hitTarget);
    }

    // respawnAttack 옵션이 켜져 있고, 맞은 대상이 Player일 때 리스폰 수행
    private void TryRespawnPlayer(Character hitTarget)
    {
        if (!attackInfo.respawnAttack)
            return;

        var player = hitTarget as Player;
        if (player == null)
            return;

        GameManager.Instance.PlayerRespawn();
    }

    // 미사일/수류탄 등 투사체를 파괴할 수 있다면 파괴하고 true 반환
    private bool TryDestroyProjectile(Collider2D col)
    {
        var projectile = col.GetComponent<IProjectile>();
        if (projectile == null)
            return false;

        // 트랩 등 캐스터가 없는 공격은 투사체를 파괴하지 않는다
        if (!castChar)
            return false;

        // 플레이어의 공격이 몬스터의 투사체 소멸 (보스 투사체는 제외)
        if (castChar.GetComponent<Player>())
        {
            var monsterName = col.name.Split('_')[0];
            if (monsterName == ConstValues.Monster && !projectile.IsBoss())
            {
                projectile.Delete();
                return true;
            }
        }
        // 몬스터의 공격이 플레이어의 투사체 소멸
        if (castChar.GetComponent<Monster>())
        {
            var characterName = col.name.Split('_')[0];
            if (characterName is ConstValues.Berserker or ConstValues.Gunner or ConstValues.Fighter)
            {
                projectile.Delete();
                return true;
            }
        }
        return false;
    }

    // 피격이 가능한 상태인지(무적/사망 필터)
    private bool IsHittable(Character hitTarget)
    {
        if ((hitTarget.Immortal || hitTarget.Dodge) && !attackInfo.ignoreImmortal)
            return false;
        if (hitTarget.IsDie)
            return false;
        if (hitTarget.BasicStat.hp <= 0)
            return false;
        return true;
    }

    // 캐스터-타깃 관계가 유효한지 + 트랩 여부 판정
    // 트랩: castChar == null → Player/Monster만 타격
    // 플레이어 공격: Monster/Npc만 타격 (+ 스프라이트 점멸)
    // 몬스터 공격: Player/Npc만 타격
    private bool IsValidTarget(Collider2D col, Character hitTarget, out bool isTrapAttack)
    {
        isTrapAttack = false;

        // 트랩
        if (!castChar)
        {
            if (col.GetComponent<Player>() == null && col.GetComponent<Monster>() == null)
                return false;
            isTrapAttack = true;
            return true;
        }

        // 플레이어의 공격
        if (castChar.GetComponent<Player>())
        {
            if (col.GetComponent<Monster>() == null && col.GetComponent<Npc>() == null)
                return false;
            // 스프라이트가 점멸한다
            hitTarget.HitMaterial();
            return true;
        }

        // 몬스터의 공격
        if (castChar.GetComponent<Monster>())
        {
            if (col.GetComponent<Player>() == null && col.GetComponent<Npc>() == null)
                return false;
            return true;
        }

        return true;
    }

    // 카운터 가능 아머일 때 처리. 카운터 성공 시 true 반환
    private bool TryHandleCounter(Character hitTarget)
    {
        if (hitTarget.BasicStat.bodyType != EBodyType.Counter)
            return false;

        if (IsCanCounter(hitTarget))
        {
            hitTarget.IsCounterAttack = true;
            return true;
        }

        // 반격 실패 시 원래 아머타입으로 돌아옴
        hitTarget.BasicStat.bodyType = hitTarget.OriginStat.bodyType;
        return false;
    }

    // 기본 데미지 적용 (랜덤 변동, 최종 보정 포함)
    private int ApplyDamage(Character hitTarget, bool critical, bool isTrapAttack)
    {
        int damage = GetDamage(critical);
        float randDmg = Random.Range(0.95f, 1.05f);
        damage = (int)(damage * randDmg);

        int finalDamage = hitTarget.FinalDamage(damage);
        hitTarget.TakeDamage(finalDamage, isTrapAttack);
        hitTarget.SpawnDamageFont(finalDamage, critical, false, false);
        return finalDamage;
    }

    // 감전 디버프가 있을 때 추가 피해 (트랩 공격은 제외)
    private void TryApplyShockBonus(Character hitTarget, int finalDamage, bool critical, bool isTrapAttack)
    {
        if (isTrapAttack)
            return;

        var shockDeBuff = hitTarget.TargetBuff(EBuffType.Shock);
        if (shockDeBuff == null)
            return;

        int additionalDamage = (int)(finalDamage * 0.1f);
        int finalAdditionalDamage = hitTarget.FinalDamage(additionalDamage);

        hitTarget.TakeDamage(finalAdditionalDamage, false);
        hitTarget.SpawnDamageFont(finalAdditionalDamage, critical, true, false);
        hitTarget.SpawnHitEffect(ConstValues.ShockHitEffect);
    }

    // 넉백/공중치의 X 방향 계산 (Fixed / Relative 분기)
    private void CalculateDirectionalForces(Character hitTarget, out float upperPowerX, out float knockBackX)
    {
        upperPowerX = attackInfo.upperPower.x;
        knockBackX = attackInfo.knockBack;

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
                    knockBackX = -Math.Abs(knockBackX);
                    upperPowerX = upperPowerX > 0 ? -Math.Abs(upperPowerX) : Math.Abs(upperPowerX);
                }
                else
                {
                    knockBackX = Math.Abs(knockBackX);
                    upperPowerX = upperPowerX > 0 ? Math.Abs(upperPowerX) : -Math.Abs(upperPowerX);
                }
                break;
        }
    }

    // 스태거 누적 후 무력화 발생 여부 판정. 무력화 시 true 반환
    // 트랩(castChar 없음)은 스태거 누적을 건너뜀
    private bool TryApplyStagger(Character hitTarget)
    {
        if (!castChar)
            return false;

        int finalStagger = (int)(attackInfo.stagger + (attackInfo.stagger * castChar.BasicStat.staggerDamage * 0.01f));
        hitTarget.TakeStagger(finalStagger);

        if (!hitTarget.ImmuneStagger && hitTarget.BasicStat.stagger <= 0 && hitTarget.OriginStat.bodyType is EBodyType.StrongArmor or EBodyType.HyperArmor)
        {
            hitTarget.Stagger();
            return true;
        }
        return false;
    }

    // 디버프(상태이상) 적용
    private void ApplyDeBuffs(Character hitTarget, bool critical)
    {
        foreach (var deBuffInfo in attackInfo.deBuffInfoList)
        {
            // 확률 계산
            int rand = Random.Range(0, 100);
            if (rand >= deBuffInfo.deBuffPercent)
                continue;

            switch (deBuffInfo.deBuff)
            {
                // 기절
                case EBuffType.Stun:
                    if (hitTarget.OriginStat.bodyType is EBodyType.Normal or EBodyType.SuperArmor or EBodyType.HeavyArmor)
                        hitTarget.Stun(deBuffInfo.deBuffTime);
                    break;
                // 갑옷 파괴
                case EBuffType.ArmorBreak:
                    if (hitTarget.OriginStat.bodyType is EBodyType.SuperArmor)
                        hitTarget.AddDeBuff(EBuffType.ArmorBreak, deBuffInfo.deBuffTime, 0);
                    break;
                // 빙결
                case EBuffType.Frozen:
                    if (hitTarget.OriginStat.bodyType is EBodyType.Normal or EBodyType.SuperArmor or EBodyType.HeavyArmor)
                        hitTarget.Frozen(deBuffInfo.deBuffTime);
                    break;
                // 감전
                case EBuffType.Shock:
                    hitTarget.AddDeBuff(EBuffType.Shock, deBuffInfo.deBuffTime, 0);
                    break;
                // 화상
                case EBuffType.Burn:
                    ApplyBurnDeBuff(hitTarget, deBuffInfo.deBuffTime, critical);
                    break;
            }
        }
    }

    // 화상 디버프 — 트랩의 경우 castChar가 없으므로 attackInfo.coefficient를 power 대용으로 사용
    private void ApplyBurnDeBuff(Character hitTarget, float duration, bool critical)
    {
        Action burnAction = () =>
        {
            int burnBasePower = castChar ? castChar.BasicStat.power : attackInfo.coefficient;
            int burnDamage = (int)(burnBasePower * 0.2f);
            hitTarget.TakeDamage(burnDamage, false);
            hitTarget.SpawnDamageFont(burnDamage, critical, false, true);
            hitTarget.SpawnHitEffect(ConstValues.BurnHitEffect);

            if (hitTarget.BasicStat.hp <= 0)
                hitTarget.Die();
        };
        hitTarget.AddDeBuff(EBuffType.Burn, duration, 0, null, 0.5f, 0.5f, burnAction);
    }

    // 피격 리액션 (Airborne / Damaged)
    private void ApplyHitReaction(Character hitTarget, float upperPowerX, float knockBackX)
    {
        // 노멀 또는 (슈퍼아머 + 슈퍼아머 무시 옵션) 일 때만 리액션이 적용된다
        bool isReactable = hitTarget.BasicStat.bodyType == EBodyType.Normal
                           || (hitTarget.BasicStat.bodyType == EBodyType.SuperArmor && attackInfo.ignoreSuperArmor);

        // 공중에 떠있는 상태(에어본/점프 중)면 effectType과 무관하게 Airborne 처리
        if (hitTarget.GetAirborneState() || hitTarget.GetJumpState())
        {
            if (isReactable)
                hitTarget.Airborne(upperPowerX, attackInfo.upperPower.y, false);
            return;
        }

        switch (attackInfo.effectType)
        {
            case EEffectType.Airborne:
                if (isReactable)
                    hitTarget.Airborne(upperPowerX, attackInfo.upperPower.y, false);
                break;

            case EEffectType.Damaged:
                if (isReactable)
                {
                    hitTarget.Damaged(attackInfo.effectTime);
                    hitTarget.KnockBack(knockBackX);
                }
                break;
        }
    }
}
