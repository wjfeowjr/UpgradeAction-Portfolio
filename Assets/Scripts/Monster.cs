using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

// 실제 이동 관련
public enum EAgroState
{
    Normal,
    Agro,
    Return,
}

// 보스타입
public enum EMonsterType
{
    Normal,
    MiniBoss,
    Boss,
    HiddenBoss,
}

[Serializable]
public class MonsterStat
{
    public bool standMotion;
    public float appearDelay;
    public float firstCoolTime;
    public float globalCoolTime;
    public int gold;
    public List<Vector2> attackRange = new List<Vector2>();
    public Vector2 agroRange;
    public Vector2 jumpRange;
    public Vector2 dropRange;

    public List<float> coolTime = new List<float>();
    public List<int> priority = new List<int>();
    public List<MonsterPattern> pattern = new List<MonsterPattern>();
    public float traceLength;
    public bool hovering;
    public List<float> hoveringHeight = new List<float>();
    public float hoveringSpeed;
    public float customPatrol;
    public List<float> appearShake = new List<float>();
    public string appearEffect;
    public string dyingMiniEffect;
    public string dyingEffect;
}

[Serializable]
public class MonsterPattern
{
    public int pageHp;
    public List<int> pagePattern = new List<int>();
}

[Serializable]
public class MonsterPatternClass
{
    public Vector2 attackRange; // 공격 인지범위
    public bool playerInAttackRange; // 플레이어가 공격 인지범위 안에 들어왔는가?
    public int priority; // 패턴의 우선순위
    public bool canPattern; // 패턴사용 가능여부
    public float patternCoolTime; // 패턴의 쿨타임
}

[Serializable]
public class JumpAndDropClass
{
    public float coolTime; // 쿨타임
    public bool playerInRange; // 플레이어가 범위 안에 들어왔는가?
}

public class Monster : Character
{
    [SerializeField] protected EAgroState agroState;
    [SerializeField] protected EMonsterType monsterType; // 보스인가?
    
    [SerializeField] private bool alwaysAgro; // 
    [SerializeField] protected bool isExplosion; // 죽을때 터지면서 죽는가?
    [SerializeField] protected MonsterStat myStat; // 내 스텟(변동되어야 함)
    [SerializeField] public List<MonsterPatternClass> patternInfo; // 스킬의 우선도와 스킬 사용 가능 여부

    [SerializeField] private JumpAndDropClass jumpInfo;
    [SerializeField] private JumpAndDropClass downJumpInfo;

    [SerializeField] private int currentSkillIdx;
    [SerializeField] private List<int> patternIdx = new List<int>(); // 선발된 패턴리스트
    [SerializeField] private int page;
    [SerializeField] private float curGlobalCoolTime;

    [SerializeField] private Transform hpBarPos;
    [SerializeField] protected TotalBar totalBar;
    [SerializeField] private SpriteRenderer[] appearMotions; // 등장 연출 이미지
    private SortingGroup sortingGroup;
    
    protected Action<int, Vector2> goldAction;

    [SerializeField] protected Vector2 startPos;
    private float leftPatrol;
    private float rightPatrol;

    private float traceDelay = 0.5f;
    private float currentTraceDelay = 0.5f;

    private float agroTime = 5.0f;
    private float currentAgroTime;
    private bool playerInAgroRange;

    private float leapHeight;
    protected float arriveHeight;

    private bool firstPatrol = false;
    private bool patrolMoving = false;
    private float patrolTimer = 0f;

    private float limitLeft;
    private float limitRight;

    // 프로퍼티
    public bool IsHovering
    {
        get => myStat.hovering;
    }

    public bool IsExplosion
    {
        get => isExplosion;
        set => isExplosion = value;
    }

    public bool AlwaysAgro
    {
        get => alwaysAgro;
        set => alwaysAgro = value;
    }

    public EMonsterType MonsterType
    {
        get => monsterType;
        set => monsterType = value;
    }

    public bool IsBoss => monsterType is EMonsterType.MiniBoss or EMonsterType.Boss or EMonsterType.HiddenBoss;

    public float LimitLeft
    {
        get => limitLeft;
        set => limitLeft = value;
    }

    public float LimitRight
    {
        get => limitRight;
        set => limitRight = value;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        InitBasicStat();
        FirstState();
        InitBonusStat();
        GravityChange(myGravity);
        startPos = transform.position;
        leftPatrol = transform.position.x - myStat.customPatrol;
        rightPatrol = transform.position.x + myStat.customPatrol;
        
        if (!IsHovering)
            return;

        CheckLeapHeight();
    }

    protected override void Update()
    {
        if (isDie || basicStat.hp <= 0 || !GameManager.Instance.ControlStart)
            return;

        base.Update();

        if (alwaysAgro)
        {
            Trace();
            Move();
        }
        else
        {
            switch (agroState)
            {
                case EAgroState.Normal:
                    Patrol();
                    break;
                case EAgroState.Agro:
                    Trace();
                    Move();
                    //MonsterLeap();
                    break;
                case EAgroState.Return:
                    ReturnMoving();
                    //MonsterLeap();
                    break;
            }
            AgroSystem();
        }

        UpdateTraceDelay();
        UpdateHoveringLeap();
        UpdateGlobalCoolTime();
        PatternCoolTimeReduce();
        UpdateBuff();
        PatternCycle();

        // 점프 관련
        //MonsterJump();
        //MonsterDownJump();
        //MonsterBridgeJump();
        JumpCoolTimeReduce();
        DownJumpCoolTimeReduce();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (!GameManager.Instance.ControlStart)
            return;
        
        PlayerInAgroRangeCheck();

        if (agroState == EAgroState.Agro)
        {
            PlayerInAttackRangeCheck();
            PlayerInJumpRangeCheck();
            PlayerInDropRangeCheck();
        }
        // UpdateStandingCheck();
        
        UpdateRoomLimit();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        ClearObjectList(attackObject);
        ClearObjectList(controlAttackObject);
        ClearObjectList(normalObject);
        ClearObjectList(buffObject);

        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Ground);

        if (!totalBar)
            return;

