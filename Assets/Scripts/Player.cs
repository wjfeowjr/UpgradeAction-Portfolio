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
    public string id;
    public List<float> maxCoolTime = new List<float>();
    public List<float> curCoolTime = new List<float>();
    public string name;
    public string explain;

    public bool IsOnCooldown
    {
        get
        {
            if (curCoolTime.Count > 1)
            {
                return maxCoolTime[0] > curCoolTime[0] || curCoolTime[2] < 1;
            }
            else
            {
                return maxCoolTime[0] > curCoolTime[0];
            }
        }
    }

    // 쿨타임 감소
    public List<float> ReducingCooldown()
    {
        if (curCoolTime[0] < maxCoolTime[0])
        {
            curCoolTime[0] += Time.deltaTime;
            if (curCoolTime[0] >= maxCoolTime[0])
                curCoolTime[0] = maxCoolTime[0];
        }

        // 스택형 스킬이라면
        if (curCoolTime.Count > 1)
        {
            // 스택 쿨타임이 별개로 돌아간다
            curCoolTime[1] += Time.deltaTime;
            // 스택 쿨타임이 다 차게 되면
            if (curCoolTime[1] >= maxCoolTime[1] && (int)curCoolTime[2] < maxCoolTime[2])
            {
                // 스킬 스택이 1 차오르고
                curCoolTime[2] += 1;
                // 스택이 찼는데도 최대 스택에 도달하지 못한다면
                if ((int)curCoolTime[2] < maxCoolTime[2])
                    // 스택 쿨타임을 0으로 바꾼다(다시 채워지도록)
                    curCoolTime[1] = 0;
            }
        }
        return curCoolTime;
    }

    // 남은 쿨타임
    public List<float> GetRemainingCooldown()
    {
        return curCoolTime;
    }
    
    // 쿨타임 초기화
    public void ResetCoolTime()
    {
        curCoolTime[0] = maxCoolTime[0];
        if (curCoolTime.Count > 1)
        {
            curCoolTime[1] = maxCoolTime[1];
            curCoolTime[2] = maxCoolTime[2];
        }
    }
    
    public List<float> GetMaxCoolTime()
    {
        return maxCoolTime;
    }
    
    public List<float> ResetCooldown()
    {
        curCoolTime[0] = maxCoolTime[0];

        // 스택형 스킬이라면
        if (curCoolTime.Count > 1)
        {
            curCoolTime[1] = maxCoolTime[1];
            curCoolTime[2] = maxCoolTime[2];
        }
        return curCoolTime;
    }

    public void SetCoolTime()
    {
        curCoolTime[0] = 0;
        if (curCoolTime.Count > 1)
        {
            if((int)curCoolTime[2] == (int)maxCoolTime[2])
                curCoolTime[1] = 0;
            
            curCoolTime[2] -= 1;
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
    [SerializeField] private GameObject dashEffectUI;
    [SerializeField] private GameObject dashFrameUI;
    
    private float globalCoolTime;
    protected float curGlobalCoolTime;
    
    private float changeGlobalCoolTime;
    private float curChangeGlobalCoolTime;
    
    private float skillGlobalCoolTime;
    private float curSkillGlobalCoolTime;
    
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
        skillGlobalCoolTime = 0.02f;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 최초 Idle상태로 전환
        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        myGravity = myRigidbody.gravityScale;
    }

    private void Start()
    {
        SetDashUIObject();
    }

    protected override void Update()
    {
        base.Update();
        UpdateFlip();
        UpdateJumpDown();
        UpdateLanding();
        UpdateDown();
        UpdateGlobalCoolTime();
        UpdateChangeGlobalCoolTime();
        UpdateSkillGlobalCoolTime();
        UpdateBuff();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        UpdateCameraLimit();
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
        immortal = false;
        
        basicStat = new BasicStat()
        {
            id = targetStat.id,
            name = targetStat.name,
            bodyType = (EBodyType)Enum.Parse(typeof(EBodyType), targetStat.bodyType),
            hp = targetStat.hp,
            maxHp = targetStat.hp,
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
                maxHp = targetStat.hp,
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

    public void InitAdditionalStat()
    {
        var finalHp = basicStat.hp;
        basicStat.maxHp = finalHp;
        basicStat.hp = finalHp;
    }

    public void ResetSkillCoolTime()
    {
        foreach (var skill in skillList)
            skill.ResetCoolTime();
    }

    // 스킬 쿨타임 진행
    public void ReduceSkillCoolTime()
    {
        foreach (var skill in skillList)
            skill.ReducingCooldown();
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
        myAnimator.ResetTrigger(ConstValues.JumpDown);
        
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
    
    protected override void StateCheck()
    {
        var stun = buffList.Find(x => x.buffType is EBuffType.Stun);

        if (stun != null)
            basicStat.bodyType = originStat.bodyType;
    }
    protected override void StateRecovery()
    {
        var stun = buffList.Find(x => x.buffType is EBuffType.Stun);

        // 스턴이 풀린 경우
        if (stun == null)
        {
            DeleteDashFrameUI();
            switch (landingState)
            {
                case ELandingState.Air:
                    StateSetting(ENormalState.Jump, ConstValues.JumpDown, ConstValues.JumpDown);
                    break;
                case ELandingState.Ground:
                    StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
                    break;
            }
        }
        // 스턴에 걸려있는 경우
        else
        {
            StateSetting(ENormalState.Stun, ConstValues.Stun, ConstValues.Stun);
        }
        StandHitBox();
    }
    
    protected void MotionFlip()
    {
        if(GameManager.Instance.CurPlayer != this)
            return;
        
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
            StateSetting(ENormalState.Jump, ConstValues.JumpDown, ConstValues.JumpDown);
    }

    // 일반점프 후 착지에만 관여
    private void UpdateLanding()
    {
        if (normalState is not ENormalState.Jump || downJumping)
            return;
        
        var distance = 0.2f;
        var down = Physics2D.Raycast(transform.position, Vector2.down, distance, groundAndPlatformLayerMask);
        Debug.DrawRay(transform.position, Vector2.down * distance, ConstValues.RedColor, 0.02f);

        if (down.collider != null && myRigidbody.linearVelocityY is <= 0.05f and >= -0.05f)
        {
            Debug.Log("UpdateLanding");
            LandingStateSetting(ELandingState.Ground);
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;
            jumpAttackCount = 0;
            StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        }
    }

    private void UpdateDown()
    {
        if (normalState is not ENormalState.Airborne)
            return;
        
        var distance = 0.2f;
        var down = Physics2D.Raycast(transform.position, Vector2.down, distance, groundAndPlatformLayerMask);
        Debug.DrawRay(transform.position, Vector2.down * distance, ConstValues.BlueColor, 0.02f);

        if (down.collider != null && myRigidbody.linearVelocityY is <= 0.05f and >= -0.05f)
        {
            Debug.Log("UpdateDown");
            LandingStateSetting(ELandingState.Ground);
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;
            jumpAttackCount = 0;
            DownAndStand();
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
    private void UpdateSkillGlobalCoolTime()
    {
        if (curSkillGlobalCoolTime < skillGlobalCoolTime)
            curSkillGlobalCoolTime += Time.deltaTime;

        if (curSkillGlobalCoolTime >= skillGlobalCoolTime)
            curSkillGlobalCoolTime = skillGlobalCoolTime;
    }
    protected bool GetSkillGlobalCoolTime()
    {
        bool isFill = curSkillGlobalCoolTime >= skillGlobalCoolTime;
        return isFill;
    }
    
    public float GetRightPosX()
    {
        return transform.position.x + physicsCollider.size.x * 0.5f;
    }
    public float GetLeftPosX()
    {
        return transform.position.x - physicsCollider.size.x * 0.5f;
    }
    public float  GetUpPosY()
    {
        return transform.position.y + physicsCollider.size.y;
    }
    public float GetDownPosY()
    {
        return transform.position.y;
    }

    // 대시UI이팩트 캐싱
    private void SetDashUIObject()
    {
        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;
        
        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        dashEffectUI = SpawnUI(ConstValues.DashEffectUI, uiInterface.GetDashSkillPos());
        dashFrameUI = SpawnUI(ConstValues.DashFrameUI, uiInterface.GetDashSkillPos());
        dashEffectUI.SetActive(false);
        dashFrameUI.SetActive(false);
    }

    // 대시UI이팩트 켜기
    private void ActiveDashEffectUI()
    {
        dashEffectUI.GetComponent<SpawnedObject>().EnableSetting();
        dashEffectUI.SetActive(true);
        dashFrameUI.SetActive(true);
    }

    private void DeleteDashFrameUI()
    {
        dashFrameUI.SetActive(false);
    }

    public void RoomMoveState()
    {
        StandHitBox();
        DeleteDashFrameUI();
    }

    private void UpdateCameraLimit()
    {
        if(!GameManager.Instance.ControlStart)
            return;
        
        // 1) 플레이어 절반 크기
        float halfWidth  = physicsCollider.size.x * 0.5f;
        float halfHeight = physicsCollider.size.y * 0.5f;

        // 2) 카메라 뷰 포인트를 월드 좌표로 변환
        Camera cam = GameManager.Instance.MainCamera.MyCamera;
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        // 3) 플레이어 센터 기준으로 클램프 경계 계산
        float leftLimit  = bottomLeft.x + halfWidth;
        float rightLimit = topRight.x - halfWidth;
        float upLimit = topRight.y - halfHeight;
        
        Vector3 pos = transform.position;
        Vector2 vel = myRigidbody.linearVelocity;
        
        if (pos.x < leftLimit)
        {
            pos.x = leftLimit;
            if (vel.x < 0)
                vel.x = 0;
        }
        else if (pos.x > rightLimit)
        {
            pos.x = rightLimit;
            if (vel.x > 0)
                vel.x = 0;
        }
        
        if (pos.y > upLimit)
        {
            pos.y = upLimit;
            if (vel.y > 0)
                vel.y = 0;
        }
        
        //transform.position = pos;
        myRigidbody.linearVelocity = vel;
    }

    public void ForceIdle()
    {
        CancelMotion();
        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
    }
    
    public void ForceJump()
    {
        CancelMotion();
        StateSetting(ENormalState.Idle, ConstValues.Jump, ConstValues.Jump);
    }

    public void ForceProduct()
    {
        if (landingState == ELandingState.Air)
        {
            ForceJump();
        }
        else
        {
            ForceIdle();
        }
    }

    public void MoveSetting(Vector2 dir)
    {
        if (!canMove)
            return;
        
        // 서 있는 상태에서 걷기 상태로 전환
        if (dir.x != 0f && normalState == ENormalState.Idle && landingState == ELandingState.Ground)
            StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        
        // 멈추는 중이었다면 다시 걷기 상태로
        if (moveState == EMoveState.Stopping && Mathf.Abs(dir.x) > 0f)
            MoveStateSetting(EMoveState.Moving);

        // // 서 있는 상태에선 움직이는 모션으로 변경
        // if (dir == Vector2.left || dir == Vector2.right)
        // {
        //     if (normalState == ENormalState.Idle && landingState == ELandingState.Ground)
        //         StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        //     
        //     if(moveState == EMoveState.Stopping)
        //         MoveStateSetting(EMoveState.Moving);
        // }
        //
        // transform.Translate(dir * (basicStat.moveSpeed * (moveRatio * 0.01f) * Time.deltaTime));
    }

    public void Move(Vector2 dir)
    {
        if (!canMove)
            return;

        float targetSpeedX = dir.x * basicStat.moveSpeed * (moveRatio * 0.01f);
        float targetSpeedY = myRigidbody.linearVelocity.y;
        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        if (damage == 0)
            return;
        
        GameManager.Instance.SetPlayerHp(basicStat.hp);
        
        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;
        
        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        uiInterface.HpPresenter.SetHpText();
        uiInterface.HpPresenter.HpReduce();
        
        // 즉사는 엌 소리 안냄
        if(basicStat.hp > 0)
            PlaySound(ConstValues.PlayerDamaged1);
    }

    public override void Die()
    {
        base.Die();
        DeleteDashFrameUI();
        StateSetting(ENormalState.Die, ConstValues.Die, ConstValues.Die);
        MoveStateSetting(EMoveState.Stopping);
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        gameObject.SetActive(false);
        Controller.Instance.StopMove();
    }
    
    public override void Airborne(float xVelocity, float yVelocity)
    {
        base.Airborne(xVelocity, yVelocity);
        if(GameManager.Instance.ControlStart && IsCanSkill($"{basicStat.id}_{ConstValues.Dash}") && !isDie)
            ActiveDashEffectUI();
    }
    
    public override void Damaged(float damagedTime)
    {
        base.Damaged(damagedTime);
        if(GameManager.Instance.ControlStart && IsCanSkill($"{basicStat.id}_{ConstValues.Dash}") && !isDie)
            ActiveDashEffectUI();
    }
    
    // 교체를 사용 할 수 있는가?
    private async UniTask<bool> IsCanChange()
    {
        var targetSkill = GameManager.Instance.ChangeSkill.playerSkill;
        if (targetSkill.IsOnCooldown)
        {
            var coolTimeList = targetSkill.GetRemainingCooldown();
            Debug.Log($"{targetSkill.id} 쿨타임 중: {coolTimeList[0]:F1}초 남음");
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
                Debug.Log($"{targetSkill.id} 기본 쿨타임 {coolTimeList[0]:F1}초 남음, 스택 쿨타임 {coolTimeList[1]:F1}초 남음, 남은 스택 개수 {coolTimeList[2]:F1}개");
            else
                Debug.Log($"{targetSkill.id} 쿨타임 중: {coolTimeList[0]:F1}초 남음");
            
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
        
        if(!GetSkillGlobalCoolTime())
        {
            Debug.Log("스킬 글로벌 쿨타임이 지나지 않음");
            return false;
        }
        
        return true;
    }

    protected void UseSkill(string id)
    {
        var targetSkill = GetSkill(id);
        Debug.Log($"{targetSkill.id} 사용!");
        targetSkill.SetCoolTime();
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
            PlaySound(ConstValues.Jump1, 2.0f);
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
                if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                    return;
            }
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 6.0f);
        }
    }
    // 아랫점프
    public async void DownJump()
    {
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }
        
        // 플랫폼 위에서만 작동함
        if(downJumping || groundObject == null || !groundObject.CompareTag(ConstValues.Platform) || IsDamaged() || normalState == ENormalState.JumpAttack)
            return;

        PlaySound(ConstValues.Jump2, 2.0f);
        CancelMotion();
        
        curGlobalCoolTime = 0;
        downJumping = true;

        IgnorePlatform(Vector2.down, 1.0f);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 3.0f);
        
        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        LandingStateSetting(ELandingState.Air);
        
        stateCancellation = new CancellationTokenSource();

        // 짧은 시간동안 좌우 움직임 봉인
        canMove = false;
        if (await NormalDelay(0.1f, stateCancellation).SuppressCancellationThrow())
        {
            canMove = true;
            return;
        }
        canMove = true;
        
        while (transform.position.y >= groundObject.transform.position.y)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        downJumping = false;
    }

    public async UniTask DialogDownJump()
    {
        await UniTask.WaitUntil(() => normalState == ENormalState.Idle);

        PlaySound(ConstValues.Jump2, 2.0f);
        CancelMotion();

        IgnorePlatform(Vector2.down, 1.0f);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 3.0f);
        
        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        LandingStateSetting(ELandingState.Air);

        stateCancellation = new CancellationTokenSource();
        while (transform.position.y >= groundObject.transform.position.y)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        await NormalDelay(1.0f, stateCancellation);
    }
    public async void JumpToChange()
    {
        stateCancellation = new CancellationTokenSource();
        while (transform.position.y < jumpLimitY)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 6.0f);
    }
    public async UniTask EntranceJump()
    {
        CancelMotion();
        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        LandingStateSetting(ELandingState.Air);
            
        jumpLimitY = transform.position.y + 2;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 20); 
            
        stateCancellation = new CancellationTokenSource();
        while (transform.position.y < jumpLimitY)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 6.0f);
        
        while (normalState != Idle)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
    }
    public async UniTask EntranceDown()
    {
        while (normalState != Idle)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
    }

    public void SetJumpState()
    {
        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        LandingStateSetting(ELandingState.Air);
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
            if (skill.caster != ConstValues.All && skill.caster != basicStat.id)
                continue;
            
            PlayerSkill addedSkill = new PlayerSkill();
            
            addedSkill.id = skill.id;
            var coolTimeArray = skill.coolTime.Split(',');
            foreach (var coolTime in coolTimeArray)
            {
                addedSkill.maxCoolTime.Add(float.Parse(coolTime));
                addedSkill.curCoolTime.Add(float.Parse(coolTime));
            }
            addedSkill.name = skill.name;
            addedSkill.explain = skill.explain;
            skillList.Add(addedSkill);
        }
    }
    
    public List<PlayerSkill> GetSkillList()
    {
        return skillList;
    }

    public PlayerSkill GetSkill(string id)
    {
        return skillList.Find(x => x.id == id);
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
        //immortal = true;
        myBoxCollider.enabled = false;
        StandHitBox();
        GravityChange(0);
        myRigidbody.linearVelocity = Vector2.zero;
        DeleteDashFrameUI();
        
        var dashSpeed = 15;
        var dashLength = 4.5f;
        
        if(transform.localScale.x > 0)
            chargeVector = new Vector2(transform.position.x + dashLength, transform.position.y);
        else
            chargeVector = new Vector2(transform.position.x - dashLength, transform.position.y);
        
        // 대시 이팩트 소환
        var dashEffect = SpawnObject($"{name}_{ConstValues.DashEffect}", transform);
        if (transform.localScale.x > 0)
            dashEffect.transform.position = new Vector3(dashEffect.transform.position.x - 1.5f, dashEffect.transform.position.y, dashEffect.transform.position.z);
        else
            dashEffect.transform.position = new Vector3(dashEffect.transform.position.x + 1.5f, dashEffect.transform.position.y, dashEffect.transform.position.z);
        
        var trace = dashEffect.GetComponent<Trace>();
        trace.enabled = true;

        // 돌진
        bool chargeFinish = await Charge(dashSpeed, 0.5f, dashLength, 0.5f);

        trace.enabled = false;
        ClearObjectList(normalObject, 0.3f);
        
        // 대시 끝
        //immortal = false;
        myBoxCollider.enabled = true;
        return chargeFinish;
    }

    public async UniTask WaitIdle()
    {
        await UniTask.WaitUntil(() => normalState == ENormalState.Idle);
    }
    // 커스텀
    public async UniTask EpisodeMove(Vector2 movePos, float speed, int finishDir)
    {
        Controller.Instance.isLeftMove = false;
        Controller.Instance.isRightMove = false;
        Stop();
        await UniTask.WaitUntil(() => normalState == ENormalState.Idle);
        
        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        
        Vector2 dir = Vector2.left;
        transform.localScale = reverseScale;
        if (transform.position.x < movePos.x)
        {
            dir = Vector2.right;
            transform.localScale = defaultScale;
        }

        stateCancellation = new CancellationTokenSource();
        while (Math.Abs(transform.position.x - movePos.x) > 0.1f)
        {
            // basicStat.moveSpeed
            if(normalState == ENormalState.Idle)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            
            CustomMoving_X(dir, speed);
            await FixedYieldDelay(stateCancellation);
        }

        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        switch (finishDir)
        {
            case -1:
                transform.localScale = reverseScale;
                break;
            case 1:
                transform.localScale = defaultScale;
                break;
        }
        Stop();
        StopVelocity();
    }

    protected void OnCollisionEnter2D(Collision2D col)
    {
        // 점프를 제외한 착지 관여
        if ((col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform)) && normalState != ENormalState.Dash && landingState == ELandingState.Air)
        {
            if (myRigidbody.gravityScale == 0 || myRigidbody.linearVelocityY is >= 0.05f or <= -0.05f)
                return;
            
            LandingStateSetting(ELandingState.Ground);
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;
            jumpAttackCount = 0;
            groundObject = col.gameObject;
    
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
            if (myRigidbody.gravityScale != 0 && myRigidbody.linearVelocityY is <= 0.05f and >= -0.05f)
                return;
            
            LandingStateSetting(ELandingState.Air);
            if (normalState is ENormalState.Idle or ENormalState.Move)
                StateSetting(ENormalState.Jump, ConstValues.JumpDown, ConstValues.JumpDown);

            // if (col.gameObject.CompareTag(ConstValues.Platform))
            //     IgnorePlatform();
        }
    }

    protected void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.SaveObject))
        {
            if (col.GetComponent<SaveObject>())
            {
                var saveObject = col.GetComponent<SaveObject>();
                saveObject.Expansion();
            }
        }
    }
    protected void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.SaveObject))
        {
            if (col.GetComponent<SaveObject>())
            {
                var saveObject = col.GetComponent<SaveObject>();
                saveObject.Reduce();
            }
        }
    }
}
