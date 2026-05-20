using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[Serializable]
public class GrenadeInfo
{
    public string id;
    public Vector2 minForce;
    public Vector2 maxForce;
    public bool dirObject;
    public List<string> hitTagList;
    public List<string> spawnObjectList;
    public Action<string, Transform, int, Vector2> explosionAction;
    public Action<string, Vector2> blockAction;
    public bool isBossProjectile;
}
public class Grenade : MonoBehaviour, IProjectile
{
    [SerializeField] private Vector2 throwForce;
    [SerializeField] private Vector2 dir;
    [SerializeField] private float angular;

    // 땅에 닿았을 때 적용할 선형 Drag
    [SerializeField] private float groundDrag;
    // 땅에 닿았을 때 적용할 Angular Drag
    [SerializeField] private float groundAngularDrag;
    // 정지로 간주할 속도 임계치
    [SerializeField] private float stopThreshold;
    // 수류탄 정보
    [SerializeField] private GrenadeInfo grenadeInfo;

    private Rigidbody2D myRigidbody;
    private Collider2D myCollider;
    private bool isDelete;
    private bool isGrounded;
    private float timer;

    private void Awake()
    {
        myRigidbody  = GetComponent<Rigidbody2D>();
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        myCollider = GetComponent<Collider2D>();
        
        angular = 720f;
        groundDrag = 1.0f;
        groundAngularDrag = 2.0f;
        stopThreshold = 0.1f;
    }

    private void OnEnable()
    {
        myCollider.enabled = true;
        isDelete = false;
        timer = 0;
    }

    private void Update()
    {
        Timer();
        VelocityControl();
    }

    private void FixedUpdate()
    {
        if (!isGrounded)
            return;
        
        if (myRigidbody.linearVelocity == Vector2.zero && myRigidbody.angularVelocity == 0f)
            return;

        // 속도가 충분히 작아지면 완전 정지
        if (myRigidbody.linearVelocity.magnitude < stopThreshold)
            myRigidbody.linearVelocity = Vector2.zero;

        if (Mathf.Abs(myRigidbody.angularVelocity) < stopThreshold)
            myRigidbody.angularVelocity = 0f;
    }
    
    // 인터페이스 함수
    public bool IsBoss()
    {
        return grenadeInfo.isBossProjectile;
    }
    public void Delete()
    {
        gameObject.SetActive(false);
        grenadeInfo.blockAction?.Invoke(ConstValues.ProjectileDestroyEffect, transform.position);
    }
    
    public void SetupData(GrenadeData grenadeData, Vector2 grenadeDir, Action<string, Transform, int, Vector2> action, Action<string, Vector2> blockAction = null)
    {
        grenadeInfo = new GrenadeInfo();
        grenadeInfo.id = grenadeData.id;

        var minForceSplit = grenadeData.minForce.Split(';');
        grenadeInfo.minForce = new Vector2(float.Parse(minForceSplit[0]), float.Parse(minForceSplit[1]));
            
        var maxForceSplit = grenadeData.maxForce.Split(';');
        grenadeInfo.maxForce = new Vector2(float.Parse(maxForceSplit[0]), float.Parse(maxForceSplit[1]));

        grenadeInfo.dirObject = grenadeData.dirObject;

        grenadeInfo.hitTagList = new List<string>();
        var hitTagSplit = grenadeData.hitTag.Split(',');
        foreach (var hitTag in hitTagSplit)
        {
            if (!string.IsNullOrWhiteSpace(hitTag))
            {
                grenadeInfo.hitTagList.Add(hitTag);
            }
        }
        grenadeInfo.spawnObjectList = new List<string>();
        var spawnObjectSplit = grenadeData.spawnObject.Split(',');
        foreach (var spawnObject in spawnObjectSplit)
        {
            if (!string.IsNullOrWhiteSpace(spawnObject))
            {
                grenadeInfo.spawnObjectList.Add(spawnObject);
            }
        }
        
        
        grenadeInfo.explosionAction = action;
        grenadeInfo.blockAction = blockAction;

        dir = grenadeDir;
        SetThrowPos();
    }
    public void BossCheck(bool isBoss)
    {
        grenadeInfo.isBossProjectile = isBoss;
    }

    private void SetThrowPos()
    {
        float xForce = Random.Range(grenadeInfo.minForce.x, grenadeInfo.maxForce.x);
        float yForce = Random.Range(grenadeInfo.minForce.y, grenadeInfo.maxForce.y);

        if (grenadeInfo.dirObject)
        {
            if (dir == Vector2.left)
                xForce = -Random.Range(grenadeInfo.minForce.x, grenadeInfo.maxForce.x);
        }
        
        throwForce = new Vector2(xForce, yForce);
    }
    
    public void Throw()
    {
        // 상태 초기화
        isGrounded = false;
        myRigidbody.linearDamping = ConstValues.DefaultLinearDamping;
        myRigidbody.angularDamping = ConstValues.DefaultAngularDamping;

        // 발사
        myRigidbody.linearVelocity = new Vector2(throwForce.x, throwForce.y);
        myRigidbody.angularVelocity = (myRigidbody.linearVelocity.x >= 0) ? -angular : angular;
    }

