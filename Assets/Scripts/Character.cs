using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[Serializable]
public class Buff
{
    public string buffId;
    public EBuffType buffType;
    public int buffValue;
    public float buffTime;
    public float currentTime;
    public int buffCount;
    public int currentCount;

    public float tickInterval; // 틱 간격
    public float nextTickTime; // 틱 대기시간
    
    public Action endAction;
    public Action tickAction; // 틱 당 보여주는 액션
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
    Frozen
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
    UnChange,
    Counter,
}

// 버프 타입
public enum EBuffType
{
    Stun,
    Stagger,
    ArmorBreak,
    Frozen,
    Shock,
    Burn,
    
    PowerUpPercent,
    ElementalIce,
    ElementalLightning,
    ElementalFire,
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

[Serializable]
public class EquipStat
{
    public int hp;
    public int power;
    public int defence;
    public float moveSpeed;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
}

[Serializable]
public class BuffStat
{
    public int hp;
    public int power;
    public int defence;
    public float moveSpeed;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
}

[Serializable]
public class PlatformObject
{
    public Collider2D collider;
    public float height;
}

public abstract class Character : InteractionController
{
    [SerializeField] protected BasicStat originStat; // 원본 스텟
    [SerializeField] protected BasicStat basicStat;  // 내 스텟(변동되어야 함)
    [SerializeField] protected EquipStat equipStat;  // 유물로 얻는 추가 스텟
    [SerializeField] protected BuffStat buffStat;    // 버프로 얻는 추가 스텟

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
    protected CancellationTokenSource jumpCancellation;
    protected CancellationTokenSource anotherCancellation; // 우선 넉백에만사용되고 있음
    protected CancellationTokenSource delayCancellation;
    
    //[SerializeField] protected Collider2D footTrigger;
    [SerializeField] protected PlatformObject lastStandPlatform;
    [SerializeField] protected List<PlatformObject> ignorePlatformList = new List<PlatformObject>();
    [SerializeField] protected MovingPlatform currentMovingPlatform;

    [SerializeField] protected List<GameObject> attackObject = new List<GameObject>();
    [SerializeField] protected List<GameObject> controlAttackObject = new List<GameObject>(); // 직접 관리하는 '공격 오브젝트'
    [SerializeField] protected List<GameObject> normalObject = new List<GameObject>(); // 직접 관리하는 '일반 오브젝트'
    [SerializeField] protected List<GameObject> buffObject = new List<GameObject>();   // 직접 관리하는 '버프 오브젝트'

    [SerializeField] protected ENormalState normalState;
    [SerializeField] protected EMoveState moveState;
    [SerializeField] protected ELandingState landingState;
    [SerializeField] protected List<Buff> buffList = new List<Buff>();

    [SerializeField] protected Transform diePos;
    [SerializeField] protected Transform buffEffectPos;
    [SerializeField] protected Transform centerPos;
    [SerializeField] protected Transform fontPos;
    [SerializeField] protected Transform physicCenterPos;

    [SerializeField] protected Vector2 standHitBoxSize;
    [SerializeField] protected Vector2 downHitBoxSize;
    [SerializeField] protected Vector2 standOffset;
    [SerializeField] protected Vector2 downOffset;

    protected Vector2 chargeVector;
    protected int landingAttackCount;
    protected int jumpAttackCount;
    protected bool isDie;
    protected float myGravity;
    protected bool downJumping;
    protected bool isOnPlatform;   // 아랫점프, 이어지는 땅 처리에만 사용

    protected int airborneCount; // 에어본 카운트
    private int platformLayerMask;
    protected int groundLayerMask;
    protected int groundAndPlatformLayerMask;
    protected int monsterWalkLayerMask;
    protected int agroLayerMask;
    protected int bossLayerMask;
    protected int wallBodyLayerMask;

    protected bool isCeilingHang;
    protected bool immortal;
    protected bool immuneStagger;
    [SerializeField] protected bool isCounterAttack;

    // 프로퍼티
    public BasicStat OriginStat => originStat;
    public BasicStat BasicStat => basicStat;
    public Rigidbody2D MyRigidbody => myRigidbody;
    public BoxCollider2D MyBoxCollider => myBoxCollider;
    public Transform CenterPos => centerPos;
    public Transform FontPos => fontPos;
    public List<Buff> BuffList => buffList;

    [Header("Box Sizes")]
    [SerializeField] private Vector2 horizontalBoxSize; // 좌우 (세로로 긴 박스)
    [SerializeField] private Vector2 verticalBoxSize;   // 상하 (가로로 긴 박스)

    [Header("Offsets")]
    [SerializeField] private float horizontalOffset; // 중심에서 좌우로 얼마나 떨어질지
    [SerializeField] private float verticalOffset;   // 중심에서 위아래로 얼마나 떨어질지
    [SerializeField] protected float footOffset; // 중심에서 좌우로 얼마나 떨어질지

    // 왼쪽 벽 체크
    private RaycastHit2D hitLeft;
    // 오른쪽 벽 체크
    private RaycastHit2D hitRight;
    // 천장 체크
    private RaycastHit2D hitUp;
    // 바닥 체크
    private RaycastHit2D downLeftHit;
    private RaycastHit2D downRightHit;
    // 에어본 체크
    private RaycastHit2D airborneLeftHit;
    private RaycastHit2D airborneRightHit;
    // 위쪽 플랫폼 체크
    [SerializeField] private Collider2D[] upPlatformHit;
    private Vector2 upPlatformBoxSize;
    private Vector2 upPlatformBoxPos;
    
    // 바닥 레이 위치
    private Vector2 leftGroundRayPos;
    private Vector2 rightGroundRayPos;
    
    // 에어본 레이 위치
    private Vector2 leftAirborneRayPos;
    private Vector2 rightAirborneRayPos;
    
    private float groundRayDistance;
    private float airborneRayDistance;
    private float addGroundDistance;
    private float addAirborneDistance;
    
