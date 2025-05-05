using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[Serializable]
public class Buff
{
    public EBuffType buffType;
    public float buffTime;
    public float currentTime;
}

// 기본 상태 모션
public enum ENormalState
{
    Normal,
    Idle,
    Move,
    Jump,
    Attack,
    JumpAttack,
    Dash,
    Skill,
    Grabbed,
    Airborne,
    Down,
    Stun,
    Damaged,
    Appear,
    AppearEnd,
    Die,
}

// 실제 이동 관련
public enum EMoveState
{
    Stopping,
    Moving,
}

// 지상 관련
public enum ELandingState
{
    Ground,
    Air,
}

// 바디 타입
public enum EBodyType
{
    Normal,
    SuperArmor,
    StrongArmor,
    HyperArmor
}

// 버프 타입
public enum EBuffType
{
    Stun,
}

[Serializable]
public class BasicStat
{
    public string id;
    public int name;
    public EBodyType bodyType;
    public int hp;
    public int maxHp;
    public int power;
    public int defence;
    public float moveSpeed;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
    public float weight;
    public int stagger;
    public int maxStagger;
    public float staggerTime;
}

public abstract class Character : MonoBehaviour
{
    [SerializeField] protected BasicStat originStat; // 원본 스텟
    [SerializeField] protected BasicStat basicStat;  // 내 스텟(변동되어야 함)
    
    protected Rigidbody2D myRigidbody;
    protected BoxCollider2D myBoxCollider;
    protected BoxCollider2D physicsCollider;
    protected Animator myAnimator;
    protected SpriteRenderer[] mySpriteRenderers;
    
    protected Vector3 defaultScale;
    protected Vector3 reverseScale;
    protected CancellationTokenSource stateCancellation;
    protected CancellationTokenSource anotherCancellation; // 우선 넉백에만사용되고 있음

    [SerializeField] protected List<GameObject> controlObject = new List<GameObject>(); // 직접 시간을 관리하는 '공격판정'
    [SerializeField] protected List<GameObject> normalObject = new List<GameObject>(); // 직접 시간을 관리하는 '일반 오브젝트'
    [SerializeField] protected List<GameObject> buffObject = new List<GameObject>(); // 직접 시간을 관리하는 '버프 오브젝트'
    
    [SerializeField] protected ENormalState normalState;
    [SerializeField] protected EMoveState moveState;
    [SerializeField] protected ELandingState landingState;
    [SerializeField] protected List<Buff> buffList = new List<Buff>();
    
    [SerializeField] private Transform diePos;
    [SerializeField] protected Transform buffEffectPos;
    [SerializeField] protected Transform centerPos;
    [SerializeField] protected Transform fontPos;
    
    [SerializeField] protected Vector2 standHitBoxSize;
    [SerializeField] protected Vector2 downHitBoxSize;
    [SerializeField] protected Vector2 standOffset;
    [SerializeField] protected Vector2 downOffset;
    
    protected Vector2 chargeVector;
    protected int jumpAttackCount;
    private int airborneCount;     // 에어본 카운트
    private int moveLayerMask;
    private int platformLayerMask;

    [SerializeField] protected bool immortal;
    [SerializeField] protected bool immuneStagger;

    // 프로퍼티
    public BasicStat BasicStat => basicStat;
    public bool Immortal => immortal;
    public bool ImmuneStagger => immuneStagger;
    
    public ENormalState NormalState => normalState;
    public EMoveState MoveState => moveState;

    // 상태 설정
    protected abstract void StateSetting(ENormalState changeNormalState, string triggerName, string animId);

    protected abstract void StateRecovery();
    