    public void TargetThrow(Vector2 target)
    {
        // 상태 초기화
        isGrounded = false;
        myRigidbody.linearDamping = ConstValues.DefaultLinearDamping;
        myRigidbody.angularDamping = ConstValues.DefaultAngularDamping;
        
        // 발사
        myRigidbody.linearVelocity = CalculateVelocity(transform.position, target, throwForce.y);
        myRigidbody.angularVelocity = (myRigidbody.linearVelocity.x >= 0) ? -angular : angular;
    }
    
    public void RandomForceThrow(float valueX, float valueY)
    {
        var pastMaxPos = grenadeInfo.maxForce;
        var pastMinPos = grenadeInfo.minForce;
        grenadeInfo.maxForce = new Vector2(pastMaxPos.x + valueX, pastMaxPos.y + valueY);
        grenadeInfo.minForce = new Vector2(pastMinPos.x - valueX, pastMinPos.y - valueY);
        SetThrowPos();
        Throw();
    }

    private void Timer()
    {
        timer += Time.deltaTime;
    }
    // 최대 중력가속도 조정
    private void VelocityControl()
    {
        if (myRigidbody.bodyType == RigidbodyType2D.Dynamic && myRigidbody.linearVelocity.y < -30)
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -30);
    }
    
    private void Bounce(Collision2D collision)
    {
        // 충돌 지점의 노멀 벡터로 속도 반사
        Vector2 inVelocity = myRigidbody.linearVelocity;
        Vector2 normal = collision.GetContact(0).normal;
        myRigidbody.linearVelocity = Vector2.Reflect(inVelocity, normal);

        // 회전 방향도 반대로
        myRigidbody.angularVelocity = -myRigidbody.angularVelocity;
    }
    
    public void AddSpawnObject(string id)
    {
        grenadeInfo.spawnObjectList.Add(id);
    }

    private async void Explosion()
    {
        if (isDelete)
            return;
        isDelete = true;
        
        await UniTask.Yield();
        
        foreach (var spawnObject in grenadeInfo.spawnObjectList)
        {
            if (!string.IsNullOrWhiteSpace(spawnObject))
            {
                grenadeInfo.explosionAction(spawnObject, transform, 0, default);
            }
        }

        myCollider.enabled = false;
        gameObject.SetActive(false);
    }

    private Vector2 CalculateVelocity(Vector2 start, Vector2 target, float height)
    {
        // 중력 가속도 (Unity 프로젝트 설정에 따름, 보통 -9.81)
        float gravity = Physics2D.gravity.y;
        
        // 목표가 시작점보다 위에 있을 경우를 대비해, 최고점은 둘 중 높은 곳 + 추가 높이로 설정
        float maxY = Mathf.Max(start.y, target.y) + height;
        
        // 1. 수직 속도 (Vy) 계산: 시작점에서 최고점(maxY)까지 도달하는 데 필요한 속도
        // 공식: v^2 = 2 * g * h
        float displacementY_up = maxY - start.y;
        Vector2 velocityY = Vector2.up * Mathf.Sqrt(-2 * gravity * displacementY_up);

        // 2. 총 소요 시간 계산
        // 최고점까지 올라가는 시간 (t_up)
        float timeUp = Mathf.Sqrt(-2 * displacementY_up / gravity);
        
        // 최고점에서 목표점까지 떨어지는 시간 (t_down)
        float displacementY_down = maxY - target.y;
        float timeDown = Mathf.Sqrt(-2 * displacementY_down / gravity);
        
        float totalTime = timeUp + timeDown;

        // 3. 수평 속도 (Vx) 계산: 총 시간 동안 수평 거리를 이동해야 함
        Vector2 displacementX = new Vector2(target.x - start.x, 0);
        Vector2 velocityX = displacementX / totalTime;

        return velocityX + velocityY;
    }
    
    private void OnCollisionEnter2D(Collision2D col)
    {
        // 충돌한 오브젝트가 groundLayer에 포함되면 접지 상태로 전환
        if (col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform))
        {
            isGrounded = true;
            myRigidbody.linearDamping = groundDrag;
            myRigidbody.angularDamping = groundAngularDrag;
        }
    }

    // 수류탄 소멸에만 관여(공격판정은 여기서 정하지 않는다)
    private void OnTriggerEnter2D(Collider2D col)
    {
        foreach (var hitTag in grenadeInfo.hitTagList)
        {
            if (!col.gameObject.CompareTag(hitTag))
                continue;
            
            if(hitTag is ConstValues.Ground or ConstValues.Platform && timer < 0.1f)
                continue;

            // 캐릭터들이 무적상태라면 무시한다
            if (hitTag is ConstValues.Player or ConstValues.Monster)
            {
                var character = col.GetComponent<Character>();
                if (character != null)
                {
                    if (character.Immortal || character.IsDie)
                        return;
                }
                
                // 이 부분 기억 (플레이어의 물리 판정)
                if (!col.isTrigger)
                    return;
            }
            
            Explosion();
            return;
        }
    }
}
