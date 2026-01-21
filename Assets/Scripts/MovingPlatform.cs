using System;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [SerializeField] private bool isCollision;
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isRepeat;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float accelerateDistance = 1f;
    [SerializeField] private float slowDownDistance = 1f;
    [SerializeField, Range(0f, 1f)] private float minSpeedMultiplier = 0.2f;

    private Rigidbody2D myRigidbody;
    private Collider2D myCollider;
    [SerializeField] private int targetIdx;
    private Vector2 prevPos;
    private Vector2 segmentStartPos;
    private Vector2 platformVelocity;
    private List<Vector2> movePos = new List<Vector2>();

    private Action arriveAction;
    public Transform[] Points => points;

    public bool IsMoving
    {
        get => isMoving;
        set => isMoving = value;
    }
    public bool IsRepeat
    {
        get => isRepeat;
        set => isRepeat = value;
    }

    public int TargetIdx
    {
        get => targetIdx;
        set => targetIdx = value;
    }
    public Vector2 Velocity => platformVelocity;  // 외부 제공용
    
    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        myRigidbody.bodyType = RigidbodyType2D.Kinematic;
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        prevPos = transform.position;
        segmentStartPos = prevPos;
        
        foreach (var point in points)
            movePos.Add(point.position);
    }

    private void FixedUpdate()
    {
        if (movePos.Count == 0 || !isMoving)
            return;
        
        Vector2 cur = myRigidbody.position;
        float distFromStart = (cur - segmentStartPos).magnitude;
        float distToTarget = (movePos[targetIdx] - cur).magnitude;
        float accelT = accelerateDistance > 0f ? Mathf.Clamp01(distFromStart / accelerateDistance) : 1f;
        float slowDownT = slowDownDistance > 0f ? Mathf.Clamp01(distToTarget / slowDownDistance) : 1f;
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, Mathf.Min(accelT, slowDownT));
        float currentSpeed = speed * speedMultiplier;
        Vector2 next = Vector2.MoveTowards(cur, movePos[targetIdx], currentSpeed * Time.fixedDeltaTime);

        myRigidbody.MovePosition(next);
        // 2) 이번 프레임 플랫폼이 실제로 이동한 delta
        Vector2 delta = next - prevPos;
        platformVelocity = delta / Time.fixedDeltaTime;
        prevPos = next;

        // 4) 목적지 도착하면 다음 포인트
        if ((next - movePos[targetIdx]).sqrMagnitude < 0.0001f)
        {
            transform.position = movePos[targetIdx];
            targetIdx = (targetIdx + 1) % movePos.Count;
            segmentStartPos = next;
            if (!isRepeat)
            {
                isMoving = false;
                platformVelocity = Vector2.zero;
            }

            if (arriveAction != null)
                arriveAction();
        }
    }
    
    public void SetSaveAction(Action action)
    {
        arriveAction = action;
    }
}
