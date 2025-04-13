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
}

// 버프 타입
public enum EBuffType
{
    Stun,
}

public abstract class Player : MonoBehaviour
{
    private Vector3 defaultScale;
    private Vector3 reverseScale;
    private PlayerStat stat;
    protected CancellationTokenSource stateCancellation;
    protected CancellationTokenSource anotherCancellation; // 우선 넉백에만사용되고 있음

    protected Rigidbody2D myRigidbody;
    protected BoxCollider2D myBoxCollider;
    private Animator myAnimator;
    private Vector2 chargeVector;
    private float globalCoolTime;
    protected float curGlobalCoolTime;
    protected int airborneCount;     // 에어본 카운트
    protected int jumpAttackCount;
    protected int moveLayerMask;

    [SerializeField] protected List<PlayerSkill> skillList = new List<PlayerSkill>();
    [SerializeField] protected List<GameObject> controlObject = new List<GameObject>(); // 직접 시간을 관리하는 '공격판정'
    [SerializeField] protected List<GameObject> normalObject = new List<GameObject>(); // 직접 시간을 관리하는 '일반 오브젝트'
    [SerializeField] protected List<GameObject> buffObject = new List<GameObject>(); // 직접 시간을 관리하는 '버프 오브젝트'
    
    [SerializeField] protected string charName;
    [SerializeField] protected bool nextAttack;
    [SerializeField] protected ENormalState normalState;
    [SerializeField] protected EMoveState moveState;
    [SerializeField] protected ELandingState landingState;
    [SerializeField] protected EBodyType bodyType;
    [SerializeField] protected List<Buff> buffList = new List<Buff>();
    
    [SerializeField] private bool canFlip;
    [SerializeField] private bool canMove;
    [SerializeField] private bool immortal;
    [SerializeField] private float moveRatio;

    [SerializeField] private Transform buffEffectPos;

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
        UpdateAirborneDown();
        UpdateGlobalCoolTime();
        UpdateBuff();
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
    private void RemoveObjectList(List<GameObject> list, GameObject obj)
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

    protected void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
    {
        myAnimator.ResetTrigger(ConstValues.ComboAttack);
        myAnimator.ResetTrigger(ConstValues.Airborne);
        myAnimator.ResetTrigger(ConstValues.Down);
        
        if (changeNormalState == ENormalState.Normal)
        {
            switch (landingState)
            {
                case ELandingState.Ground:
                    normalState = ENormalState.Idle;
                    break;
                case ELandingState.Air:
                    normalState = ENormalState.Jump;
                    break;
            }
        }
        else
        {
            normalState = changeNormalState;
        }
        
        if (triggerName == ConstValues.Normal)
        {
            switch (landingState)
            {
                case ELandingState.Ground:
                    myAnimator.SetTrigger(ConstValues.Idle);
                    break;
                case ELandingState.Air:
                    myAnimator.SetTrigger(ConstValues.JumpDown);
                    break;
            }
        }
        else
        {
            myAnimator.SetTrigger(triggerName);
        }

        if (animId == ConstValues.Normal)
        {
            switch (landingState)
            {
                case ELandingState.Ground:
                    animId = ConstValues.Idle;
                    break;
                case ELandingState.Air:
                    animId = ConstValues.JumpDown;
                    break;
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

    protected void MoveStateSetting(EMoveState changeState)
    {
        moveState = changeState;
    }
    private void LandingStateSetting(ELandingState changeState)
    {
        landingState = changeState;
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
    
    private void UpdateAirborneDown()
    {
        if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.Airborne) && myRigidbody.linearVelocity.y < 0)
        {
            StateSetting(ENormalState.Airborne, ConstValues.AirborneDown, ConstValues.AirborneDown);
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

    private void UpdateBuff()
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
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
            }
        }
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
            if (normalState == ENormalState.Idle && landingState == ELandingState.Ground)
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
        anotherCancellation?.Cancel();
        
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
        
        // 대시
        if (skillKey == GameManager.Instance.dashKey && IsCc())
        {
            Debug.Log("대시를 사용 할 수 있는 상태가 아님");
            return false;
        }
        // 일반 스킬
        if (skillKey != GameManager.Instance.dashKey && (normalState == ENormalState.Skill || IsDamaged()))
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
        
        if (landingState == ELandingState.Ground && normalState is ENormalState.Idle or ENormalState.Move or ENormalState.Attack)
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
    protected void SpawnAttack(string id, Transform attackTransform)
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
                AddObjectList(controlObject, obj);

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
            missile.SetupData(missileData, dir, SpawnAttack);
        }
    }
    private GameObject SpawnObject(string id, Transform attackTransform, bool isBuff = false)
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
        chargeVector = RayCheckLength(dashLength, 0);
        // 대시 이팩트 소환
        var dashEffect = SpawnObject($"{charName}_{ConstValues.DashEffect}", transform);
        if (transform.localScale.x > 0)
            dashEffect.transform.position = new Vector3(dashEffect.transform.position.x - 1.5f, dashEffect.transform.position.y, dashEffect.transform.position.z);
        else
            dashEffect.transform.position = new Vector3(dashEffect.transform.position.x + 1.5f, dashEffect.transform.position.y, dashEffect.transform.position.z);
        