    public bool isWallLeft;
    public bool isWallRight;
    public bool isCeilingHit;
    public bool isGrounded;
    public bool isAirborneGrounded;

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

    public bool IsCounterAttack
    {
        get => isCounterAttack;
        set => isCounterAttack = value;
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
        
        var physicsColSize = physicsCollider.size;
        
        horizontalBoxSize = new Vector2(0.1f, physicsColSize.y); // 좌우 (세로로 긴 박스)  * 0.8f
        verticalBoxSize = new Vector2(physicsColSize.x, 0.1f); // 상하 (가로로 긴 박스) * 0.9f
        horizontalOffset = physicsColSize.x * 0.5f; // 중심에서 좌우로 얼마나 떨어질지
        verticalOffset = physicsColSize.y * 0.5f; // 중심에서 위아래로 얼마나 떨어질지

        footOffset = physicsColSize.x * 0.5f;
        addGroundDistance = 0.1f;
        addAirborneDistance = 0.05f;
        groundRayDistance = physicsColSize.y * 0.5f + addGroundDistance;
        airborneRayDistance = physicsColSize.y * 0.5f + addAirborneDistance; // 수정을 해야하나?
        
        upPlatformBoxSize = new Vector2(10, 10);
    }

    protected virtual void OnEnable()
    {
        isDie = false;
        StandHitBox();
        SetIgnorePlatform();
    }

    protected virtual void Update()
    {
        UpdateAirborneDown();
    }

    protected virtual void FixedUpdate()
    {
        UpdateVelocity();
        CheckCollisions();
        CeilingCheck();
        AddIgnorePlatform();
        RemoveIgnorePlatform();
        UpdateMovingPlatform();
    }

    protected virtual void CheckCollisions()
    {
        WallCheck();
        UpPlatformCheck();
        GroundCheck();
        AirborneCheck();
    }

    // 벽 체크
    private void WallCheck()
    {
        // 왼쪽 벽 체크
        hitLeft = Physics2D.BoxCast((Vector2)physicCenterPos.position + Vector2.left * horizontalOffset, horizontalBoxSize, 0f, Vector2.left, 0, groundLayerMask);

        // 오른쪽 벽 체크
        hitRight = Physics2D.BoxCast((Vector2)physicCenterPos.position + Vector2.right * horizontalOffset, horizontalBoxSize, 0f, Vector2.right, 0, groundLayerMask);

        isWallLeft = hitLeft.collider != null;
        isWallRight = hitRight.collider != null;
    }
    
    // 천장 체크
    protected virtual async void CeilingCheck()
    {
        // 천장 체크 (위)
        hitUp = Physics2D.BoxCast((Vector2)physicCenterPos.position + Vector2.up * verticalOffset, verticalBoxSize, 0f, Vector2.up, 0, groundLayerMask);
        isCeilingHit = hitUp.collider != null;

        if (landingState == ELandingState.Air)
        {
            if (myRigidbody.linearVelocityY > 0.01f)
            {
                if (!isCeilingHang && isCeilingHit)
                {
                    jumpCancellation?.Cancel();
                    jumpCancellation = new CancellationTokenSource();

                    myRigidbody.linearVelocityY = 0;
                    GravityChange(0);
                    isCeilingHang = true;
                    float timer = 0.0f;
                    float ceilingTime = 0.08f;
                    while (timer < ceilingTime)
                    {
                        timer += Time.fixedDeltaTime;
                        if (await FixedYieldDelay(jumpCancellation).SuppressCancellationThrow())
                        {
                            Debug.Log("천장 딜레이 캔슬");
                            isCeilingHang = false;
                            return;
                        }
                    }
                    GravityChange(myGravity);
                    isCeilingHang = false;
                }
            }
        }
    }
    
    // 위쪽 플랫폼 체크
    private void UpPlatformCheck()
    {
        // physicCenterPos.position.y + upPlatformBoxSize.y * 0.5f
        upPlatformBoxPos = new Vector2(transform.position.x, transform.position.y + upPlatformBoxSize.y * 0.5f + 0.1f);
        upPlatformHit = Physics2D.OverlapBoxAll(upPlatformBoxPos, upPlatformBoxSize, 0, platformLayerMask);
        foreach (var upPlatform in upPlatformHit)
            IgnorePlatformCheck(upPlatform);
    }

    // 바닥 및 착지 체크
    protected virtual void GroundCheck()
    {
        leftGroundRayPos = (Vector2)physicCenterPos.position + new Vector2(-footOffset, 0);
        rightGroundRayPos = (Vector2)physicCenterPos.position + new Vector2(footOffset, 0);
        downLeftHit = Physics2D.Raycast(leftGroundRayPos, Vector2.down, groundRayDistance, groundAndPlatformLayerMask);
        downRightHit = Physics2D.Raycast(rightGroundRayPos, Vector2.down, groundRayDistance, groundAndPlatformLayerMask);

        isGrounded = downLeftHit || downRightHit;
        // 그라운드 오브젝트
        if(downLeftHit.collider != null)
            groundObject = downLeftHit.collider.gameObject;
        if(downRightHit.collider != null)
            groundObject = downRightHit.collider.gameObject;
        
        // 낙하 도중 플랫폼 중간라인에 걸칠 때 예외처리
        // if (myRigidbody.linearVelocityY < 0.01f && downLeftHit.collider != null && downLeftHit.collider.CompareTag(ConstValues.Platform))
        // {
        //     if (!ignorePlatformList.Exists(x => x.collider == downLeftHit.collider))
        //     {
        //         IgnorePlatformCheck(downLeftHit.collider);
        //     }
        // }
        // if (myRigidbody.linearVelocityY < 0.01f && downRightHit.collider != null && downRightHit.collider.CompareTag(ConstValues.Platform))
        // {
        //     if (!ignorePlatformList.Exists(x => x.collider == downRightHit.collider))
        //     {
        //         IgnorePlatformCheck(downRightHit.collider);
        //     }
        // }

        // 무시된 플랫폼 감지
        if ((downLeftHit.collider != null && ignorePlatformList.Exists(x => x.collider == downLeftHit.collider)) || 
            (downRightHit.collider != null && ignorePlatformList.Exists(x => x.collider == downRightHit.collider)))
            isGrounded = false;
        
        if (isGrounded)
            SetGroundState();
        else
            GetOffGroundState();
    }

