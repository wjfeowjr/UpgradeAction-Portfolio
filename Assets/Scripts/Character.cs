using System;
using System.Collections.Generic;
using System.Text;
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
    Landing,
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
    Stagger,
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
    HyperArmor,
    UnChange
}

// 버프 타입
public enum EBuffType
{
    Stun,
    Stagger,
}

[Serializable]
public class BasicStat
{
    public string id;
    public string name;
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
    [SerializeField] protected GameObject groundObject;
    
    [SerializeField] protected Vector3 defaultScale;
    [SerializeField] protected Vector3 reverseScale;
    
    protected Vector3 defaultAnimatorScale;

    protected CancellationTokenSource stateCancellation;
    protected CancellationTokenSource anotherCancellation; // 우선 넉백에만사용되고 있음

    [SerializeField] protected Collider2D ignorePlatformCollider;

    [SerializeField] protected List<GameObject> controlObject = new List<GameObject>(); // 직접 시간을 관리하는 '공격판정'
    [SerializeField] protected List<GameObject> normalObject = new List<GameObject>(); // 직접 시간을 관리하는 '일반 오브젝트'
    [SerializeField] protected List<GameObject> buffObject = new List<GameObject>(); // 직접 시간을 관리하는 '버프 오브젝트'

    [SerializeField] protected ENormalState normalState;
    [SerializeField] protected EMoveState moveState;
    [SerializeField] protected ELandingState landingState;
    [SerializeField] protected List<Buff> buffList = new List<Buff>();
    
    [SerializeField] protected Transform diePos;
    [SerializeField] protected Transform buffEffectPos;
    [SerializeField] protected Transform centerPos;
    [SerializeField] protected Transform fontPos;
    
    [SerializeField] protected Vector2 standHitBoxSize;
    [SerializeField] protected Vector2 downHitBoxSize;
    [SerializeField] protected Vector2 standOffset;
    [SerializeField] protected Vector2 downOffset;
    
    protected Vector2 chargeVector;
    protected int jumpAttackCount;
    [SerializeField] protected bool isDie;
    [SerializeField] protected float myGravity;
    protected bool downJumping;
    
    private int airborneCount;     // 에어본 카운트
    private int platformLayerMask;
    protected int groundAndPlatformLayerMask;
    protected int wallLayerMask;

    [SerializeField] protected bool immortal;
    [SerializeField] protected bool immuneStagger;

    // 프로퍼티
    public BasicStat OriginStat => originStat;
    public BasicStat BasicStat => basicStat;
    public Rigidbody2D MyRigidbody => myRigidbody;
    public BoxCollider2D MyBoxCollider => myBoxCollider;
    public GameObject GroundObject => groundObject;
    public Transform CenterPos => centerPos;
    public Transform FontPos => fontPos;

    public bool Immortal
    {
        get => immortal;
        set => immortal = value;
    }
    public bool ImmuneStagger => immuneStagger;
    public bool IsDie
    {
        get => isDie;
        set => isDie = value;
    }

    public ENormalState NormalState
    {
        get => normalState;
        set => normalState = value;
    }
    public EMoveState MoveState => moveState;

    // 상태 설정
    protected abstract void StateSetting(ENormalState changeNormalState, string triggerName, string animId);

    protected abstract void StateCheck();
    protected abstract void StateRecovery();