    protected virtual void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myBoxCollider = GetComponent<BoxCollider2D>();
        foreach (var component in GetComponentsInChildren<BoxCollider2D>())
        {
            if (!component.isTrigger)
            {
                physicsCollider = component;
                break;
            }
        }
        myAnimator = GetComponentInChildren<Animator>();
        mySpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        moveLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Wall);
        platformLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Platform);
        
        ScaleSetting();
        ColSizeSetting();
    }

    private void FixedUpdate()
    {
        UpdateVelocity();
        JumpIgnorePlatform();
    }

    private void ScaleSetting()
    {
        defaultScale = transform.localScale;
        reverseScale = new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z);
    }
    
    private void ColSizeSetting()
    {
        var hitBoxSize = myBoxCollider.size;
        standHitBoxSize = new Vector2(hitBoxSize.x, hitBoxSize.y);
        downHitBoxSize = new Vector2(hitBoxSize.y, hitBoxSize.x);

        var hitBoxOffset = myBoxCollider.offset;
        standOffset = new Vector2(hitBoxOffset.x, hitBoxOffset.y);
        downOffset = new Vector2(hitBoxOffset.x, hitBoxOffset.y - hitBoxOffset.y * 0.4f);
    }
    
    protected void UpdateBuff()
    {
        int expiredCount = 0;
        foreach (var deBuff in buffList)
        {
            if (deBuff.currentTime < deBuff.buffTime)
                deBuff.currentTime += Time.deltaTime;
            else
                expiredCount += 1;
        }

        if (expiredCount == 0)
            return;

        var expiredDeBuffList = buffList.FindAll(x => x.currentTime >= x.buffTime);
        foreach (var expiredDeBuff in expiredDeBuffList)
        {
            buffList.Remove(expiredDeBuff);
            var removeEffect = buffObject.Find(x => x.name == $"{expiredDeBuff.buffType}{ConstValues.Effect}(Clone)");
            if(removeEffect != null) 
                RemoveObjectList(buffObject, removeEffect);
            
            // 스턴상태 회복
            if (expiredDeBuff.buffType == EBuffType.Stun && normalState == ENormalState.Stun)
            {
                basicStat.bodyType = originStat.bodyType;
                StateRecovery();
            }
        }
    }
    
    // 최대 중력가속도 조정
    private void UpdateVelocity()
    {
        if (myRigidbody.bodyType == RigidbodyType2D.Dynamic && myRigidbody.linearVelocity.y < -30)
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -30);
    }

    public void IgnorePlatform(bool value)
    {
        foreach (var platform in GameManager.Instance.PlatformColliderList)
        {
            if (Physics2D.GetIgnoreCollision(physicsCollider, platform) == !value)
            {
                Physics2D.IgnoreCollision(physicsCollider, platform, value);
            }
        }
    }

    // 콜라이더 무시 설정
    private void JumpIgnorePlatform()
    {
        // if (landingState != ELandingState.Air)
        //     return;
        
        if (myRigidbody.linearVelocityY > 0)
        {
            IgnorePlatform(true);
        }
        else if (myRigidbody.linearVelocityY < 0)
        {
            var rayVector1 = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);
            var rayVector2 = new Vector2(transform.position.x, transform.position.y);
            var rayVector3 = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);

            PlatformRay(rayVector1);
            PlatformRay(rayVector2);
            PlatformRay(rayVector3);
        }
        // 대시하는 경우 사용, 특수
        else if  (normalState == ENormalState.Dash)
        {
            IgnorePlatform(true);
        }
    }

    private void PlatformRay(Vector2 rayVector)
    {
        var downRay = Physics2D.Raycast(rayVector, Vector2.down, 3.0f, platformLayerMask);
        Debug.DrawRay(rayVector, Vector2.down * 3.0f, ConstValues.BlueColor, 0.025f);
        if (downRay.collider != null)
        {
            if (Physics2D.GetIgnoreCollision(physicsCollider, downRay.collider))
            {
                Physics2D.IgnoreCollision(physicsCollider, downRay.collider, false);
            }
        }
    }

    // 반동
    protected void Rebound(float force)
    {
        if(transform.localScale.x > 0)
            myRigidbody.linearVelocity = new Vector2(-force, myRigidbody.linearVelocity.y);
        else
            myRigidbody.linearVelocity = new Vector2(force, myRigidbody.linearVelocity.y);
    }

    public bool GetAirborneState()
    {
        return normalState == ENormalState.Airborne;
    }
    public bool GetJumpState()
    {
        return landingState == ELandingState.Air;
    }

    public Vector2 GetVelocity()
    {
        return myRigidbody.linearVelocity;
    }

    protected virtual void SetTriggerAnimator(string parameter)
    {
        myAnimator.SetTrigger(parameter);
    }
    protected virtual void ResetTriggerAnimator(string parameter)
    {
        myAnimator.ResetTrigger(parameter);
    }

    // 공격속도 설정
    private void SetAttackSpeed(float percent)
    {
        var valuePercent = percent * 0.01f;
        var addSpeed = originStat.attackSpeed * valuePercent;
        
        // 최종적으로 도출되는 공격속도 계수
        basicStat.attackSpeed += addSpeed;
        var animSpeed = basicStat.attackSpeed / originStat.attackSpeed;
        myAnimator.SetFloat(ConstValues.AttackSpeed, animSpeed);
    } 
    // 이동속도 설정
    private void SetMoveSpeed(float percent)
    {
        var valuePercent = percent * 0.01f;
        var value = (basicStat.moveSpeed / originStat.moveSpeed) + valuePercent;
        
        // 최종적으로 도출되는 공격속도 계수
        basicStat.moveSpeed = originStat.moveSpeed * value;
        myAnimator.SetFloat(ConstValues.MoveSpeed, value);
    }

    public virtual void TakeDamage(int damage)
    {
        if (damage == 0)
            return;
        
        // 체력 다는 알고리즘 삽입
        basicStat.hp -= damage;
    }

    public void SpawnDamageFont(int damage, bool critical)
    {
        var textFont = GameManager.Instance.SpawnToUIObjectPool(ConstValues.TextFont, fontPos).GetComponent<TextFont>();

        if (critical)
            textFont.ColorSetting(EFontType.Critical);
        else
            textFont.ColorSetting(EFontType.Damage);
        
        textFont.DisplayFont(55, damage.ToString());
    }

    public void SpawnHitEffect(string id, float minScale = 1.0f)
    {
        var randomVector = Vector3.one;
        
        float effectRangeX = myBoxCollider.size.x * 0.5f;
        float effectRangeY = myBoxCollider.size.y * 0.5f;

        float randXpos = Random.Range(-effectRangeX, effectRangeX);
        float randYpos = Random.Range(-effectRangeY, effectRangeY);

        Vector2 finalVector = new Vector2(transform.position.x + randXpos, transform.position.y + myBoxCollider.offset.y + randYpos);
        var effectObj = SpawnObject(id, finalVector);
        
        if (minScale < 1.0f)
        {
            var randomScale = Random.Range(minScale, 1.0f);
            randomVector = new Vector3(randomScale, randomScale, randomScale);
        }
        effectObj.transform.localScale = randomVector;
    }

    // 공격 소환
    protected void SpawnAttack(string id, Transform attackTransform, int zAngle = 0)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackTransform); 
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData != null)
        {
            var spawnedObject = obj.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = obj.AddComponent<SpawnedObject>();
            
            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
            if (zAngle != 0)
            {
                var finalAngle = zAngle;
                if (transform.localScale.x < 0)
                    finalAngle = -zAngle;
            
                var objectAngle = spawnedObject.transform.eulerAngles;
                spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
            }
            
            if(spawnedObject.GetObjectTime() == 0)
                AddObjectList(controlObject, obj);

            if (spawnedObject.GetTrace())
            {
                var trace = obj.GetComponent<Trace>();
                if(!trace)
                    trace = obj.AddComponent<Trace>();
                
                trace.SetTarget(attackTransform);
            }
        }

        var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == id);
        if (attackData != null)
        {
            var attack = obj.GetComponent<Attack>();
            if (!attack)
            {
                attack = obj.AddComponent<Attack>();
                attack.SetupData(this, attackData);
            }

            attack.EnableSetting();
        }
        
        var missileData = TableManager.Instance.missileTable.Missile.Find(x => x.id == id);
        if (missileData != null)
        {
            var missile = obj.GetComponent<Missile>();
            if (!missile)
                missile = obj.AddComponent<Missile>();
            
            var dir = Vector2.right;
            if(transform.localScale.x < 0)
                dir = Vector2.left;
            missile.SetupData(missileData, dir, SpawnAttack);
        }
        
        var grenadeData = TableManager.Instance.grenadeTable.Grenade.Find(x => x.id == id);
        if (grenadeData != null)
        {
            var grenade = obj.GetComponent<Grenade>();
            if (!grenade)
                grenade = obj.AddComponent<Grenade>();
            
            var dir = Vector2.right;
            if(transform.localScale.x < 0)
                dir = Vector2.left;
            grenade.SetupData(grenadeData, dir, SpawnAttack);
            grenade.Throw();
        }
    }
    protected void SpawnAttack(string id, Vector2 pos, int zAngle = 0)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos); 
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData != null)
        {
            var spawnedObject = obj.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = obj.AddComponent<SpawnedObject>();
            
            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
            if (zAngle != 0)
            {
                var finalAngle = zAngle;
                if (transform.localScale.x < 0)
                    finalAngle = -zAngle;
            
                var objectAngle = spawnedObject.transform.eulerAngles;
                spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
            }
            
            if(spawnedObject.GetObjectTime() == 0)
                AddObjectList(controlObject, obj);
        }

        var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == id);
        if (attackData != null)
        {
            var attack = obj.GetComponent<Attack>();
            if (!attack)
            {
                attack = obj.AddComponent<Attack>();
                attack.SetupData(this, attackData);
            }

            attack.EnableSetting();
        }
        
        var missileData = TableManager.Instance.missileTable.Missile.Find(x => x.id == id);
        if (missileData != null)
        {
            var missile = obj.GetComponent<Missile>();
            if (!missile)
                missile = obj.AddComponent<Missile>();
            
            var dir = Vector2.right;
            if(transform.localScale.x < 0)
                dir = Vector2.left;
            missile.SetupData(missileData, dir, SpawnAttack);
        }
        
        var grenadeData = TableManager.Instance.grenadeTable.Grenade.Find(x => x.id == id);
        if (grenadeData != null)
        {
            var grenade = obj.GetComponent<Grenade>();
            if (!grenade)
                grenade = obj.AddComponent<Grenade>();
            
            var dir = Vector2.right;
            if(transform.localScale.x < 0)
                dir = Vector2.left;
            grenade.SetupData(grenadeData, dir, SpawnAttack);
            grenade.Throw();
        }
    }
    
    // 공격판정이 없는 오브젝트 소환
    protected GameObject SpawnObject(string id, Transform attackTransform, int zAngle = 0, bool isBuff = false)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackTransform);
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
            
        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();
        if (zAngle != 0)
        {
            var finalAngle = zAngle;
            if (transform.localScale.x < 0)
                finalAngle = -zAngle;
            
            var objectAngle = spawnedObject.transform.eulerAngles;
            spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
        }
        
        if (spawnedObject.GetObjectTime() == 0)
        {
            if(isBuff)
                AddObjectList(buffObject, obj);
            else
                AddObjectList(normalObject, obj);
        }

        if (spawnedObject.GetTrace())
        {
            var trace = obj.GetComponent<Trace>();
            if(!trace)
                trace = obj.AddComponent<Trace>();
            
            trace.SetTarget(attackTransform);
        }
        
        var missileData = TableManager.Instance.missileTable.Missile.Find(x => x.id == id);
        if (missileData != null)
        {
            var missile = obj.GetComponent<Missile>();
            if (!missile)
                missile = obj.AddComponent<Missile>();
            
            var dir = Vector2.right;
            if(transform.localScale.x < 0)
                dir = Vector2.left;
            missile.SetupData(missileData, dir, SpawnAttack);
        }
        
        var grenadeData = TableManager.Instance.grenadeTable.Grenade.Find(x => x.id == id);
        if (grenadeData != null)
        {
            var grenade = obj.GetComponent<Grenade>();
            if (!grenade)
                grenade = obj.AddComponent<Grenade>();
            
            var dir = Vector2.right;
            if(transform.localScale.x < 0)
                dir = Vector2.left;
            grenade.SetupData(grenadeData, dir, SpawnAttack);
            grenade.Throw();
        }
        return obj;
    }
    protected GameObject SpawnObject(string id, Vector2 pos, int zAngle = 0)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
            
        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();
        if (zAngle != 0)
        {
            var finalAngle = zAngle;
            if (transform.localScale.x < 0)
                finalAngle = -zAngle;
            
            var objectAngle = spawnedObject.transform.eulerAngles;
            spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
        }

        return obj;
    }
    protected GameObject SpawnUI(string id, Transform uiTransform)
    {
        var obj = GameManager.Instance.SpawnToUIObjectPool(id, uiTransform);
        
        var uiData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (uiData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
        
        spawnedObject.SetupData(uiData, transform.localScale.x);
        spawnedObject.EnableSetting();
        
        if (spawnedObject.GetTrace())
        {
            var trace = obj.GetComponent<Trace>();
            if(!trace)
                trace = obj.AddComponent<Trace>();
            
            trace.SetTarget(uiTransform);
        }

        return obj;
    }
    
    // 1프레임 딜레이
    protected async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }
    // 일반 딜레이
    protected async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }

    // 행동 캔슬
    protected void CancelMotion()
    {
        stateCancellation?.Cancel();
        anotherCancellation?.Cancel();
        immortal = false;
        
        ClearObjectList(controlObject);
        ClearObjectList(normalObject);
        GravityChange(ConstValues.BasicGravity);
        myRigidbody.linearVelocity = Vector2.zero;
    }

    private void AddObjectList(List<GameObject> list, GameObject obj)
    {
        list.Add(obj);
    }
    protected void RemoveObjectList(List<GameObject> list, GameObject obj)
    {
        var removeObj = list.Find(x => x == obj);

        obj.gameObject.SetActive(false);
        if (removeObj != null)
            list.Remove(removeObj);
    }
    protected async void ClearObjectList(List<GameObject> list, float timer = 0.0f)
    {
        if (timer > 0)
            await UniTask.WaitForSeconds(timer);
        
        foreach (var obj in list)
            obj.gameObject.SetActive(false);
        
        list.Clear();
    }
    
    protected void Flip(int dir)
    {
        switch (dir)
        {
            case -1:
                transform.localScale = reverseScale;
                break;
            
            case 1:
                transform.localScale = defaultScale;
                break;
        }
    }
    
    protected void MoveStateSetting(EMoveState changeState)
    {
        moveState = changeState;
    }
    protected void LandingStateSetting(ELandingState changeState)
    {
        landingState = changeState;
    }
    protected void BodyTypeSetting(string bodyTypeName)
    {
        basicStat.bodyType = (EBodyType)Enum.Parse(typeof(EBodyType), bodyTypeName);
    }
    protected bool SameBodyType(string bodyTypeName)
    {
        return basicStat.bodyType.ToString() == bodyTypeName;
    }
    
    // 중력값 변경
    protected void GravityChange(float value)
    {
        myRigidbody.gravityScale = value;
    }
    // 히트박스 기상 사이즈로 변경
    protected virtual void StandHitBox()
    {
        myBoxCollider.size = standHitBoxSize;
        myBoxCollider.offset = new Vector2(standOffset.x, standOffset.y);
    }
    // 히트박스 다운 사이즈로 변경
    protected virtual void DownHitBox()
    {
        myBoxCollider.size = downHitBoxSize;
        myBoxCollider.offset = new Vector2(downOffset.x, downOffset.y);
    }
    
    // 피해를 입고있는 모션인가?
    protected bool IsDamaged()
    {
        return normalState is ENormalState.Grabbed or ENormalState.Airborne or ENormalState.Down or ENormalState.Stun or ENormalState.Damaged;
    }
    // 군중제어에 걸렸는가?
    public bool IsCc()
    {
        bool normalCondition = normalState is ENormalState.Grabbed or ENormalState.Stun;
        bool buffCondition = FindBuff(EBuffType.Stun);
        return normalCondition || buffCondition;
    }
    private bool FindBuff(EBuffType buffType)
    {
        return buffList.Find(x => x.buffType == buffType) != null;
    }
    
    // 돌진 (기본스피드, 제한스피드 배율, 돌진거리, 가속도)
    protected async UniTask<bool> Charge(float basicSpeed, float limitMag, float chargeLength, float acceleration)
    {
        float realDashSpeed = basicSpeed;
        float limitDashSpeed = basicSpeed * limitMag;
        float finalSpeed = basicSpeed + limitDashSpeed * 0.5f;
        float finalTime = chargeLength / finalSpeed;
        
        float accelerationTime = finalTime + finalTime * 0.5f;
        float finalAcceleration = 0.0f;
        float time = 0.0f;

        Vector2 startVector = transform.position;
        
        while (time < accelerationTime)
        {
            time += Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, chargeVector, realDashSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, chargeVector) * 2 < Vector2.Distance(startVector, chargeVector))
            {
                finalAcceleration += acceleration;

                if (acceleration > 0)
                    finalAcceleration = Mathf.Abs(finalAcceleration);
                else
                    finalAcceleration = -Mathf.Abs(finalAcceleration);
                
                realDashSpeed += finalAcceleration;
            }

            if (limitMag >= 1)
            {
                if (realDashSpeed > limitDashSpeed)
                    realDashSpeed = limitDashSpeed;
            }
            else
            {
                if (realDashSpeed < limitDashSpeed)
                    realDashSpeed = limitDashSpeed;
            }

            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }

        return true;
    }
    
    // 레이체크(벽 등을 판정하여 최종적으로 도착하는 지점 확인용도)
    protected Vector2 RayCheckLength(float chargeLengthX, float chargeLengthY)
    {
        // 왼쪽
        if (transform.localScale.x < 0)
        {
            var leftRay = Physics2D.Raycast(centerPos.position, Vector2.left, chargeLengthX, moveLayerMask);
            Debug.DrawRay(centerPos.position, Vector2.left * chargeLengthX, ConstValues.RedColor, 0.1f);
            
            // 레이에 닿은 콜라이더가 한개라도 있을 경우 (닿은 레이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
            if (leftRay.collider != null)
                return new Vector2(leftRay.point.x + myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 - 레이의 길이) + (자신의 콜라이더/2) 만큼 벡터가 정해진다
            else
                return new Vector2(transform.position.x - chargeLengthX + (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
        }
        // 오른쪽
        else
        {
            var rightRay = Physics2D.Raycast(centerPos.position, Vector2.right, chargeLengthX, moveLayerMask);
            Debug.DrawRay(centerPos.position, Vector2.right * chargeLengthX, ConstValues.RedColor, 0.1f);

            // 레이에 닿은 콜라이더가 한개라도 있을 경우 체크가 참이된다
            if (rightRay.collider != null)
                return new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 + 레이의 길이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
            else
                return new Vector2(transform.position.x + chargeLengthX - (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
        }
    }
    private Vector2 RayCheckLength(float chargeLengthX)
    {
        var absLengthX = Mathf.Abs(chargeLengthX);
        // 오른쪽
        if (chargeLengthX > 0)
        {
            var rightRay = Physics2D.Raycast(centerPos.position, Vector2.right, absLengthX, moveLayerMask);
            Debug.DrawRay(centerPos.position, Vector2.right * absLengthX, ConstValues.RedColor, 0.1f);

            // 레이에 닿은 콜라이더가 한개라도 있을 경우 체크가 참이된다
            if (rightRay.collider != null)
                return new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 + 레이의 길이) 만큼 벡터가 정해진다
            else
                return new Vector2(transform.position.x + absLengthX, transform.position.y);
        }
        // 왼쪽
        else
        {
            var leftRay = Physics2D.Raycast(centerPos.position, Vector2.left, absLengthX, moveLayerMask);
            Debug.DrawRay(centerPos.position, Vector2.left * absLengthX, ConstValues.RedColor, 0.1f);
            
            // 레이에 닿은 콜라이더가 한개라도 있을 경우 (닿은 레이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
            if (leftRay.collider != null)
                return new Vector2(leftRay.point.x + myBoxCollider.size.x / 2, transform.position.y);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 - 레이의 길이) 만큼 벡터가 정해진다
            else
                return new Vector2(transform.position.x - absLengthX, transform.position.y);
        }
    }
    
    public virtual void Die()
    {
        CancelMotion();
        ClearObjectList(buffObject);
        
        StateSetting(ENormalState.Die, ConstValues.Die, ConstValues.Die);
        MoveStateSetting(EMoveState.Stopping);
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        gameObject.SetActive(false);
    }
    public async void Grabbed(Vector3 grabVector)
    {
        CancelMotion();
        StateSetting(ENormalState.Grabbed, ConstValues.Grabbed, ConstValues.Grabbed);
        MoveStateSetting(EMoveState.Stopping);
        
        GravityChange(0);
        myRigidbody.linearVelocity = Vector2.zero;
        
        float grabSpeed = ConstValues.GrabbedSpeed;
        float grabBoundX = ConstValues.GrabbedBoundX;
        float grabBoundY = ConstValues.GrabbedBoundY;
        if (transform.position.x < grabVector.x)
            grabBoundX = -ConstValues.GrabbedBoundX;
        
        stateCancellation = new CancellationTokenSource();
        while (transform.position != grabVector)
        {
            transform.position = Vector2.MoveTowards(transform.position, grabVector, grabSpeed * Time.deltaTime);
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        Airborne(grabBoundX, grabBoundY);
    }
    public void Airborne(float xVelocity, float yVelocity)
    {
        CancelMotion();
        
        airborneCount = 1;
        LandingStateSetting(ELandingState.Air);
        MoveStateSetting(EMoveState.Stopping);
        
        stateCancellation = new CancellationTokenSource();
        Bound(xVelocity, yVelocity);
        DownHitBox();
    }
    private void Bound(float xVelocity, float yVelocity)
    {
        StateSetting(ENormalState.Airborne, ConstValues.Airborne, ConstValues.Airborne);
        GravityChange(ConstValues.BasicGravity);
        myRigidbody.linearVelocity = new Vector2(xVelocity, yVelocity);
    }
    protected async void DownAndStand()
    {
        StateSetting(ENormalState.Down, ConstValues.Down, ConstValues.Down);
        MoveStateSetting(EMoveState.Stopping);
        
        // 최초 공중에 떴을 때는, 땅에 닿자마자 다시 공중으로 고정높이만큼 뜬다
        if (airborneCount > 0)
        {
            airborneCount -= 1;
            //GameObject downDust = CharacterObjectPool.Instance.SpawnFromPool("DownDust_Monster");
            //downDust.transform.position = transform.position;
            //downDust.SetActive(true);
            //AddStaticEffect(GameManager.IDDown, 0.05f);
            //await UniTask.WaitUntil(() => !EffectInfo(GameManager.IDDown).isApplied, cancellationToken: cancellationToken);
            if (await NormalDelay(ConstValues.ReboundSecond, stateCancellation).SuppressCancellationThrow())
                return;
            
            Bound(0, ConstValues.ReboundForce);
        }
        // 이후에는 고정된 시간만큼 누워있다가 일어난다
        else
        {
            if (await NormalDelay(ConstValues.DownSecond, stateCancellation).SuppressCancellationThrow())
                return;
            
            StateRecovery();
        }
    }

    private void AddBuff(EBuffType buffType, float buffTime)
    {
        var findDeBuff = buffList.Find(x => x.buffType == buffType);
        // 해당 디버프가 적용되어있지 않음
        if (findDeBuff == null)
        {
            var newDeBuff = new Buff()
            {
                buffType = buffType,
                buffTime = buffTime,
                currentTime = 0,
            };
            buffList.Add(newDeBuff);
            switch (buffType)
            {
                case EBuffType.Stun:
                    basicStat.bodyType = EBodyType.Normal;
                    break;
            }
            
            SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", buffEffectPos, 0, true);
        }
        // 해당 디버프가 적용되어 있음
        else
        {
            var leftTime = findDeBuff.buffTime - findDeBuff.currentTime;

            if (leftTime < buffTime)
            {
                findDeBuff.buffTime = buffTime;
                findDeBuff.currentTime = 0;
            }
        }
    }
    public void Stun(float stunTime)
    {
        // 스턴 디버프 추가
        AddBuff(EBuffType.Stun, stunTime);
        MoveStateSetting(EMoveState.Stopping);
        
        // 이후 현재 판정에 따라서 애니메이션을 변화함
        if (normalState is ENormalState.Grabbed or ENormalState.Airborne or ENormalState.Down or ENormalState.Stun)
        {
            Debug.Log($"상위 판정이 존재함: {normalState}");
            return;
        }
        
        CancelMotion();
        stateCancellation = new CancellationTokenSource();
        StateSetting(ENormalState.Stun, ConstValues.Stun, ConstValues.Stun);
    }
    
    public async void Damaged(float damagedTime) 
    {
        if (normalState is ENormalState.Grabbed or ENormalState.Airborne or ENormalState.Down or ENormalState.Stun)
        {
            Debug.Log($"상위 판정이 존재함: {normalState}");
            return;
        }
        
        CancelMotion();
        stateCancellation = new CancellationTokenSource();
        StateSetting(ENormalState.Damaged, ConstValues.Damaged, ConstValues.Damaged);
        MoveStateSetting(EMoveState.Stopping);
        if (await NormalDelay(damagedTime, stateCancellation).SuppressCancellationThrow())
            return;
        
        StateRecovery();
    }

    // 넉백
    public async void KnockBack(float knockBackLength)
    {
        if(normalState == ENormalState.Down)
            return;
        
        var knockPosX = RayCheckLength(knockBackLength).x;
        var startDir = transform.position;
        var endDir = new Vector2(knockPosX, transform.position.y);
        float duration = ConstValues.KnockBackTime;
        float elapsed = 0f;
        
        anotherCancellation = new CancellationTokenSource();
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startDir, endDir, elapsed / duration);
            elapsed += Time.deltaTime;
            if (await YieldDelay(anotherCancellation).SuppressCancellationThrow())
                return;
        }
    }
    
    // 바라보기
    public virtual void LookAt(float xPos)
    {
        // xPos가 내 위치보다 오른쪽에 있고 내가 왼쪽을 보고 있을 때
        if (xPos > transform.position.x && transform.localScale.x < 0)
        {
            // 오른쪽으로 돈다
            transform.localScale = defaultScale; // 스케일의 값이 바뀌어 방향이 바뀐다
        }
        // xPos가 내 위치보다 왼쪽에 있고 내가 오른쪽을 보고 있을 때
        else if (xPos < transform.position.x && transform.localScale.x > 0)
        {
            // 왼쪽으로 돈다
            transform.localScale = reverseScale; // 스케일의 값이 바뀌어 방향이 바뀐다
        }
    }
    public void LookAt(int dir)
    {
        // dir이 1이라면
        switch (dir)
        {
            // 오른쪽 방향으로 진행된 공격
            case 1:
                // 왼쪽으로 돈다
                transform.localScale = reverseScale; // 스케일의 값이 바뀌어 방향이 바뀐다
                break;
            
            // 왼쪽 방향으로 진행된 공격
            case -1:
                // 오른쪽으로 돈다
                transform.localScale = defaultScale; // 스케일의 값이 바뀌어 방향이 바뀐다
                break;
        }
    }
    
    // 맞았을때의 색깔 변화
    public async void HitMaterial()
    {
        if (!gameObject.activeSelf)
            return;
        
        SpriteRendererMaterialChange(GameManager.Instance.hitMaterial);
        await UniTask.Delay(TimeSpan.FromSeconds(ConstValues.WhiteSecond));
        SpriteRendererMaterialChange(GameManager.Instance.defaultMaterial);
    }

    protected void SpriteRendererMaterialChange(Material material)
    {
        foreach (var mySpriteRenderer in mySpriteRenderers)
            mySpriteRenderer.material = material;
    }
    
    // 스프라이트 활성화 / 비활성화
    protected void SpriteRendererSetting(bool active)
    {
        foreach (var mySpriteRenderer in mySpriteRenderers)
            mySpriteRenderer.enabled = active;
    }
    
    protected void OnCollisionEnter2D(Collision2D col)
    {
        // 착지
        if ((col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform)) && landingState == ELandingState.Air)
        {
            LandingStateSetting(ELandingState.Ground);
            
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;

            jumpAttackCount = 0;

            // 점프도중, 또는 에어본 도중 지면에 닿았을 경우의 애니메이션 처리
            switch (normalState)
            {
                case ENormalState.Jump:
                    StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
                    break;
                case ENormalState.Airborne:
                    DownAndStand();
                    break;
            }
        }
    }

    protected void OnCollisionExit2D(Collision2D col)
    {
        // 점프
        if (col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform))
        {
            LandingStateSetting(ELandingState.Air);
            
            if (col.gameObject.CompareTag(ConstValues.Platform))
            {
                IgnorePlatform(true);
                
                if(normalState == ENormalState.Move)
                    StateSetting(ENormalState.Jump, ConstValues.JumpDown, ConstValues.JumpDown);
            }
        }
    }
}
