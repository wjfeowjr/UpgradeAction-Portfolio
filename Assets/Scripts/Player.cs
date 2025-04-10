using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using static ENormalState;

public class PlayerStat
{
    public float speed;
}

[Serializable]
public class PlayerSkill
{
    public string skillName;
    public float coolTime;
    private float lastUsedTime = -Mathf.Infinity;
    public bool IsOnCooldown => Time.time < lastUsedTime + coolTime;
    public KeyCode skillKeyCode;
    
    public float GetRemainingCooldown()
    {
        float remaining = (lastUsedTime + coolTime) - Time.time;
        return Mathf.Max(0f, remaining);
    }

    public void SetCoolTime()
    {
        lastUsedTime = Time.time;
    }
}

// 기본 상태 모션
public enum ENormalState
{
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
}

// 실제 이동 관련
public enum EMoveState
{
    Stopping,
    Moving,
}

// 점프 관련
public enum EJumpState
{
    Landing,
    Jumping,
}

// 바디 타입
public enum EBodyType
{
    Normal,
    SuperArmor,
}

public abstract class Player : MonoBehaviour
{
    private Vector3 defaultScale;
    private Vector3 reverseScale;
    private PlayerStat stat;
    protected CancellationTokenSource stateCancellation;

    protected Rigidbody2D myRigidbody;
    protected BoxCollider2D myBoxCollider;
    private Animator myAnimator;
    private Vector2 chargeVector;
    private float globalCoolTime;
    [SerializeField] protected float curGlobalCoolTime;
    protected int jumpAttackCount;
    protected int moveLayerMask;

    [SerializeField] protected List<PlayerSkill> skillList = new List<PlayerSkill>();
    [SerializeField] protected List<GameObject> controlObject = new List<GameObject>(); // 직접 시간을 관리하는 '공격판정'
    [SerializeField] protected List<GameObject> normalObject = new List<GameObject>(); // 직접 시간을 관리하는 '일반 오브젝트'