    protected virtual void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        
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
        platformLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Platform);
        groundAndPlatformLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform));
        wallLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Wall);
        
        ScaleSetting();
        ColSizeSetting();
    }

    protected virtual void OnEnable()
    {
        isDie = false;
        StandHitBox();
    }

    protected virtual void Update()
    {
        UpdateBungee();
        UpdateAirborneDown();
    }

    protected virtual void FixedUpdate()
    {
        UpdateVelocity();
        FindGroundObject();
    }

    private void ScaleSetting()
    {
        defaultScale = transform.localScale;
        reverseScale = new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z);
        
        defaultAnimatorScale = myAnimator.transform.localScale;
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
    
    protected void UpdateAirborneDown()
    {
        if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.Airborne) && myRigidbody.linearVelocity.y < 0)
        {
            SetTriggerAnimator(ConstValues.AirborneDown);
        }
    }
    
    protected virtual void UpdateBungee()
    {
        // if (!isDie && transform.position.y < ConstValues.BungeePosY)
        // {
        //     TakeDamage(basicStat.maxHp);
        //     Die();
        // }
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
            
            // 스턴, 무력화 상태 회복
            if (expiredDeBuff.buffType is EBuffType.Stun or EBuffType.Stagger)
            {
                StateCheck();
                if(normalState is ENormalState.Stun or ENormalState.Stagger)
                {
                    StateRecovery();
                }
            }
        }
    }

    // 최대 중력가속도 조정
    private void UpdateVelocity()
    {
        if (myRigidbody.bodyType == RigidbodyType2D.Dynamic && myRigidbody.linearVelocity.y < -30)
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -30);
    }

    protected virtual void FindGroundObject()
    {
        if (groundObject && !downJumping)
            return;
        
        var rayVector1 = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);
        var rayVector2 = new Vector2(transform.position.x, transform.position.y);
        var rayVector3 = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);

        GroundRay(rayVector1);
        GroundRay(rayVector2);
        GroundRay(rayVector3);
    }

    private void GroundRay(Vector2 rayVector)
    {
        var downRay = Physics2D.Raycast(rayVector, Vector2.down, 1.0f, groundAndPlatformLayerMask);
        Debug.DrawRay(rayVector, Vector2.down * 1.0f, ConstValues.BlueColor, 0.025f);
        if (downRay.collider != null)
            groundObject = downRay.collider.gameObject;
    }
    
    protected void IgnorePlatform(Vector2 dir, float distance)
    {
        var rayVector1 = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);
        var downRay1 = Physics2D.Raycast(rayVector1, dir, distance, platformLayerMask);
        Debug.DrawRay(rayVector1, dir * 1.0f, ConstValues.BlueColor, 0.02f);
        if (downRay1.collider != null)
        {
            if (!ignorePlatformCollider)
            {
                ignorePlatformCollider = downRay1.collider;
                Physics2D.IgnoreCollision(physicsCollider, ignorePlatformCollider, true);
            }
        }
        
        var rayVector2 = new Vector2(transform.position.x, transform.position.y);
        var downRay2 = Physics2D.Raycast(rayVector2, dir, distance, platformLayerMask);
        Debug.DrawRay(rayVector2, dir * distance, ConstValues.BlueColor, 0.02f);
        if (downRay2.collider != null)
        {
            if (!ignorePlatformCollider)
            {
                ignorePlatformCollider = downRay2.collider;
                Physics2D.IgnoreCollision(physicsCollider, ignorePlatformCollider, true);
            }
        }
        
        var rayVector3 = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);
        var downRay3 = Physics2D.Raycast(rayVector3, dir, distance, platformLayerMask);
        Debug.DrawRay(rayVector3, dir * distance, ConstValues.BlueColor, 0.02f);
        if (downRay3.collider != null)
        {
            if (!ignorePlatformCollider)
            {
                ignorePlatformCollider = downRay3.collider;
                Physics2D.IgnoreCollision(physicsCollider, ignorePlatformCollider, true);
            }
        }
    }

    protected void ClearIgnorePlatform()
    {
        if (ignorePlatformCollider)
        {
            Physics2D.IgnoreCollision(physicsCollider, ignorePlatformCollider, false);
            ignorePlatformCollider = null;
        }
    }

    private bool GroundAndPlatformRay(Vector2 rayVector)
    {
        var downRay = Physics2D.Raycast(rayVector, Vector2.down, 0.1f, groundAndPlatformLayerMask);
        Debug.DrawRay(rayVector, Vector2.down * 0.1f, ConstValues.GreenColor, 0.02f);

        if (downRay.collider == null)
        {
            if (!GetJumpState())
            {
                LandingStateSetting(ELandingState.Air);
            }
            return false;
        }
        else
        {
            if (GetJumpState())
            {
                LandingStateSetting(ELandingState.Ground);
                // if (transform.position.y > downRay.point.y)
                //     transform.position = new Vector2(transform.position.x, downRay.point.y);
            }
            return true;
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
    
    public void StopVelocity()
    {
        myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocityY);
    }
    public void ZeroVelocity()
    {
        myRigidbody.linearVelocity = new Vector2(0, 0);
    }

    public virtual void TakeDamage(int damage)
    {
        if (damage == 0)
            return;
        
        // 체력 다는 알고리즘 삽입
        basicStat.hp -= damage;
        if (basicStat.hp < 0)
            basicStat.hp = 0;
    }

    public virtual void TakeStagger(int stagger)
    {
        if (immuneStagger)
            return;
        
        basicStat.stagger -= stagger;
        if (basicStat.stagger <= 0)
            basicStat.stagger = 0;
    }

    public void SpawnDamageFont(int damage, bool critical)
    {
        if (damage == 0)
            return;
        
        var textFont = GameManager.Instance.SpawnToUIObjectPool(ConstValues.TextFont, fontPos).GetComponent<TextFont>();
        StringBuilder damageText = new StringBuilder();
        damageText.Append(damage);
        
        if (critical)
        {
            if(GetComponent<Player>())
                textFont.ColorSetting(EFontType.EnemyCritical);
            else if(GetComponent<Monster>())
                textFont.ColorSetting(EFontType.MyCritical);
            
            damageText.Append("!");
        }
        else
        {
            if(GetComponent<Player>())
                textFont.ColorSetting(EFontType.EnemyDamage);
            else if(GetComponent<Monster>())
                textFont.ColorSetting(EFontType.MyDamage);
        }
        
        textFont.DisplayFont(55, damageText.ToString());
    }

    public void SpawnHitEffect(string id, float minScale = 1.0f, float maxScale = 1.0f)
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
            var randomScale = Random.Range(minScale, maxScale);
            randomVector = new Vector3(randomScale, randomScale, randomScale);
        }
        effectObj.transform.localScale = randomVector;
    }
    
    // 공격 소환(데이터 삽입용)
    protected GameObject SpawnAttackObject(string id, Transform attackTransform, int zAngle = 0, int missileDir = 0)
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

            Vector2 dir;
            if (missileDir == 0)
            {
                dir = Vector2.right;
                if(transform.localScale.x < 0)
                    dir = Vector2.left;
            }
            else
            {
                dir = new Vector2(missileDir, 0);
            }
            
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
                attack = obj.AddComponent<Attack>();

            attack.SetupData(this, attackData);
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
    public GameObject SpawnObject(string id, Vector2 pos, int zAngle = 0)
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
    protected GameObject SpawnUIObject(string id, Transform uiTransform)
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
    protected GameObject SpawnUI(string id, Vector2 objectVector)
    {
        var obj = GameManager.Instance.SpawnToUIPool(id, objectVector);
        
        var uiData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (uiData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
        
        spawnedObject.SetupData(uiData, transform.localScale.x);
        spawnedObject.EnableSetting();

        return obj;
    }
    
    // 1프레임 딜레이
    protected async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }
    protected async UniTask FixedYieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.WaitForFixedUpdate(cancellationToken: tokenSource.Token);
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

        downJumping = false;
        ClearIgnorePlatform();
        ClearObjectList(controlObject);
        ClearObjectList(normalObject);
        GravityChange(myGravity);
        myRigidbody.linearVelocity = Vector2.zero;

        switch (normalState)
        {
            case ENormalState.Dash:
                //immortal = false;
                myBoxCollider.enabled = true;
                break;
        }
    }

    private void AddObjectList(List<GameObject> list, GameObject obj)
    {
        if(!list.Contains(obj))
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
    
    public void Flip(int dir)
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
        if (changeState == ELandingState.Ground)
            downJumping = false;
        
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
    public void GravityChange(float value)
    {
        myRigidbody.gravityScale = value;
    }
    // 히트박스 기상 사이즈로 변경
    protected void StandHitBox()
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
    protected bool IsCc()
    {
        bool normalCondition = normalState is ENormalState.Grabbed or ENormalState.Stun;
        bool buffCondition = FindBuff(EBuffType.Stun);
        return normalCondition || buffCondition;
    }
    private bool FindBuff(EBuffType buffType)
    {
        return buffList.Find(x => x.buffType == buffType) != null;
    }
    
    // 지형을 무시하는 돌진 (기본스피드, 가속 배율, 돌진거리, 가속되는 시점)
    protected async UniTask<bool> Charge(float basicSpeed, float limitMag, float chargeLength, float accelPercent)
    {
        // 1) 초기 계산
        float realDashSpeed = basicSpeed;
        float targetSpeed   = basicSpeed * limitMag;  
        Vector2 startPos    = transform.position;
        Vector2 direction   = (chargeVector - startPos).normalized;
        float totalDist     = chargeLength;
        if (totalDist <= 0f)
            return false;

        // 2) 전체 돌진 시간 계산 (평균 속도 = (basic + target) / 2)
        float duration = totalDist / ((basicSpeed + targetSpeed) * 0.5f);
        float elapsed  = 0f;

        // 3) FixedUpdate 루프: elapsed < duration 동안 실행
        while (elapsed < duration)
        {
            // 시간 누적
            elapsed += Time.fixedDeltaTime;

            // 전체 시간 대비 현재 위치한 비율 (0→1)
            float normTime = Mathf.Clamp01(elapsed / duration);

            // accelPercent 이후부터 속도 보간
            if (normTime > accelPercent)
            {
                // t = 0 (accelPercent 지점) → 1 (끝)
                float t = (normTime - accelPercent) / (1f - accelPercent);
                realDashSpeed = Mathf.Lerp(basicSpeed, targetSpeed, t);
            }

            // Rigidbody2D에 속도 적용
            // direction * realDashSpeed
            myRigidbody.linearVelocity = new Vector2(direction.x * realDashSpeed, myRigidbody.linearVelocityY);

            // FixedYieldDelay 대기, 취소 시 false 반환
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
            
            // 대시 레이체크
            var rayVector1 = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);
            var rayVector2 = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);
            if (transform.localScale.x < 0)
            {
                rayVector1 = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);
                rayVector2 = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);
            }
            if(!GroundAndPlatformRay(rayVector1))
                GroundAndPlatformRay(rayVector2);
        }

        // 4) 돌진 종료 후 정지
        myRigidbody.linearVelocity = Vector2.zero;
        return true;
    }

    public virtual void Die()
    {
        CancelMotion();
        ClearObjectList(buffObject);
        isDie = true;
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
    public virtual void Airborne(float xVelocity, float yVelocity)
    {
        CancelMotion();

        airborneCount = 1;
        LandingStateSetting(ELandingState.Air);
        MoveStateSetting(EMoveState.Stopping);
        ResetTriggerAnimator(ConstValues.Jump);
        
        stateCancellation = new CancellationTokenSource();
        Bound(xVelocity, yVelocity);
        DownHitBox();
    }
    private void Bound(float xVelocity, float yVelocity)
    {
        StateSetting(ENormalState.Airborne, ConstValues.Airborne, ConstValues.Airborne);
        // 공중몹도 떴다 떨어지기 때문에 기본 중력값으로 변환
        GravityChange(ConstValues.BasicGravity);
        myRigidbody.linearVelocity = new Vector2(xVelocity, yVelocity);
    }
    protected virtual async void DownAndStand()
    {
        StateSetting(ENormalState.Down, ConstValues.Down, ConstValues.Down);
        MoveStateSetting(EMoveState.Stopping);
        SpawnObject(ConstValues.DownDust, transform.position);
        
        // 최초 공중에 떴을 때는, 땅에 닿자마자 다시 공중으로 고정높이만큼 뜬다
        if (airborneCount > 0)
        {
            airborneCount -= 1;
            if (await NormalDelay(ConstValues.ReboundSecond, stateCancellation).SuppressCancellationThrow())
                return;
            
            Bound(0, ConstValues.ReboundForce);
        }
        // 이후에는 고정된 시간만큼 누워있다가 일어난다
        else
        {
            if (isDie)
            {
                await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
                if(gameObject.activeSelf)
                    BlinkDelete();
                return;
            }
            
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
                case EBuffType.Stagger:
                    // 스트롱 아머만 깨짐
                    if(originStat.bodyType == EBodyType.StrongArmor)
                        basicStat.bodyType = EBodyType.Normal;
                    SpawnObject($"{buffType.ToString()}{ConstValues.Explosion}", buffEffectPos);
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
    
    // 상태이상
    // 스턴
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
    // 무력화
    public virtual void Stagger()
    {
        // 무력화 디버프 추가
        AddBuff(EBuffType.Stagger, basicStat.staggerTime);
        MoveStateSetting(EMoveState.Stopping);

        CancelMotion();
        stateCancellation = new CancellationTokenSource();
        StateSetting(ENormalState.Stagger, ConstValues.Stagger, ConstValues.Stagger);
        immuneStagger = true;
    }
    
    public virtual async void Damaged(float damagedTime) 
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
        // 넉백 중 다운 상태라면 무시
        if (normalState == ENormalState.Down)
            return;

        // 방향 결정 (knockBackLength 양수→오른쪽, 음수→왼쪽)
        Vector2 dir = (knockBackLength >= 0) ? Vector2.right : Vector2.left;
        float duration = ConstValues.KnockBackTime;

        // 시작 속도: 거리 / 시간
        float speed  = Mathf.Abs(knockBackLength) / duration;
        Vector2 constantVelocity  = dir * speed ;
        
        // 넉백 시작—바로 초기 속도 설정
        myRigidbody.linearVelocity = constantVelocity;

        anotherCancellation = new CancellationTokenSource();
        int steps = Mathf.CeilToInt(duration / Time.fixedDeltaTime);
        for (int i = 0; i < steps; i++)
        {
            // FixedUpdate 타이밍 대기 및 취소 체크
            if (await FixedYieldDelay(anotherCancellation).SuppressCancellationThrow())
            {
                // 취소되면 속도 리셋 후 종료
                myRigidbody.linearVelocity = Vector2.zero;
                return;
            }
        }

        // 넉백 끝나면 속도 0으로
        myRigidbody.linearVelocity = Vector2.zero;
    }

    // 바라보기
    public virtual void LookAt(float xPos)
    {
        // xPos가 내 위치보다 오른쪽에 있고 내가 왼쪽을 보고 있을 때  && transform.localScale.x < 0
        if (xPos > transform.position.x)
        {
            // 오른쪽으로 돈다
            transform.localScale = defaultScale; // 스케일의 값이 바뀌어 방향이 바뀐다
        }
        // xPos가 내 위치보다 왼쪽에 있고 내가 오른쪽을 보고 있을 때 && transform.localScale.x > 0
        if (xPos < transform.position.x)
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

    protected void PlaySound(string soundId, float volumeScale = 0.8f)
    {
        SoundManager.Instance.PlaySound(soundId, volumeScale);
    }
    
    // 정지
    public void Stop()
    {
        if (normalState == ENormalState.Move)
        {
            myAnimator.ResetTrigger(ConstValues.Move);
            StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        }
        
        if(moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);
    }
    
    // 커스텀
    public void CustomJump(Vector2 jumpVelocity)
    {
        myRigidbody.linearVelocity = jumpVelocity;
    }
    
    protected void CustomMoving_X(Vector2 dir, float speed)
    {
        float targetSpeedX = dir.x * speed;
        float targetSpeedY = myRigidbody.linearVelocity.y;

        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
    }
    protected void CustomMoving_Y(Vector2 dir, float speed)
    {
        float targetSpeedX = myRigidbody.linearVelocity.x;
        float targetSpeedY = dir.y * speed;

        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
    }
    
    public void CustomAnimTrigger(ENormalState state, string triggerName, string animId = null)
    {
        StateSetting(state, triggerName, animId);
    }
    
    // 깜빡이며 사라지기
    public virtual async void BlinkDelete()
    {
        stateCancellation = new CancellationTokenSource();
        for (int i = 0; i < 7; i++)
        {
            foreach (var mySpriteRenderer in mySpriteRenderers)
                mySpriteRenderer.enabled = false;
            if (await NormalDelay(ConstValues.BlinkSecond, stateCancellation).SuppressCancellationThrow())
                return;
            
            foreach (var mySpriteRenderer in mySpriteRenderers)
                mySpriteRenderer.enabled = true;
            if (await NormalDelay(ConstValues.BlinkSecond, stateCancellation).SuppressCancellationThrow())
                return;
        }
        gameObject.SetActive(false);
    }

    public bool IsOnPlatform()
    {
        return groundObject.CompareTag(ConstValues.Platform);
    }

    // protected async UniTask<bool> Charge(float basicSpeed, float limitMag, float chargeLength, float acceleration)
    // {
    //     float realDashSpeed = basicSpeed;
    //     float limitDashSpeed = basicSpeed * limitMag;
    //     float finalSpeed = basicSpeed + limitDashSpeed * 0.5f;
    //     float finalTime = chargeLength / finalSpeed;
    //     
    //     float accelerationTime = finalTime + finalTime * 0.5f;
    //     float finalAcceleration = 0.0f;
    //     float time = 0.0f;
    //
    //     Vector2 startVector = transform.position;
    //     
    //     while (time < accelerationTime)
    //     {
    //         time += Time.deltaTime;
    //         transform.position = Vector2.MoveTowards(transform.position, chargeVector, realDashSpeed * Time.deltaTime);
    //
    //         if (Vector2.Distance(transform.position, chargeVector) * 2 < Vector2.Distance(startVector, chargeVector))
    //         {
    //             finalAcceleration += acceleration;
    //
    //             if (acceleration > 0)
    //                 finalAcceleration = Mathf.Abs(finalAcceleration);
    //             else
    //                 finalAcceleration = -Mathf.Abs(finalAcceleration);
    //             
    //             realDashSpeed += finalAcceleration;
    //         }
    //
    //         if (limitMag >= 1)
    //         {
    //             if (realDashSpeed > limitDashSpeed)
    //                 realDashSpeed = limitDashSpeed;
    //         }
    //         else
    //         {
    //             if (realDashSpeed < limitDashSpeed)
    //                 realDashSpeed = limitDashSpeed;
    //         }
    //
    //         if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
    //             return false;
    //     }
    //
    //     return true;
    // }
    
    // 레이체크(벽 등을 판정하여 최종적으로 도착하는 지점 확인용도)
    // protected Vector2 RayCheckLength(float chargeLengthX, float chargeLengthY)
    // {
    //     var rayVector = transform.position;
    //     
    //     // 왼쪽
    //     if (transform.localScale.x < 0)
    //     {
    //         var leftRay = Physics2D.Raycast(rayVector, Vector2.left, chargeLengthX, moveLayerMask);
    //         Debug.DrawRay(rayVector, Vector2.left * chargeLengthX, ConstValues.RedColor, 0.1f);
    //         
    //         // 레이에 닿은 콜라이더가 한개라도 있을 경우 (닿은 레이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
    //         if (leftRay.collider != null)
    //             return new Vector2(leftRay.point.x + myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
    //         // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 - 레이의 길이) + (자신의 콜라이더/2) 만큼 벡터가 정해진다
    //         else
    //             return new Vector2(transform.position.x - chargeLengthX + (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
    //     }
    //     // 오른쪽
    //     else
    //     {
    //         var rightRay = Physics2D.Raycast(rayVector, Vector2.right, chargeLengthX, moveLayerMask);
    //         Debug.DrawRay(rayVector, Vector2.right * chargeLengthX, ConstValues.RedColor, 0.1f);
    //
    //         // 레이에 닿은 콜라이더가 한개라도 있을 경우 체크가 참이된다
    //         if (rightRay.collider != null)
    //             return new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
    //         // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 + 레이의 길이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
    //         else
    //             return new Vector2(transform.position.x + chargeLengthX - (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
    //     }
    // }
    
    // public async void KnockBack(float knockBackLength)
    // {
    //     if(normalState == ENormalState.Down)
    //         return;
    //     
    //     var knockPosX = RayCheckLength(knockBackLength).x;
    //     var startDir = transform.position;
    //     var endDir = new Vector2(knockPosX, transform.position.y);
    //     float duration = ConstValues.KnockBackTime;
    //     float elapsed = 0f;
    //     
    //     anotherCancellation = new CancellationTokenSource();
    //     while (elapsed < duration)
    //     {
    //         transform.position = Vector3.Lerp(startDir, endDir, elapsed / duration);
    //         elapsed += Time.deltaTime;
    //         if (await YieldDelay(anotherCancellation).SuppressCancellationThrow())
    //             return;
    //     }
    // }
    
    // private Vector2 RayCheckLength(float chargeLengthX)
    // {
    //     var absLengthX = Mathf.Abs(chargeLengthX);
    //     // 오른쪽
    //     if (chargeLengthX > 0)
    //     {
    //         var rightRay = Physics2D.Raycast(centerPos.position, Vector2.right, absLengthX, moveLayerMask);
    //         Debug.DrawRay(centerPos.position, Vector2.right * absLengthX, ConstValues.RedColor, 0.1f);
    //
    //         // 레이에 닿은 콜라이더가 한개라도 있을 경우 체크가 참이된다
    //         if (rightRay.collider != null)
    //             return new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y);
    //         // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 + 레이의 길이) 만큼 벡터가 정해진다
    //         else
    //             return new Vector2(transform.position.x + absLengthX, transform.position.y);
    //     }
    //     // 왼쪽
    //     else
    //     {
    //         var leftRay = Physics2D.Raycast(centerPos.position, Vector2.left, absLengthX, moveLayerMask);
    //         Debug.DrawRay(centerPos.position, Vector2.left * absLengthX, ConstValues.RedColor, 0.1f);
    //         
    //         // 레이에 닿은 콜라이더가 한개라도 있을 경우 (닿은 레이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
    //         if (leftRay.collider != null)
    //             return new Vector2(leftRay.point.x + myBoxCollider.size.x / 2, transform.position.y);
    //         // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 - 레이의 길이) 만큼 벡터가 정해진다
    //         else
    //             return new Vector2(transform.position.x - absLengthX, transform.position.y);
    //     }
    // }
    
    // public async void KnockBack(float knockBackLength)
    // {
    //     if(normalState == ENormalState.Down)
    //         return;
    //
    //     var knockPosX = transform.position.x + knockBackLength;
    //     if(knockBackLength < 0)
    //         knockPosX = transform.position.x - knockBackLength;
    //     
    //     var startDir = transform.position;
    //     var endDir = new Vector2(knockPosX, transform.position.y);
    //     float duration = ConstValues.KnockBackTime;
    //     float elapsed = 0f;
    //     
    //     anotherCancellation = new CancellationTokenSource();
    //     while (elapsed < duration)
    //     {
    //         transform.position = Vector3.Lerp(startDir, endDir, elapsed / duration);
    //         elapsed += Time.deltaTime;
    //         if (await FixedYieldDelay(anotherCancellation).SuppressCancellationThrow())
    //             return;
    //     }
    // }
}