    // 에어본 도중 바닥체크
    private void AirborneCheck()
    {
        leftAirborneRayPos = (Vector2)physicCenterPos.position + new Vector2(-footOffset, 0); 
        rightAirborneRayPos = (Vector2)physicCenterPos.position + new Vector2(footOffset, 0);
        airborneLeftHit = Physics2D.Raycast(leftAirborneRayPos, Vector2.down, airborneRayDistance, groundAndPlatformLayerMask);
        airborneRightHit = Physics2D.Raycast(rightAirborneRayPos, Vector2.down, airborneRayDistance, groundAndPlatformLayerMask);

        isAirborneGrounded = airborneLeftHit || airborneRightHit;
        
        // 에어본 도중 플랫폼 중간라인에 걸칠 때 예외처리
        // if (myRigidbody.linearVelocityY < 0.01f && airborneLeftHit.collider != null && airborneLeftHit.collider.CompareTag(ConstValues.Platform))
        // {
        //     if (!ignorePlatformList.Exists(x => x.collider == airborneLeftHit.collider))
        //     {
        //         IgnorePlatformCheck(airborneLeftHit.collider);
        //     }
        // }
        // if (myRigidbody.linearVelocityY < 0.01f && airborneRightHit.collider != null && airborneRightHit.collider.CompareTag(ConstValues.Platform))
        // {
        //     if (!ignorePlatformList.Exists(x => x.collider == airborneRightHit.collider))
        //     {
        //         IgnorePlatformCheck(airborneRightHit.collider);
        //     }
        // }
        
        // 무시된 플랫폼 감지
        if ((airborneLeftHit.collider != null && ignorePlatformList.Exists(x => x.collider == airborneLeftHit.collider)) || 
            (airborneRightHit.collider != null && ignorePlatformList.Exists(x => x.collider == airborneRightHit.collider)))
            isAirborneGrounded = false;
        
        if (isAirborneGrounded)
            AirborneState();
    }

    // 정상적인 상태에서 땅으로 내려왔을 때 상태변환
    private void SetGroundState()
    {
        bool leftPlatform = downLeftHit && downLeftHit.collider.CompareTag(ConstValues.Platform);
        bool rightPlatform = downRightHit && downRightHit.collider.CompareTag(ConstValues.Platform);
        bool leftGround = downLeftHit && downLeftHit.collider.CompareTag(ConstValues.Ground);
        bool rightGround = downRightHit && downRightHit.collider.CompareTag(ConstValues.Ground);

        // 올라서있는 플랫폼 감지
        if ((leftPlatform || rightPlatform) && !leftGround && !rightGround)
        {
            isOnPlatform = true;
            if (leftPlatform)
            {
                lastStandPlatform = new PlatformObject()
                {
                    collider = downLeftHit.collider,
                    height = ColliderHeight(downLeftHit.collider)
                };
                if (downLeftHit.collider.GetComponent<MovingPlatform>())
                {
                    currentMovingPlatform = downLeftHit.collider.GetComponent<MovingPlatform>();
                    //lastStandPlatform = currentMovingPlatform.PlatformObject;
                    //currentMovingPlatform.PlatformObject = lastStandPlatform;
                }
            }
            if (rightPlatform)
            {
                lastStandPlatform = new PlatformObject()
                {
                    collider = downRightHit.collider,
                    height = ColliderHeight(downRightHit.collider)
                };
                if (downRightHit.collider.GetComponent<MovingPlatform>())
                {
                    currentMovingPlatform = downRightHit.collider.GetComponent<MovingPlatform>();
                    //lastStandPlatform = currentMovingPlatform.PlatformObject;
                    //currentMovingPlatform.PlatformObject = lastStandPlatform;
                }
            }
        }
        else
        {
            isOnPlatform = false;
        }

        if (myRigidbody.linearVelocityY < 0.01f)
        {
            // 랜딩상태
            if (landingState == ELandingState.Air && normalState != ENormalState.Dash)
            {
                LandingStateSetting(ELandingState.Ground);
            }
            jumpAttackCount = 0;
            // 점프 착지
            LandingAction();
        }
    }

    // 땅을 벗어날때의 상태변화
    protected virtual void GetOffGroundState()
    {
        LandingStateSetting(ELandingState.Air);

        if (normalState is ENormalState.Idle or ENormalState.Move)
        {
            if (lastStandPlatform.collider != null)
            {
                IgnorePlatformCheck(lastStandPlatform, true);
                //downJumping = true;
            }
            
            StateSetting(ENormalState.Jump, ConstValues.JumpDown, ConstValues.JumpDown);
        }
        isOnPlatform = false;
        currentMovingPlatform = null;
    }

    // 에어본 당하여 땅으로 떨어질때의 상태변화
    private void AirborneState()
    {
        if (normalState == ENormalState.Airborne)
        {
            if (myRigidbody.linearVelocityY < 0.01f)
            {
                DownAndStand();
            }
        }
    }
    
    private void UpdateMovingPlatform()
    {
        // 입력이 없어서 Move가 안 불릴 때도 플랫폼 속도는 유지
        if (currentMovingPlatform != null)
        {
            if (IsPlatformFollow())
            {
                // 여기에 플랫폼별로 상하, 좌우인지 구별
                var v = myRigidbody.linearVelocity;
                v.x = currentMovingPlatform.Velocity.x; // 기본은 플랫폼 속도
                v.y = currentMovingPlatform.Velocity.y;
                myRigidbody.linearVelocity = v;
            }
            else if (normalState == ENormalState.Jump)
            {
                if (MathF.Abs(myRigidbody.linearVelocityY - currentMovingPlatform.Velocity.y) < 0.5f)
                {
                    LandingStateSetting(ELandingState.Ground);
                    jumpAttackCount = 0;
                    LandingAction();
                }
            }
        }
    }

