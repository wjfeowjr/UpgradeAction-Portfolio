using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class GrenadeInfo
{
    public string id;
    public Vector2 minForce;
    public Vector2 maxForce;
    public List<string> hitLayerList;
    public string spawnObject;
    public Action<string, Transform, int> explosionAction;
}
public class Grenade : MonoBehaviour
{
    [SerializeField] private Vector2 throwForce;
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
    private bool isGrounded;

    private void Awake()
    {
        myRigidbody  = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        angular = 720f;
        groundDrag = 1.0f;
        groundAngularDrag = 2.0f;
        stopThreshold = 0.1f;
    }

    private void OnEnable()
    {
        myCollider.enabled = true;
    }

    private void Update()
    {
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
    
    public void SetupData(GrenadeData grenadeData, Vector2 dir, Action<string, Transform, int> action)
    {
        if (grenadeInfo == null)
        {
            grenadeInfo = new GrenadeInfo();
            grenadeInfo.id = grenadeData.id;

            var minForceSplit = grenadeData.minForce.Split(';');
            grenadeInfo.minForce = new Vector2(float.Parse(minForceSplit[0]), float.Parse(minForceSplit[1]));
            
            var maxForceSplit = grenadeData.maxForce.Split(';');
            grenadeInfo.maxForce = new Vector2(float.Parse(maxForceSplit[0]), float.Parse(maxForceSplit[1]));

            var hitLayerSplit = grenadeData.hitLayer.Split(',');
            grenadeInfo.hitLayerList = new List<string>();
            foreach (var hitLayer in hitLayerSplit)
                grenadeInfo.hitLayerList.Add(hitLayer);
            
            grenadeInfo.spawnObject = grenadeData.spawnObject;
            grenadeInfo.explosionAction = action;
        }

        float xForce = Random.Range(grenadeInfo.minForce.x, grenadeInfo.maxForce.x);
        float yForce = Random.Range(grenadeInfo.minForce.y, grenadeInfo.maxForce.y);
        
        if (dir == Vector2.left)
            xForce = -Random.Range(grenadeInfo.minForce.x, grenadeInfo.maxForce.x);

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
    
    private void Delete()
    {
        if (grenadeInfo.spawnObject != ConstValues.None)
            grenadeInfo.explosionAction(grenadeInfo.spawnObject, transform, 0);

        myCollider.enabled = false;
        gameObject.SetActive(false);
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

    // private void OnCollisionExit2D(Collision2D collision)
    // {
    //     // groundLayer를 벗어나면 공중 상태로 되돌리기
    //     if (((1 << collision.gameObject.layer) & groundLayer) != 0)
    //     {
    //         isGrounded       = false;
    //         myRigidbody.drag          = DefaultLinearDamping;
    //         myRigidbody.angularDrag   = DefaultAngularDamping;
    //     }
    // }
    
    // 수류탄 소멸에만 관여(공격판정은 여기서 정하지 않는다)
    private void OnTriggerEnter2D(Collider2D col)
    {
        foreach (var hitTag in grenadeInfo.hitLayerList)
        {
            if (!col.gameObject.CompareTag(hitTag))
                continue;

            // 캐릭터들이 무적상태라면 무시한다
            if (hitTag is ConstValues.Player or ConstValues.Monster)
            {
                var character = col.GetComponent<Character>();
                if (character != null)
                {
                    if (character.Immortal)
                        return;
                }
            }

            Delete();
            return;
        }
    }
}
