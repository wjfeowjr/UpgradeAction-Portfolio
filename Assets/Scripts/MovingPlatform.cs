using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class MovingPlatform : Platform, IMovingPlatform
{
    [SerializeField] private Transform[] points;
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isRepeat;
    [SerializeField] private float delay;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float accelerateDistance = 1f;
    [SerializeField] private float slowDownDistance = 1f;
    [SerializeField, Range(0f, 1f)] private float minSpeedMultiplier = 0.2f;
    [SerializeField] private GameObject[] gearObjects;      // 톱니바퀴들 (짝수 인덱스: 반대, 홀수 인덱스: 기본 방향)
    [SerializeField] private float gearRotateDuration = 1f; // 기어 1회전에 걸리는 시간

    private float curDelay;
    private bool gearRotating;
    private Rigidbody2D myRigidbody;
    private int targetIdx;
    private Vector2 prevPos;
    private Vector2 segmentStartPos;
    private Vector2 platformVelocity;
    private List<Vector2> movePos = new List<Vector2>();
    [SerializeField] private PlatformObject platformObject;

    private Action arriveAction;
    public Transform[] Points => points;

    public PlatformObject PlatformObject
    {
        get => platformObject;
        set => platformObject = value;
    }

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
    public float Delay
    {
        get => delay;
        set => delay = value;
    }

    public int TargetIdx
    {
        get => targetIdx;
        set => targetIdx = value;
    }
    public Vector2 Velocity => platformVelocity;  // 외부 제공용
    
    protected override void Awake()
    {
        base.Awake();
        
        myRigidbody = GetComponent<Rigidbody2D>();
        myRigidbody.bodyType = RigidbodyType2D.Kinematic;
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        prevPos = transform.position;
        segmentStartPos = prevPos;
        
        foreach (var point in points)
            movePos.Add(point.position);

        SetPlatformObject();
    }

    private void Update()
    {
        UpdateDelay();
        SetHeight();
    }

    private void FixedUpdate()
    {
        if (movePos.Count == 0 || !isMoving)
        {
            myRigidbody.linearVelocity = Vector2.zero;
            platformVelocity = Vector2.zero;

            // 멈췄으면 톱니바퀴도 정지 + 45도 스냅
            if (gearRotating)
            {
                StopGearRotation();
                gearRotating = false;
            }
            return;
        }

        Vector2 cur = myRigidbody.position;
        Vector2 target = movePos[targetIdx];
        Vector2 toTarget = target - cur;
        float distToTarget = toTarget.magnitude;
        float distFromStart = (cur - segmentStartPos).magnitude;

        float accelT = accelerateDistance > 0f ? Mathf.Clamp01(distFromStart / accelerateDistance) : 1f;
        float slowDownT = slowDownDistance > 0f ? Mathf.Clamp01(distToTarget / slowDownDistance) : 1f;
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, Mathf.Min(accelT, slowDownT));
        float currentSpeed = speed * speedMultiplier;

        // 이번 프레임에 목적지 도달하면 정확히 스냅하고 정지
        if (distToTarget <= currentSpeed * Time.fixedDeltaTime || distToTarget < 0.0001f)
        {
            myRigidbody.linearVelocity = Vector2.zero;
            myRigidbody.position = target;
            platformVelocity = Vector2.zero;

            targetIdx = (targetIdx + 1) % movePos.Count;
            segmentStartPos = target;
            prevPos = target;
            isMoving = false;

            if (isRepeat && delay > 0)
                curDelay = 0;

            if (arriveAction != null)
                arriveAction();

            return;
        }

        Vector2 direction = toTarget / distToTarget;

        // 이동 시작 시 톱니바퀴 회전 시작 (이 구간의 방향 기준)
        if (!gearRotating)
        {
            StartGearRotation(direction.y > 0f);
            gearRotating = true;
        }

        Vector2 velocity = direction * currentSpeed;
        myRigidbody.linearVelocity = velocity;

        platformVelocity = velocity;
        prevPos = cur;
    }
    
    private void UpdateDelay()
    {
        if (delay == 0)
            return;
        
        if (curDelay < delay)
            curDelay += Time.deltaTime;

        if (curDelay > delay)
        {
            curDelay = delay;
            isMoving = true;
        }
    }

    private void SetPlatformObject()
    {
        var platform = new PlatformObject
        {
            collider = myBoxCollider,
            height = transform.position.y + myBoxCollider.size.y * 0.5f + myBoxCollider.offset.y - 0.2f,
        };
        platformObject = platform;
    }

    private void SetHeight()
    {
        if (platformObject != null)
            platformObject.height = transform.position.y + myBoxCollider.size.y * 0.5f + myBoxCollider.offset.y - 0.2f;
    }

    public void SetSaveAction(Action action)
    {
        arriveAction = action;
        platformVelocity = Vector2.zero;
    }

    // 톱니바퀴 회전 시작 (짝수 인덱스는 반대 방향, 홀수 인덱스는 기본 방향으로 무한 회전)
    private void StartGearRotation(bool goingUp)
    {
        if (gearObjects == null)
            return;

        float baseDir = goingUp ? 1f : -1f;
        for (int i = 0; i < gearObjects.Length; i++)
        {
            if (!gearObjects[i])
                continue;

            var t = gearObjects[i].transform;
            t.DOKill();

            float dir = (i % 2 == 0) ? -baseDir : baseDir;
            t.DOLocalRotate(new Vector3(0f, 0f, 360f * dir), gearRotateDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }
    }

    // 톱니바퀴 정지: 각각 가장 가까운 45도 배수로 0.2초에 걸쳐 스냅
    private void StopGearRotation()
    {
        if (gearObjects == null)
            return;

        foreach (var gear in gearObjects)
        {
            if (!gear)
                continue;

            var t = gear.transform;
            t.DOKill();

            float z = t.localEulerAngles.z;
            float snapped = Mathf.Round(z / 45f) * 45f;
            t.DOLocalRotate(new Vector3(0f, 0f, snapped), 0.2f, RotateMode.Fast);
        }
    }
}
