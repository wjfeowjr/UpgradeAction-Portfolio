using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class PlayerAnimations
{
    public string id;
    public string caster;
    public bool canFlip;
    public bool canMove;
    public float moveRatio;
}

[Serializable]
public class PlayerSkill
{
    public string id;
    public List<float> maxCoolTime = new List<float>();
    public List<float> curCoolTime = new List<float>();
    public List<string> buffName = new List<string>();
    public List<int> buffValue = new List<int>();
    public float buffTime;
    public int buffCount;
    public float skillSpeed;
    public string skillArmor;
    public string talk;
    public string explainTalk;

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
            curCoolTime[0] += Time.deltaTime;
        
        if (curCoolTime[0] > maxCoolTime[0])
            curCoolTime[0] = maxCoolTime[0];

        // 스택형 스킬이라면
        if (curCoolTime.Count > 1)
        {
            // 스택 개수가 적다면
            if ((int)curCoolTime[2] < maxCoolTime[2])
            {
                curCoolTime[1] += Time.deltaTime;
                // 스택 쿨타임이 다 차게 되면
                if (curCoolTime[1] >= maxCoolTime[1])
                {
                    // 스킬 스택이 1 차오르고
                    curCoolTime[2] += 1;
                    // 스택이 찼는데도 최대 스택에 도달하지 못한다면
                    if ((int)curCoolTime[2] < maxCoolTime[2])
                        // 스택 쿨타임을 0으로 바꾼다(다시 채워지도록)
                        curCoolTime[1] = 0;
                }
            }
            if ((int)curCoolTime[2] > maxCoolTime[2])
                curCoolTime[2] = maxCoolTime[2];
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
    // 벽 판정 몬스터 체크
    private RaycastHit2D wallBodyDownLeftHit;
    private RaycastHit2D wallBodyDownCenterHit;
    private RaycastHit2D wallBodyDownRightHit;

    // 벽판정 몬스터 체크 레이 위치
    private Vector2 leftDownWallBodyRayPos;
    private Vector2 centerDownWallBodyRayPos;
    private Vector2 rightDownWallBodyRayPos;

    // 벽판정 몬스터 체크 레이 길이
    private float wallBodyDownRayDistance;  // 하단

    // 하단 벽판정 몬스터 체크
    public bool isWallBodyLeft;
    public bool isWallBodyRight;
    public bool isWallBodyDown;
    
    private bool isChanging;
    private float jumpLimitY;
    protected bool attackBuffer;

    // 메트로배니아 점프 어시스트
    private float coyoteTimer;             // 땅을 벗어난 직후 점프 허용 잔여 시간
    private float jumpBufferTimer;         // 점프 입력 버퍼 잔여 시간
    private const float CoyoteTime = 0.10f;
    private const float JumpBufferTime = 0.12f;

    private CancellationTokenSource dashDelayCancellation;
    private CancellationTokenSource attackDelayCancellation;

    [SerializeField] protected PlayerStat myStat;  // 내 스텟(변동되어야 함)
    [SerializeField] protected List<PlayerSkill> originSkillList = new List<PlayerSkill>();
    [SerializeField] protected List<PlayerSkill> skillList = new List<PlayerSkill>();
    [SerializeField] protected List<PlayerAnimations> originAnimationList = new List<PlayerAnimations>();
    [SerializeField] protected List<PlayerAnimations> animationList = new List<PlayerAnimations>();
    
    [SerializeField] protected bool canAttack;
    [SerializeField] private bool canFlip;
    [SerializeField] private bool canMove;
    [SerializeField] private float moveRatio;
    [SerializeField] private GameObject dashEffectUI;
    [SerializeField] private GameObject dashFrameUI;
    [SerializeField] private GameObject waitCharacterUI;

    protected float globalCoolTime;
    protected float curGlobalCoolTime;
    
    private float dashDelay;
    private float curDashDelay;
    
    private float changeGlobalCoolTime;
    private float curChangeGlobalCoolTime;
    
    private float skillGlobalCoolTime;
    private float curSkillGlobalCoolTime;

    // 프로퍼티
    public int JumpAttackCount
    {
        get => jumpAttackCount;
        set => jumpAttackCount = value;
    }

    // 스킬
    public abstract void Skill(KeyCode skillKey);
    // 교체공격
    public abstract void ChangeAttack();
    
    protected override void Awake()
    {
        base.Awake();
        var colSize = physicsCollider.size;
        wallBodyDownRayDistance = colSize.y * 0.5f + 0.05f;

        globalCoolTime = 0.1f;
        dashDelay = 0.2f;
        changeGlobalCoolTime = 0.1f;
        skillGlobalCoolTime = 0.02f;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 최초 Idle상태로 전환
        StateSetting(ENormalState.Normal, ConstValues.Idle, ConstValues.Idle);
        myGravity = myRigidbody.gravityScale;
        canAttack = true;
    }

    private void Start()
    {
        SetDashUIObject();
    }

    protected override void Update()
    {
        base.Update();
        UpdateFlip();
        UpdateGlobalCoolTime();
        UpdateDashDelay();
        UpdateChangeGlobalCoolTime();
        UpdateSkillGlobalCoolTime();
        UpdateBuff();
        UpdateJumpAssist();

        // 스킬 추가 테스트
        if (Input.GetKeyDown(KeyCode.P))
        {
            GameManager.Instance.AddNewSkill(ConstValues.BerserkerCrash);
            GameManager.Instance.AddNewSkill(ConstValues.GunnerGrenade);
            GameManager.Instance.AddNewSkill(ConstValues.GunnerKnockBackShot);
            GameManager.Instance.AddNewSkill(ConstValues.GunnerCrazyShot);
            GameManager.Instance.AddNewSkill(ConstValues.GunnerElementalInfusion);
            GameManager.Instance.AddNewSkill(ConstValues.FighterLightningKick);
            GameManager.Instance.AddNewSkill(ConstValues.FighterLightningPunch);
            GameManager.Instance.AddNewSkill(ConstValues.FighterLightningSmash);
            GameManager.Instance.AddNewSkill(ConstValues.FighterStrongPunch);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            GameManager.Instance.RemoveSkill(ConstValues.BerserkerChargeCrash);
            GameManager.Instance.RemoveSkill(ConstValues.GunnerGrenade);
            GameManager.Instance.RemoveSkill(ConstValues.GunnerKnockBackShot);
            GameManager.Instance.RemoveSkill(ConstValues.GunnerCrazyShot);
            GameManager.Instance.RemoveSkill(ConstValues.GunnerElementalInfusion);
            GameManager.Instance.RemoveSkill(ConstValues.FighterLightningKick);
            GameManager.Instance.RemoveSkill(ConstValues.FighterLightningPunch);
            GameManager.Instance.RemoveSkill(ConstValues.FighterLightningSmash);
            GameManager.Instance.RemoveSkill(ConstValues.FighterStrongPunch);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        UpdateCameraLimit();
        UpdateJumpDown();
    }

    private void OnDisable()
    {
        stateCancellation?.Cancel();
    }

    protected override void CheckCollisions()
    {
        base.CheckCollisions();
        WallMonsterDownCheck();
    }

    protected override void LayerMaskSetting()
    {
        base.LayerMaskSetting();
        groundLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.WallBody));
    }