    public void ClearLastPlatform()
    {
        lastStandPlatform.collider = null;
        lastStandPlatform.height = 0;
    }

    // 디버그 시각화 (Scene 뷰에서 확인 가능)
    protected virtual void OnDrawGizmos()
    {
        // 왼쪽 벽 박스
        Gizmos.color = isWallLeft ? ConstValues.BlueColor : ConstValues.RedColor;
        DrawBox((Vector2)physicCenterPos.position + Vector2.left * horizontalOffset, horizontalBoxSize);

        // 오른쪽 벽 박스
        Gizmos.color = isWallRight ? ConstValues.BlueColor : ConstValues.RedColor;
        DrawBox((Vector2)physicCenterPos.position + Vector2.right * horizontalOffset, horizontalBoxSize);

        // 천장 박스
        Gizmos.color = isCeilingHit ? ConstValues.BlueColor : ConstValues.RedColor;
        DrawBox((Vector2)physicCenterPos.position + Vector2.up * verticalOffset, verticalBoxSize);
        
        Gizmos.color = Color.cyan; // 하늘색으로 표시
        // 현재 위치에 설정한 size만큼의 와이어 박스를 그림
        Gizmos.DrawWireCube(upPlatformBoxPos, upPlatformBoxSize);

        if (myRigidbody)
        {
            if (normalState is ENormalState.Airborne or ENormalState.Down)
            {
                // 왼쪽 아래 레이 디버그
                Gizmos.color = Physics2D.Raycast(leftGroundRayPos, Vector2.down, airborneRayDistance, groundAndPlatformLayerMask) ? ConstValues.BlueColor : ConstValues.RedColor;
                Gizmos.DrawLine(leftGroundRayPos, leftGroundRayPos + Vector2.down * airborneRayDistance);

                // 오른쪽 아래 레이 디버그
                Gizmos.color = Physics2D.Raycast(rightGroundRayPos, Vector2.down, airborneRayDistance, groundAndPlatformLayerMask) ? ConstValues.BlueColor : ConstValues.RedColor;
                Gizmos.DrawLine(rightGroundRayPos, rightGroundRayPos + Vector2.down * airborneRayDistance);
            }
            else
            {
                // 왼쪽 아래 레이 디버그
                Gizmos.color = Physics2D.Raycast(leftGroundRayPos, Vector2.down, groundRayDistance, groundAndPlatformLayerMask) ? ConstValues.BlueColor : ConstValues.RedColor;
                Gizmos.DrawLine(leftGroundRayPos, leftGroundRayPos + Vector2.down * groundRayDistance);

                // 오른쪽 아래 레이 디버그
                Gizmos.color = Physics2D.Raycast(rightGroundRayPos, Vector2.down, groundRayDistance, groundAndPlatformLayerMask) ? ConstValues.BlueColor : ConstValues.RedColor;
                Gizmos.DrawLine(rightGroundRayPos, rightGroundRayPos + Vector2.down * groundRayDistance);
            }
        }
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
        
        LayerMaskSetting();
        ScaleSetting();
        ColSizeSetting();
    }

