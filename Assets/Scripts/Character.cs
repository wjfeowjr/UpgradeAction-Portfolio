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
    Leap,
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
    HeavyArmor,
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

public abstract class Character : InteractionController
{
    [SerializeField] protected BasicStat originStat; // 원본 스텟
    [SerializeField] protected BasicStat basicStat; // 내 스텟(변동되어야 함)

    protected Rigidbody2D myRigidbody;
    protected BoxCollider2D myBoxCollider;
    protected Collider2D physicsCollider;
    protected Animator myAnimator;
    protected SpriteRenderer[] mySpriteRenderers;
    [SerializeField] protected GameObject groundObject;

    [SerializeField] protected Vector3 defaultScale;
    [SerializeField] protected Vector3 reverseScale;

    protected Vector3 defaultAnimatorScale;

    protected CancellationTokenSource stateCancellation;
    protected CancellationTokenSource jumpCancellation;
    protected CancellationTokenSource anotherCancellation; // 우선 넉백에만사용되고 있음
    
    [SerializeField] protected Collider2D footTrigger;
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
    
    [SerializeField] protected MovingPlatform currentPlatform;

    protected Vector2 chargeVector;
    [SerializeField] protected int landingAttackCount;
    protected int jumpAttackCount;
    protected bool isDie;
    protected float myGravity;
    protected bool downJumping;
    
    [SerializeField] protected bool isOnGround;     // 이어지는 플랫폼 처리에만 사용
    [SerializeField] protected bool isOnPlatform;   // 아랫점프, 이어지는 땅 처리에만 사용

    protected int airborneCount; // 에어본 카운트
    private int platformLayerMask;
    protected int groundLayerMask;
    protected int groundAndPlatformLayerMask;
    protected int monsterWalkLayerMask;
    protected int agroLayerMask;
    protected int bossLayerMask;

    protected bool isCeilingHang;
    protected bool immortal;
    protected bool immuneStagger;

    // 프로퍼티
    public BasicStat OriginStat => originStat;
    public BasicStat BasicStat => basicStat;
    public Rigidbody2D MyRigidbody => myRigidbody;
    public BoxCollider2D MyBoxCollider => myBoxCollider;
    public GameObject GroundObject => groundObject;
    public Transform CenterPos => centerPos;
    public Transform FontPos => fontPos;
    
    // 실험
    [SerializeField] private float castDistance;

    [Header("Box Sizes")]
    [SerializeField] private Vector2 horizontalBoxSize; // 좌우 (세로로 긴 박스)
    [SerializeField] private Vector2 verticalBoxSize;   // 상하 (가로로 긴 박스)

    [Header("Offsets")]
    [SerializeField] private float horizontalOffset; // 중심에서 좌우로 얼마나 떨어질지
    [SerializeField] private float verticalOffset;   // 중심에서 위아래로 얼마나 떨어질지

    public bool isGrounded;
    public bool isCeilingHit;
    public bool isWallLeft;
    public bool isWallRight;

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
        DataCaching();