    // 하단 벽판정 몬스터 체크
    private void WallMonsterDownCheck()
    {
        leftDownWallBodyRayPos = (Vector2)physicCenterPos.position + new Vector2(-footOffset, 0);
        centerDownWallBodyRayPos = physicCenterPos.position;
        rightDownWallBodyRayPos = (Vector2)physicCenterPos.position + new Vector2(footOffset, 0);
        
        wallBodyDownLeftHit = Physics2D.Raycast(leftDownWallBodyRayPos, Vector2.down, wallBodyDownRayDistance, wallBodyLayerMask);
        wallBodyDownCenterHit = Physics2D.Raycast(centerDownWallBodyRayPos, Vector2.down, wallBodyDownRayDistance, wallBodyLayerMask);
        wallBodyDownRightHit = Physics2D.Raycast(rightDownWallBodyRayPos, Vector2.down, wallBodyDownRayDistance, wallBodyLayerMask);
        
        isWallBodyDown = wallBodyDownLeftHit || wallBodyDownCenterHit || wallBodyDownRightHit;
        if (isWallBodyDown)
        {
            if (myRigidbody.linearVelocityY > 0.01f)
                return;
            
            Character wallMonster = null;
            if (wallBodyDownLeftHit.collider != null)
                wallMonster = wallBodyDownLeftHit.collider.GetComponentInParent<Character>();
            if (wallBodyDownCenterHit.collider != null)
                wallMonster = wallBodyDownCenterHit.collider.GetComponentInParent<Character>();
            if (wallBodyDownRightHit.collider != null)
                wallMonster = wallBodyDownRightHit.collider.GetComponentInParent<Character>();

            if (wallMonster != null)
            {
                // 체공중 벽 타입 몬스터가 감지되었을 때
                if (landingState == ELandingState.Air && normalState != ENormalState.Dash)
                {
                    // 몹이 오른쪽을 보고있음
                    if (wallMonster.transform.localScale.x > 0)
                    {
                        if (transform.position.x > wallMonster.transform.position.x)
                        {
                            transform.position = new Vector2(wallMonster.ColFront(), transform.position.y - 0.1f);
                        }
                        else
                        {
                            transform.position = new Vector2(wallMonster.ColBehind(), transform.position.y - 0.1f);
                        }
                    }
                    // 몹이 왼쪽을 보고있음
                    else
                    {
                        if (transform.position.x > wallMonster.transform.position.x)
                        {
                            transform.position = new Vector2(wallMonster.ColBehind(), transform.position.y - 0.1f);
                            
                        }
                        else
                        {
                            transform.position = new Vector2(wallMonster.ColFront(), transform.position.y - 0.1f);
                            
                        }
                    }
                }
            }
        }
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
            name = GameManager.Instance.GetTalk(targetStat.talk),
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
                name = GameManager.Instance.GetTalk(targetStat.talk),
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
        
        if(myAnimator)
            myAnimator.SetFloat(ConstValues.IgnoreTime, Time.unscaledDeltaTime / Time.deltaTime);
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
                    normalState = Controller.Instance.IsMoving() ? ENormalState.Move : ENormalState.Idle;
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
                    SetTriggerAnimator(Controller.Instance.IsMoving() ? ConstValues.Move : ConstValues.Idle);
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
                    animId = Controller.Instance.IsMoving() ? ConstValues.Move : ConstValues.Idle;
                    break;
                case ELandingState.Air:
                    animId = ConstValues.JumpDown;
                    break;
            }
        }
        
        var animationsInfo = animationList.Find(x => x.id == animId && (x.caster == ConstValues.All || x.caster == basicStat.id));
        if (animationsInfo != null)
        {
            // 애니메이션 테이블을 체크하여, 해당 애니메이션 도중 전환, 이동이 가능한지 판단
            canFlip = animationsInfo.canFlip;
            canMove = animationsInfo.canMove;
            moveRatio = animationsInfo.moveRatio;
        }
        else
        {
            canFlip = false;
            canMove = false;
            moveRatio = 0;
        }
    }
    protected override void StateCheck()
    {
        var stun = buffList.Find(x => x.buffId is ConstValues.Stun);

        if (stun != null)
            basicStat.bodyType = originStat.bodyType;
    }
    protected override void StateRecovery()
    {
        var isCcState = buffList.FindAll(x => x.buffId is ConstValues.Stun or ConstValues.Stagger or ConstValues.Frozen);

        if (isCcState.Count == 0)
        {
            DeleteDashFrameUI();
            StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
        }
        else
        {
            // 스턴에 걸려있는 경우
            if (isCcState.Find(x => x.buffId == ConstValues.Stun) != null)
                StateSetting(ENormalState.Stun, ConstValues.Stun, ConstValues.Stun);

            // 빙결에 걸려있는 경우
            if (isCcState.Find(x => x.buffId == ConstValues.Frozen) != null)
                StateSetting(ENormalState.Frozen, ConstValues.Stun, ConstValues.Stun);
        }

        StandHitBox();
    }

    protected void MotionFlip()
    {
        if(GameManager.Instance.CurPlayer != this)
            return;
        
        switch (transform.localScale.x)
        {
            case > 0 when Controller.Instance.IsLeftMove:
                Flip(-1);
                break;
            case < 0 when Controller.Instance.IsRightMove:
                Flip(1);
                break;
        }
    }

    private void UpdateFlip()
    {
        if (!GameManager.Instance.ControlStart || !canFlip)
            return;

        MotionFlip();
    }
    
    private void UpdateJumpDown()
    {
        if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.Jump) && myRigidbody.linearVelocity.y < 0.01f)
            StateSetting(ENormalState.Jump, ConstValues.JumpDown, ConstValues.JumpDown);
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
    
    private void UpdateDashDelay()
    {
        if (curDashDelay < dashDelay)
            curDashDelay += Time.deltaTime;

        if (curDashDelay >= dashDelay)
            curDashDelay = dashDelay;
    }
    protected bool GetDashDelay()
    {
        bool isFill = curDashDelay >= dashDelay;
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
    public float GetUpPosY()
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
        waitCharacterUI = SpawnUI(ConstValues.WaitCharacterUI, uiInterface.GetWaitingCharacterPos());
        
        dashEffectUI.SetActive(false);
        dashFrameUI.SetActive(false);
        waitCharacterUI.SetActive(false);
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
        ClearIgnorePlatform();
        ClearObjectList(attackObject);
        delayCancellation?.Cancel();
    }

    private void UpdateCameraLimit()
    {
        if(!GameManager.Instance.ControlStart)
            return;
        
        // 1) 플레이어 절반 크기
        // float halfWidth  = physicsCollider.size.x * 0.5f;
        // float halfHeight = physicsCollider.size.y * 0.5f;
        float halfWidth  = 0;
        float halfHeight = 0;

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
    }

    // 플레이어 이동
    public void Move(Vector2 dir)
    {
        if (!canMove)
            return;

        float inputSpeedX = dir.x * basicStat.moveSpeed * (moveRatio * 0.01f);

        float platformSpeedX = 0f;
        if (landingState == ELandingState.Ground && currentMovingPlatform != null)
            platformSpeedX = currentMovingPlatform.Velocity.x;

        float targetSpeedY = myRigidbody.linearVelocity.y;

        if (inputSpeedX < 0 && (isWallLeft || isWallBodyLeft))
            inputSpeedX = 0;
        
        if (inputSpeedX > 0 && (isWallRight || isWallBodyRight))
            inputSpeedX = 0;

        myRigidbody.linearVelocity = new Vector2(inputSpeedX + platformSpeedX, targetSpeedY);
    }
    
    // 플레이어 공격
    public virtual async UniTask<bool> Attack()
    {
        if (normalState is ENormalState.Skill || IsDamaged() || downJumping)
        {
            Debug.Log("공격을 할 수 없는 상태임");
            return false;
        }

        // if(!GetGlobalCoolTime())
        // {
        //     Debug.Log("글로벌 쿨타임이 지나지 않음");
        //     return false;
        // }
        
        if ((normalState is ENormalState.Attack or ENormalState.JumpAttack) && !canAttack)
        {
            //Debug.Log("공격 진행중임");
            return false;
        }

        // 대시 도중 공격 입력으로 캔슬되는 경우, 서브클래스의 landingState 분기보다 먼저
        // 발 밑 플랫폼 판정을 갱신하여 공중공격이 잘못 발동되지 않도록 함
        if (normalState == ENormalState.Dash)
            DashEndLandingCheck();

        canAttack = false;
        curGlobalCoolTime = 0;
        //Debug.Log("공격 시작");
        return true;
    }

    public override void TakeDamage(int damage, bool isTrapAttack)
    {
        base.TakeDamage(damage, isTrapAttack);

        if (damage == 0)
            return;
        
        GameManager.Instance.SetPlayerHp(basicStat.hp);
        
        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;
        
        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        uiInterface.HpPresenter.SetHpText();
        uiInterface.HpPresenter.HpReduce();
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
        ClearIgnorePlatform();
    }
    
    public override async void Airborne(float xVelocity, float yVelocity)
    {
        // 가드절
        if (normalState is ENormalState.Grabbed or ENormalState.Frozen)
        {
            Debug.Log($"상위 판정이 존재함: {normalState}");
            return;
        }
        if(basicStat.hp > 0)
            PlaySound(ConstValues.PlayerDamaged1);
        
        base.Airborne(xVelocity, yVelocity);
        
        curDashDelay = 0f;
        dashDelayCancellation = new CancellationTokenSource();
        if(await NormalDelay(dashDelay, dashDelayCancellation).SuppressCancellationThrow())
            return;
        
        if(GameManager.Instance.ControlStart && IsCanSkill($"{basicStat.id}_{ConstValues.Dash}") && !isDie)
            ActiveDashEffectUI();
    }
    
    public override void Damaged(float damagedTime)
    {
        base.Damaged(damagedTime);
        if(basicStat.hp > 0)
            PlaySound(ConstValues.PlayerDamaged1);
        
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
        
        bool immediateChange = normalState is ENormalState.Idle or ENormalState.Move || (normalState is ENormalState.Jump && curChangeGlobalCoolTime >= changeGlobalCoolTime);
        if (!immediateChange)
        {
            waitCharacterUI.SetActive(true);
            PlaySound(ConstValues.Upgrade);
        }
        
        // 점프 도중에만 글로벌 쿨타임을 준다
        await UniTask.WaitUntil(()=> normalState is ENormalState.Idle or ENormalState.Move || (normalState is ENormalState.Jump && curChangeGlobalCoolTime >= changeGlobalCoolTime));
        waitCharacterUI.SetActive(false);
        
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
        if (id == ConstValues.GunnerGrenade && GameManager.Instance.IsHaveAttribute(id, ConstValues.MadBomber))
            return;
        
        var targetSkill = GetSkill(id);
        Debug.Log($"{targetSkill.id} 사용!"); 
        targetSkill.SetCoolTime();
    }

    // 공격 전진
    protected void AttackAdvance(float distance)
    {
        var dir = 0;
        
        // 오른쪽
        if (transform.localScale.x > 0 && Input.GetKey(GameManager.Instance.rightKey))
            dir = 1;
        // 왼쪽
        if (transform.localScale.x < 0 && Input.GetKey(GameManager.Instance.leftKey))
            dir = -1;
        
        if(dir != 0)
            myRigidbody.linearVelocity = dir * new Vector2(distance, myRigidbody.linearVelocity.y);
    }

    // 점프 입력 요청 (Controller에서 호출) — 버퍼에 등록만 하고 실제 발동은 UpdateJumpAssist에서
    public void RequestJump()
    {
        jumpBufferTimer = JumpBufferTime;
    }

    // 점프 (실제 발동) — 상태 검증은 호출자(UpdateJumpAssist)가 담당
    public async void Jump()
    {
        PlaySound(ConstValues.Jump1, 2.0f);
        jumpAttackCount = 0;
        CancelMotion();
        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        LandingStateSetting(ELandingState.Air);

        jumpLimitY = transform.position.y + myStat.jumpHeight;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 17.5f);

        // 코요테/버퍼 소비
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;

        float timer = 0.0f;
        float jumpTime = 0.125f;
        jumpCancellation = new CancellationTokenSource();
        while (timer < jumpTime)
        {
            timer += Time.fixedDeltaTime;
            if (await FixedYieldDelay(jumpCancellation).SuppressCancellationThrow())
            {
                Debug.Log("점프 캔슬");
                return;
            }
        }

        if (myRigidbody.linearVelocityY > 0)
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 6.0f);
    }

    // 점프 어시스트: 코요테 타임 + 점프 버퍼
    private void UpdateJumpAssist()
    {
        // 코요테 타이머: 땅에 있으면 매 프레임 갱신, 공중이면 감소
        if (landingState == ELandingState.Ground)
            coyoteTimer = CoyoteTime;
        else
            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);

        // 점프 버퍼 감소
        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);

        // 버퍼된 점프 소비 — 절벽에서 걸어 떨어진 경우(state=Jump이지만 코요테 잔여)도 허용
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && !downJumping && !IsDamaged()
            && normalState is ENormalState.Idle or ENormalState.Move or ENormalState.Attack or ENormalState.Jump)
        {
            Jump();
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
        
        // 플랫폼 위에서 '서있거나, 움직일때' 만 작동함
        if((normalState != ENormalState.Idle && normalState != ENormalState.Move) || downJumping || !isOnPlatform || IsDamaged() || normalState == ENormalState.JumpAttack)
            return;

        PlaySound(ConstValues.Jump2, 2.0f);
        CancelMotion();
        
        curGlobalCoolTime = 0;
        downJumping = true;
        
        IgnorePlatformCheck(lastStandPlatform, true);
        GravityChange(myGravity);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 3.0f);
        
        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        LandingStateSetting(ELandingState.Air);
        
        jumpCancellation = new CancellationTokenSource();

        // 짧은 시간동안 좌우 움직임 봉인
        canMove = false;
        if (await NormalDelay(0.1f, jumpCancellation).SuppressCancellationThrow())
        {
            canMove = true;
            return;
        }
        canMove = true;
        
        while (transform.position.y >= lastStandPlatform.height - 0.64f)
        {
            if (await FixedYieldDelay(jumpCancellation).SuppressCancellationThrow())
                return;
        }
        downJumping = false;
    }

    public async UniTask DialogDownJump()
    {
        await UniTask.WaitUntil(() => normalState == ENormalState.Idle);

        PlaySound(ConstValues.Jump2, 2.0f);
        CancelMotion();

        IgnorePlatformCheck(lastStandPlatform);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 3.0f);
        
        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
        LandingStateSetting(ELandingState.Air);

        jumpCancellation = new CancellationTokenSource();
        while (transform.position.y >= groundObject.transform.position.y)
        {
            if (await FixedYieldDelay(jumpCancellation).SuppressCancellationThrow())
                return;
        }
        await NormalDelay(1.0f, jumpCancellation);
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
        
        while (landingState != ELandingState.Ground)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
    }
    public async UniTask EntranceDown()
    {
        while (normalState != ENormalState.Idle)
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
        
        if(xVelocity == 0)
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, yVelocity);
        else
            myRigidbody.linearVelocity = new Vector2(xVelocity, yVelocity);
        
        stateCancellation = new CancellationTokenSource();
        while (transform.position.y < currentHeight + leapHeight)
        {
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        transform.position = new Vector2(transform.position.x, currentHeight + leapHeight);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 3.0f);
    }
    
    // 공격 딜레이
    protected async UniTask AttackDelay(float attackDelay)
    {
        float delay = 0;
        //float real = 0;
        while (delay < attackDelay)
        {
            delay += Time.deltaTime * basicStat.attackSpeed;
            //real += Time.deltaTime;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
        }
        //Debug.Log(real);
    }
    // 공격 딜레이(공속의 영향을 받지 않음)
    protected async UniTask AttackDelayNonAttackSpeed(float attackDelay)
    {
        float delay = 0;
        while (delay < attackDelay)
        {
            delay += Time.deltaTime;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
        }
    }
    // 시간값 무시 딜레이
    protected async UniTask IgnoreTimeDelay(float timeDelay)
    {
        float delay = 0;
        while (delay < timeDelay)
        {
            delay += Time.unscaledDeltaTime;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
        }
    }
    // 버퍼 딜레이
    protected async UniTask BufferDelay(float originDelay, float afterDelay)
    {
        float delay = 0;
        float maxDelay = originDelay + afterDelay;
        while (delay < maxDelay)
        {
            delay += Time.deltaTime * basicStat.attackSpeed;
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
            if (delay > originDelay && attackBuffer)
            {
                break;
            }
        }
    }

    // 공격 체커
    protected async void AttackChecker(float startDelay, float endDelay)
    {
        attackDelayCancellation = new CancellationTokenSource();
        
        float delay = 0;
        while (delay < startDelay)
        {
            delay += Time.deltaTime * basicStat.attackSpeed;
            await UniTask.Yield(cancellationToken: attackDelayCancellation.Token);
        }
        
        while (delay < endDelay)
        {
            delay += Time.deltaTime * basicStat.attackSpeed;
            await UniTask.Yield(cancellationToken: attackDelayCancellation.Token);
            if (Input.GetKeyDown(GameManager.Instance.attackKey))
            {
                attackBuffer = true;
                break;
            }
        }
    }
    
    // 스킬 시전 시 적용되는 스킬 시전속도와 바디타입 체크
    protected void SkillSpeedAndArmorCheck(string skillId)
    {
        var skillData = skillList.Find(x => x.id == skillId);
        SetAttackSpeed(skillData.skillSpeed);
        var armor = skillData.skillArmor;
        if (originStat.bodyType == EBodyType.SuperArmor && armor == ConstValues.Normal)
            armor = ConstValues.SuperArmor;
        
        BodyTypeSetting((EBodyType)Enum.Parse(typeof(EBodyType), armor));
    }

    public void InitSkill()
    {
        foreach (var skill in TableManager.Instance.skillTable.Skill)
        {
            if (skill.caster != ConstValues.All && skill.caster != basicStat.id)
                continue;
            
            PlayerSkill addedSkill = new PlayerSkill();
            addedSkill.id = skill.id;
            var coolTimeArray = skill.coolTime.Split(';');
            foreach (var coolTime in coolTimeArray)
            {
                addedSkill.maxCoolTime.Add(float.Parse(coolTime));
                addedSkill.curCoolTime.Add(float.Parse(coolTime));
            }

            if (!string.IsNullOrWhiteSpace(skill.buffName))
            {
                var buffNameArray = skill.buffName.Split(';');
                foreach (var buff in buffNameArray)
                    addedSkill.buffName.Add(buff);
            }

            addedSkill.buffTime = skill.buffTime;
            addedSkill.buffCount = skill.buffCount;
            
            var buffValueArray = skill.buffValue.Split(';');
            foreach (var buffValue in buffValueArray)
                addedSkill.buffValue.Add(int.Parse(buffValue));
            
            var skillTableData = TableManager.Instance.skillTable.Skill.Find(x => x.id == skill.id);
            addedSkill.skillSpeed = skillTableData.skillSpeed;
            addedSkill.skillArmor = skillTableData.skillArmor;

            addedSkill.talk = GameManager.Instance.GetTalk(skill.talk);;
            addedSkill.explainTalk = GameManager.Instance.GetTalk(skill.explainTalk);
            originSkillList.Add(addedSkill);
        }

        foreach (var originSkill in originSkillList)
        {
            PlayerSkill addedSkill = new PlayerSkill();
            addedSkill.id = originSkill.id;
            addedSkill.maxCoolTime = originSkill.maxCoolTime.ToList();
            addedSkill.curCoolTime = originSkill.curCoolTime.ToList();
            addedSkill.buffName = originSkill.buffName.ToList();
            addedSkill.buffTime = originSkill.buffTime;
            addedSkill.buffCount = originSkill.buffCount;
            addedSkill.buffValue = originSkill.buffValue.ToList();
            addedSkill.talk = originSkill.talk;
            addedSkill.explainTalk = originSkill.explainTalk;
            addedSkill.skillSpeed = originSkill.skillSpeed;
            addedSkill.skillArmor = originSkill.skillArmor;
            skillList.Add(addedSkill);
        }

    }

    public void InitAnimation()
    {
        foreach (var animations in TableManager.Instance.animationsTable.Animations)
        {
            var data = new PlayerAnimations();
            data.id = animations.id;
            data.caster = animations.caster;
            data.canFlip = animations.canFlip;
            data.canMove = animations.canMove;
            data.moveRatio = animations.moveRatio;
            originAnimationList.Add(data);
        }
        
        foreach (var originAnimations in originAnimationList)
        {
            var data = new PlayerAnimations();
            data.id = originAnimations.id;
            data.caster = originAnimations.caster;
            data.canFlip = originAnimations.canFlip;
            data.canMove = originAnimations.canMove;
            data.moveRatio = originAnimations.moveRatio;
            animationList.Add(data);
        }
    }

    // 스킬 특성 체크 및 초기화
    public void SkillAttributeCheck()
    {
        // 애니메이션 데이터 초기화
        for (var i = 0; i < originAnimationList.Count; i++)
        {
            animationList[i].canFlip = originAnimationList[i].canFlip;
            animationList[i].canMove = originAnimationList[i].canMove;
            animationList[i].moveRatio = originAnimationList[i].moveRatio;
        }

        for (var i = 0; i < originSkillList.Count; i++)
        {
            // 스택에서 일반으로 바꼈을 경우
            if (skillList[i].maxCoolTime.Count > originSkillList[i].maxCoolTime.Count)
            {
                var curCoolTime = skillList[i].curCoolTime[1];
                bool isStack = skillList[i].curCoolTime[2] > 0;

                skillList[i].curCoolTime[1] = skillList[i].maxCoolTime[1];
                skillList[i].curCoolTime[2] = skillList[i].maxCoolTime[2];
                
                skillList[i].maxCoolTime = originSkillList[i].maxCoolTime.ToList();
                skillList[i].curCoolTime = originSkillList[i].curCoolTime.ToList();
                
                if(isStack)
                    skillList[i].curCoolTime[0] = skillList[i].maxCoolTime[0];
                else
                    skillList[i].curCoolTime[0] = curCoolTime;
            }

            // 쿨타임 보정
            bool isCoolTimeFill;
            if (skillList[i].maxCoolTime.Count > 1)
            {
                isCoolTimeFill = (int)skillList[i].maxCoolTime[1] == (int)skillList[i].curCoolTime[1];
            }
            else
            {
                isCoolTimeFill = (int)skillList[i].maxCoolTime[0] == (int)skillList[i].curCoolTime[0];
            }
            
            skillList[i].maxCoolTime = originSkillList[i].maxCoolTime.ToList();
            
            if (skillList[i].maxCoolTime.Count > 1)
            {
                if (isCoolTimeFill)
                    skillList[i].curCoolTime[1] = skillList[i].maxCoolTime[1];
                
                if ((int)skillList[i].maxCoolTime[2] <= (int)skillList[i].curCoolTime[2])
                    skillList[i].curCoolTime[1] = (int)skillList[i].maxCoolTime[1];
            }
            else
            {
                if (isCoolTimeFill)
                    skillList[i].curCoolTime[0] = skillList[i].maxCoolTime[0];
            }
            
            // 버프 시간 및 횟수 보정
            skillList[i].buffTime = originSkillList[i].buffTime;
            skillList[i].buffCount = originSkillList[i].buffCount;
            
            // 바디타입 및 스킬속도 보정
            skillList[i].skillSpeed = originSkillList[i].skillSpeed;
            skillList[i].skillArmor = originSkillList[i].skillArmor;
        }
        
        foreach (var skill in skillList)
        {
            var passiveList = GameManager.Instance.GetAttributePassive(skill.id);
            foreach (var passive in passiveList)
            {
                switch (passive)
                {
                    // 슈퍼아머
                    case ConstValues.SuperArmor:
                        skill.skillArmor = ConstValues.SuperArmor;
                        break;
                }
            }

            var upgradeList = GameManager.Instance.GetAttributeUpgrade(skill.id);
            foreach (var upgrade in upgradeList)
            {
                switch (upgrade.upgradeId)
                {
                    // 쿨타임 감소
                    case ConstValues.CoolTimeReduce:
                        if (skill.maxCoolTime.Count > 1)
                            skill.maxCoolTime[1] -= upgrade.upgradeValue;
                        else
                            skill.maxCoolTime[0] -= upgrade.upgradeValue;
                        break;
                    // 쿨타임 증가
                    case ConstValues.CoolTimeIncrease:
                        if (skill.maxCoolTime.Count > 1)
                            skill.maxCoolTime[1] += upgrade.upgradeValue;
                        else
                            skill.maxCoolTime[0] += upgrade.upgradeValue;
                        break;
                    // 스택 증가
                    case ConstValues.StackUp:
                        if (skill.maxCoolTime.Count > 2)
                        {
                            var stackFill = (int)skill.curCoolTime[2] >= (int)skill.maxCoolTime[2];
                            skill.maxCoolTime[2] += upgrade.upgradeValue;
                            if (stackFill)
                            {
                                skill.curCoolTime[1] = skill.maxCoolTime[1];
                                skill.curCoolTime[2] = skill.maxCoolTime[2];
                            }
                        }
                        else
                        {
                            var sameCoolTime = (int)skill.curCoolTime[0] >= (int)skill.maxCoolTime[0];
                            
                            float maxCoolTime = skill.maxCoolTime[0];
                            skill.maxCoolTime.Add(maxCoolTime);
                            skill.maxCoolTime.Add(1);
                            skill.maxCoolTime[0] = 0.1f;
                            skill.maxCoolTime[2] += upgrade.upgradeValue;
                            
                            float curCoolTime = skill.curCoolTime[0];
                            skill.curCoolTime.Add(curCoolTime);
                            if(sameCoolTime)
                                skill.curCoolTime.Add(skill.maxCoolTime[2]);
                            else
                                skill.curCoolTime.Add(0);
                            
                            skill.curCoolTime[0] = skill.maxCoolTime[0];
                        }
                        break;
                    
                    // 시전속도 증가
                    case ConstValues.SpeedUp:
                        skill.skillSpeed += skill.skillSpeed * (upgrade.upgradeValue * 0.01f);
                        break;
                    
                    // 버프 횟수 증가
                    case ConstValues.BuffCountUp:
                        skill.buffCount += upgrade.upgradeValue;
                        break;
                }
            }
        }

        foreach (var animations in animationList)
        {
            var upgradeList = GameManager.Instance.GetAttributeUpgrade(animations.id);
            foreach (var upgrade in upgradeList)
            {
                switch (upgrade.upgradeId)
                {
                    // 이동 가능
                    case ConstValues.CanMove:
                        animations.canMove = true;
                        animations.moveRatio = upgrade.upgradeValue;
                        break;
                }
            }
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

    public void ReceiveChangeData(Player player)
    {
        currentMovingPlatform = player.currentMovingPlatform;
        if (player.landingState == ELandingState.Ground)
            landingState = ELandingState.Ground;
    }

    // 대시
    protected async UniTask<bool> Dash()
    {
        jumpCancellation?.Cancel();
        StateSetting(ENormalState.Dash, ConstValues.Dash, ConstValues.Dash);
        immortal = true;
        myBoxCollider.enabled = false;
        StandHitBox();
        GravityChange(0);
        myRigidbody.linearVelocity = Vector2.zero;
        DeleteDashFrameUI();
        
        var dashSpeed = 20; // 15
        var dashLength = 4.0f; // 4.5
        
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

        Trace traceLightning = null;
        switch (basicStat.id)
        {
            case ConstValues.Fighter:
                var lightningEffect = SpawnObject(ConstValues.FighterLightningTrail, centerPos);
                if (transform.localScale.x > 0)
                    lightningEffect.transform.position = new Vector3(centerPos.position.x - 1.5f, centerPos.position.y, centerPos.position.z);
                else
                    lightningEffect.transform.position = new Vector3(centerPos.position.x + 1.5f, centerPos.position.y, centerPos.position.z);

                traceLightning = lightningEffect.GetComponent<Trace>();
                traceLightning.enabled = true;
                break;
        }

        // 돌진
        bool chargeFinish = await Charge(dashSpeed, 0.5f, dashLength, 0.5f);
        
        trace.enabled = false;
        if (traceLightning)
            traceLightning.enabled = false;
        
        ClearObjectList(normalObject, 0.3f);
        
        // 대시 끝
        immortal = false;
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
        Controller.Instance.IsLeftMove = false;
        Controller.Instance.IsRightMove = false;

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
        await FixedYieldDelay(stateCancellation);
        
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
        StopVelocity_X();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!GameManager.Instance.ControlStart)
            return;

        // 상호작용
        if (col.CompareTag(ConstValues.Interaction))
        {
            if (col.GetComponent<InteractionController>())
            {
                var controller = col.GetComponent<InteractionController>();
                controller.SpawnInteractionObject();
                controller.IsPlayerTouch = true;
            }
        }

        // Npc 상호작용
        if (col.CompareTag(ConstValues.Npc))
        {
            if (col.GetComponent<Npc>())
            {
                var npc = col.GetComponent<Npc>();
                npc.SpawnInteractionObject();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if(!GameManager.Instance.InGame)
            return;
        
        // 상호작용
        if (col.CompareTag(ConstValues.Interaction))
        {
            if (col.GetComponent<InteractionController>())
            {
                var controller = col.GetComponent<InteractionController>();
                controller.ReduceInteractionObject();
                controller.IsPlayerTouch = false;
            }
        }

        // 대화 상호작용
        if (col.CompareTag(ConstValues.Npc))
        {
            if (col.GetComponent<Npc>())
            {
                var npc = col.GetComponent<Npc>();
                npc.ReduceInteractionObject();
            }
        }
    }
}