    protected virtual void LayerMaskSetting()
    {
        platformLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Platform);
        groundLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Ground);
        groundAndPlatformLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform));
        monsterWalkLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform)) | (1 << LayerMask.NameToLayer(ConstValues.Trap));
        agroLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform)) | (1 << LayerMask.NameToLayer(ConstValues.Player));
        bossLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Player));
        wallBodyLayerMask = 1 << LayerMask.NameToLayer(ConstValues.WallBody);
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
    
    // 추가스텟, 장비 탈착, 버프 시작 또는 끝나는 시점에 사용
    public void InitBonusStat()
    {
        equipStat.hp = 0;
        equipStat.power = 0;
        equipStat.defence = 0;
        equipStat.moveSpeed = 0;
        equipStat.attackSpeed = 0; // 1이 기본값. %단위로 적을것
        equipStat.criticalChance = 0;
        equipStat.criticalDamage = 0;
        
        foreach (var relic in GameManager.Instance.GetPlayerRelicList())
        {
            if(string.IsNullOrWhiteSpace(relic))
                continue;
            
            var relicInfo = GameManager.Instance.relicList.Find(x => x.id == relic);

            for (var i = 0; i < relicInfo.statList.Count; i++)
            {
                switch (relicInfo.statList[i])
                {
                    case eEquipStat.Power:
                        equipStat.power += relicInfo.valueList[i];
                        break;

                    case eEquipStat.PowerPercent:
                        equipStat.power += Mathf.RoundToInt(originStat.power * (relicInfo.valueList[i] * 0.01f));
                        break;
                }
            }
        }

        buffStat.hp = 0;
        buffStat.power = 0;
        buffStat.defence = 0;
        buffStat.moveSpeed = 0;
        buffStat.attackSpeed = 0; // 1이 기본값. %단위로 적을것
        buffStat.criticalChance = 0;
        buffStat.criticalDamage = 0;

        foreach (var buff in buffList)
        {
            switch (buff.buffType)
            {
                case EBuffType.PowerUpPercent:
                    buffStat.power += Mathf.RoundToInt(originStat.power * (buff.buffValue * 0.01f));
                    break;
            }
        }
        
        // 항상 hp <= maxHp
        basicStat.maxHp = originStat.hp + equipStat.hp + buffStat.hp;
        var finalHp = basicStat.hp + equipStat.hp + buffStat.hp;
        basicStat.hp = finalHp;
        
        basicStat.power = originStat.power + equipStat.power + buffStat.power;
        basicStat.defence = originStat.defence + equipStat.defence + buffStat.defence;
        
        // 이동속도
        var equipMoveSpeed = originStat.moveSpeed + equipStat.moveSpeed;
        var finalMoveSpeed = equipMoveSpeed + (equipMoveSpeed * (buffStat.moveSpeed * 0.01f));
        var finalMoveAnimSpeed = finalMoveSpeed / originStat.moveSpeed;
        basicStat.moveSpeed = finalMoveSpeed;
        if(myAnimator)
            myAnimator.SetFloat(ConstValues.MoveSpeed, finalMoveAnimSpeed);
        
        // 공격속도
        SetAttackSpeed(1);
        
        basicStat.criticalChance = originStat.criticalChance + equipStat.criticalChance + buffStat.criticalChance;
        basicStat.criticalDamage = originStat.criticalDamage + equipStat.criticalChance + buffStat.criticalDamage;
    }

    protected void SetAttackSpeed(float skillSpeed)
    {
        var equipAttackSpeed = originStat.attackSpeed + equipStat.attackSpeed;
        var finalAttackSpeed = (equipAttackSpeed + equipAttackSpeed * (buffStat.attackSpeed * 0.01f)) * skillSpeed;
        var attackAnimSpeed = finalAttackSpeed / originStat.attackSpeed;
        basicStat.attackSpeed = finalAttackSpeed;
        if(myAnimator)
            myAnimator.SetFloat(ConstValues.AttackSpeed, attackAnimSpeed);
    }
    protected void ResetSkillSpeed()
    {
        SetAttackSpeed(1);
    }

    protected void ResetBodyType()
    {
        if(basicStat.bodyType != originStat.bodyType)
            BodyTypeSetting(originStat.bodyType);
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

    private void UpdateAirborneDown()
    {
        if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.Airborne) && myRigidbody.linearVelocity.y < 0)
        {
            SetTriggerAnimator(ConstValues.AirborneDown);
        }
    }

    protected void UpdateBuff()
    {
        int expiredCount = 0;
        foreach (var deBuff in buffList)
        {
            if (deBuff.buffTime > 0)
            {
                if (deBuff.currentTime > 0)
                {
                    deBuff.currentTime -= Time.deltaTime;

                    // --- 도트 데미지(Tick) 처리 추가 ---
                    if (deBuff.tickInterval > 0) 
                    {
                        deBuff.nextTickTime -= Time.deltaTime;
                        if (deBuff.nextTickTime <= 0)
                        {
                            deBuff.tickAction?.Invoke(); // 데미지 실행
                            deBuff.nextTickTime = deBuff.tickInterval; // 시간 초기화
                        }
                    }
                }
                else
                {
                    deBuff.currentTime = 0;
                    expiredCount += 1;
                }
            }
            else
            {
                if (deBuff.currentCount == 0)
                    expiredCount += 1;
            }
        }

        // 버프 만료
        if (expiredCount == 0)
            return;

        var expiredDeBuffList = buffList.FindAll(x => (x.buffTime > 0 && x.currentTime <= 0) || (x.buffTime == 0 && x.currentCount == 0));
        foreach (var expiredDeBuff in expiredDeBuffList)
        {
            buffList.Remove(expiredDeBuff);
            var removeEffect = buffObject.Find(x => x.name == $"{expiredDeBuff.buffType}{ConstValues.Effect}(Clone)");
            if (removeEffect != null)
                RemoveObjectList(buffObject, removeEffect);

            // 스턴, 무력화, 빙결 상태 회복
            if (expiredDeBuff.buffId is ConstValues.Stun or ConstValues.Stagger or ConstValues.Frozen)
            {
                StateCheck();
                if (normalState is ENormalState.Stun or ENormalState.Stagger or ENormalState.Frozen)
                {
                    StateRecovery();
                }
            }
            // 버프가 끝날때 존재하는 액션이 있다면 실행
            expiredDeBuff.endAction?.Invoke();
            InitBonusStat();
        }
    }

    protected void BuffCountDown(string buffId)
    {
        var targetBuff = buffList.Find(x => x.buffId == buffId);
        if (targetBuff != null)
            targetBuff.currentCount -= 1;
    }

    public void AllBuffCancel()
    {
        buffList.Clear();
        foreach (var buff in buffObject)
            buff.SetActive(false);
        
        buffObject.Clear();
        InitBonusStat();
    }

    // 최대 중력가속도 조정
    private void UpdateVelocity()
    {
        if (myRigidbody.bodyType == RigidbodyType2D.Dynamic && myRigidbody.linearVelocity.y < -30)
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -30);
    }
    
    // 플랫폼 무시
    private void SetIgnorePlatform()
    {
        foreach (var ignorePlatform in ignorePlatformList)
            Physics2D.IgnoreCollision(physicsCollider, ignorePlatform.collider, true);
    }
    
    private void IgnorePlatformCheck(Collider2D col, bool force = false)
    {
        var height = ColliderHeight(col);

        if (transform.position.y < height || force)
        {
            if (!ignorePlatformList.Exists(x => x.collider == col))
            {
                var movingPlatform = col.GetComponent<MovingPlatform>();
                if (movingPlatform)
                {
                    ignorePlatformList.Add(movingPlatform.PlatformObject);
                    Physics2D.IgnoreCollision(physicsCollider, col, true);
                }
                else
                {
                    if (col == lastStandPlatform.collider)
                    {
                        ignorePlatformList.Add(lastStandPlatform);
                        Physics2D.IgnoreCollision(physicsCollider, col, true);
                    }
                    else
                    {
                        var platformObject = new PlatformObject()
                        {
                            collider = col,
                            height = height,
                        };
                        ignorePlatformList.Add(platformObject);
                        Physics2D.IgnoreCollision(physicsCollider, col, true);
                    }
                }
            }
        }
    }
    // 플랫폼 무시 오버로딩
    protected void IgnorePlatformCheck(PlatformObject platformObject, bool forceIgnore = false)
    {
        var height = ColliderHeight(platformObject.collider);
        if (transform.position.y < height || forceIgnore)
        {
            Physics2D.IgnoreCollision(physicsCollider, platformObject.collider, true);
            if (!ignorePlatformList.Exists(x => x.collider == platformObject.collider))
            {
                ignorePlatformList.Add(platformObject);
            }
        }
    }
    
    // 플랫폼의 무시 취소 높이
    private float ColliderHeight(Collider2D col)
    {
        float height = 0;
        if (col.GetComponent<BoxCollider2D>())
        {
            height = col.transform.position.y + col.GetComponent<BoxCollider2D>().size.y * 0.5f + col.offset.y - 0.2f;
        }

        if (col.GetComponent<TilemapCollider2D>())
        {
            Tilemap tilemap = col.GetComponent<Tilemap>();
    
            BoundsInt bounds = tilemap.cellBounds;
            float halfHeight = tilemap.layoutGrid.cellSize.y / 2f;
            
            // 2. 해당 영역 안의 모든 칸(Cell)을 순회합니다.
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                // 3. 해당 칸에 타일이 존재하는지 확인
                if (!tilemap.HasTile(pos))
                    continue;
                
                // 4. 타일의 중앙 좌표를 구하고 윗면 좌표를 계산
                Vector3 center = tilemap.GetCellCenterWorld(pos);
                Vector3 topPos = new Vector3(center.x, center.y + halfHeight, center.z);
                height = topPos.y - 0.2f;
                break;
            }
        }

        return (float)Math.Round(height, 2);;
    }

    private void AddIgnorePlatform()
    {
        if (lastStandPlatform.collider == null || !lastStandPlatform.collider.gameObject.activeInHierarchy)
            return;
        
        if (transform.position.y < lastStandPlatform.height)
            IgnorePlatformCheck(lastStandPlatform);
    }
    private void RemoveIgnorePlatform()
    {
        if (ignorePlatformList.Count == 0 || downJumping || normalState == ENormalState.Dash)
            return;
        
        PlatformObject removedPlatformObject = null;
        foreach (var ignorePlatform in ignorePlatformList)
        {
            if (transform.position.y > ignorePlatform.height)
            {
                Physics2D.IgnoreCollision(physicsCollider, ignorePlatform.collider, false);
                removedPlatformObject = ignorePlatform;
            }
        }

        if (removedPlatformObject != null)
        {
            ignorePlatformList.Remove(removedPlatformObject);
        }
    }

    protected void ClearIgnorePlatform()
    {
        foreach (var ignorePlatform in ignorePlatformList)
        {
            if (ignorePlatform.collider != null)
            {
                Physics2D.IgnoreCollision(physicsCollider, ignorePlatform.collider, false);
            }
        }
        
        ignorePlatformList.Clear();
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

    protected virtual void SetTriggerAnimator(string parameter)
    {
        myAnimator.SetTrigger(parameter);
    }

    protected virtual void ResetTriggerAnimator(string parameter)
    {
        myAnimator.ResetTrigger(parameter);
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
    
    public virtual void TakeStagger(int stagger)
    {
        if (immuneStagger)
            return;

        basicStat.stagger -= stagger;
        if (basicStat.stagger <= 0)
            basicStat.stagger = 0;
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
    public void SpawnDamageFont(int damage, bool critical, bool additional, bool dot)
    {
        if (damage == 0)
            return;

        int fontSize = 55;

        Vector3 randomPos = fontPos.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0, 1.0f));
        var textFont = GameManager.Instance.SpawnToUIObjectPool(ConstValues.TextFont, randomPos).GetComponent<TextFont>();
        StringBuilder damageText = new StringBuilder();
        damageText.Append(damage);

        if (additional)
        {
            fontSize = 35;
            textFont.ColorSetting(EFontType.AdditionalDamage);
        }
        else if (dot)
        {
            fontSize = 35;
            textFont.ColorSetting(EFontType.Dot);
        }
        else
        {
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
        }
        textFont.DisplayFont(fontSize, damageText.ToString());
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
            spawnedObject.AttributeCheck();
            if (zAngle != 0)
            {
                var finalAngle = zAngle;
                if (transform.localScale.x < 0)
                    finalAngle = -zAngle;
            
                var objectAngle = spawnedObject.transform.eulerAngles;
                spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
            }

            // 몬스터 체력바는 제외된다
            if (obj.GetComponent<TotalBar>() == null && obj.GetComponent<Attack>() == null)
            {
                if (spawnedObject.GetObjectTime() == 0)
                {
                    if (isBuff)
                        AddObjectList(buffObject, obj);
                    else
                        AddObjectList(normalObject, obj);
                }
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
            if (obj.GetComponent<SpawnedObject>())
            {
                if (obj.GetComponent<SpawnedObject>().GetObjectTime() == 0)
                    AddObjectList(controlAttackObject, obj);
                else
                    AddObjectList(attackObject, obj);
            }

            var attack = obj.GetComponent<Attack>();
            if (!attack)
                attack = obj.AddComponent<Attack>();
            
            
            attack.SetupCastChar(this);
            attack.SetupData(attackData);
            attack.AttributeCheck();
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
            
            missile.SetupData(missileData, dir, SpawnAttack, SpawnObjectAction);
            missile.AttributeCheck();
            missile.SetLimit();
            missile.BossCheck(GetComponent<Monster>() && GetComponent<Monster>().IsBoss);
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
            grenade.SetupData(grenadeData, dir, SpawnAttack, SpawnObjectAction);

            if (targetVector == default)
                grenade.Throw();
            else
                grenade.TargetThrow(targetVector);
            
            grenade.BossCheck(GetComponent<Monster>() && GetComponent<Monster>().IsBoss);
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
    protected GameObject SpawnAttackObject(string id, Vector2 attackVector, int zAngle = 0, int missileDir = 0, Vector2 targetVector = default)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackVector);
        SetSpawnedObjectData(id, obj, zAngle);
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
    // 공격판정이 없는 오브젝트 소환(액션 삽입용)
    private void SpawnObjectAction(string id, Vector2 pos)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        SetSpawnedObjectData(id, obj, 0);
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
    
    protected void ShakeSpritePos(float range)
    {
        if (myAnimator)
        {
            float randX = Random.Range(-range, range);
            myAnimator.transform.localPosition = new Vector3(randX, 0, 0);
        }
    }

    protected void ResetSpritePos()
    {
        if(myAnimator)
            myAnimator.transform.localPosition = Vector3.zero;
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
        isCounterAttack = false;
        GravityChange(myGravity);
        ResetSpritePos();

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
        ClearObjectList(controlAttackObject, timer);
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
    
    protected void BodyTypeSetting(EBodyType bodyType)
    {
        basicStat.bodyType = bodyType;
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
        return normalState is ENormalState.Grabbed or ENormalState.Frozen or ENormalState.Airborne or ENormalState.Down or ENormalState.Stun or ENormalState.Damaged;
    }

    // 움직이는 플랫폼 위에서 따라가는 조건
    protected bool IsPlatformFollow()
    {
        return moveState == EMoveState.Stopping && (normalState is ENormalState.Idle or ENormalState.Attack or ENormalState.JumpAttack || IsDamaged());
    }

    // 군중제어에 걸렸는가?
    protected bool IsCc()
    {
        bool normalCondition = normalState is ENormalState.Grabbed or ENormalState.Frozen or ENormalState.Stun;
        bool buffCondition = FindBuff(ConstValues.Stun);
        return normalCondition || buffCondition;
    }

    private bool FindBuff(string buffId)
    {
        return buffList.Find(x => x.buffId == buffId) != null;
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

        var physicsColSize = physicsCollider.size;
        var boxSize1 = new Vector2(0.1f, physicsColSize.y); // 좌우 (세로로 긴 박스)  * 0.8f
        var boxSize2 = new Vector2(0.5f, physicsColSize.y * 1.1f); // 좌우 (세로로 긴 박스)  * 0.8f

        var boxVector = physicCenterPos.position;
        var boxRay1 = Physics2D.BoxCast(boxVector, boxSize1, 0f, dir, distance, groundLayerMask);
        var boxRay2 = Physics2D.BoxCastAll(boxVector, boxSize2, 0f, dir, distance, platformLayerMask);

        if (boxRay1.collider != null)
            realDist = Vector2.Distance(boxVector, boxRay1.point);

        foreach (var ray in boxRay2)
        {
            if (ray.collider != null)
            {
                IgnorePlatformCheck(ray.collider, true);
            }
        }

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

    private void ChargeTest()
    {
        Vector2 dir = Vector2.right;
        if(transform.localScale.x < 0)
            dir = Vector2.left;

        float distance = 5;

        var physicsColSize = physicsCollider.size;
        var boxSize = new Vector2(0.1f, physicsColSize.y); // 좌우 (세로로 긴 박스)  * 0.8f

        var boxVector = physicCenterPos.position;
        var boxRay = Physics2D.BoxCast(boxVector, boxSize, 0f, dir, distance, groundAndPlatformLayerMask);

        if (boxRay.collider != null)
        {
            if (boxRay.collider.CompareTag(ConstValues.Platform))
                IgnorePlatformCheck(boxRay.collider, true);
        }
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
        if (normalState is ENormalState.Grabbed or ENormalState.Frozen)
        {
            Debug.Log($"상위 판정이 존재함: {normalState}");
            return;
        }
        
        Vector2 airborneVector = new Vector2(xVelocity, yVelocity);
        if (airborneVector == Vector2.zero)
            return;
        
        CancelMotion();

        airborneCount = 1;
        LandingStateSetting(ELandingState.Air);
        MoveStateSetting(EMoveState.Stopping);
        ResetTriggerAnimator(ConstValues.Jump);

        stateCancellation = new CancellationTokenSource();
        AirborneBound(xVelocity, yVelocity);
        DownHitBox();
    }

    protected virtual void AirborneBound(float xVelocity, float yVelocity)
    {
        StateSetting(ENormalState.Airborne, ConstValues.Airborne, ConstValues.Airborne);
        LandingStateSetting(ELandingState.Air);
        // 공중몹도 떴다 떨어지기 때문에 기본 중력값으로 변환
        GravityChange(ConstValues.BasicGravity);
        myRigidbody.linearVelocity = new Vector2(xVelocity, yVelocity);
    }

    protected virtual async void DownAndStand()
    {
        StateSetting(ENormalState.Down, ConstValues.Down, ConstValues.Down);
        LandingStateSetting(ELandingState.Ground);
        MoveStateSetting(EMoveState.Stopping);
        SpawnObject(ConstValues.DownDust, transform.position);

        // 최초 공중에 떴을 때는, 땅에 닿자마자 다시 공중으로 고정높이만큼 뜬다
        if (airborneCount > 0)
        {
            airborneCount -= 1;
            if (await NormalDelay(ConstValues.ReboundSecond, stateCancellation).SuppressCancellationThrow())
                return;

            AirborneBound(0, ConstValues.ReboundForce);
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
    
    // 버프
    protected void AddBuff(string buffId, int buffValue, float buffTime, int buffCount, Action endAction = null, float tickInterval = 0, float nextTickTime = 0, Action tickAction = null)
    {
        var findDeBuff = TargetBuff(buffId);
        // 해당 버프가 적용되어있지 않음
        if (findDeBuff == null)
        {
            var buffData = TableManager.Instance.buffTable.Buff.Find(x => x.id == buffId);
            if (buffData != null)
            {
                var buffType = (EBuffType)Enum.Parse(typeof(EBuffType), buffData.buffType);
                var newDeBuff = new Buff()
                {
                    buffId = buffId,
                    buffType = buffType,
                    buffValue = buffValue,
                    buffTime = buffTime,
                    currentTime = buffTime,
                    buffCount = buffCount,
                    currentCount = buffCount,
                    endAction = endAction,
                    
                    tickInterval = tickInterval,
                    nextTickTime = nextTickTime,
                    tickAction = tickAction,
                };
                buffList.Add(newDeBuff);

                Transform posTransform = transform;
                switch (buffData.buffPos)
                {
                    case ConstValues.Center:
                        posTransform = centerPos;
                        break;
                }
                SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", posTransform, 0, true);
            }
            else
            {
                Debug.Log("버프가 테이블 시트에 등록되지 않았음");
            }
        }
        // 해당 디버프가 적용되어 있음
        else
        {
            var leftTime = findDeBuff.currentTime;

            if (leftTime < buffTime)
            {
                findDeBuff.buffTime = buffTime;
                findDeBuff.currentTime = buffTime;
                findDeBuff.buffCount = buffCount;
                findDeBuff.currentCount = buffCount;
            }
        }
        InitBonusStat();
    }
    
    public void AddDeBuff(EBuffType buffType, float buffTime, int buffCount, Action endAction = null, float tickInterval = 0, float nextTickTime = 0, Action tickAction = null)
    {
        var findDeBuff = TargetBuff(buffType.ToString());
        // 해당 디버프가 적용되어있지 않음
        if (findDeBuff == null)
        {
            var newDeBuff = new Buff()
            {
                buffId = buffType.ToString(),
                buffType = buffType,
                buffValue = 0,
                buffTime = buffTime,
                currentTime = buffTime,
                buffCount = buffCount,
                currentCount = buffCount,
                endAction = endAction,
                    
                tickInterval = tickInterval,
                nextTickTime = nextTickTime,
                tickAction = tickAction,
            };
            buffList.Add(newDeBuff);
            switch (buffType)
            {
                case EBuffType.Stun:
                    SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", buffEffectPos, 0, true);
                    break;
                case EBuffType.Stagger:
                    // 스트롱 아머만 깨짐
                    if (originStat.bodyType == EBodyType.StrongArmor)
                        basicStat.bodyType = EBodyType.Normal;
                    SpawnObject($"{buffType.ToString()}{ConstValues.Explosion}", buffEffectPos);
                    SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", buffEffectPos, 0, true);
                    break;
                case EBuffType.ArmorBreak:
                    if (originStat.bodyType == EBodyType.SuperArmor)
                    {
                        basicStat.bodyType = EBodyType.Normal;
                        SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", centerPos, 0, true);
                    }
                    break;
                case EBuffType.Frozen:
                    var frozenEffect = SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", centerPos, 0, true);
                    frozenEffect.transform.localScale = new Vector3(myBoxCollider.size.x, myBoxCollider.size.y, 1);
                    break;
                case EBuffType.Shock:
                    var shockEffect = SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", centerPos, 0, true);
                    var shockSize = myBoxCollider.size.x;
                    shockEffect.transform.localScale = new Vector3(shockSize, shockSize, shockSize);
                    break;
                case EBuffType.Burn:
                    var burnEffect = SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", centerPos, 0, true);
                    var burnSize = myBoxCollider.size.x;
                    burnEffect.transform.localScale = new Vector3(burnSize, burnSize, burnSize);
                    break;
            }
        }
        // 해당 디버프가 적용되어 있음
        else
        {
            var leftTime = findDeBuff.currentTime;

            if (leftTime < buffTime)
            {
                findDeBuff.buffTime = buffTime;
                findDeBuff.currentTime = buffTime;
                findDeBuff.buffCount = buffCount;
                findDeBuff.currentCount = buffCount;
            }
        }
    }
    // 버프를 가지고 있는가(id로 찾기)
    private Buff TargetBuff(string buffId)
    {
        return buffList.Find(x => x.buffId == buffId);
    }
    // 버프를 가지고 있는가(타입으로 찾기)
    public Buff TargetBuff(EBuffType buffType)
    {
        return buffList.Find(x => x.buffType == buffType);
    }
    
    // 상태이상
    // 스턴
    public void Stun(float stunTime)
    {
        // 스턴 디버프 추가
        AddDeBuff(EBuffType.Stun, stunTime, 0);
        MoveStateSetting(EMoveState.Stopping);

        // 이후 현재 판정에 따라서 애니메이션을 변화함
        if (normalState is ENormalState.Grabbed or ENormalState.Frozen or ENormalState.Airborne or ENormalState.Down or ENormalState.Stun)
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
        AddDeBuff(EBuffType.Stagger, basicStat.staggerTime, 0);
        MoveStateSetting(EMoveState.Stopping);

        CancelMotion();
        stateCancellation = new CancellationTokenSource();
        StateSetting(ENormalState.Stagger, ConstValues.Stagger, ConstValues.Stagger);
        immuneStagger = true;

        // 투명화 해제
        foreach (var spriteRenderer in mySpriteRenderers)
            spriteRenderer.color = ConstValues.WhiteColor;
    }
    
    // 빙결
    public void Frozen(float frozenTime)
    {
        // 스턴 디버프 추가
        AddDeBuff(EBuffType.Frozen, frozenTime, 0, FrozenEnd);
        MoveStateSetting(EMoveState.Stopping);

        // 이후 현재 판정에 따라서 애니메이션을 변화함
        if (normalState is ENormalState.Grabbed or ENormalState.Frozen)
        {
            Debug.Log($"상위 판정이 존재함: {normalState}");
            return;
        }
        
        CancelMotion();
        stateCancellation = new CancellationTokenSource();
        
        // 강제로 세움 + 얼음꽝꽝 이팩트
        StandHitBox();
        StateSetting(ENormalState.Frozen, ConstValues.Stun, ConstValues.Stun);
    }

    private void FrozenEnd()
    {
        var endObject = SpawnObject(ConstValues.FrozenEndEffect, centerPos);
        float size = myBoxCollider.size.x;
        endObject.transform.localScale = new Vector3(size, size, size);
    }

    public virtual async void Damaged(float damagedTime)
    {
        if (damagedTime == 0)
            return;
        
        if (normalState is ENormalState.Grabbed or ENormalState.Frozen or ENormalState.Airborne or ENormalState.Down or ENormalState.Stun)
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
        
        // 넉백 중 다운, 빙결 상태라면 무시
        if (normalState is ENormalState.Down or ENormalState.Frozen)
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
}