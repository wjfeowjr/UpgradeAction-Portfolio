using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using static ENormalState;

[Serializable]
public class PlayerSkill
{
    public string skillName;
    public List<float> coolTime = new List<float>();
    public int maxStack;
    public string icon;
    private float lastUsedTime = -Mathf.Infinity;
    private float stackUsedTime = -Mathf.Infinity;
    
    public bool IsOnCooldown
    {
        get
        {
            if (coolTime.Count > 1)
            {
                return Time.time < lastUsedTime + coolTime[0] || coolTime[2] < 1;
            }
            else
            {
                return Time.time < lastUsedTime + coolTime[0];
            }
        }
    }

    public List<float> GetRemainingCooldown()
    {
        List<float> remainingList = new List<float>();
        float remaining = (lastUsedTime + coolTime[0]) - Time.time;
        remainingList.Add(Mathf.Max(0f, remaining));
        
        // 스택형 스킬이라면
        if (coolTime.Count > 1)
        {
            // 스택 쿨타임이 별개로 돌아간다
            float remaining2 = (stackUsedTime + coolTime[1]) - Time.time;
            // 스택 쿨타임이 다 차게 되면
            if (remaining2 < 0 && (int)coolTime[2] < maxStack)
            {
                // 스킬 스택이 1 차오르고
                coolTime[2] += 1;
                // 스택이 찼는데도 최대 스택에 도달하지 못한다면
                if ((int)coolTime[2] < maxStack)
                    // 스택 쿨타임만큼 시간을 늘려준다
                    stackUsedTime += coolTime[1];
            }
            
            remainingList.Add(Mathf.Max(0f, remaining2));
            
            float remaining3 = coolTime[2];
            remainingList.Add(remaining3);
        }

        return remainingList;
    }

    public void SetCoolTime()
    {
        lastUsedTime = Time.time;
        if (coolTime.Count > 1)
        {
            if((int)coolTime[2] == maxStack)
                stackUsedTime = Time.time;
            
            coolTime[2] -= 1;
        }
    }
}

[Serializable]
public class PlayerStat
{
    public int passiveComment;
    public string passive;
    public float jumpForce;
    public float jumpHeight;
    public int jumpAttackCount;
    public float jumpAttackForce;
}

public abstract class Player : Character
{
    private bool isChanging;
    private float jumpLimitY;

    [SerializeField] protected PlayerStat myStat;  // 내 스텟(변동되어야 함)
    [SerializeField] protected List<PlayerSkill> skillList = new List<PlayerSkill>();
    [SerializeField] protected bool nextAttack;
    [SerializeField] private bool canFlip;
    [SerializeField] private bool canMove;
    [SerializeField] private float moveRatio;

    private float globalCoolTime;
    protected float curGlobalCoolTime;
    
    private float changeGlobalCoolTime;
    private float curChangeGlobalCoolTime;
    
    // 프로퍼티
    public bool IsChanging => isChanging;
    public int JumpAttackCount
    {
        get => jumpAttackCount;
        set => jumpAttackCount = value;
    }

    // 스킬
    public abstract void Skill(KeyCode skillKey);
    // 공격
    public abstract void Attack();
    // 교체공격
    public abstract void ChangeAttack();
    
    protected override void Awake()
    {
        base.Awake();
        globalCoolTime = 0.1f;
        changeGlobalCoolTime = 0.1f;
    }

    protected void OnEnable()
    {
        InitAdditionalStat();
        // 최초 Idle상태로 전환
        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
    }

    private void Update()
    {
        UpdateFlip();
        UpdateJumpDown();
        UpdateAirborneDown();
        UpdateGlobalCoolTime();
        UpdateChangeGlobalCoolTime();
        UpdateBuff();
    }

    private void OnDisable()
    {
        stateCancellation?.Cancel();
    }

    // 테이블의 값으로 스텟 초기화(기본 스텟)
    public void InitBasicStat()
    {
        var myName = name.Split('(')[0];
        var targetStat = TableManager.Instance.playerTable.Player.Find(x => x.id == myName);
        
        basicStat = new BasicStat()
        {
            id = targetStat.id,
            name = targetStat.name,
            bodyType = (EBodyType)Enum.Parse(typeof(EBodyType), targetStat.bodyType),
            hp = targetStat.hp,
            power = targetStat.power,
            defence = targetStat.defence,
            moveSpeed = targetStat.moveSpeed,
            attackSpeed = targetStat.attackSpeed,
            criticalChance = targetStat.criticalChance,
            criticalDamage = targetStat.criticalDamage,
            weight = targetStat.weight,
            stagger = targetStat.stagger,
            staggerTime = targetStat.staggerTime,
        };
        myStat = new PlayerStat()
        {
            passiveComment = targetStat.passiveComment,
            passive = targetStat.passive,
            jumpForce = targetStat.jumpForce,
            jumpHeight = targetStat.jumpHeight,
            jumpAttackCount = targetStat.jumpAttackCount,
            jumpAttackForce = targetStat.jumpAttackForce,
        };
        
        if (string.IsNullOrEmpty(originStat.id))
        {
            originStat = new BasicStat()
            {
                id = targetStat.id,
                name = targetStat.name,
                bodyType = (EBodyType)Enum.Parse(typeof(EBodyType), targetStat.bodyType),
                hp = targetStat.hp,
                power = targetStat.power,
                defence = targetStat.defence,
                moveSpeed = targetStat.moveSpeed,
                attackSpeed = targetStat.attackSpeed,
                criticalChance = targetStat.criticalChance,
                criticalDamage = targetStat.criticalDamage,
                weight = targetStat.weight,
                stagger = targetStat.stagger,
                staggerTime = targetStat.staggerTime,
            };
        }
    }