        var trace = dashEffect.GetComponent<Trace>();
        trace.enabled = true;

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
    
    // 레이체크(벽 등을 판정하여 최종적으로 도착하는 지점 확인용도)
    private Vector2 RayCheckLength(float chargeLengthX, float chargeLengthY)
    {
        // 왼쪽
        if (transform.localScale.x < 0)
        {
            var leftRay = Physics2D.Raycast(transform.position, Vector2.left, chargeLengthX, moveLayerMask);
            Debug.DrawRay(transform.position, Vector2.left * chargeLengthX, ConstValues.RedColor, 0.1f);
            
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
            var rightRay = Physics2D.Raycast(transform.position, Vector2.right, chargeLengthX, moveLayerMask);
            Debug.DrawRay(transform.position, Vector2.right * chargeLengthX, ConstValues.RedColor, 0.1f);

            // 레이에 닿은 콜라이더가 한개라도 있을 경우 체크가 참이된다
            if (rightRay.collider != null)
                return new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 + 레이의 길이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
            else
                return new Vector2(transform.position.x + chargeLengthX - (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
        }
    }
    private Vector2 RayCheckReverse(float chargeLengthX, float chargeLengthY)
    {
        // 왼쪽
        if (transform.localScale.x > 0)
        {
            var leftRay = Physics2D.Raycast(transform.position, Vector2.left, chargeLengthX, moveLayerMask);
            Debug.DrawRay(transform.position, Vector2.left * chargeLengthX, ConstValues.RedColor, 0.1f);
            
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
            var rightRay = Physics2D.Raycast(transform.position, Vector2.right, chargeLengthX, moveLayerMask);
            Debug.DrawRay(transform.position, Vector2.right * chargeLengthX, ConstValues.RedColor, 0.1f);

            // 레이에 닿은 콜라이더가 한개라도 있을 경우 체크가 참이된다
            if (rightRay.collider != null)
                return new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y + chargeLengthY);
            // 레이에 닿은 콜라이더가 아무것도 없을 경우 (자신x축 + 레이의 길이) - (자신의 콜라이더/2) 만큼 벡터가 정해진다
            else
                return new Vector2(transform.position.x + chargeLengthX - (myBoxCollider.size.x / 2), transform.position.y + chargeLengthY);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // 착지
        if (col.gameObject.layer == LayerMask.NameToLayer("Ground") && landingState == ELandingState.Air)
        {
            LandingStateSetting(ELandingState.Ground);
            
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;

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

    private void OnCollisionExit2D(Collision2D col)
    {
        // 점프
        if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            LandingStateSetting(ELandingState.Air);
        }
    }
    
    // 상속화 할 예정
    
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
    
    public async void Grabbed(Vector3 grabVector)
    {
        CancelMotion();
        StateSetting(ENormalState.Grabbed, ConstValues.Grabbed, ConstValues.Grabbed);
        
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

        stateCancellation = new CancellationTokenSource();
        Bound(xVelocity, yVelocity);
        //DownHitBox();

        if (xVelocity == 0)
            return;
        
        transform.localScale = xVelocity > 0 ? reverseScale : defaultScale;
    }
    private void Bound(float xVelocity, float yVelocity)
    {
        StateSetting(ENormalState.Airborne, ConstValues.Airborne, ConstValues.Airborne);
        GravityChange(ConstValues.BasicGravity);
        myRigidbody.linearVelocity = new Vector2(xVelocity, yVelocity);
    }
    private async void DownAndStand()
    {
        StateSetting(ENormalState.Down, ConstValues.Down, ConstValues.Down);
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
            SpawnObject($"{buffType.ToString()}{ConstValues.Effect}", buffEffectPos, true);
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
        if (await NormalDelay(damagedTime, stateCancellation).SuppressCancellationThrow())
            return;
        
        StateRecovery();
    }

    // 상태 회복
    private void StateRecovery()
    {
        var findDeBuff = buffList.Find(x => x.buffType == EBuffType.Stun);
        
        // 스턴상태가 걸려있지 않은 경우
        if (findDeBuff == null)
        {
            StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        }
        // 스턴상태가 걸려있는 경우
        else
        {
            StateSetting(ENormalState.Stun, ConstValues.Stun, ConstValues.Stun);
        }
    }

    // 넉백
    public async void KnockBack(float knockBackLength)
    {
        var knockPosX = RayCheckReverse(knockBackLength, 0).x;
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
}
