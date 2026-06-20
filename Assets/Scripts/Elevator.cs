using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Elevator : InteractionController, IMovingPlatform
{
    // Elevator 본체(이 GameObject)가 직접 움직인다. 상호작용 트리거(이 오브젝트)와
    // 자식 발판 콜라이더가 항상 함께 이동하도록 하기 위함.
    [SerializeField] private BoxCollider2D platformCollider;  // 자식 발판 콜라이더 (탑승 판정/높이, 태그 Platform)
    [SerializeField] private Transform[] points;              // 정차 포인트들 (Elevator 바깥의 고정 마커 권장)

    private Rigidbody2D myRigidbody;                          // 본체 Rigidbody2D (Kinematic)

    [Header("이동 설정")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float accelerateDistance = 1f;
    [SerializeField] private float slowDownDistance = 1f;
    [SerializeField, Range(0f, 1f)] private float minSpeedMultiplier = 0.2f;

    [Header("연출")]
    [SerializeField] private AudioSource myAudioSource;
    [SerializeField] private Elevator_Lever[] levers;

    private Action startAction;
    private Action arriveAction;

    // 이동 상태
    private bool isMoving;
    private int moveTargetIdx;                 // 이번 구간에서 도달할 포인트 인덱스
    private Vector2 platformVelocity;
    private Vector2 segmentStartPos;
    private Vector2 prevPos;
    private readonly List<Vector2> movePos = new List<Vector2>();
    private PlatformObject platformObject;

    // 핑퐁 순회 상태
    private int curIdx;        // 현재 정차한 포인트
    private int destIdx;       // 다음으로 이동할 목표 포인트
    private int moveDir = 1;   // +1: 정방향, -1: 역방향

    private int PointCount => points != null ? points.Length : 0;

    // IMovingPlatform
    public Vector2 Velocity => platformVelocity;
    public PlatformObject PlatformObject => platformObject;

    // 저장/복원용 값. 현재 정차한 포인트 인덱스를 의미한다.
    public int TargetIdx
    {
        get => curIdx;
        set => curIdx = value;
    }

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        if (myRigidbody)
        {
            myRigidbody.bodyType = RigidbodyType2D.Kinematic;
            myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            segmentStartPos = myRigidbody.position;
            prevPos = segmentStartPos;
        }

        movePos.Clear();
        if (points != null)
        {
            foreach (var point in points)
                movePos.Add(point.position);
        }

        SetPlatformObject();
    }

    private void Update()
    {
        SetHeight();
    }

    private void FixedUpdate()
    {
        if (movePos.Count == 0 || !isMoving || !myRigidbody)
        {
            if (myRigidbody)
                myRigidbody.linearVelocity = Vector2.zero;
            platformVelocity = Vector2.zero;
            return;
        }

        Vector2 cur = myRigidbody.position;
        Vector2 target = movePos[moveTargetIdx];
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

            segmentStartPos = target;
            prevPos = target;
            isMoving = false;

            arriveAction?.Invoke();
            return;
        }

        Vector2 direction = toTarget / distToTarget;
        Vector2 velocity = direction * currentSpeed;
        myRigidbody.linearVelocity = velocity;

        platformVelocity = velocity;
        prevPos = cur;
    }

    public override void RefreshTalkText()
    {
        foreach (var lever in levers)
            lever.RefreshTalkText();

        base.RefreshTalkText();
    }

    public void SetUpDown(int idx)
    {
        curIdx = PointCount > 0 ? Mathf.Clamp(idx, 0, PointCount - 1) : 0;
        UpdateDirection();

        for (var i = 0; i < levers.Length; i++)
        {
            levers[i].SetState(i != destIdx);   // 다음 목표 포인트의 레버만 활성
            levers[i].SetInteractionAction();
        }
    }

    // 현재 위치(curIdx) 기준으로 핑퐁 방향과 다음 목표 포인트를 계산
    private void UpdateDirection()
    {
        int count = PointCount;
        if (count <= 1)
        {
            destIdx = curIdx;
            return;
        }

        if (curIdx <= 0)
            moveDir = 1;
        else if (curIdx >= count - 1)
            moveDir = -1;

        destIdx = Mathf.Clamp(curIdx + moveDir, 0, count - 1);
    }

    // 엘리베이터 최초 위치 설정
    public void PosSetting()
    {
        if (PointCount == 0 || !myRigidbody)
            return;

        int idx = Mathf.Clamp(curIdx, 0, PointCount - 1);
        Vector2 pos = points[2].position;
        myRigidbody.transform.position = pos;
        myRigidbody.position = pos;
        segmentStartPos = pos;
        prevPos = pos;
    }

    public void SetInteractionAction()
    {
        SetInteractionAction(Operation, 30003, GameManager.Instance.upKey);
    }

    public void SetLeverAction()
    {
        foreach (var lever in levers)
            lever.SetAction(LeverAction);
    }

    // 엘베 작동
    private void Operation()
    {
        startAction();
        MovingStart();
    }

    // 포인트 사이를 왕복 이동 (한 번에 다음 포인트까지 이동 후 정지)
    private void MovingStart()
    {
        if (isMoving)
            return;
        if (PointCount <= 1)
            return;

        moveTargetIdx = destIdx;                // 이번에 이동할 목표 포인트
        segmentStartPos = myRigidbody.position;
        isMoving = true;

        // 도착 시점 상태로 미리 갱신 (핑퐁 방향 및 다음 목표 포인트)
        curIdx = moveTargetIdx;
        UpdateDirection();

        for (var i = 0; i < levers.Length; i++)
        {
            levers[i].SetState(i != destIdx);   // 도착 후 다음 목표 포인트의 레버만 활성
            levers[i].AnimSwitch();
        }
        SoundManager.Instance.PlaySound(ConstValues.Lever);
        GameManager.Instance.ControlStart = false;
        myAudioSource.Play();
    }

    public async void MovingStop()
    {
        myAudioSource.Stop();
        SoundManager.Instance.PlaySound(ConstValues.ElevatorHiss);
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

        isPlayerTouch = false;
        GameManager.Instance.CurPlayer.MyRigidbody.WakeUp();
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.SaveGame();
    }

    // 레버를 건드릴 때 나오는 액션
    private void LeverAction()
    {
        if (isMoving)
            return;

        MovingStart();
    }

    public void SetAction(Action getAction)
    {
        startAction = getAction;
    }

    public void SetSaveAction(Action getAction)
    {
        arriveAction = getAction;
        platformVelocity = Vector2.zero;
    }

    private void SetPlatformObject()
    {
        platformObject = new PlatformObject
        {
            collider = platformCollider,
            height = PlatformHeight(),
        };
    }

    private void SetHeight()
    {
        if (platformObject != null)
            platformObject.height = PlatformHeight();
    }

    private float PlatformHeight()
    {
        if (!platformCollider)
            return 0f;

        var t = platformCollider.transform;
        return t.position.y + platformCollider.size.y * 0.5f + platformCollider.offset.y - 0.2f;
    }
}