    [SerializeField] protected string charName;
    [SerializeField] protected bool nextAttack;
    [SerializeField] protected ENormalState normalState;
    [SerializeField] protected EMoveState moveState;
    [SerializeField] protected EJumpState jumpState;
    [SerializeField] protected EBodyType bodyType;
    [SerializeField] private bool canFlip;
    [SerializeField] private bool canMove;
    [SerializeField] private bool immortal;
    [SerializeField] private float moveRatio;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myBoxCollider = GetComponent<BoxCollider2D>();
        myAnimator = GetComponent<Animator>();
        globalCoolTime = 0.1f;
        moveLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Wall);
        InitSkill();
    }

    private void OnEnable()
    {
        // 최초 Idle상태로 전환
        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
    }

    private void Start()
    {
        DefaultSetting();
        StatSetting();
    }

    protected void Update()
    {
        UpdateFlip();
        UpdateJumpDown();
        UpdateGlobalCoolTime();
    }

    private void OnDisable()
    {
        stateCancellation?.Cancel();
    }

    private void DefaultSetting()
    {
        defaultScale = transform.localScale;
        reverseScale = new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z);
    }

    private void StatSetting()
    {
        stat = new PlayerStat()
        {
            speed = 6.0f
        };
    }

    private void AddObjectList(List<GameObject> list, GameObject obj)
    {
        list.Add(obj);
    }
    protected async void ClearObjectList(List<GameObject> list, float timer = 0.0f)
    {
        if (timer > 0)
            await UniTask.WaitForSeconds(timer);
        
        foreach (var obj in list)
            obj.gameObject.SetActive(false);
        
        list.Clear();
    }

    protected void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
    {
        myAnimator.ResetTrigger(ConstValues.ComboAttack);
        normalState = changeNormalState;

        // None트리거는 무시된다
        if (triggerName != ConstValues.None)
        {
            if (triggerName == ConstValues.Normal)
            {
                switch (jumpState)
                {
                    case EJumpState.Landing:
                        myAnimator.SetTrigger(ConstValues.Idle);
                        animId = ConstValues.Idle;
                        break;
                    case EJumpState.Jumping:
                        myAnimator.SetTrigger(ConstValues.JumpDown);
                        animId = ConstValues.JumpDown;
                        break;
                }
            }
            else
            {
                myAnimator.SetTrigger(triggerName);
            }
        }

        var animationsData = TableManager.Instance.animations.Animations.Find(x => x.id == animId);
        if (animationsData != null)
        {
            // 애니메이션 테이블을 체크하여, 해당 애니메이션 도중 전환, 이동이 가능한지 판단
            canFlip = animationsData.canFlip;
            canMove = animationsData.canMove;
            moveRatio = animationsData.moveRatio;
            
            if(!SameBodyType(animationsData.bodyType))
                BodyTypeSetting(animationsData.bodyType);
        }
    }

    protected ENormalState ParseState(string animId)
    {
        if (Enum.TryParse(animId, out ENormalState parsedState))
            return parsedState;

        return ENormalState.Idle;
    }

    protected string FinishTrigger()
    {
        var animationsData = TableManager.Instance.animations.Animations.Find(x => myAnimator.GetCurrentAnimatorStateInfo(0).IsName(x.id));
        return animationsData.finishAnim;
    }

    protected void MoveStateSetting(EMoveState changeState)
    {
        moveState = changeState;
    }
    private void JumpStateSetting(EJumpState changeState)
    {
        jumpState = changeState;
    }
    private void BodyTypeSetting(string bodyTypeName)
    {
        bodyType = (EBodyType)Enum.Parse(typeof(EBodyType), bodyTypeName);
    }
    private bool SameBodyType(string bodyTypeName)
    {
        return bodyType.ToString() == bodyTypeName;
    }

    private void Flip(int dir)
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

    protected void MotionFlip()
    {
        switch (transform.localScale.x)
        {
            case > 0 when Controller.Instance.isLeftMove:
                Flip(-1);
                break;
            case < 0 when Controller.Instance.isRightMove:
                Flip(1);
                break;
        }
    }

    private void UpdateFlip()
    {
        if (!canFlip)
            return;

        MotionFlip();
    }
    
    private void UpdateJumpDown()
    {
        if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.Jump) && myRigidbody.linearVelocity.y < 0)
        {
            StateSetting(ENormalState.Jump, ConstValues.JumpDown, ConstValues.JumpDown);
        }
    }

    private void UpdateGlobalCoolTime()
    {
        if (curGlobalCoolTime < globalCoolTime)
            curGlobalCoolTime += Time.deltaTime;

        if (curGlobalCoolTime >= globalCoolTime)
            curGlobalCoolTime = globalCoolTime;
    }
    protected bool GetGlobalCoolTime()
    {
        bool isFill = curGlobalCoolTime >= globalCoolTime;
        return isFill;
    }
    
    // 중력값 변경
    protected void GravityChange(float value)
    {
        myRigidbody.gravityScale = value;
    }
    
    public void Move(Vector2 dir)
    {
        if (!canMove)
            return;

        // 서 있는 상태에선 움직이는 모션으로 변경
        if (dir == Vector2.left || dir == Vector2.right)
        {
            if (normalState == ENormalState.Idle && jumpState == EJumpState.Landing)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            
            if(moveState == EMoveState.Stopping)
                MoveStateSetting(EMoveState.Moving);
        }
        
        //myRigidbody.velocity = new Vector3(dir * stat.speed, myRigidbody.velocity.y);
        transform.Translate(dir * (stat.speed * (moveRatio * 0.01f) * Time.deltaTime));
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
    // 행동 캔슬
    protected void CancelMotion()
    {
        stateCancellation?.Cancel();
        ClearObjectList(controlObject);
        ClearObjectList(normalObject);
        GravityChange(ConstValues.BasicGravity);
    }

    // 스킬을 사용 할 수 있는가?
    protected bool IsCanSkill(KeyCode skillKey)
    {
        var targetSkill = GetSkill(skillKey);
        if (targetSkill.IsOnCooldown)
        {
            Debug.Log($"{targetSkill.skillName} 쿨타임 중: {targetSkill.GetRemainingCooldown():F1}초 남음");
            return false;
        }
        if (skillKey != GameManager.Instance.dashKey && normalState == ENormalState.Skill)
        {
            Debug.Log("스킬을 사용 할 수 있는 상태가 아님");
            return false;
        }
        Debug.Log($"{targetSkill.skillName} 사용!");
        targetSkill.SetCoolTime();
        return true;
    }
    
    // 스킬
    public abstract void Skill(KeyCode skillKey);
    // 공격
    public abstract void Attack();


    // 공격 전진
    protected void AttackAdvance(float distance)
    {
        var dir = 0;
        
        // 오른쪽
        if (transform.localScale.x > 0 && Input.GetKey(GameManager.Instance.moveRightKey))
            dir = 1;
        // 왼쪽
        if (transform.localScale.x < 0 && Input.GetKey(GameManager.Instance.moveLeftKey))
            dir = -1;
        
        if(dir != 0)
            myRigidbody.linearVelocity = dir * new Vector2(distance, myRigidbody.linearVelocity.y);
    }
    
    // 점프
    public async void Jump()
    {
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }
        
        if (jumpState == EJumpState.Landing && normalState is ENormalState.Idle or ENormalState.Move or ENormalState.Attack)
        {
            jumpAttackCount = 0;
            CancelMotion();
            StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);

            float jumpPosY = transform.position.y + 1.5f;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 12.0f); 
            stateCancellation = new CancellationTokenSource();
            while (transform.position.y < jumpPosY)
            {
                if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                    return;
            }
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 6.0f);
        }
    }
    // 도약
    protected async void Leap(float xVelocity, float yVelocity, float leapHeight)
    {
        jumpAttackCount = 0;
        float currentHeight = transform.position.y;
        myRigidbody.linearVelocity = new Vector2(transform.localScale.x * xVelocity, yVelocity);
        stateCancellation = new CancellationTokenSource();
        while (transform.position.y < currentHeight + leapHeight)
        {
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        myRigidbody.linearVelocity = new Vector2(0, 6.0f);
    }
    
    // 공격 소환
    protected void SpawnObject(string id, Transform attackTransform)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackTransform);
        
        var objectData = TableManager.Instance.spawnedObject.SpawnedObject.Find(x => x.id == id);
        if (objectData != null)
        {
            var spawnedObject = obj.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = obj.AddComponent<SpawnedObject>();
            
            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
            if(spawnedObject.GetObjectTime() == 0)
                AddObjectList(controlObject, spawnedObject.gameObject);

            if (spawnedObject.GetTrace())
            {
                var trace = obj.GetComponent<Trace>();
                if(!trace)
                    trace = obj.AddComponent<Trace>();
                
                trace.SetTarget(attackTransform);
            }
        }

        var attackData = TableManager.Instance.attack.Attack.Find(x => x.id == id);
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
        
        var missileData = TableManager.Instance.missile.Missile.Find(x => x.id == id);
        if (missileData != null)
        {
            var missile = obj.GetComponent<Missile>();
            if (!missile)
                missile = obj.AddComponent<Missile>();
            
            var dir = Vector2.right;
            if(transform.localScale.x < 0)
                dir = Vector2.left;
            missile.SetupData(missileData, dir, SpawnObject);
        }
    }
    protected GameObject GetSpawnObject(string id, Transform attackTransform)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, attackTransform);
        
        var objectData = TableManager.Instance.spawnedObject.SpawnedObject.Find(x => x.id == id);
        if (objectData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
            
        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();
        if(spawnedObject.GetObjectTime() == 0)
            AddObjectList(controlObject, spawnedObject.gameObject);

        if (spawnedObject.GetTrace())
        {
            var trace = obj.GetComponent<Trace>();
            if(!trace)
                trace = obj.AddComponent<Trace>();
            
            trace.SetTarget(attackTransform);
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
    // 공격 딜레이
    protected async UniTask AttackDelay(float attackDelay)
    {
        float delay = 0;
        while (delay < attackDelay)
        {
            //float totalAttackSpeed = finalAttackSpeed;
            delay += Time.deltaTime;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
            if (Input.GetKeyDown(GameManager.Instance.attackKey))
                nextAttack = true;
        }
    }
    protected async UniTask NextAttackDelay(float originDelay, float afterDelay)
    {
        float timer = 0;
        float maxDelay = originDelay + afterDelay;
        while (timer < maxDelay)
        {
            //float totalAttackSpeed = finalAttackSpeed;
            timer += Time.deltaTime;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
            if (Input.GetKeyDown(GameManager.Instance.attackKey))
                nextAttack = true;
            if (timer > originDelay && nextAttack)
            {
                break;
            }
        }
    }

    private void InitSkill()
    {
        PlayerSkill dash = new PlayerSkill()
        {
            skillName = "회피",
            coolTime = 1,
            skillKeyCode = KeyCode.Z,
        };
        skillList.Add(dash);
        
        PlayerSkill upperSlash = new PlayerSkill()
        {
            skillName = "올려베기",
            coolTime = 1,
            skillKeyCode = KeyCode.S,
        };
        skillList.Add(upperSlash);
        
        PlayerSkill crash = new PlayerSkill()
        {
            skillName = "박살내기",
            coolTime = 1,
            skillKeyCode = KeyCode.D,
        };
        skillList.Add(crash);
        
        PlayerSkill fireStrike = new PlayerSkill()
        {
            skillName = "불꽃강타",
            coolTime = 1,
            skillKeyCode = KeyCode.F,
        };
        skillList.Add(fireStrike);
    }
    
    public List<PlayerSkill> GetSkillList()
    {
        return skillList;
    }

    private PlayerSkill GetSkill(KeyCode skillCode)
    {
        return skillList.Find(x => x.skillKeyCode == skillCode);
    }

    // 대시
    protected async UniTask<bool> Dash()
    {
        StateSetting(ENormalState.Dash, ConstValues.Dash, ConstValues.Dash);
        immortal = true;
        //StandHitBox();
        GravityChange(0);
        myRigidbody.linearVelocity = Vector2.zero;

        var dashSpeed = 15;
        var dashLength = 5;
        
        // 대시 레이캐스트 체크
        RayCheckCharge(dashLength, 0);
        // 대시 이팩트 소환
        var dashEffect = GetSpawnObject($"{charName}_{ConstValues.DashEffect}", transform);
        if (transform.localScale.x > 0)
            dashEffect.transform.position = new Vector3(dashEffect.transform.position.x - 1.5f, dashEffect.transform.position.y, dashEffect.transform.position.z);
        else
            dashEffect.transform.position = new Vector3(dashEffect.transform.position.x + 1.5f, dashEffect.transform.position.y, dashEffect.transform.position.z);
        
        var trace = dashEffect.GetComponent<Trace>();
        trace.enabled = true;
        AddObjectList(normalObject, dashEffect);

        // 돌진
        bool chargeFinish = await Charge(dashSpeed, 0.5f, dashLength, -0.2f);

        trace.enabled = false;
        ClearObjectList(normalObject, 0.3f);
        
        // 대시 끝
        immortal = false;
        return chargeFinish;
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
    
    // 레이체크_돌진
    protected void RayCheckCharge(float chargeLengthX, float chargeLengthY)
    {
        // 왼쪽
        if (transform.localScale.x < 0)
        {
            var leftRay = Physics2D.Raycast(transform.position, Vector2.left, chargeLengthX, moveLayerMask);
            Debug.DrawRay(transform.position, Vector2.left * chargeLengthX, ConstValues.RedColor, 0.1f);
            
            // 레이에 닿은 콜라이더가 한개라도 있을 경우 (닿은 레이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
            if (leftRay.collider != null)
                chargeVector = new Vector2(leftRay.point.x + myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 - 레이의 길이) + (자신의 콜라이더/2) 만큼 벡터가 정해진다
            else
                chargeVector = new Vector2(transform.position.x - chargeLengthX + (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
        }
        // 오른쪽
        else
        {
            var rightRay = Physics2D.Raycast(transform.position, Vector2.right, chargeLengthX, moveLayerMask);
            Debug.DrawRay(transform.position, Vector2.right * chargeLengthX, ConstValues.RedColor, 0.1f);

            // 레이에 닿은 콜라이더가 한개라도 있을 경우 체크가 참이된다
            if (rightRay.collider != null)
                chargeVector = new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 + 레이의 길이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
            else
                chargeVector = new Vector2(transform.position.x + chargeLengthX - (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // 착지
        if (col.gameObject.layer == LayerMask.NameToLayer("Ground") && jumpState == EJumpState.Jumping)
        {
            JumpStateSetting(EJumpState.Landing);
            
            var animationsData = TableManager.Instance.animations.Animations.Find(x => myAnimator.GetCurrentAnimatorStateInfo(0).IsName(x.id));
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;
            
            if (animationsData != null)
            {
                switch (animationsData.landingAnim)
                {
                    case ConstValues.Idle:
                        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
                        break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        // 점프
        if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            JumpStateSetting(EJumpState.Jumping);
        }
    }
}