        totalBar.gameObject.SetActive(false);
        totalBar = null;
    }

    protected override void OnDrawGizmos()
    {
        if (isDie)
            return;

        base.OnDrawGizmos();

        var myPosition = transform.position;

        if (myStat.agroRange != Vector2.zero)
        {
            Vector2 agroRange = new Vector2(myStat.agroRange.x * 2, myStat.agroRange.y * 2);
            Gizmos.color = ConstValues.BlueColor;
            Gizmos.DrawWireCube(new Vector2(myPosition.x, myPosition.y + CenterPos.localPosition.y), agroRange);
        }

        // if (myStat.jumpRange != Vector2.zero)
        // {
        //     Vector2 jumpRange = new Vector2(myStat.jumpRange.x * 2, myStat.jumpRange.y * 2);
        //     Gizmos.color = ConstValues.CyanColor;
        //     Gizmos.DrawWireCube(new Vector2(myPosition.x, myPosition.y + myStat.jumpRange.y), jumpRange);
        // }
        //
        // if (myStat.dropRange != Vector2.zero)
        // {
        //     Vector2 dropRange = new Vector2(myStat.dropRange.x * 2, myStat.dropRange.y * 2);
        //     Gizmos.color = ConstValues.MagentaColor;
        //     Gizmos.DrawWireCube(new Vector2(myPosition.x, myPosition.y - myStat.dropRange.y), dropRange);
        // }

        if (patternInfo == null)
            return;

        for (var i = 0; i < patternInfo.Count; i++)
        {
            Vector3 patternRange = new Vector3(patternInfo[i].attackRange.x * 2, patternInfo[i].attackRange.y * 2, 1);

            switch (i)
            {
                case 0:
                    Gizmos.color = ConstValues.RedColor;
                    break;
                case 1:
                    Gizmos.color = ConstValues.OrangeColor;
                    break;
                case 2:
                    Gizmos.color = ConstValues.YellowColor;
                    break;
                case 3:
                    Gizmos.color = ConstValues.GreenColor;
                    break;
                case 4:
                    Gizmos.color = ConstValues.BlueColor;
                    break;
            }
            
            Gizmos.DrawWireCube(new Vector2(myPosition.x, myPosition.y + physicsCollider.size.y * 0.5f), patternRange);
        }
    }

    protected override void CeilingCheck()
    {
        if (myStat.hovering)
            return;

        base.CeilingCheck();
    }

    protected override void GroundCheck()
    {
        if (myStat.hovering)
            return;

        base.GroundCheck();
    }

    protected override void GetOffGroundState()
    {
        if (myStat.hovering)
            return;
        
        LandingStateSetting(ELandingState.Air);
        if (normalState is ENormalState.Idle or ENormalState.Move)
            StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
    }

    // 테이블의 값으로 스텟 초기화(기본 스텟)
    private void InitBasicStat()
    {
        var myName = name.Split(' ')[0];
        var targetStat = TableManager.Instance.monsterTable.Monster.Find(x => x.id == myName);
        if (targetStat == null)
        {
            myName = name.Split('(')[0];
            targetStat = TableManager.Instance.monsterTable.Monster.Find(x => x.id == myName);
        }

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
            maxStagger = targetStat.stagger,
            staggerTime = targetStat.staggerTime,
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
                maxStagger = targetStat.stagger,
                staggerTime = targetStat.staggerTime,
            };
        }

        // 파싱을 다르게 해야 하는 데이터 존재
        if (string.IsNullOrEmpty(myStat.appearEffect))
        {
            myStat = new MonsterStat();
            myStat.standMotion = targetStat.standMotion;
            myStat.appearDelay = targetStat.appearDelay;
            myStat.firstCoolTime = targetStat.firstCoolTime;
            myStat.globalCoolTime = targetStat.globalCoolTime;
            myStat.gold = targetStat.gold;

            var totalAttackRangeArray = targetStat.attackRange.Split('ㅗ');
            foreach (var totalRange in totalAttackRangeArray)
            {
                var attackRangeArray = totalRange.Split(';');
                Vector2 attackRange = new Vector2(float.Parse(attackRangeArray[0]), float.Parse(attackRangeArray[1]));
                myStat.attackRange.Add(attackRange);
            }

            var agroRangeArray = targetStat.agroRange.Split(';');
            myStat.agroRange = new Vector2(float.Parse(agroRangeArray[0]), float.Parse(agroRangeArray[1]));

            var jumpRangeArray = targetStat.jumpRange.Split(';');
            myStat.jumpRange = new Vector2(float.Parse(jumpRangeArray[0]), float.Parse(jumpRangeArray[1]));

            var dropRangeArray = targetStat.dropRange.Split(';');
            myStat.dropRange = new Vector2(float.Parse(dropRangeArray[0]), float.Parse(dropRangeArray[1]));

            var coolTimeArray = targetStat.coolTime.Split(';');
            foreach (var coolTime in coolTimeArray)
                myStat.coolTime.Add(float.Parse(coolTime));

            var priorityArray = targetStat.priority.Split(';');
            foreach (var priority in priorityArray)
                myStat.priority.Add(int.Parse(priority));

            var pageHpArray = targetStat.pageHp.Split(';');
            var pagePatternArray = targetStat.pagePattern.Split('ㅗ');
            for (var i = 0; i < pagePatternArray.Length; i++)
            {
                MonsterPattern monsterPattern = new MonsterPattern();
                var patternArray = pagePatternArray[i].Split(';');
                List<int> pagePatternList = new List<int>();
                foreach (var pattern in patternArray)
                    pagePatternList.Add(int.Parse(pattern));

                monsterPattern.pageHp = int.Parse(pageHpArray[i]);
                monsterPattern.pagePattern = pagePatternList;
                myStat.pattern.Add(monsterPattern);
            }

            // 패턴 정보
            if (patternInfo.Count == 0)
            {
                patternInfo = new List<MonsterPatternClass>();
                for (var i = 0; i < myStat.coolTime.Count; i++)
                {
                    MonsterPatternClass monsterPatternClass = new MonsterPatternClass();
                    monsterPatternClass.attackRange = myStat.attackRange[i];
                    monsterPatternClass.playerInAttackRange = false;
                    monsterPatternClass.priority = myStat.priority[i];
                    monsterPatternClass.canPattern = false;
                    monsterPatternClass.patternCoolTime = 0;
                    patternInfo.Add(monsterPatternClass);
                }
            }
            else
            {
                // 재생성 시 초기화 되는 상태들
                for (int i = 0; i < myStat.coolTime.Count; i++)
                {
                    patternInfo[i].playerInAttackRange = false;
                    patternInfo[i].canPattern = false;
                    patternInfo[i].patternCoolTime = 0;
                }
            }

            myStat.traceLength = targetStat.traceLength;
            myStat.hovering = targetStat.hovering;
            myGravity = myStat.hovering ? 0 : 1;

            var hoveringHeightArray = targetStat.hoveringHeight.Split(';');
            foreach (var hoveringHeight in hoveringHeightArray)
                myStat.hoveringHeight.Add(float.Parse(hoveringHeight));

            myStat.hoveringSpeed = targetStat.hoveringSpeed;
            myStat.customPatrol = targetStat.customPatrol;

            var appearShakeArray = targetStat.appearShake.Split(';');
            foreach (var appearShake in appearShakeArray)
                myStat.appearShake.Add(float.Parse(appearShake));

            myStat.appearEffect = targetStat.appearEffect;
            myStat.dyingMiniEffect = targetStat.dyingMiniEffect;
            myStat.dyingEffect = targetStat.dyingEffect;
        }

        foreach (var pattern in patternInfo)
            pattern.playerInAttackRange = false;
    }

    private void FirstState()
    {
        curGlobalCoolTime = 0;
        isDie = false;

        agroState = alwaysAgro ? EAgroState.Agro : EAgroState.Normal;
        if (myStat.hovering)
            landingState = ELandingState.Air;
        else
            landingState = ELandingState.Ground;
    }

    public async void SpawnHpBar()
    {
        if (IsBoss)
        {
            delayCancellation = new CancellationTokenSource();
            if(await GameManager.Instance.WaitUntilDelay(() => GameManager.Instance.ControlStart, delayCancellation).SuppressCancellationThrow())
                return;

            var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
            if (uiInterfaceObj == null)
                return;

            var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
            var bossHpModel = new UIBossHpModel()
            {
                character = this
            };
            uiInterface.BossHpPresenter.SetModel(bossHpModel);
            uiInterface.BossHpPresenter.SetHp();
            uiInterface.BossHpPresenter.SetHpText();
            uiInterface.BossHpPresenter.SetStagger();
        }
        else
        {
            if (!gameObject.activeSelf)
                return;

            if (totalBar)
                totalBar.gameObject.SetActive(true);
            else
                totalBar = SpawnUIObject(ConstValues.TotalBar, hpBarPos).GetComponent<TotalBar>();
            
            totalBar.SetCastCharacter(this);
        }
    }

    public void SetGoldAction(Action<int, Vector2> action)
    {
        goldAction = action;
    }

    public void SetSortingGroup(int value)
    {
        if (!sortingGroup)
            sortingGroup = transform.AddComponent<SortingGroup>();
        
        sortingGroup.sortingOrder = value;
    }

    protected override void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
    {
        normalState = changeNormalState;
        SetTriggerAnimator(triggerName);
        StopVelocity_X();
    }

    public void IdleOrMove()
    {
        // 원거리몹
        if (myStat.standMotion)
        {
            if (patternInfo[0].playerInAttackRange)
            {
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
                MoveStateSetting(EMoveState.Stopping);
            }
            else
            {
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                MoveStateSetting(EMoveState.Moving);
            }
        }
        // 근접몹
        else
        {
            if (basicStat.moveSpeed == 0)
            {
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
                MoveStateSetting(EMoveState.Stopping);
            }
            else
            {
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                MoveStateSetting(EMoveState.Moving);
            }
        }
    }

    private void CheckLeapHeight()
    {
        Vector2 rayVector = transform.position;
        float distance = 30;
        RaycastHit2D downRay = Physics2D.Raycast(rayVector, Vector2.down, distance, groundAndPlatformLayerMask);
        Debug.DrawRay(rayVector, Vector2.down * distance, ConstValues.RedColor, 0.1f);
        if (downRay.collider == null)
            leapHeight = 3.0f;
        else
            leapHeight = transform.position.y - downRay.point.y;
    }

    // 호버링 높이까지 도약
    protected virtual void HoveringLeap()
    {
        // 위쪽 레이캐스트 사용 (원본 사이즈만큼)
        Vector2 rayVector = transform.position;
        float distance = leapHeight;
        RaycastHit2D upRay = Physics2D.Raycast(rayVector, Vector2.up, distance, groundLayerMask);
        Debug.DrawRay(rayVector, Vector2.up * distance, ConstValues.RedColor, 0.1f);
        if (upRay.collider == null)
        {
            arriveHeight = transform.position.y + leapHeight;
        }
        else
        {
            arriveHeight = upRay.point.y - myBoxCollider.size.y * 0.5f - myBoxCollider.offset.y - 0.2f;
            if (arriveHeight > startPos.y)
                arriveHeight = startPos.y;
        }
        
        // 시작지점과 차이가 별로 나지 않으면 씹는 코드 추가
        StateSetting(ENormalState.Leap, ConstValues.Leap, ConstValues.Leap);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Air);
    }

    protected override void StateCheck()
    {
        var armorBreakOrStagger = buffList.FindAll(x => x.buffId is ConstValues.ArmorBreak or ConstValues.Stagger);

        if (armorBreakOrStagger.Count == 0)
        {
            //immuneStagger = false;
            basicStat.bodyType = originStat.bodyType;
            basicStat.stagger = basicStat.maxStagger;
            curGlobalCoolTime = 0;
            SetStagger();
        }
    }

    protected override void StateRecovery()
    {
        var isCcState = buffList.FindAll(x => x.buffId is ConstValues.Stun or ConstValues.Stagger or ConstValues.Frozen);
        if (isCcState.Count == 0)
        {
            switch (landingState)
            {
                case ELandingState.Air:
                    if (myStat.hovering)
                        IdleOrMove();
                    else
                        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
                    break;
                case ELandingState.Ground:
                    if (myStat.hovering)
                        HoveringLeap();
                    else
                        IdleOrMove();
                    break;
            }
        }
        else
        {
            // 스턴에 걸려있는 경우
            if (isCcState.Find(x => x.buffId == ConstValues.Stun) != null)
                StateSetting(ENormalState.Stun, ConstValues.Stun, ConstValues.Stun);

            // 무력화에 걸려있는 경우
            if (isCcState.Find(x => x.buffId == ConstValues.Stagger) != null)
                StateSetting(ENormalState.Stagger, ConstValues.Stagger, ConstValues.Stagger);
            
            // 빙결에 걸려있는 경우
            if (isCcState.Find(x => x.buffId == ConstValues.Frozen) != null)
                StateSetting(ENormalState.Frozen, ConstValues.Stun, ConstValues.Stun);
        }

        StandHitBox();
    }

    // 아랫점프
    // private async void DownJump()
    // {
    //     // 플랫폼 위에서만 작동함
    //     if (downJumping || groundObject == null || !groundObject.CompareTag(ConstValues.Platform) || IsDamaged())
    //         return;
    //
    //     PlaySound(ConstValues.Jump2, 2.0f);
    //     CancelMotion();
    //
    //     downJumping = true;
    //
    //     IgnorePlatformCheck(lastStandPlatform);
    //     myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 3.0f);
    //
    //     StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
    //     LandingStateSetting(ELandingState.Air);
    //
    //     stateCancellation = new CancellationTokenSource();
    //     while (transform.position.y >= groundObject.transform.position.y)
    //     {
    //         if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
    //             return;
    //     }
    //
    //     downJumping = false;
    // }

    // 등장(연출 포함)
    public virtual async void Appear(Action<string, EMonsterType> bossProduct)
    {
        if (IsDamaged())
        {
            normalState = ENormalState.Idle;
        }

        StandHitBox();
        StateSetting(ENormalState.Appear, ConstValues.Appear, ConstValues.Appear);
        MoveStateSetting(EMoveState.Stopping);
        if(myStat.hovering)
            LandingStateSetting(ELandingState.Air);
        else
            LandingStateSetting(ELandingState.Ground);

        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        immortal = false;
        GravityChange(myGravity);

        stateCancellation = new CancellationTokenSource();
        bool finishSuccess = true;
        finishSuccess = await AppearMotion();

        if (finishSuccess)
        {
            SpawnObject(myStat.appearEffect, transform);
            StateSetting(ENormalState.AppearEnd, ConstValues.AppearEnd, ConstValues.AppearEnd);
            if (await NormalDelay(myStat.appearDelay, stateCancellation).SuppressCancellationThrow())
            {
                FirstCoolTimeReduce();
                return;
            }

            if (!GameManager.Instance.ControlStart)
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
            
            IdleOrMove();
        }

        bossProduct?.Invoke(basicStat.name, monsterType);
        FirstCoolTimeReduce();
    }
    
    // 이벤트 등장
    public void EventAppear(Action<string, EMonsterType> bossProduct)
    {
        FirstCoolTimeReduce();
        IdleOrMove();
        immortal = false;
        bossProduct?.Invoke(basicStat.name, monsterType);
    }

    public void MonsterAwake()
    {
        stateCancellation = new CancellationTokenSource();
        firstPatrol = true;
        patrolTimer = 0;
        FirstCoolTimeReduce();
    }

    // 등장모션
    private async UniTask<bool> AppearMotion()
    {
        if (appearMotions.Length == 0)
            return false;

        myAnimator.transform.localScale =
            new Vector3(defaultAnimatorScale.x, defaultAnimatorScale.y * 2.6f, defaultAnimatorScale.z);

        foreach (var appearMotion in appearMotions)
        {
            appearMotion.gameObject.SetActive(true);
            appearMotion.color = ConstValues.WhiteColor;
        }

        float yScale = myAnimator.transform.localScale.y;
        float alpha = 1.0f;
        while (yScale > defaultAnimatorScale.y)
        {
            // 0.35f
            // 0.26f
            yScale -= 25 * defaultAnimatorScale.y * Time.deltaTime;
            alpha -= 20 * Time.deltaTime;
            myAnimator.transform.localScale = new Vector3(myAnimator.transform.localScale.x, yScale, 1);
            foreach (var appearMotion in appearMotions)
            {
                Color color = appearMotion.color;
                color.a = alpha;
                appearMotion.color = color;
            }

            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
            {
                DeleteAppearObject();
                Debug.Log("등장 도중 처맞음");
                return false;
            }
        }

        DeleteAppearObject();
        return true;
    }

    private void DeleteAppearObject()
    {
        myAnimator.transform.localScale = defaultAnimatorScale;
        foreach (var appearMotion in appearMotions)
            appearMotion.gameObject.SetActive(false);
    }

    private bool IsCanAttackAndJump()
    {
        return normalState is ENormalState.Idle or ENormalState.Move;
    }

    private bool IsCanJump()
    {
        return normalState is ENormalState.Idle or ENormalState.Move && landingState is ELandingState.Ground;
    }

    // 이동
    protected virtual void Move()
    {
        // 호버링몹 위아래로 움직이게 하기
        // if (myStat.hovering && normalState == ENormalState.Idle)
        // {
        //     
        // }
        if (basicStat.moveSpeed == 0)
            return;
        
        // 움직이기
        if (moveState != EMoveState.Moving)
            return;

        if (!alwaysAgro)
        {
            switch (MonsterCheckWalk())
            {
                case 0:
                    Flip(-1);
                    currentTraceDelay = 0;
                    break;
            
                case 1:
                    Flip(1);
                    currentTraceDelay = 0;
                    break;
            }
            switch (MonsterCheckTrap())
            {
                case 0:
                    Flip(-1);
                    currentTraceDelay = 0;
                    break;
            
                case 1:
                    Flip(1);
                    currentTraceDelay = 0;
                    break;
            }
            
            if (!myStat.hovering)
            {
                switch (MonsterCheckFall())
                {
                    case 0:
                        Flip(-1);
                        currentTraceDelay = 0;
                        break;
            
                    case 1:
                        Flip(1);
                        currentTraceDelay = 0;
                        break;
                }
            }
            if (agroState == EAgroState.Normal && myStat.customPatrol > 0 && myStat.hovering)
            {
                if (transform.position.x > rightPatrol)
                    Flip(-1);
        
                if (transform.position.x < leftPatrol)
                    Flip(1);
            }
        }
        
        float targetSpeedX = basicStat.moveSpeed;
        if (transform.localScale.x < 0)
            targetSpeedX = -basicStat.moveSpeed;

        float targetSpeedY = myRigidbody.linearVelocity.y;
        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
    }

    // 추적
    private void Trace()
    {
        if (moveState != EMoveState.Moving || agroState == EAgroState.Normal || normalState == ENormalState.Jump || currentTraceDelay < traceDelay)
            return;

        if (transform.localScale.x > 0)
        {
            if (GameManager.Instance.CurPlayer.transform.position.x < transform.position.x - myStat.traceLength)
                LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        }
        else
        {
            if (GameManager.Instance.CurPlayer.transform.position.x > transform.position.x + myStat.traceLength)
                LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        }
    }

    // 복귀
    private void ReturnMoving()
    {
        LookAt(startPos.x);
        
        Vector2 currentPos = myRigidbody.position;
        Vector2 toTarget = startPos - currentPos;

        float distance = toTarget.magnitude;

        // 멈출 거리(여유)
        float stopDistance = 0.01f;

        // 거의 도착했으면 스냅 + 정지
        if (distance <= stopDistance)
        {
            transform.position = startPos;
            agroState = EAgroState.Normal;
            return;
        }

        // 목표 방향으로 이동
        Vector2 dir = toTarget / distance;
        myRigidbody.linearVelocity = dir * basicStat.moveSpeed;
    }

    private void AgroSystem()
    {
        if (playerInAgroRange)
        {
            if (!IsAgroRay(ConstValues.MagentaColor))
                return;
            
            if (agroState is EAgroState.Normal or EAgroState.Return)
            {
                agroState = EAgroState.Agro;
                patrolTimer = 0;
            }

            if (normalState == ENormalState.Idle)
                LookAt(GameManager.Instance.CurPlayer.transform.position.x);

            if (normalState == ENormalState.Idle && moveState == EMoveState.Stopping)
            {
                if (myStat.standMotion)
                {
                    if (!patternInfo[0].playerInAttackRange)
                    {
                        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                        MoveStateSetting(EMoveState.Moving);
                    }
                }
                // 근접몹
                else
                {
                    if (basicStat.moveSpeed != 0)
                    {
                        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                        MoveStateSetting(EMoveState.Moving);
                    }
                }
            }
        }
        else if (agroState == EAgroState.Agro)
        {
            switch (normalState)
            {
                case ENormalState.Idle:
                    if (basicStat.moveSpeed != 0)
                    {
                        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                        MoveStateSetting(EMoveState.Moving);
                    }
                    break;

                case ENormalState.Move:
                    currentAgroTime += Time.deltaTime;
                    if (currentAgroTime >= agroTime)
                    {
                        agroState = EAgroState.Normal;
                        currentAgroTime = 0;
                        TotalBarOff();
                    }
                    break;
            }
        }
    }

    private void TotalBarOff()
    {
        if (totalBar)
            totalBar.gameObject.SetActive(false);
    }

    private void Patrol()
    {
        if (basicStat.moveSpeed == 0)
            return;
        
        if (patrolTimer <= 0f)
        {
            if (firstPatrol)
            {
                patrolMoving = true;
                firstPatrol = false;
            }
            else
            {
                patrolMoving = !patrolMoving;
            }
            
            patrolTimer = patrolMoving ? Random.Range(3.0f, 5.0f) : 3.0f;
            int randomDIr = Random.Range(0, 2);
            int dir = randomDIr == 0 ? -1 : 0;

            if (patrolMoving)
            {
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                MoveStateSetting(EMoveState.Moving);
                Flip(dir);
            }
            else
            {
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
                MoveStateSetting(EMoveState.Stopping);
            }
        }

        patrolTimer -= Time.deltaTime;

        if (patrolMoving)
        {
            Move();
        }
    }

    // 바라보기
    public override void LookAt(float xPos)
    {
        bool lookAt = myStat.traceLength > 0;
        if (!lookAt)
            return;

        base.LookAt(xPos);
    }

    public override void TakeDamage(int damage, bool isTrapAttack)
    {
        SpawnHpBar();
        base.TakeDamage(damage, isTrapAttack);

        if (agroState == EAgroState.Normal)
        {
            agroState = EAgroState.Agro;
            patrolTimer = 0;
        }

        if (damage == 0)
            return;

        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;

        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        if (!isTrapAttack)
        {
            GameManager.Instance.ComboCount += 1;
            uiInterface.ComboPresenter.ComboProduct();
        }

        // 대미지를 입을 시, 페이즈가 넘어가는 체력관리
        if (myStat.pattern.Count > 1)
        {
            var hpPercent = (float)basicStat.hp / OriginStat.hp;
            int percentage = (int)(hpPercent * 100);
            
            for (var i = 0; i < myStat.pattern.Count; i++)
            {
                // page
                if (percentage <= myStat.pattern[i].pageHp)
                {
                    page = i;
                }
            }
        }

        if (IsBoss)
        {
            // 보스가 두 마리 이상일 때를 대비
            var bossHpModel = new UIBossHpModel()
            {
                character = this
            };
            uiInterface.BossHpPresenter.SetModel(bossHpModel);
            uiInterface.BossHpPresenter.SetHpText();
            uiInterface.BossHpPresenter.HpReduce();
        }
        else
        {
            if (totalBar)
                totalBar.ReduceHpBar(basicStat.hp, basicStat.maxHp, 1.5f);
        }
    }

    public override void TakeStagger(int stagger)
    {
        base.TakeStagger(stagger);

        if (immuneStagger)
            return;

        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;

        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        if (IsBoss)
        {
            // 보스가 두 마리 이상일 때를 대비
            var bossHpModel = new UIBossHpModel()
            {
                character = this
            };
            uiInterface.BossHpPresenter.SetModel(bossHpModel);

            if (basicStat.stagger > 0)
            {
                uiInterface.BossHpPresenter.StaggerReduce();
            }
            else
            {
                // 터지는 전기 연출
                uiInterface.BossHpPresenter.StaggerExplosion(this, uiInterface.BossHpPresenter.StaggerGaugeTransform());
                uiInterface.BossHpPresenter.SetStagger();
            }
        }
    }

    private void SetStagger()
    {
        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;

        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();

        if (IsBoss)
        {
            // 보스가 두 마리 이상일 때를 대비
            var bossHpModel = new UIBossHpModel()
            {
                character = this
            };
            uiInterface.BossHpPresenter.SetModel(bossHpModel);
            uiInterface.BossHpPresenter.SetStagger();
        }
    }

    // 무력화
    public override void Stagger()
    {
        // 무력화 디버프 추가
        AddDeBuff(EBuffType.Stagger, basicStat.staggerTime, 0);
        MoveStateSetting(EMoveState.Stopping);

        CancelMotion();
        stateCancellation = new CancellationTokenSource();
        
        // 호버링 몹일 경우 에어본으로 대체(땅에 툭 떨어지게)
        if (myStat.hovering)
        {
            if (basicStat.bodyType == EBodyType.Normal)
            {
                Airborne(0, 5, false);
            }
        }
        else
        {
            StateSetting(ENormalState.Stagger, ConstValues.Stagger, ConstValues.Stagger);
        }
        
        immuneStagger = true;
    }
    
    public override void Die()
    {
        base.Die();
        //removeAction?.Invoke();
        //GameManager.Instance.RemoveMonster(this);
        if (!IsBoss)
            goldAction?.Invoke(myStat.gold, centerPos.position);

        if (isExplosion)
            DieExplosion();

        if (!totalBar)
            return;

        totalBar.gameObject.SetActive(false);
        totalBar = null;
    }
    
    public async void DieShake()
    {
        anotherCancellation = new CancellationTokenSource();
        Vector2 myVector = transform.position;

        while (gameObject.activeSelf)
        {
            var randPos = Random.Range(-0.05f, 0.05f);
            transform.position = new Vector2(myVector.x + randPos, myVector.y + randPos);
            await YieldDelay(anotherCancellation);
        }
    }

    public virtual void DieExplosion()
    {
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        gameObject.SetActive(false);
    }

    // 공격 딜레이
    protected async UniTask AttackDelay(float attackDelay, bool lookAtPlayer = false)
    {
        if (stateCancellation == null || stateCancellation.IsCancellationRequested)
            stateCancellation = new CancellationTokenSource();

        float delay = 0;
        while (delay < attackDelay)
        {
            //float totalAttackSpeed = finalAttackSpeed;
            delay += Time.deltaTime * basicStat.attackSpeed;
            if (lookAtPlayer)
                LookAt(GameManager.Instance.CurPlayer.transform.position.x);
            await UniTask.Yield(cancellationToken: stateCancellation.Token);
        }
    }

    // 플레이어가 공격범위 안에 들어왔는지 체크
    private void PlayerInAttackRangeCheck()
    {
        foreach (var pattern in patternInfo)
        {
            if (GameManager.Instance.CurPlayer.GetRightPosX() > transform.position.x - pattern.attackRange[0] &&
                GameManager.Instance.CurPlayer.GetLeftPosX() < transform.position.x + pattern.attackRange[0] &&
                GameManager.Instance.CurPlayer.CenterPos.position.y >
                transform.position.y - pattern.attackRange[1] + CenterPos.localPosition.y &&
                GameManager.Instance.CurPlayer.CenterPos.position.y <
                transform.position.y + pattern.attackRange[1] + CenterPos.localPosition.y)
            {
                pattern.playerInAttackRange = true;
            }
            else
            {
                pattern.playerInAttackRange = false;
            }
        }
    }

    private void PlayerInAgroRangeCheck()
    {
        float leftPlayerPos = GameManager.Instance.CurPlayer.GetLeftPosX();
        float rightPlayerPos = GameManager.Instance.CurPlayer.GetRightPosX();
        Vector2 centerPlayerPos = GameManager.Instance.CurPlayer.CenterPos.position;

        Vector2 agroRange = myStat.agroRange;
        Vector2 myPos = transform.position;
        Vector2 myScale = transform.localScale;
        float centerLocalPos = CenterPos.localPosition.y;
        
        float leftAgroRange = myPos.x - agroRange.x;
        float rightAgroRange = myPos.x + agroRange.x;
        float upAgroRange =  myPos.y - agroRange.y + centerLocalPos;
        float downAgroRange = myPos.y + agroRange.y + centerLocalPos;

        bool condition1 = rightPlayerPos > leftAgroRange;
        bool condition2 = leftPlayerPos < rightAgroRange;
        bool condition3 = centerPlayerPos.y > upAgroRange;
        bool condition4 = centerPlayerPos.y < downAgroRange;
        
        // 추가된 방향 처리(몹이 플레이어를 바라보아야 함)
        bool condition5 = (myPos.x >= centerPlayerPos.x && myScale.x < 0) || (myPos.x < centerPlayerPos.x && myScale.x > 0);
        
        // && condition5
        if (condition1 && condition2 && condition3 && condition4)
            playerInAgroRange = true;
        else
            playerInAgroRange = false;
    }

    private void PlayerInJumpRangeCheck()
    {
        if (GameManager.Instance.CurPlayer.GetRightPosX() > transform.position.x - myStat.jumpRange.x &&
            GameManager.Instance.CurPlayer.GetLeftPosX() < transform.position.x + myStat.jumpRange.x &&
            GameManager.Instance.CurPlayer.GetDownPosY() < transform.position.y + myStat.jumpRange.y * 2)
        {
            jumpInfo.playerInRange = true;
        }
        else
        {
            jumpInfo.playerInRange = false;
        }
    }

    private void PlayerInDropRangeCheck()
    {
        if (GameManager.Instance.CurPlayer.GetRightPosX() > transform.position.x - myStat.dropRange.x &&
            GameManager.Instance.CurPlayer.GetLeftPosX() < transform.position.x + myStat.dropRange.x &&
            GameManager.Instance.CurPlayer.GetUpPosY() > transform.position.y - myStat.dropRange.y * 2 &&
            GameManager.Instance.CurPlayer.GetDownPosY() < transform.position.y)
        {
            downJumpInfo.playerInRange = true;
        }
        else
        {
            downJumpInfo.playerInRange = false;
        }
    }

    // 적의 패턴
    protected virtual void MonsterPattern(int idx)
    {
        // 기존의 공격 트리거를 전부 해제
        foreach (var parameters in myAnimator.parameters)
        {
            if (parameters.name.Split('_')[0] == ConstValues.Attack)
            {
                myAnimator.ResetTrigger(parameters.name);
            }
        }

        ResetTriggerAnimator(ConstValues.Pattern);
        SetTriggerAnimator($"{ConstValues.Attack}_{idx}");
    }

    // 최초 쿨타임 리셋
    protected void FirstCoolTimeReduce()
    {
        for (int i = 0; i < patternInfo.Count; i++)
        {
            if (i == 0)
            {
                patternInfo[i].patternCoolTime = myStat.coolTime[i] - myStat.firstCoolTime;
            }
            else
            {
                patternInfo[i].patternCoolTime = myStat.coolTime[i] * 0.5f;
            }
        }

        foreach (var canSkillArray in patternInfo)
            canSkillArray.canPattern = false;

        currentSkillIdx = 0;
    }

    private void UpdateTraceDelay()
    {
        if (currentTraceDelay < traceDelay)
            currentTraceDelay += Time.deltaTime;

        if (currentTraceDelay > traceDelay)
            currentTraceDelay = traceDelay;
    }

    private void UpdateHoveringLeap()
    {
        if (normalState != ENormalState.Leap)
            return;

        if (transform.position.y < arriveHeight)
        {
            myRigidbody.linearVelocity = new Vector2(0, 15.0f);
        }
        else
        {
            transform.position = new Vector2(transform.position.x, arriveHeight);
            myRigidbody.linearVelocity = Vector2.zero;
            GravityChange(myGravity);
            IdleOrMove();
            
            // 패트롤 위치 실시간으로 수정
            leftPatrol = transform.position.x - myStat.customPatrol;
            rightPatrol = transform.position.x + myStat.customPatrol;
            
            // 도약이 끝날때 바로 공격하는 불쾌한 현상 방지
            for (var i = 0; i < patternInfo.Count; i++)
            {
                patternInfo[i].patternCoolTime = myStat.coolTime[i] - 0.5f;
                patternInfo[i].canPattern = false;
            }
        }
    }

    private void UpdateGlobalCoolTime()
    {
        if (curGlobalCoolTime < myStat.globalCoolTime)
            curGlobalCoolTime += Time.deltaTime;

        if (curGlobalCoolTime >= myStat.globalCoolTime)
            curGlobalCoolTime = myStat.globalCoolTime;
    }

    protected bool GetGlobalCoolTime()
    {
        bool isFill = curGlobalCoolTime >= myStat.globalCoolTime;
        return isFill;
    }

    private void JumpCoolTimeReduce()
    {
        if (jumpInfo.coolTime < 2.0f)
        {
            jumpInfo.coolTime += Time.deltaTime;
        }
        else
        {
            jumpInfo.coolTime = 2.0f;
        }
    }

    private void DownJumpCoolTimeReduce()
    {
        if (downJumpInfo.coolTime < 2.0f)
        {
            downJumpInfo.coolTime += Time.deltaTime;
        }
        else
        {
            downJumpInfo.coolTime = 2.0f;
        }
    }

    // 적 패턴 쿨타임 감소
    private void PatternCoolTimeReduce()
    {
        for (var i = 0; i < patternInfo.Count; i++)
        {
            if (patternInfo[i].patternCoolTime < myStat.coolTime[i])
            {
                patternInfo[i].patternCoolTime += Time.deltaTime;
            }
            else
            {
                patternInfo[i].patternCoolTime = myStat.coolTime[i];
                patternInfo[i].canPattern = true;
            }
        }
    }

    // 적 패턴 끝(해당하는 패턴의 쿨타임이 돌아감)
    protected virtual void PatternEnd(bool movingStart = true)
    {
        patternInfo[currentSkillIdx].canPattern = false;

        curGlobalCoolTime = 0;
        patternInfo[currentSkillIdx].patternCoolTime = 0;
        currentSkillIdx = 0;

        if (!movingStart)
            return;

        IdleOrMove();
    }

    // 적 공격
    private async void MonsterAttack(int idx)
    {
        if (IsCanAttackAndJump() && patternInfo[idx].canPattern && patternInfo[idx].playerInAttackRange)
        {
            if (!IsAgroRay(ConstValues.OrangeColor))
                return;
            
            // 특정 행동이 끝나는 즉시 행동하면 애니메이션 갭이 일어나서 1프레임 뒤에 실행
            if (stateCancellation == null || stateCancellation.IsCancellationRequested)
                stateCancellation = new CancellationTokenSource();
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return;

            MoveStateSetting(EMoveState.Stopping);
            StateSetting(ENormalState.Attack, $"{ConstValues.Attack}_{idx}", $"{ConstValues.Attack}_{idx}");
            LookAt(GameManager.Instance.CurPlayer.transform.position.x);
            MonsterPattern(idx);
        }
    }

    private void UpdateRoomLimit()
    {
        // 1) 절반 크기
        float halfWidth = physicsCollider.size.x * 0.5f;

        // 3) 플레이어 센터 기준으로 클램프 경계 계산
        float leftLimit = limitLeft + halfWidth;
        float rightLimit = limitRight - halfWidth;

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

        myRigidbody.linearVelocity = vel;
    }

    // 패턴 사이클
    private void PatternCycle()
    {
        if (!IsCanAttackAndJump() || !GetGlobalCoolTime())
            return;

        patternIdx.Clear();
        // 사용 할 수 있는 패턴 색출(쿨타임, 범위)
        for (int i = 0; i < patternInfo.Count; i++)
        {
            // 적의 페이지에 따라 다른 패턴이 들어간다
            foreach (var pattern in myStat.pattern[page].pagePattern)
            {
                if (i != pattern)
                    continue;

                if (patternInfo[i].priority > -1 && patternInfo[i].canPattern && patternInfo[i].playerInAttackRange)
                {
                    patternIdx.Add(i);
                    break;
                }
            }
        }

        // 한 개의 패턴밖에 나올 수 없다면 그 패턴을 그냥 발동시킴 
        if (patternIdx.Count == 1)
        {
            currentSkillIdx = patternIdx[0];
            MonsterAttack(patternIdx[0]);
            Debug.Log($"나올 수 있는 패턴이 {currentSkillIdx}패턴 뿐이다");
        }
        else if (patternIdx.Count > 1)
        {
            currentSkillIdx = patternIdx[0];
            int temp = patternInfo[patternIdx[0]].priority;
            bool sameCount = true;

            for (int i = 0; i < patternIdx.Count; i++)
            {
                if (temp != patternInfo[patternIdx[i]].priority)
                {
                    sameCount = false;
                    if (temp < patternInfo[patternIdx[i]].priority)
                    {
                        temp = patternInfo[patternIdx[i]].priority;
                        currentSkillIdx = patternIdx[i];
                    }
                }
            }

            // 들어있는 패턴들의 우선순위가 모두 같을 때
            if (sameCount)
            {
                // 같은 우선순위의 패턴 중 랜덤한 패턴이 발동한다
                int rand = Random.Range(0, patternIdx.Count);
                currentSkillIdx = patternIdx[rand];
                MonsterAttack(currentSkillIdx);
                Debug.Log($"우선순위가 같은 스킬들이 {patternIdx.Count}개다, 최종적으로 나온건 {currentSkillIdx}");
            }
            // 들어있는 패턴들의 우선순위가 하나라도 다를 때
            else
            {
                // 가장 높은 우선순위의 패턴이 발동한다
                MonsterAttack(currentSkillIdx);
                Debug.Log($"가장 높은 우선순위{currentSkillIdx}발동");
            }
        }
    }

    // 몬스터 뒤돌기
    private int MonsterCheckWalk()
    {
        // 오른쪽
        if (transform.localScale.x > 0)
        {
            if (isWallRight)
            {
                return 0;
            }
        }
        // 왼쪽
        else
        {
            if (isWallLeft)
            {
                return 1;
            }
        }

        return -1;
    }

    // 몬스터 절벽감지
    private int MonsterCheckFall()
    {
        var rayVector = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);
        if (transform.localScale.x < 0)
            rayVector = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);

        float distance = 1.0f;
        RaycastHit2D downRay = Physics2D.Raycast(rayVector, Vector2.down, distance, groundAndPlatformLayerMask);
        Debug.DrawRay(rayVector, Vector2.down * distance, ConstValues.OrangeColor, 0.02f);
        if (downRay.collider == null)
        {
            switch (transform.localScale.x)
            {
                case > 0:
                    return 0;
                case < 0:
                    return 1;
            }
        }
        return -1;
    }
    
    // 몬스터 함정감지
    private int MonsterCheckTrap()
    {
        var rayVector = new Vector2(transform.position.x, transform.position.y);

        float distance = myBoxCollider.size.x + 1.0f;
        RaycastHit2D trapRay;
        if (transform.localScale.x > 0)
        {
            trapRay = Physics2D.Raycast(rayVector, Vector2.right, distance, trapLayerMask);
            Debug.DrawRay(rayVector, Vector2.right * distance, ConstValues.CyanColor, 0.02f);
        }
        else
        {
            trapRay = Physics2D.Raycast(rayVector, Vector2.left, distance, trapLayerMask);
            Debug.DrawRay(rayVector, Vector2.left * distance, ConstValues.CyanColor, 0.02f);
        }

        if (trapRay.collider != null)
        {
            switch (transform.localScale.x)
            {
                case > 0:
                    return 0;
                case < 0:
                    return 1;
            }
        }
        return -1;
    }

    protected Vector2 CalculateLaunchVelocity(Vector2 start, Vector2 end, float t)
    {
        Vector2 d = end - start;
        float dx = d.x;
        float dy = d.y;
        // 양수 중력 가속도
        float g = -Physics2D.gravity.y * myRigidbody.gravityScale;

        float vx = dx / t;
        // vy = (dy + ½ g t²) / t
        float vy = (dy + 0.5f * g * t * t) / t;

        return new Vector2(vx, vy);
    }

    // 위험영역 소환
    private FadeSystem WarningAreaSpawn(float duration, Color color, Vector2 pos, Vector3 angle, Vector2 scale)
    {
        GameObject warningArea = SpawnObject(ConstValues.WarningArea, pos);
        warningArea.transform.eulerAngles = angle;
        warningArea.transform.localScale = scale;
        warningArea.SetActive(true);

        var fadeSystem = warningArea.GetComponent<FadeSystem>();
        fadeSystem.SetParameter(1.0f, 0f, duration, true);
        fadeSystem.ColorInput(color);
        return fadeSystem;
    }

    // 위험영역 표시(콜라이더) (위치, 각도, 콜라이더, 길이, 색깔)
    protected async UniTask WarningAreaSpawnCollider(Vector2 pos, Vector3 angle, BoxCollider2D targetCollider, float duration, Color color)
    {
        float scaleX = Mathf.Abs(targetCollider.gameObject.transform.localScale.x);
        float scaleY = Mathf.Abs(targetCollider.gameObject.transform.localScale.y);

        float posX = targetCollider.offset.x * scaleX;
        float posY = targetCollider.offset.y * scaleY;

        Vector2 finalPos = new Vector2(pos.x + posX, pos.y + posY);
        Vector2 finalScale = new Vector2(scaleX * targetCollider.size.x, scaleY * targetCollider.size.y);

        FadeSystem warningArea = WarningAreaSpawn(duration, color, finalPos, angle, finalScale);
        await warningArea.Fade(false);
    }

    // 위험영역 표시(궤적) 좌표, x크기, y크기, x축 높이, y축 높이
    protected async UniTask WarningAreaSpawnTrajectory(Vector2 startPos, Vector2 endPos, Vector3 angle, float duration, Color color, float thickness, float addDistance = 0)
    {
        float distance = Vector2.Distance(startPos, endPos);

        // (시작좌표 + 끝좌표) / 2를 생성 좌표로 설정
        Vector2 spawnPos = (startPos + endPos) * 0.5f;
        Vector2 spawnSize = new Vector2(distance + addDistance, thickness);

        FadeSystem warningArea = WarningAreaSpawn(duration, color, spawnPos, angle, spawnSize);
        await warningArea.Fade(false);
    }

    // 커스텀
    public async UniTask EpisodeMove_X(Vector2 movePos, float speed, int finishDir)
    {
        Stop();
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
            if (normalState == ENormalState.Idle)
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
        StopVelocity_X();
    }

    public async UniTask EpisodeMove_Y(Vector2 movePos, float speed, int finishDir)
    {
        Stop();
        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);

        Vector2 dir = Vector2.down;
        if (transform.position.y < movePos.y)
            dir = Vector2.up;

        stateCancellation = new CancellationTokenSource();
        while (Math.Abs(transform.position.y - movePos.y) > 0.1f)
        {
            if (normalState == ENormalState.Idle)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);

            CustomMoving_Y(dir, speed);
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
        StopVelocity_X();
    }

    private bool IsAgroRay(Color rayColor)
    {
        Vector2 from = CenterPos.position;
        Vector2 to   = GameManager.Instance.CurPlayer.CenterPos.position;
        Vector2 dir  = (to - from).normalized;
        float distance   = Vector2.Distance(from, to);

        int mask = agroLayerMask;
        if(IsBoss)
            mask = bossLayerMask;
        
        var ray = Physics2D.Raycast(from, dir, distance, mask);
        Debug.DrawRay(from, dir * distance, rayColor, 0.02f);

        return ray.collider != null && ray.collider.CompareTag(ConstValues.Player);
    }
    
    protected override void LandingAction()
    {
        // 점프 착지
        if (normalState is ENormalState.Jump)
        {
            if (alwaysAgro)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            else
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        }
    }
}