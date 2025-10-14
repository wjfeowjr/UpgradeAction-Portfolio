using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

// 실제 이동 관련
public enum EAgroState
{
    Normal,
    Agro,
    Return,
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

    [SerializeField] private bool isBoss; // 보스인가?
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
    protected CancellationTokenSource dieCancellation;
    protected Action<int, Vector2> goldAction;

    private float startPosX;
    private float agroTime = 5.0f;
    private float currentAgroTime;
    private bool playerInAgroRange;
    private float firstHeight;
    private float leapHeight;

    private bool patrolMoving = false;
    private float patrolTimer = 0f;
    private float limitLeft;
    private float limitRight;

    private PhysicsMaterial2D _airLowFrictionMat;
    private PhysicsMaterial2D _originalMaterial;

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

    public bool IsBoss
    {
        get => isBoss;
        set => isBoss = value;
    }

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
        InitAdditionalStat();
        GravityChange(myGravity);
        firstHeight = transform.position.y;
        startPosX = transform.position.x;
    }

    protected override void Update()
    {
        if (isDie || basicStat.hp <= 0 || !GameManager.Instance.ControlStart)
            return;

        base.Update();

        if (isBoss)
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
                    MonsterLeap();
                    break;
                case EAgroState.Return:
                    ReturnMoving();
                    MonsterLeap();
                    break;
            }

            AgroSystem();
        }

        UpdateDown();
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
        if (!GameManager.Instance.ControlStart)
            return;

        base.FixedUpdate();
        PlayerInAttackRangeCheck();
        PlayerInAgroRangeCheck();
        PlayerInJumpRangeCheck();
        PlayerInDropRangeCheck();
        // UpdateStandingCheck();
        UpdateRoomLimit();
    }

    private void OnDisable()
    {
        dieCancellation?.Cancel();
        stateCancellation?.Cancel();

        ClearObjectList(controlObject);
        ClearObjectList(normalObject);

        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Ground);

        if (!totalBar)
            return;

        totalBar.gameObject.SetActive(false);
        totalBar = null;
    }

    protected override void CancelMotion()
    {
        base.CancelMotion();
        physicsCollider.sharedMaterial = _originalMaterial;
    }

    protected void OnDrawGizmos()
    {
        if (isDie)
            return;

        var myPosition = transform.position;

        if (myStat.agroRange != Vector2.zero)
        {
            Vector2 agroRange = new Vector2(myStat.agroRange.x * 2, myStat.agroRange.y * 2);
            Gizmos.color = ConstValues.BlueColor;
            Gizmos.DrawWireCube(new Vector2(myPosition.x, myPosition.y + CenterPos.localPosition.y), agroRange);
        }

        if (myStat.jumpRange != Vector2.zero)
        {
            Vector2 jumpRange = new Vector2(myStat.jumpRange.x * 2, myStat.jumpRange.y * 2);
            Gizmos.color = ConstValues.CyanColor;
            Gizmos.DrawWireCube(new Vector2(myPosition.x, myPosition.y + myStat.jumpRange.y), jumpRange);
        }

        if (myStat.dropRange != Vector2.zero)
        {
            Vector2 dropRange = new Vector2(myStat.dropRange.x * 2, myStat.dropRange.y * 2);
            Gizmos.color = ConstValues.MagentaColor;
            Gizmos.DrawWireCube(new Vector2(myPosition.x, myPosition.y - myStat.dropRange.y), dropRange);
        }

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

    // 테이블의 값으로 스텟 초기화(기본 스텟)
    public async void InitBasicStat()
    {
        //await UniTask.WaitUntil(() => TableManager.Instance.monsterTable.Monster.Count > 0);
        var myName = name.Split(' ')[0];
        var targetStat = TableManager.Instance.monsterTable.Monster.Find(x => x.id == myName);

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
            maxStagger = targetStat.stagger,
            staggerTime = targetStat.staggerTime,
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

            var appearShakeArray = targetStat.appearShake.Split(';');
            foreach (var appearShake in appearShakeArray)
                myStat.appearShake.Add(float.Parse(appearShake));

            myStat.appearEffect = targetStat.appearEffect;
            myStat.dyingMiniEffect = targetStat.dyingMiniEffect;
            myStat.dyingEffect = targetStat.dyingEffect;
        }
    }

    private async void InitAdditionalStat()
    {
        //await UniTask.WaitUntil(() => basicStat.hp > 0);
        var finalHp = basicStat.hp;
        basicStat.hp = finalHp;
        basicStat.maxHp = finalHp;

        var finalStagger = basicStat.stagger;
        basicStat.maxStagger = finalStagger;
        basicStat.stagger = finalStagger;

        curGlobalCoolTime = 0;
        isDie = false;

        agroState = isBoss ? EAgroState.Agro : EAgroState.Normal;
        if (myStat.hovering)
            landingState = ELandingState.Air;
        else
            landingState = ELandingState.Ground;
    }

    protected override void FindGroundObject()
    {
        if (myStat.hovering)
            return;

        base.FindGroundObject();
    }

    public async void SpawnHpBar()
    {
        //await UniTask.WaitUntil(() => basicStat.hp > 0);
        await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);

        if (isBoss)
        {
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

            totalBar = SpawnUIObject(ConstValues.TotalBar, hpBarPos).GetComponent<TotalBar>();
            totalBar.SetCastCharacter(this);
        }
    }

    public void SetGoldAction(Action<int, Vector2> action)
    {
        goldAction = action;
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
            StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            MoveStateSetting(EMoveState.Moving);
        }
    }

    // 호버링 높이까지 도약
    private void HoveringLeap()
    {
        leapHeight = firstHeight;
        StateSetting(ENormalState.Leap, ConstValues.Leap, ConstValues.Leap);
        MoveStateSetting(EMoveState.Stopping);
    }

    protected override void StateCheck()
    {
        var stunOrStagger = buffList.FindAll(x => x.buffType is EBuffType.Stun or EBuffType.Stagger);

        if (stunOrStagger.Count == 0)
        {
            immuneStagger = false;
            basicStat.bodyType = originStat.bodyType;
            basicStat.stagger = basicStat.maxStagger;
            curGlobalCoolTime = 0;
            SetStagger();
        }
    }

    protected override void StateRecovery()
    {
        var stunOrStagger = buffList.FindAll(x => x.buffType is EBuffType.Stun or EBuffType.Stagger);
        if (stunOrStagger.Count == 0)
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
            if (stunOrStagger.Find(x => x.buffType == EBuffType.Stun) != null)
                StateSetting(ENormalState.Stun, ConstValues.Stun, ConstValues.Stun);

            // 무력화에 걸려있는 경우
            if (stunOrStagger.Find(x => x.buffType == EBuffType.Stagger) != null)
                StateSetting(ENormalState.Stagger, ConstValues.Stagger, ConstValues.Stagger);
        }

        StandHitBox();
    }

    // 아랫점프
    private async void DownJump()
    {
        // 플랫폼 위에서만 작동함
        if (downJumping || groundObject == null || !groundObject.CompareTag(ConstValues.Platform) || IsDamaged())
            return;

        PlaySound(ConstValues.Jump2, 2.0f);
        CancelMotion();

        downJumping = true;

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

        downJumping = false;
    }

    // 등장(연출 포함)
    public virtual async void Appear(Action<string> bossProduct)
    {
        if (IsDamaged())
        {
            Debug.Log("꽥");
            normalState = ENormalState.Idle;
        }

        StandHitBox();
        StateSetting(ENormalState.Appear, ConstValues.Appear, ConstValues.Appear);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Ground);

        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        immortal = false;
        GravityChange(myGravity);

        stateCancellation = new CancellationTokenSource();
        bool finishSuccess = true;
        finishSuccess = await AppearMotion();

        if (finishSuccess)
        {
            SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
            StateSetting(ENormalState.AppearEnd, ConstValues.AppearEnd, ConstValues.AppearEnd);
            if (await NormalDelay(myStat.appearDelay, stateCancellation).SuppressCancellationThrow())
            {
                FirstCoolTimeReduce();
                return;
            }

            if (!GameManager.Instance.ControlStart)
                StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);

            await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
            // Hovering();
            // AppearShake();
            IdleOrMove();
        }

        bossProduct?.Invoke(basicStat.name);
        FirstCoolTimeReduce();
    }

    public void MonsterAwake()
    {
        stateCancellation = new CancellationTokenSource();
        IdleOrMove();
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
        // 움직이기
        if (moveState != EMoveState.Moving)
            return;

        float targetSpeedX = basicStat.moveSpeed;
        if (transform.localScale.x < 0)
            targetSpeedX = -basicStat.moveSpeed;

        float targetSpeedY = myRigidbody.linearVelocity.y;
        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
    }

    // 추적
    private void Trace()
    {
        if (moveState != EMoveState.Moving || agroState == EAgroState.Normal)
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
        if (moveState != EMoveState.Moving)
            return;

        LookAt(startPosX);
        float targetSpeedX = basicStat.moveSpeed;
        if (transform.localScale.x < 0)
            targetSpeedX = -basicStat.moveSpeed;

        float targetSpeedY = myRigidbody.linearVelocity.y;
        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
        if (Math.Abs(transform.position.x - startPosX) <= 0.1f)
            agroState = EAgroState.Normal;
    }

    private void AgroSystem()
    {
        if (playerInAgroRange)
        {
            if (agroState is EAgroState.Normal or EAgroState.Return)
                agroState = EAgroState.Agro;

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
                    StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                    MoveStateSetting(EMoveState.Moving);
                }
            }
        }
        else if (agroState == EAgroState.Agro)
        {
            switch (normalState)
            {
                case ENormalState.Idle:
                    StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
                    MoveStateSetting(EMoveState.Moving);
                    break;

                case ENormalState.Move:
                    currentAgroTime += Time.deltaTime;
                    if (currentAgroTime >= agroTime)
                    {
                        agroState = EAgroState.Return;
                        currentAgroTime = 0;
                    }

                    break;
            }
        }
    }

    private void Patrol()
    {
        if (patrolTimer <= 0f)
        {
            // int randomDIr = Random.Range(0, 2);
            // int randomMoving = Random.Range(0, 2);
            //
            // int dir = randomDIr == 0 ? -1 : 0;
            // patrolMoving = randomMoving != 0;
            //
            // patrolTimer = 3.0f;
            //
            // if (patrolMoving)
            // {
            //     StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            //     MoveStateSetting(EMoveState.Moving);
            //     Flip(dir);
            // }
            // else
            // {
            //     StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
            //     MoveStateSetting(EMoveState.Stopping);
            // }

            patrolMoving = !patrolMoving;
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
            MonsterBackWall();
            MonsterBackFall();
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

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        if (agroState == EAgroState.Normal)
            agroState = EAgroState.Agro;

        if (damage == 0)
            return;

        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;

        GameManager.Instance.ComboCount += 1;
        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        uiInterface.ComboPresenter.ComboProduct();

        // 대미지를 입을 시, 페이즈가 넘어가는 체력관리
        if (myStat.pattern.Count > 1)
        {
            var hpPercent = (float)basicStat.hp / OriginStat.hp;
            for (var i = 0; i < myStat.pattern.Count; i++)
            {
                if (hpPercent <= myStat.pattern[page].pageHp)
                    page = i;
            }
        }

        if (isBoss)
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
        if (isBoss)
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

        if (isBoss)
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

    public override void Die()
    {
        base.Die();
        //removeAction?.Invoke();
        //GameManager.Instance.RemoveMonster(this);
        if (!isBoss)
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
        if (stateCancellation == null)
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
        // myBoxCollider.size.y * 0.5f
        // GetRightPosX()
        // GetLeftPosX()
        // GetUpPosY()
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
        if (GameManager.Instance.CurPlayer.GetRightPosX() > transform.position.x - myStat.agroRange.x &&
            GameManager.Instance.CurPlayer.GetLeftPosX() < transform.position.x + myStat.agroRange.x &&
            GameManager.Instance.CurPlayer.CenterPos.position.y >
            transform.position.y - myStat.agroRange.y + CenterPos.localPosition.y &&
            GameManager.Instance.CurPlayer.CenterPos.position.y <
            transform.position.y + myStat.agroRange.y + CenterPos.localPosition.y)
        {
            playerInAgroRange = true;
        }
        else
        {
            playerInAgroRange = false;
        }
    }

    private void PlayerInJumpRangeCheck()
    {
        // GameManager.Instance.CurPlayer.GetUpPosY() > transform.position.y &&
        //Debug.Log($"{GameManager.Instance.CurPlayer.GetDownPosY()}, {transform.position.y + myStat.jumpRange.y}");

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

    private void UpdateHoveringLeap()
    {
        if (normalState != ENormalState.Leap)
            return;

        if (transform.position.y < leapHeight)
        {
            myRigidbody.linearVelocity = new Vector2(0, 15.0f);
        }
        else
        {
            transform.position = new Vector2(transform.position.x, leapHeight);
            myRigidbody.linearVelocity = Vector2.zero;
            GravityChange(myGravity);
            IdleOrMove();
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
    public void PatternEnd(bool movingStart = true)
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
            // 특정 행동이 끝나는 즉시 행동하면 애니메이션 갭이 일어나서 1프레임 뒤에 실행
            if (stateCancellation == null)
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
    private void MonsterBackWall()
    {
        var rayVector = CenterPos.transform.position;
        var distance = myBoxCollider.size.x * 0.5f + 0.2f;
        float halfWidth = physicsCollider.size.x * 0.5f;

        float leftLimit = limitLeft + halfWidth;
        float leftRight = limitRight - halfWidth;

        // 오른쪽
        if (transform.localScale.x > 0)
        {
            RaycastHit2D rightRay = Physics2D.Raycast(rayVector, Vector2.right, distance, groundAndPlatformLayerMask);
            Debug.DrawRay(rayVector, Vector2.right * distance, ConstValues.CyanColor, 0.02f);
            if (transform.position.x >= leftRight || rightRay.collider != null)
            {
                Flip(-1);
            }
        }
        else
        {
            RaycastHit2D leftRay = Physics2D.Raycast(rayVector, Vector2.left, distance, groundAndPlatformLayerMask);
            Debug.DrawRay(rayVector, Vector2.left * distance, ConstValues.CyanColor, 0.02f);
            if (transform.position.x <= leftLimit || leftRay.collider != null)
            {
                Flip(1);
            }
        }
    }

    // 몬스터 절벽감지
    private void MonsterBackFall()
    {
        if (myStat.hovering)
            return;
        
        var rayVector = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);
        if (transform.localScale.x < 0)
            rayVector = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);

        RaycastHit2D downRay = Physics2D.Raycast(rayVector, Vector2.down, 3.0f, groundAndPlatformLayerMask);
        Debug.DrawRay(rayVector, Vector2.down * 3.0f, ConstValues.OrangeColor, 0.02f);
        if (downRay.collider == null)
        {
            switch (transform.localScale.x)
            {
                case > 0:
                    Flip(-1);
                    break;
                case < 0:
                    Flip(1);
                    break;
            }
        }
    }

    // 몬스터 장애물 넘기
    private async void MonsterLeap()
    {
        if(myStat.hovering)
            return;
        
        if (IsCanJump() && jumpInfo.coolTime >= ConstValues.JumpCoolTime)
        {
            Vector2 start = transform.position;
            Vector2 end = transform.position;

            float rayPosX = transform.position.x + myBoxCollider.size.x / 2 + 0.3f;
            if (transform.localScale.x < 0)
                rayPosX = transform.position.x - myBoxCollider.size.x / 2 - 0.3f;

            float rayPosY = transform.position.y + myBoxCollider.size.y + 3.0f;

            Vector2 rayPos = new Vector2(rayPosX, rayPosY);
            RaycastHit2D downRay = Physics2D.Raycast(rayPos, Vector2.down, 10.0f, groundAndPlatformLayerMask);
            Debug.DrawRay(rayPos, Vector2.down * 10.0f, ConstValues.CyanColor, 0.02f);

            if (downRay.collider != null && downRay.point.y > transform.position.y)
            {
                // 난간 턱 넘김을 위해 목표 지점을 살짝 더 앞으로/위로 보정
                float xOffset = 0.5f;
                float yOffset = 0.3f;
                float dirSign = transform.localScale.x > 0 ? 1f : -1f;

                end = new Vector2(downRay.point.x + xOffset * dirSign, downRay.point.y + yOffset);
            }

            if (Vector2.Distance(start, end) < 0.01f)
                return;

            CancelMotion();
            stateCancellation = new CancellationTokenSource();
            StateSetting(ENormalState.Landing, ConstValues.Landing, ConstValues.Landing);
            MoveStateSetting(EMoveState.Stopping);

            var delay1 = 0.2f;
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return;

            // === 점프 직전 세팅: 마찰↓, 히트박스↓ ===
            // 낮은 마찰 머티리얼 준비
            if (_airLowFrictionMat == null)
            {
                _airLowFrictionMat = new PhysicsMaterial2D("AirLowFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }

            // 원래 머티리얼 기억하고 낮은 마찰 적용
            _originalMaterial = physicsCollider.sharedMaterial;
            physicsCollider.sharedMaterial = _airLowFrictionMat;

            // 윗모서리 걸림 줄이기 위한 다운형 히트박스
            DownHitBox();

            PlaySound(ConstValues.Jump1, 2.0f);
            StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);

            float travelTime = 0.5f;
            Vector2 velocity = CalculateLaunchVelocity(start, end, travelTime);
            myRigidbody.linearVelocity = velocity;
            LandingStateSetting(ELandingState.Air);

            // === 초기 구간 전진유지 보정 ===
            float dirKeep = Mathf.Sign(velocity.x);
            MaintainForwardFor(0.2f, dirKeep).Forget();

            // 착지까지 대기(간단 버전: Y가 내려오기 시작하면 착지 준비로 간주)
            // 필요 시 레이로 땅 감지 루프를 추가하세요.
            while (landingState == ELandingState.Air && gameObject.activeInHierarchy)
            {
                if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                    return;

                // 내려오는 중이고, 발밑 레이가 지면을 찾으면 착지 처리
                var foot = new Vector2(transform.position.x, transform.position.y);
                var hit = Physics2D.Raycast(foot, Vector2.down, 1.0f, groundAndPlatformLayerMask);
                if (myRigidbody.linearVelocity.y <= 0 && hit.collider != null)
                    break;
            }

            // === 원복: 머티리얼/히트박스 ===
            physicsCollider.sharedMaterial = _originalMaterial;
            StandHitBox();
        }
    }

    private async UniTask MaintainForwardFor(float seconds, float dirSign)
    {
        float t = 0f;
        float minKeepSpeed = basicStat.moveSpeed * 0.75f; // 전진 유지 최소 속도

        while (t < seconds && landingState == ELandingState.Air && gameObject.activeInHierarchy)
        {
            var v = myRigidbody.linearVelocity;
            // X가 벽에 의해 0으로 깎이면 다시 살짝 밀어준다
            if (Mathf.Abs(v.x) < minKeepSpeed)
                myRigidbody.linearVelocity = new Vector2(minKeepSpeed * dirSign, v.y);

            t += Time.fixedDeltaTime;
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
    }

    // 플레이어쪽으로 점프
    private async void MonsterJump()
    {
        if (IsCanJump() && jumpInfo.playerInRange && jumpInfo.coolTime >= ConstValues.JumpCoolTime)
        {
            float minY = 0.2f;
            // 점프의 조건, 플레이어의 위치가 나보다 위에 있고, 캐릭터가 점프중이 아니다
            if (GameManager.Instance.CurPlayer.GetDownPosY() > transform.position.y + minY &&
                !GameManager.Instance.CurPlayer.GetJumpState())
            {
                Vector2 start = transform.position;
                var playerPos = GameManager.Instance.CurPlayer.transform.position;

                RaycastHit2D upRay =
                    Physics2D.Raycast(transform.position, Vector2.up, 3.0f, groundAndPlatformLayerMask);
                Debug.DrawRay(transform.position, Vector2.up * 3.0f, ConstValues.CyanColor, 0.02f);
                if (upRay.collider != null)
                    playerPos = new Vector2(GameManager.Instance.CurPlayer.transform.position.x, upRay.point.y);

                Vector2 end = new Vector2(playerPos.x, playerPos.y);
                if (myStat.standMotion)
                    end = new Vector2(transform.position.x, playerPos.y);

                if (Vector2.Distance(start, end) < 0.01f)
                    return;

                CancelMotion();
                LookAt(playerPos.x);
                stateCancellation = new CancellationTokenSource();
                StateSetting(ENormalState.Landing, ConstValues.Landing, ConstValues.Landing);
                MoveStateSetting(EMoveState.Stopping);

                var delay1 = 0.2f;
                if (await AttackDelay(delay1).SuppressCancellationThrow())
                    return;

                PlaySound(ConstValues.Jump1, 2.0f);
                LookAt(playerPos.x);
                StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
                float travelTime = 0.5f;
                Vector2 velocity = CalculateLaunchVelocity(start, end, travelTime);
                myRigidbody.linearVelocity = velocity;
            }
        }
    }

    private async void MonsterDownJump()
    {
        if (IsCanJump() && downJumpInfo.playerInRange && downJumpInfo.coolTime >= ConstValues.JumpCoolTime)
        {
            float minY = 0.2f;
            // 밑점의 조건, 플레이어의 위치가 나보다 밑에 있고, 밟고 있는 지면이 나와 다른 게임 오브젝트다
            if (GameManager.Instance.CurPlayer.GetDownPosY() < transform.position.y - minY &&
                GameManager.Instance.CurPlayer.GroundObject != GroundObject)
            {
                CancelMotion();
                var playerPos = GameManager.Instance.CurPlayer.transform.position;
                LookAt(playerPos.x);
                stateCancellation = new CancellationTokenSource();
                StateSetting(ENormalState.Landing, ConstValues.Landing, ConstValues.Landing);
                MoveStateSetting(EMoveState.Stopping);

                var delay1 = 0.2f;
                if (await AttackDelay(delay1).SuppressCancellationThrow())
                    return;

                DownJump();
            }
        }
    }

    protected async void MonsterBridgeJump()
    {
        if (IsCanJump())
        {
            var rayVector = new Vector2(transform.position.x + physicsCollider.size.x / 2, transform.position.y);
            if (transform.localScale.x < 0)
                rayVector = new Vector2(transform.position.x - physicsCollider.size.x / 2, transform.position.y);

            RaycastHit2D downRay = Physics2D.Raycast(rayVector, Vector2.down, 20.0f, groundAndPlatformLayerMask);
            Debug.DrawRay(rayVector, Vector2.down * 20.0f, ConstValues.OrangeColor, 0.02f);
            if (downRay.collider == null)
            {
                var verticalVector = new Vector2(rayVector.x, rayVector.y - 0.2f);
                RaycastHit2D verticalRay;
                if (transform.localScale.x > 0)
                {
                    verticalRay = Physics2D.Raycast(verticalVector, Vector2.right, 20.0f, groundAndPlatformLayerMask);
                    Debug.DrawRay(verticalVector, Vector2.right * 20.0f, ConstValues.OrangeColor, 0.02f);
                }
                else
                {
                    verticalRay = Physics2D.Raycast(verticalVector, Vector2.left, 20.0f, groundAndPlatformLayerMask);
                    Debug.DrawRay(verticalVector, Vector2.left * 20.0f, ConstValues.OrangeColor, 0.02f);
                }

                if (verticalRay.collider != null)
                {
                    if (agroState == EAgroState.Agro)
                    {
                        Vector2 start = transform.position;
                        var arrivePos = new Vector2(verticalRay.point.x + 1.0f, transform.position.y);
                        if (transform.localScale.x < 0)
                            arrivePos = new Vector2(verticalRay.point.x - 1.0f, transform.position.y);

                        Vector2 end = new Vector2(arrivePos.x, arrivePos.y);

                        if (Vector2.Distance(start, end) < 0.01f)
                            return;

                        CancelMotion();
                        LookAt(arrivePos.x);
                        stateCancellation = new CancellationTokenSource();
                        StateSetting(ENormalState.Landing, ConstValues.Landing, ConstValues.Landing);
                        MoveStateSetting(EMoveState.Stopping);

                        var delay1 = 0.2f;
                        if (await AttackDelay(delay1).SuppressCancellationThrow())
                            return;

                        PlaySound(ConstValues.Jump1, 2.0f);
                        LookAt(arrivePos.x);
                        StateSetting(ENormalState.Jump, ConstValues.Jump, ConstValues.Jump);
                        float travelTime = 0.5f;
                        Vector2 velocity = CalculateLaunchVelocity(start, end, travelTime);
                        myRigidbody.linearVelocity = velocity;
                    }
                    // 평소에 절벽이 있으면 뒤를 돌기
                    else if (agroState == EAgroState.Normal)
                    {
                        switch (transform.localScale.x)
                        {
                            case > 0:
                                Flip(-1);
                                break;
                            case < 0:
                                Flip(1);
                                break;
                        }
                    }
                }
            }
        }
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
    protected async UniTask WarningAreaSpawnCollider(Vector2 pos, Vector3 angle, BoxCollider2D targetCollider,
        float duration, Color color)
    {
        float scaleX = Mathf.Abs(targetCollider.gameObject.transform.localScale.x);
        float scaleY = Mathf.Abs(targetCollider.gameObject.transform.localScale.y);

        float posX = targetCollider.offset.x * scaleX;
        float posY = targetCollider.offset.y * scaleY;

        Vector2 finalPos = new Vector2(pos.x + posX, pos.y + posY);
        Vector2 finalScale = new Vector2(scaleX * targetCollider.size.x, scaleY * targetCollider.size.y);

        FadeSystem warningArea = WarningAreaSpawn(duration, color, finalPos, angle, finalScale);
        await warningArea.Fade();
    }

    // 위험영역 표시(궤적) 좌표, x크기, y크기, x축 높이, y축 높이
    protected async UniTask WarningAreaSpawnTrajectory(Vector2 startPos, Vector2 endPos, Vector3 angle, float duration,
        Color color, float thickness, float addDistance = 0)
    {
        float distance = Vector2.Distance(startPos, endPos);

        // (시작좌표 + 끝좌표) / 2를 생성 좌표로 설정
        Vector2 spawnPos = (startPos + endPos) * 0.5f;
        Vector2 spawnSize = new Vector2(distance + addDistance, thickness);

        FadeSystem warningArea = WarningAreaSpawn(duration, color, spawnPos, angle, spawnSize);
        await warningArea.Fade();
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

    protected async void OnCollisionEnter2D(Collision2D col)
    {
        // 착지
        if ((col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform)) &&
            landingState == ELandingState.Air)
        {
            if (myRigidbody.gravityScale == 0 || myRigidbody.linearVelocityY is >= 0.05f or <= -0.05f)
                return;

            LandingStateSetting(ELandingState.Ground);
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;
            groundObject = col.gameObject;

            // 점프도중, 또는 에어본 도중 지면에 닿았을 경우의 애니메이션 처리
            switch (normalState)
            {
                case ENormalState.Jump:
                    StateSetting(ENormalState.Landing, ConstValues.Landing, ConstValues.Landing);
                    MoveStateSetting(EMoveState.Stopping);

                    var delay1 = 0.3f;
                    if (await AttackDelay(delay1).SuppressCancellationThrow())
                        return;

                    IdleOrMove();
                    jumpInfo.coolTime = 0;
                    downJumpInfo.coolTime = 0;
                    break;

                case ENormalState.Airborne:
                    if (!myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.Die))
                        DownAndStand();
                    break;
            }
        }
    }

    // 버그 방지
    // protected void OnCollisionStay2D(Collision2D col)
    // {
    //     if ((col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform)) && landingState == ELandingState.Air)
    //     {
    //         if (myRigidbody.linearVelocityY != 0)
    //             return;
    //         
    //         LandingStateSetting(ELandingState.Ground);
    //         myRigidbody.bodyType = RigidbodyType2D.Dynamic;
    //         myRigidbody.linearVelocity = Vector2.zero;
    //         groundObject = col.gameObject;
    //         
    //         switch (normalState)
    //         {
    //             case ENormalState.Airborne:
    //                 if (!myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.Die))
    //                     DownAndStand();
    //                 break;
    //         }
    //     }
    //
    //     if ((col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform)) && landingState == ELandingState.Ground)
    //     {
    //         if(myAnimator.GetCurrentAnimatorStateInfo(0).IsName(ConstValues.AirborneDown) && myRigidbody.linearVelocityY == 0 && airborneCount == 0)
    //             DownAndStand();
    //     }
    // }

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
        }
    }
}