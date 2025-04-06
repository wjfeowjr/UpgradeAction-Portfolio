using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static EState;

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

// 애니메이션(껍데기) 모션
public enum EState
{
    Idle,
    Move,
    Jump,
    Attack,
    JumpAttack,
    Skill,
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
    private Animator myAnimator;
    protected int jumpAttackCount;

    [SerializeField] protected List<PlayerSkill> skillList = new List<PlayerSkill>();
    [SerializeField] protected List<GameObject> controlObject = new List<GameObject>();
        
    [SerializeField] protected bool nextAttack;
    [SerializeField] protected EState state;
    [SerializeField] protected EMoveState moveState;
    [SerializeField] protected EJumpState jumpState;
    [SerializeField] protected EBodyType bodyType;
    [SerializeField] private bool canFlip;
    [SerializeField] private bool canMove;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        InitSkill();
    }

    private void OnEnable()
    {
        // 최초 Idle상태로 전환
        StateSetting(EState.Idle, ConstValues.Idle, ConstValues.Idle);
    }

    private void Start()
    {
        DefaultSetting();
        StatSetting();
    }

    protected void Update()
    {
        UpdateFlip();
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

    private void ControlObjectAdd(GameObject obj)
    {
        controlObject.Add(obj);
    }
    protected void ControlObjectClear()
    {
        foreach (var obj in controlObject)
            obj.gameObject.SetActive(false);
        
        controlObject.Clear();
    }

    protected void StateSetting(EState changeState, string triggerName, string animId)
    {
        state = changeState;

        // None트리거는 무시된다
        if (triggerName != ConstValues.None)
        {
            if (triggerName == ConstValues.Normal)
            {
                if (myRigidbody.linearVelocity.y == 0)
                {
                    myAnimator.SetTrigger(ConstValues.Idle);
                    animId = ConstValues.Idle;
                }
                else
                {
                    myAnimator.SetTrigger(ConstValues.Jump);
                    animId = ConstValues.Jump;
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
            
            if(!SameBodyType(animationsData.bodyType))
                BodyTypeSetting(animationsData.bodyType);
        }
    }

    protected EState ParseState(string animId)
    {
        if (Enum.TryParse(animId, out EState parsedState))
            return parsedState;

        return EState.Idle;
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
            if (state == EState.Idle && jumpState == EJumpState.Landing)
                StateSetting(EState.Move, ConstValues.Move, ConstValues.Move);
            
            if(moveState == EMoveState.Stopping)
                MoveStateSetting(EMoveState.Moving);
        }
        
        //myRigidbody.velocity = new Vector3(dir * stat.speed, myRigidbody.velocity.y);
        transform.Translate(dir * (stat.speed * Time.deltaTime));
    }

    // 정지
    public void Stop()
    {
        if (state == EState.Move)
        {
            myAnimator.ResetTrigger(ConstValues.Move);
            StateSetting(EState.Idle, ConstValues.Idle, ConstValues.Idle);
        }
        
        if(moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);
    }
    // 행동 캔슬
    protected void CancelMotion()
    {
        stateCancellation?.Cancel();
        ControlObjectClear();
    }
    protected void CancelMotion(EState targetState)
    {
        if (state != targetState)
            return;
        
        stateCancellation?.Cancel();
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
        if (state is EState.Skill)
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
        if (jumpState == EJumpState.Jumping)
            return;
        
        jumpAttackCount = 0;
        CancelMotion(EState.Attack);
        StateSetting(EState.Jump, ConstValues.Jump, ConstValues.Jump);

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
        
        var attackData = TableManager.Instance.attack.Attack.Find(x => x.id == id);
        if (attackData != null)
        {
            var attack = obj.GetComponent<Attack>();
            if (!attack)
            {
                attack = obj.AddComponent<Attack>();
                attack.SetupData(this, attackData, attackTransform);
            }
            attack.EnableSetting();
            if(attack.GetObjectTime() == 0)
                ControlObjectAdd(attack.gameObject);
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
    protected void SpawnAttack(string id, Vector2 attackVector)
    {
        GameManager.Instance.SpawnToObjectPool(id, attackVector);
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
        PlayerSkill upperSlash = new PlayerSkill()
        {
            skillName = "올려베기",
            coolTime = 1,
            skillKeyCode = KeyCode.S,
        };
        skillList.Add(upperSlash);
        
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
                        StateSetting(EState.Idle, ConstValues.Idle, ConstValues.Idle);
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