    private void InitAdditionalStat()
    {
        var finalHp = basicStat.hp;
        basicStat.maxHp = finalHp;
        basicStat.hp = finalHp;
    }

    public void MoveChange()
    {
        var changeAttackId = TableManager.Instance.animationsTable.Animations.Find(x => x.id == ConstValues.ChangeAttack && x.caster == basicStat.id);
        if(changeAttackId == null)
            StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        else
            ChangeAttack();
    }
    public void JumpChange(Vector2 velocity)
    {
        var changeAttackId = TableManager.Instance.animationsTable.Animations.Find(x => x.id == ConstValues.ChangeAttack && x.caster == basicStat.id);
        if (changeAttackId == null)
        {
            myRigidbody.linearVelocity = velocity;
            JumpToChange();
            StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        }
        else
        {
            ChangeAttack();
        }
        LandingStateSetting(ELandingState.Air);
    }

    protected override void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
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
                    SetTriggerAnimator(ConstValues.Idle);
                    break;
                case ELandingState.Air:
                    SetTriggerAnimator(ConstValues.JumpDown);
                    break;
            }
        }
        else
        {
            SetTriggerAnimator(triggerName);
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

        var animationsData = TableManager.Instance.animationsTable.Animations.Find(x => x.id == animId && (x.caster == ConstValues.All || x.caster == basicStat.id));
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
    
    protected override void StateRecovery()
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
        StandHitBox();
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
        if (basicStat.id == ConstValues.Gunner)
            return;
        
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
    
    private void UpdateChangeGlobalCoolTime()
    {
        if (curChangeGlobalCoolTime < changeGlobalCoolTime)
            curChangeGlobalCoolTime += Time.deltaTime;

        if (curChangeGlobalCoolTime >= changeGlobalCoolTime)
            curChangeGlobalCoolTime = changeGlobalCoolTime;
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
        transform.Translate(dir * (basicStat.moveSpeed * (moveRatio * 0.01f) * Time.deltaTime));
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
    
    // 교체를 사용 할 수 있는가?
    private async UniTask<bool> IsCanChange()
    {
        var targetSkill = GameManager.Instance.ChangeSkill.playerSkill;
        if (targetSkill.IsOnCooldown)
        {
            var coolTimeList = targetSkill.GetRemainingCooldown();
            Debug.Log($"{targetSkill.skillName} 쿨타임 중: {coolTimeList[0]:F1}초 남음");
            return false;
        }

        if (isChanging)
        {
            Debug.Log($"이미 교체가 진행중임");
            return false;
        }

        curChangeGlobalCoolTime = 0;
        isChanging = true;
        // 점프 도중에만 글로벌 쿨타임을 준다
        await UniTask.WaitUntil(()=> normalState is Idle or ENormalState.Move || (normalState is ENormalState.Jump && curChangeGlobalCoolTime >= changeGlobalCoolTime));
        isChanging = false;
        targetSkill.SetCoolTime();
        return true;
    }

    // 스킬을 사용 할 수 있는가?
    protected bool IsCanSkill(string id)
    {
        var targetSkill = GetSkill(id);
        if (targetSkill == null)
        {
            Debug.Log($"해당 키에 등록된 스킬이 없음");
            return false;
        }
        
        if (targetSkill.IsOnCooldown)
        {
            var coolTimeList = targetSkill.GetRemainingCooldown();
            if(coolTimeList.Count > 1)
                Debug.Log($"{targetSkill.skillName} 기본 쿨타임 {coolTimeList[0]:F1}초 남음, 스택 쿨타임 {coolTimeList[1]:F1}초 남음, 남은 스택 개수 {coolTimeList[2]:F1}개");
            else
                Debug.Log($"{targetSkill.skillName} 쿨타임 중: {coolTimeList[0]:F1}초 남음");
            
            return false;
        }

        var type = TableManager.Instance.skillTable.Skill.Find(x => x.id == id).type;

        // 대시
        if (type == ConstValues.Dash && IsCc())
        {
            Debug.Log("대시를 사용 할 수 있는 상태가 아님");
            return false;
        }
        // 일반 스킬
        if (type == ConstValues.Skill && (normalState == ENormalState.Skill || IsDamaged()))
        {
            Debug.Log("스킬을 사용 할 수 있는 상태가 아님");
            return false;
        }
        
        Debug.Log($"{targetSkill.skillName} 사용!");
        targetSkill.SetCoolTime();
        return true;
    }

    // 공격 전진
    protected void AttackAdvance(float distance)
    {
        var dir = 0;
        
        // 오른쪽
        if (transform.localScale.x > 0 && Input.GetKey(GameManager.Instance.rightMoveKey))
            dir = 1;
        // 왼쪽
        if (transform.localScale.x < 0 && Input.GetKey(GameManager.Instance.leftMoveKey))
            dir = -1;
        
        if(dir != 0)
            myRigidbody.linearVelocity = dir * new Vector2(distance, myRigidbody.linearVelocity.y);
    }
    
    // 점프
    public async void Jump()
    {
        if (landingState == ELandingState.Ground && normalState is ENormalState.Idle or ENormalState.Move or ENormalState.Attack)
        {
            if(!GetGlobalCoolTime())
            {
                Debug.Log("글로벌 쿨타임이 지나지 않음");
                return;
            }
            
            Debug.Log("점프");
            curGlobalCoolTime = 0;
            jumpAttackCount = 0;
            CancelMotion();
            StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
            LandingStateSetting(ELandingState.Air);
            
            jumpLimitY = transform.position.y + myStat.jumpHeight;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 20); 
            
            stateCancellation = new CancellationTokenSource();
            while (transform.position.y < jumpLimitY)
            {
                if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                    return;
            }
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 6.0f);
        }
    }

    public async void JumpToChange()
    {
        stateCancellation = new CancellationTokenSource();
        while (transform.position.y < jumpLimitY)
        {
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 6.0f);
    }
    // 도약
    protected async void Leap(float xVelocity, float yVelocity, float leapHeight)
    {
        if(landingState == ELandingState.Ground)
            jumpAttackCount = 0;
        
        Debug.Log("도약");
        LandingStateSetting(ELandingState.Air);
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
    
    // 공격 딜레이
    protected async UniTask AttackDelay(float attackDelay)
    {
        float delay = 0;
        while (delay < attackDelay)
        {
            delay += Time.deltaTime * basicStat.attackSpeed;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
            // if (Input.GetKeyDown(GameManager.Instance.attackKey))
            //     nextAttack = true;
        }
    }
    protected async UniTask NextAttackDelay(float originDelay, float afterDelay)
    {
        float delay = 0;
        float maxDelay = originDelay + afterDelay;
        while (delay < maxDelay)
        {
            delay += Time.deltaTime * basicStat.attackSpeed;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
            if (Input.GetKey(GameManager.Instance.attackKey))
                nextAttack = true;
            if (delay > originDelay && nextAttack)
            {
                break;
            }
        }
    }

    public void InitSkill()
    {
        foreach (var skill in TableManager.Instance.skillTable.Skill)
        {
            if (skill.caster != basicStat.id)
                continue;
            
            PlayerSkill addedSkill = new PlayerSkill();
            
            addedSkill.skillName = skill.id;
            var coolTimeArray = skill.coolTime.Split(',');
            foreach (var coolTime in coolTimeArray)
                addedSkill.coolTime.Add(float.Parse(coolTime));

            if (addedSkill.coolTime.Count > 1)
                addedSkill.maxStack = (int)addedSkill.coolTime[2];
            
            addedSkill.icon = skill.icon;
                
            skillList.Add(addedSkill);
        }
    }
    
    public List<PlayerSkill> GetSkillList()
    {
        return skillList;
    }

    private PlayerSkill GetSkill(string id)
    {
        return skillList.Find(x => x.skillName == id);
    }

    // 캐릭터 교체
    public async void ChangeCharacter()
    {
        var changing = await IsCanChange();
        
        if (!changing)
            return;

        Debug.Log("교체!");
        GameManager.Instance.CharacterChange();
    }
 
    // 대시
    protected async UniTask<bool> Dash()
    {
        StateSetting(ENormalState.Dash, ConstValues.Dash, ConstValues.Dash);
        immortal = true;
        StandHitBox();
        GravityChange(0);
        myRigidbody.linearVelocity = Vector2.zero;

        var dashSpeed = 15;
        var dashLength = 5;
        
        // 대시 레이캐스트 체크
        chargeVector = RayCheckLength(dashLength, 0);
        // 대시 이팩트 소환
        var dashEffect = SpawnObject($"{name}_{ConstValues.DashEffect}", transform);
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
}
