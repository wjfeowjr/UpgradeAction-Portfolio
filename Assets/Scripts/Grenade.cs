using System;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] private float launchSpeedX;
    [SerializeField] private float launchSpeedY;
    [SerializeField] private float angular;

    // 땅에 닿았을 때 적용할 선형 Drag
    [SerializeField] private float groundDrag;
    // 땅에 닿았을 때 적용할 Angular Drag
    [SerializeField] private float groundAngularDrag;
    // 정지로 간주할 속도 임계치
    [SerializeField] private float stopThreshold;

    // 지면 레이어
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D myRigidbody;
    private bool isGrounded;

    void Awake()
    {
        myRigidbody  = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // 상태 초기화
        isGrounded = false;
        myRigidbody.linearDamping = ConstValues.DefaultLinearDamping;
        myRigidbody.angularDamping = ConstValues.DefaultAngularDamping;

        // 발사
        myRigidbody.linearVelocity = new Vector2(launchSpeedX, launchSpeedY);
        myRigidbody.angularVelocity = (myRigidbody.linearVelocity.x >= 0) ? -angular : angular;
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
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌한 오브젝트가 groundLayer에 포함되면 접지 상태로 전환
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
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
}