        castDistance = 0.0f;
        horizontalBoxSize = new Vector2(0.1f, myBoxCollider.size.y * 0.8f); // 좌우 (세로로 긴 박스)
        verticalBoxSize = new Vector2(myBoxCollider.size.x * 0.8f, 0.1f); // 상하 (가로로 긴 박스)
        horizontalOffset = myBoxCollider.size.x * 0.5f; // 중심에서 좌우로 얼마나 떨어질지
        verticalOffset = myBoxCollider.size.y * 0.5f; // 중심에서 위아래로 얼마나 떨어질지
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
        CheckCollisions();
    }

    protected virtual void FixedUpdate()
    {
        UpdateVelocity();
        FindGroundObject();
    }

    private void CheckCollisions()
    {
        // if (hit.collider != null)
        // {
        //     // 충돌 지점에 빨간색 점(짧은 선)을 그려서 확인
        //     Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.red);
        // }
        
        // 1. 왼쪽 벽 체크
        RaycastHit2D hitLeft = Physics2D.BoxCast((Vector2)CenterPos.position + Vector2.left * horizontalOffset, 
            horizontalBoxSize, 0f, Vector2.left, castDistance, groundLayerMask);

        // 2. 오른쪽 벽 체크
        RaycastHit2D hitRight = Physics2D.BoxCast((Vector2)CenterPos.position + Vector2.right * horizontalOffset, 
            horizontalBoxSize, 0f, Vector2.right, castDistance, groundLayerMask);

        // 3. 천장 체크 (위)
        RaycastHit2D hitUp = Physics2D.BoxCast((Vector2)CenterPos.position + Vector2.up * verticalOffset, 
            verticalBoxSize, 0f, Vector2.up, castDistance, groundLayerMask);
        
        // 4. 바닥 체크 (아래)
        RaycastHit2D hitDown = Physics2D.BoxCast((Vector2)CenterPos.position + Vector2.down * verticalOffset, 
            verticalBoxSize, 0f, Vector2.down, castDistance, groundAndPlatformLayerMask);

        isWallLeft = hitLeft.collider != null;
        isWallRight = hitRight.collider != null;
        isCeilingHit = hitUp.collider != null;
        isGrounded = hitDown.collider != null;

        if (isGrounded)
        {
            Debug.DrawRay(hitDown.point, hitDown.normal * 0.2f, Color.red);
        }
    }

    // 디버그 시각화 (Scene 뷰에서 확인 가능)
    private void OnDrawGizmos()
    {
        // 왼쪽 벽 박스
        Gizmos.color = isWallLeft ? Color.green : Color.red;
        DrawBox((Vector2)CenterPos.position + Vector2.left * (horizontalOffset + castDistance), horizontalBoxSize);

        // 오른쪽 벽 박스
        Gizmos.color = isWallRight ? Color.green : Color.red;
        DrawBox((Vector2)CenterPos.position + Vector2.right * (horizontalOffset + castDistance), horizontalBoxSize);

        // 바닥 박스 (감지 시 초록색)
        Gizmos.color = isGrounded ? Color.green : Color.red;
        DrawBox((Vector2)CenterPos.position + Vector2.down * (verticalOffset + castDistance), verticalBoxSize);

        // 천장 박스
        Gizmos.color = isCeilingHit ? Color.green : Color.red;
        DrawBox((Vector2)CenterPos.position + Vector2.up * (verticalOffset + castDistance), verticalBoxSize);
    }

    private void DrawBox(Vector2 position, Vector2 size)
    {
        Gizmos.DrawWireCube(position, size);
    }
    
    public void DataCaching()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        myBoxCollider = GetComponent<BoxCollider2D>();
        foreach (var component in GetComponentsInChildren<Collider2D>())
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
        groundLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Ground);
        groundAndPlatformLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform));
        monsterWalkLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform)) | (1 << LayerMask.NameToLayer(ConstValues.Trap));
        agroLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform)) | (1 << LayerMask.NameToLayer(ConstValues.Player));
        bossLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Player));
        
        ScaleSetting();
        ColSizeSetting();
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
    
    public float ColFront()
    {
        float dir = 1;
        if (transform.localScale.x < 0)
            dir = -1;

        return transform.position.x + dir * myBoxCollider.size.x * 0.5f;
    }

    public float ColBehind()
    {
        float dir = -1;
        if (transform.localScale.x < 0)
            dir = 1;

        return transform.position.x + dir * myBoxCollider.size.x * 0.5f;
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
            if (removeEffect != null)
                RemoveObjectList(buffObject, removeEffect);

            // 스턴, 무력화 상태 회복
            if (expiredDeBuff.buffType is EBuffType.Stun or EBuffType.Stagger)
            {
                StateCheck();
                if (normalState is ENormalState.Stun or ENormalState.Stagger)
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

        // physicsCollider.size.x
        var rayVector1 = new Vector2(transform.position.x - myBoxCollider.size.x / 2, transform.position.y);
        var rayVector2 = new Vector2(transform.position.x, transform.position.y);
        // physicsCollider.size.x
        var rayVector3 = new Vector2(transform.position.x + myBoxCollider.size.x / 2, transform.position.y);

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
        // physicsCollider.size.x / 2
        var rayVector1 = new Vector2(transform.position.x - myBoxCollider.size.x / 2, transform.position.y);
        var downRay1 = Physics2D.Raycast(rayVector1, dir, distance, platformLayerMask);
        Debug.DrawRay(rayVector1, dir * 1.0f, ConstValues.BlueColor, 0.02f);
        if (downRay1.collider != null)
        {
            if (!ignorePlatformCollider)
            {
                ignorePlatformCollider = downRay1.collider;
                Physics2D.IgnoreCollision(physicsCollider, ignorePlatformCollider, true);
                Physics2D.IgnoreCollision(footTrigger, ignorePlatformCollider, true);
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
                Physics2D.IgnoreCollision(footTrigger, ignorePlatformCollider, true);
            }
        }

        // physicsCollider.size.x / 2
        var rayVector3 = new Vector2(transform.position.x + myBoxCollider.size.x / 2, transform.position.y);
        var downRay3 = Physics2D.Raycast(rayVector3, dir, distance, platformLayerMask);
        Debug.DrawRay(rayVector3, dir * distance, ConstValues.BlueColor, 0.02f);
        if (downRay3.collider != null)
        {
            if (!ignorePlatformCollider)
            {
                ignorePlatformCollider = downRay3.collider;
                Physics2D.IgnoreCollision(physicsCollider, ignorePlatformCollider, true);
                Physics2D.IgnoreCollision(footTrigger, ignorePlatformCollider, true);
            }
        }
    }

    protected void ClearIgnorePlatform()
    {
        if (ignorePlatformCollider)
        {
            Physics2D.IgnoreCollision(physicsCollider, ignorePlatformCollider, false);
            Physics2D.IgnoreCollision(footTrigger, ignorePlatformCollider, false);
            ignorePlatformCollider = null;
        }
    }

    // 반동
    protected void Rebound(float force)
    {
        if (transform.localScale.x > 0)
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
        bool isHovering = false;
        var monster = GetComponent<Monster>();
        if (monster != null)
            isHovering = monster.IsHovering;
        
        return !isHovering && landingState == ELandingState.Air;
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

    public void StopVelocity_X()
    {
        myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocityY);
    }

    public void StopVelocity_Y()
    {
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocityX, 0);
    }

    public void ZeroVelocity()
    {
        myRigidbody.linearVelocity = new Vector2(0, 0);
    }

    public virtual void TakeDamage(int damage, bool isTrapAttack)
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
            if (GetComponent<Player>())
                textFont.ColorSetting(EFontType.EnemyCritical);
            else if (GetComponent<Monster>())
                textFont.ColorSetting(EFontType.MyCritical);

            damageText.Append("!");
        }
        else
        {
            if (GetComponent<Player>())
                textFont.ColorSetting(EFontType.EnemyDamage);
            else if (GetComponent<Monster>())
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

        Vector2 finalVector = new Vector2(transform.position.x + randXpos,
            transform.position.y + myBoxCollider.offset.y + randYpos);
        var effectObj = SpawnObject(id, finalVector);

        if (minScale < 1.0f)
        {
            var randomScale = Random.Range(minScale, maxScale);
            randomVector = new Vector3(randomScale, randomScale, randomScale);
        }

        effectObj.transform.localScale = randomVector;
    }

    private void SetSpawnedObjectData(string id, GameObject obj, int zAngle, Transform traceTransform = null, bool isBuff = false)
    {
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

            // 몬스터 체력바는 제외된다
            if (obj.GetComponent<TotalBar>() == null)
            {
                if (spawnedObject.GetObjectTime() == 0)
                {
                    if (isBuff)
                        AddObjectList(buffObject, obj);
                    else
                        AddObjectList(normalObject, obj);
                }
            
                if (spawnedObject.GetObjectTime() == 0)
                    AddObjectList(controlObject, obj);
            }

            if (traceTransform != null)
            {
                if (spawnedObject.GetTrace())
                {
                    var trace = obj.GetComponent<Trace>();
                    if (!trace)
                        trace = obj.AddComponent<Trace>();

                    trace.SetTarget(traceTransform);
                }
            }
        }
    }

    private void SetAttackData(string id, GameObject obj)
    {
        var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == id);
        if (attackData != null)
        {
            var attack = obj.GetComponent<Attack>();
            if (!attack)
                attack = obj.AddComponent<Attack>();
            
            attack.SetupCastChar(this);
            attack.SetupData(attackData);
            attack.EnableSetting();
        }
    }
    private void SetMissileData(string id, GameObject obj, int missileDir = 0)
    {
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
                if (transform.localScale.x < 0)
                    dir = Vector2.left;
            }
            else
            {
                dir = new Vector2(missileDir, 0);
            }

            missile.SetupData(missileData, dir, SpawnAttack);
        }
    }
    private void SetGrenadeData(string id, GameObject obj, Vector2 targetVector = default)
    {
        var grenadeData = TableManager.Instance.grenadeTable.Grenade.Find(x => x.id == id);
        if (grenadeData != null)
        {
            var grenade = obj.GetComponent<Grenade>();
            if (!grenade)
                grenade = obj.AddComponent<Grenade>();

            var dir = Vector2.right;
            if (transform.localScale.x < 0)
                dir = Vector2.left;
            grenade.SetupData(grenadeData, dir, SpawnAttack);

            if (targetVector == default)
                grenade.Throw();
            else
                grenade.TargetThrow(targetVector);
        }
    }
    
    // 공격 소환(데이터 삽입용)
    protected GameObject SpawnAttackObject(string id, Transform attackTransform, int zAngle = 0, int missileDir = 0, Vector2 targetVector = default)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackTransform);
        SetSpawnedObjectData(id, obj, zAngle, attackTransform);
        SetAttackData(id, obj);
        SetMissileData(id, obj, missileDir);
        SetGrenadeData(id, obj, targetVector);
        
        return obj;
    }

    // 공격 소환
    protected void SpawnAttack(string id, Transform attackTransform, int zAngle = 0, Vector2 targetVector = default)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackTransform);
        SetSpawnedObjectData(id, obj, zAngle, attackTransform);
        SetAttackData(id, obj);
        SetMissileData(id, obj);
        SetGrenadeData(id, obj, targetVector);
    } 
    // 공격 소환 (오버로딩)
    protected void SpawnAttack(string id, Vector2 pos, int zAngle = 0, Vector2 targetVector = default)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        SetSpawnedObjectData(id, obj, zAngle);
        SetAttackData(id, obj);
        SetMissileData(id, obj);
        SetGrenadeData(id, obj, targetVector);
    }

    // 공격판정이 없는 오브젝트 소환
    protected GameObject SpawnObject(string id, Transform attackTransform, int zAngle = 0, bool isBuff = false, Vector2 targetVector = default)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackTransform);
        SetSpawnedObjectData(id, obj, zAngle, attackTransform, isBuff);
        SetMissileData(id, obj);
        SetGrenadeData(id, obj, targetVector);

        return obj;
    } 
    // 공격판정이 없는 오브젝트 소환 (오버로딩)
    public GameObject SpawnObject(string id, Vector2 pos, int zAngle = 0, Vector2 targetVector = default)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        SetSpawnedObjectData(id, obj, zAngle);
        SetMissileData(id, obj);
        SetGrenadeData(id, obj, targetVector);

        return obj;
    }

    protected GameObject SpawnUIObject(string id, Transform uiTransform)
    {
        var obj = GameManager.Instance.SpawnToUIObjectPool(id, uiTransform);
        SetSpawnedObjectData(id, obj, 0, uiTransform);

        return obj;
    }

    protected GameObject SpawnUI(string id, Vector2 objectVector)
    {
        var obj = GameManager.Instance.SpawnToUIPool(id, objectVector);
        SetSpawnedObjectData(id, obj, 0);

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

    // 대기 딜레이
    protected async UniTask WaitUntilDelay(Func<bool> condition, CancellationTokenSource tokenSource)
    {
        await UniTask.WaitUntil(condition, cancellationToken: tokenSource.Token);
    }

    // 행동 캔슬
    public virtual void CancelMotion(bool cancelJump = true, bool velocity0 = true, bool zeroLandingAttack = true)
    {
        stateCancellation?.Cancel();
        anotherCancellation?.Cancel();
        
        if(cancelJump)
            jumpCancellation?.Cancel();
        
        if(myRigidbody && velocity0)
            myRigidbody.linearVelocity = Vector2.zero;

        if(zeroLandingAttack)
            landingAttackCount = 0;
        
        downJumping = false;
        ClearIgnorePlatform();
        GravityChange(myGravity);

        float timer = 0.0f;
        switch (normalState)
        {
            case ENormalState.Dash:
                immortal = false;
                myBoxCollider.enabled = true;
                myRigidbody.linearVelocity = Vector2.zero;
                timer = 0.8f;
                break;
        }
        ClearObjectList(controlObject, timer);
        ClearObjectList(normalObject, timer);
    }

    private void AddObjectList(List<GameObject> list, GameObject obj)
    {
        if (!list.Contains(obj))
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
        if(myRigidbody)
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

    // 움직이는 플랫폼 위에서 따라가는 조건
    protected bool IsPlatformFollow()
    {
        return moveState == EMoveState.Stopping && (normalState is ENormalState.Idle or ENormalState.Attack or ENormalState.JumpAttack || IsDamaged());
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
        float targetSpeed = basicSpeed * limitMag;
        Vector2 startPos = transform.position;
        Vector2 direction = (chargeVector - startPos).normalized;
        float totalDist = chargeLength;
        if (totalDist <= 0f)
            return false;

        // 벽 거리 감지하기
        float realDist = chargeLength;
        
        Vector2 dir = Vector2.right;
        if(transform.localScale.x < 0)
            dir = Vector2.left;

        float distance = chargeLength;

        var rayVector = transform.position;
        Vector2 rayVector1 = new Vector2(rayVector.x, rayVector.y + 0.1f);
        // physicsCollider.size.y * 0.5f
        Vector2 rayVector2 = new Vector2(rayVector.x, rayVector.y + myBoxCollider.size.y * 0.5f);
        // physicsCollider.size.y
        Vector2 rayVector3 = new Vector2(rayVector.x, rayVector.y + myBoxCollider.size.y);
        
        var ray1 = Physics2D.Raycast(rayVector1, dir, distance, groundLayerMask);
        Debug.DrawRay(rayVector1, dir, ConstValues.BlueColor, 0.1f);
        var ray2 = Physics2D.Raycast(rayVector2, dir, distance, groundLayerMask);
        Debug.DrawRay(rayVector2, dir, ConstValues.BlueColor, 0.1f);
        var ray3 = Physics2D.Raycast(rayVector2, dir, distance, groundLayerMask);
        Debug.DrawRay(rayVector3, dir, ConstValues.BlueColor, 0.1f);

        if (ray1.collider != null)
            realDist = Vector2.Distance(rayVector1, ray1.point);
        if (ray2.collider != null)
            realDist = Vector2.Distance(rayVector2, ray2.point);
        if (ray3.collider != null)
            realDist = Vector2.Distance(rayVector3, ray3.point);

        // 2) 전체 돌진 시간 계산 (평균 속도 = (basic + target) / 2)
        float totalDuration = totalDist / ((basicSpeed + targetSpeed) * 0.5f);
        // 벽을 감지한 돌진 시간 계산
        float realDuration = realDist / ((basicSpeed + targetSpeed) * 0.5f);
        
        float elapsed = 0f;

        //Debug.Log($"totalDuration:{totalDuration}, realDuration{realDuration}");
        
        // 3) FixedUpdate 루프: elapsed < duration 동안 실행
        while (elapsed < realDuration)
        {
            // 시간 누적
            elapsed += Time.fixedDeltaTime;

            // 전체 시간 대비 현재 위치한 비율 (0→1)
            float normTime = Mathf.Clamp01(elapsed / realDuration);

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
        }

        // 4) 돌진 종료 후 정지
        myRigidbody.linearVelocity = Vector2.zero;
        
        // if(totalDuration - realDuration > 0) 
        //     Debug.Log($"{totalDuration - realDuration}만큼 대기시간 추가");
        
        // 추가 시간 만큼 정지
        while (elapsed < totalDuration)
        {
            // 시간 누적
            elapsed += Time.fixedDeltaTime;
            // FixedYieldDelay 대기, 취소 시 false 반환
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        
        return true;
    }

    /// <summary>
    /// 목표 좌표까지 무조건 돌진
    /// </summary>
    /// <param name="basicSpeed">시작 속도</param>
    /// <param name="limitMag">최고 속도 배율</param>
    /// <param name="accelPercent">가속 시작 비율 (0~1)</param>
    /// <param name="targetPos">돌진할 목표 좌표</param>
    /// <param name="tolerance">목표 도달 허용 오차</param>
    protected async UniTask<bool> ChargeToTarget(
        float basicSpeed,
        float limitMag,
        float accelPercent,
        Vector2 targetPos,
        float limitTime,
        float tolerance = 0.2f)
    {
        if (basicSpeed <= 0f || limitMag <= 0f)
            return false;

        Vector2 startPos = transform.position;
        Vector2 dir = (targetPos - startPos).normalized;
        if (dir.sqrMagnitude < 0.0001f)
            return false;

        float realDashSpeed = basicSpeed;
        float targetSpeed   = basicSpeed * limitMag;
        float elapsed       = 0f;
        float totalDist     = Vector2.Distance(startPos, targetPos);
        float time = 0f;

        if (totalDist <= 0f)
            return false;

        float duration = totalDist / ((basicSpeed + targetSpeed) * 0.5f);

        // 루프
        while (true)
        {
            if (stateCancellation.IsCancellationRequested)
                return false;

            elapsed += Time.fixedDeltaTime;
            time += Time.fixedDeltaTime;
            
            // 가속 처리
            float normTime = Mathf.Clamp01(elapsed / duration);
            if (normTime > accelPercent)
            {
                float t = (normTime - accelPercent) / (1f - accelPercent);
                realDashSpeed = Mathf.Lerp(basicSpeed, targetSpeed, t);
            }
            else
            {
                realDashSpeed = basicSpeed;
            }

            // 속도 적용
            myRigidbody.linearVelocity = dir * realDashSpeed;

            // 목표 도달 체크
            float remain = Vector2.Distance(transform.position, targetPos);
            if (remain <= tolerance)
                break;

            if (time >= limitTime)
                break;

            // 프레임 대기
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }

        // 종료 처리
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

    protected virtual void Bound(float xVelocity, float yVelocity)
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
                // if (gameObject.activeSelf)
                //     BlinkDelete();
                return;
            }

            if (await NormalDelay(ConstValues.DownSecond, stateCancellation).SuppressCancellationThrow())
                return;

            StateRecovery();
        }
    }

    public void AddBuff(EBuffType buffType, float buffTime)
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
                    // 슈퍼 아머만 깨짐
                    if (originStat.bodyType == EBodyType.SuperArmor)
                        basicStat.bodyType = EBodyType.Normal;
                    break;
                case EBuffType.Stagger:
                    // 스트롱 아머만 깨짐
                    if (originStat.bodyType == EBodyType.StrongArmor)
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

        // 투명화 해제
        foreach (var spriteRenderer in mySpriteRenderers)
            spriteRenderer.color = ConstValues.WhiteColor;
    }

    public virtual async void Damaged(float damagedTime)
    {
        if (damagedTime == 0)
            return;
        
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
        if (knockBackLength == 0)
            return;
        
        // 넉백 중 다운 상태라면 무시
        if (normalState == ENormalState.Down)
            return;

        // 방향 결정 (knockBackLength 양수→오른쪽, 음수→왼쪽)
        Vector2 dir = (knockBackLength >= 0) ? Vector2.right : Vector2.left;
        float duration = ConstValues.KnockBackTime;

        // 시작 속도: 거리 / 시간
        float speed = Mathf.Abs(knockBackLength) / duration;
        Vector2 constantVelocity = dir * speed;

        // 넉백 시작—바로 초기 속도 설정
        myRigidbody.linearVelocity = constantVelocity;

        anotherCancellation = new CancellationTokenSource();
        int steps = Mathf.CeilToInt(duration / Time.fixedDeltaTime);
        for (int i = 0; i < steps; i++)
        {
            // FixedUpdate 타이밍 대기 및 취소 체크
            if (await FixedYieldDelay(anotherCancellation).SuppressCancellationThrow())
                return;
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
        SoundManager.Instance.PlaySound(soundId, false, volumeScale);
    }

    // 정지
    public void Stop()
    {
        if (normalState == ENormalState.Move)
        {
            myAnimator.ResetTrigger(ConstValues.Move);
            StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        }

        if (moveState == EMoveState.Moving)
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
    
    public async void ForceIdle()
    {
        stateCancellation = new CancellationTokenSource();
        if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
            return;
        
        MoveStateSetting(EMoveState.Stopping);
        CancelMotion();
        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
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

    protected virtual void LandingAction()
    {
        // 점프 착지
        if (normalState is ENormalState.Jump)
        {
            // myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            // myRigidbody.linearVelocity = Vector2.zero;
            StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        }
    }

    // 물리 처리(발 콜라이더의 충돌만 감지)
    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if ((col.CompareTag(ConstValues.Ground) || col.CompareTag(ConstValues.Platform)))
        {
            // 땅 감지
            if (!isOnGround && col.CompareTag(ConstValues.Ground))
                isOnGround = true;
            
            // 플랫폼 감지
            if (!isOnPlatform && col.CompareTag(ConstValues.Platform))
                isOnPlatform = true;
            
            int intVel = (int)myRigidbody.linearVelocity.y;
            if (intVel > 0)
                return;
            
            float disNormal = Mathf.Abs(footTrigger.Distance(col).normal.y);
            if (disNormal < 0.5f)
                return;
            
            var movingPlatform = col.GetComponent<MovingPlatform>();
            if (movingPlatform != null)
            {
                if(movingPlatform.Velocity != Vector2.zero)
                    currentPlatform = movingPlatform;
            }

            // 랜딩상태
            if (landingState == ELandingState.Air && normalState != ENormalState.Dash)
            {
                LandingStateSetting(ELandingState.Ground);
                jumpAttackCount = 0;
            }
            // 에어본 처리
            if (normalState == ENormalState.Airborne)
            {
                myRigidbody.bodyType = RigidbodyType2D.Dynamic;
                myRigidbody.linearVelocity = Vector2.zero;
                DownAndStand();
            }
            LandingAction();
        }
    }
    protected virtual void OnTriggerStay2D(Collider2D col)
    {
        if (!isCeilingHang && (col.CompareTag(ConstValues.Ground) || col.CompareTag(ConstValues.Platform)) && myRigidbody.linearVelocityY is >= -0.01f and <= 0.01f)
        {
            // 랜딩상태
            Vector2 contactPoint = col.ClosestPoint(transform.position);
            if (landingState == ELandingState.Air && normalState != ENormalState.Dash && transform.position.y > contactPoint.y)
            {
                LandingStateSetting(ELandingState.Ground);
                jumpAttackCount = 0;
                Debug.Log($"Landing {footTrigger.Distance(col).normal.y}");
            }
            
            // 점프 착지
            if (normalState is ENormalState.Jump)
            {
                myRigidbody.bodyType = RigidbodyType2D.Dynamic;
                myRigidbody.linearVelocity = Vector2.zero;
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
            }
                
            // 에어본 처리
            if (normalState == ENormalState.Airborne)
            {
                myRigidbody.bodyType = RigidbodyType2D.Dynamic;
                myRigidbody.linearVelocity = Vector2.zero;
                DownAndStand();
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D col)
    {
        var movingPlatform = col.GetComponent<MovingPlatform>();
        if (movingPlatform != null && currentPlatform == movingPlatform)
            currentPlatform = null;
    }
}