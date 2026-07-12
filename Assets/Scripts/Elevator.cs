using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class Elevator : InteractionController, IMovingPlatform
{
    // Elevator 본체(이 GameObject)가 직접 움직인다. 상호작용 트리거(이 오브젝트)와
    // 자식 발판 콜라이더가 항상 함께 이동하도록 하기 위함.
    [SerializeField] private Collider2D myCollider;    // 상호작용 콜라이더
    [SerializeField] private Collider2D bottomCollider;         // 자식 발판 콜라이더 (탑승 판정/높이, 태그 Platform)
    [SerializeField] private GameObject colliderObject;         // 벽, 천장 세팅할 컴포지트 콜라이더
    [SerializeField] private GameObject[] wallObjects;       // 벽 오브젝트
    [SerializeField] private Transform[] points;            // 정차 포인트들 (Elevator 바깥의 고정 마커 권장)
    [SerializeField] private GameObject[] gearObjects;      // 톱니바퀴들 (짝수 인덱스: 반대, 홀수 인덱스: 기본 방향)
    [SerializeField] private float gearRotateDuration = 1f; // 기어 1회전에 걸리는 시간
    [SerializeField] private int startIndex = 2;            // 최초 시작 포인트 인덱스

    private CancellationTokenSource elevatorCancellation;
    private CancellationTokenSource wallCancellation;        // 벽 연출 대기용
    private Rigidbody2D myRigidbody;                          // 본체 Rigidbody2D (Kinematic)

    [Header("이동 설정")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float accelerateDistance = 1f;
    [SerializeField] private float slowDownDistance = 1f;
    [SerializeField, Range(0f, 1f)] private float minSpeedMultiplier = 0.2f;

    [Header("하강 안전 감지")]
    [SerializeField] private Vector2 playerCheckOffset = new Vector2(0f, -1f);  // 엘리베이터 원점 기준 감지 박스 중심
    [SerializeField] private Vector2 playerCheckSize = new Vector2(2.5f, 0.4f); // 감지 박스 크기

    [Header("연출")]
    [SerializeField] private AudioSource myAudioSource;
    [SerializeField] private Elevator_Lever[] levers;

    private Action startAction;
    private Action arriveAction;

    // 이동 상태
    private bool isMoving;
    private bool gearPaused;                    // 플레이어 감지로 톱니바퀴가 일시정지됐는지
    private bool playWallEffect;               // 이번 이동이 직접 상호작용(Operation)인지 → 벽 연출 여부
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

    // 신규 게임에서 엘리베이터 데이터를 처음 만들 때 사용할 최초 시작 인덱스
    public int StartIndex => PointCount > 0 ? Mathf.Clamp(startIndex, 0, PointCount - 1) : 0;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
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

        // 발판 위에서 좌우 이동이 수직 접촉 마찰에 끌리지 않도록 마찰 0 머티리얼 적용
        // (상승 가속 시 수직력이 커져 x 이동을 방해하는 문제 방지)
        if (bottomCollider)
            bottomCollider.sharedMaterial = new PhysicsMaterial2D("ElevatorFrictionless")
            {
                friction = 0f,
                bounciness = 0f
            };

        // 벽은 직접 상호작용으로 출발할 때만 생성되므로 시작 시 비활성
        if (colliderObject)
            colliderObject.SetActive(false);

        foreach (var wallObject in wallObjects)
            wallObject.SetActive(false);
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

        // 하강 중 발판 아래에 플레이어가 감지되면 이동 보류 (감지 안 되면 기존대로 진행)
        if (toTarget.y < 0f && IsPlayerBelow())
        {
            myRigidbody.linearVelocity = Vector2.zero;
            platformVelocity = Vector2.zero;

            // 톱니바퀴도 정지 (스냅 보정 없이)
            if (!gearPaused)
            {
                PauseGearRotation();
                myAudioSource.volume = 0;
            }

            return;   // isMoving 유지 → 플레이어가 비키면 다음 프레임에 재개
        }

        // 플레이어가 비켜 재개되면 톱니바퀴 회전도 재시작
        if (gearPaused)
        {
            StartGearRotation(toTarget.y > 0f);
            myAudioSource.volume = 1;
        }

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

            // 기어 정지 + 가장 가까운 45도 배수로 스냅
            StopGearRotation();

            // 직접 상호작용으로 출발한 경우에만 도착 시 벽 제거 연출
            if (playWallEffect)
            {
                WallClose();
                playWallEffect = false;
            }

            arriveAction?.Invoke();
            return;
        }

        Vector2 direction = toTarget / distToTarget;
        Vector2 velocity = direction * currentSpeed;
        myRigidbody.linearVelocity = velocity;

        platformVelocity = velocity;
        prevPos = cur;
    }

    // 하강 시 엘리베이터 아래에 플레이어가 있는지 감지
    // (토글되는 콜라이더에 의존하지 않도록 본체 원점 기준 박스로 검사)
    private bool IsPlayerBelow()
    {
        Vector2 origin = myRigidbody ? myRigidbody.position : (Vector2)transform.position;
        Vector2 center = origin + playerCheckOffset;

        foreach (var hit in Physics2D.OverlapBoxAll(center, playerCheckSize, 0f))
        {
            if (hit && hit.GetComponentInParent<Player>())
                return true;
        }

        return false;
    }

    // 감지 박스 시각화 (Scene 뷰에서 위치/크기 조정용)
    private void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying && myRigidbody ? myRigidbody.position : (Vector2)transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(origin + playerCheckOffset, playerCheckSize);
    }

    // 톱니바퀴 회전 시작 (짝수 인덱스는 반대 방향, 홀수 인덱스는 기본 방향으로 무한 회전)
    private void StartGearRotation(bool goingUp)
    {
        gearPaused = false;

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

    // 톱니바퀴 일시정지 (플레이어 감지로 멈출 때 — 스냅 보정 없이 현재 각도에서 멈춤)
    private void PauseGearRotation()
    {
        gearPaused = true;

        if (gearObjects == null)
            return;

        foreach (var gear in gearObjects)
            if (gear)
                gear.transform.DOKill();
    }

    // 톱니바퀴 정지 (도착 시 — 각각 가장 가까운 45도 배수로 0.2초에 걸쳐 스냅)
    private void StopGearRotation()
    {
        gearPaused = false;

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
            levers[i].SetState(i == curIdx);   // 현재 층 레버만 비활성, 나머지는 모두 활성
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

    // 엘리베이터 최초 위치 설정 (저장된 curIdx 위치에서 시작 = resume)
    public void PosSetting()
    {
        if (PointCount == 0 || !myRigidbody)
            return;

        int idx = Mathf.Clamp(curIdx, 0, PointCount - 1);
        Vector2 pos = points[idx].position;
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
        for (var i = 0; i < levers.Length; i++)
        {
            int idx = i;
            levers[i].SetAction(() => LeverAction(idx));
        }
    }

    // 엘리베이터 본체 상호작용(위 키): 핑퐁 순서로 다음 층 이동
    private void Operation()
    {
        startAction();
        if (isMoving)
            return;

        playWallEffect = true;   // 직접 상호작용 → 벽 연출 ON
        UpdateDirection();
        MoveTo(destIdx);
    }

    // 지정한 층(포인트)으로 직행 이동 (한 번에 그 층까지 이동 후 정지). 실제로 출발했는지 반환
    private bool MoveTo(int target)
    {
        if (isMoving)
            return false;
        if (PointCount <= 1)
            return false;

        target = Mathf.Clamp(target, 0, PointCount - 1);
        if (target == curIdx)
            return false;

        int prevIdx = curIdx;

        moveTargetIdx = target;
        segmentStartPos = myRigidbody.position;
        isMoving = true;

        // 기어 회전 시작 (올라갈 때와 내려갈 때 반대 방향)
        StartGearRotation(movePos[target].y > segmentStartPos.y);

        // 도착 예정 위치로 미리 갱신
        curIdx = target;
        UpdateDirection();   // Operation(본체 위 키)용 핑퐁 방향 갱신

        // 상태가 바뀌는 두 레버만 갱신 (이전 층 활성화, 도착 층 비활성화)
        SetLeverActive(prevIdx, true);
        SetLeverActive(target, false);

        // 직접 상호작용으로 출발한 경우에만 벽 생성 연출
        if (playWallEffect)
            WallOpen();

        SoundManager.Instance.PlaySound(ConstValues.Lever);
        //GameManager.Instance.ControlStart = false;
        myAudioSource.Play();
        return true;
    }

    // 출발 연출: wallObjects 활성화 → 마지막 오브젝트 먼저 펼치고(0.825), 끝나면 나머지를 펼친다(1)
    private async void WallOpen()
    {
        if (myCollider)
            myCollider.enabled = false;
        
        if (colliderObject)
            colliderObject.SetActive(true);

        if (wallObjects == null || wallObjects.Length == 0)
            return;

        // 활성화 + 스케일 0으로 초기화
        foreach (var wall in wallObjects)
        {
            if (!wall)
                continue;

            wall.SetActive(true);
            wall.transform.DOKill();
            wall.transform.localScale = Vector3.zero;
        }

        int lastIdx = wallObjects.Length - 1;

        // 1) 마지막 오브젝트 먼저 0.825로 확장
        if (wallObjects[lastIdx])
            wallObjects[lastIdx].transform.DOScale(0.825f, 0.2f);

        // 2) 마지막 오브젝트 연출이 끝날 때까지 대기
        wallCancellation = new CancellationTokenSource();
        if (await NormalDelay(0.2f, wallCancellation).SuppressCancellationThrow())
            return;

        // 3) 나머지 오브젝트들을 1로 확장
        for (int i = 0; i < lastIdx; i++)
        {
            if (!wallObjects[i])
                continue;

            wallObjects[i].transform.DOScale(1f, 0.2f);
        }
    }

    // 도착 연출: 마지막 오브젝트 먼저 축소(0) → 끝나면 나머지 축소(0) → 끝나면 비활성화
    private async void WallClose()
    {
        if (wallObjects == null || wallObjects.Length == 0)
        {
            if (colliderObject)
                colliderObject.SetActive(false);
            return;
        }

        int lastIdx = wallObjects.Length - 1;

        // 1) 나머지 오브젝트들을 먼저 0으로 축소
        for (int i = 0; i < lastIdx; i++)
        {
            if (!wallObjects[i])
                continue;

            wallObjects[i].transform.DOKill();
            wallObjects[i].transform.DOScale(0f, 0.1f);
        }

        // 2) 나머지 축소 연출이 끝날 때까지 대기
        wallCancellation = new CancellationTokenSource();
        if (await NormalDelay(0.1f, wallCancellation).SuppressCancellationThrow())
            return;

        // 3) 마지막 오브젝트를 가장 나중에 0으로 축소
        if (wallObjects[lastIdx])
        {
            wallObjects[lastIdx].transform.DOKill();
            wallObjects[lastIdx].transform.DOScale(0f, 0.1f);
        }

        // 4) 마지막 오브젝트 축소 연출이 끝날 때까지 대기
        wallCancellation = new CancellationTokenSource();
        if (await NormalDelay(0.1f, wallCancellation).SuppressCancellationThrow())
            return;

        // 5) 연출 종료 후 비활성화
        foreach (var wall in wallObjects)
            if (wall)
                wall.SetActive(false);

        if (myCollider)
            myCollider.enabled = true;
        
        if (colliderObject)
            colliderObject.SetActive(false);
    }

    private void SetLeverActive(int idx, bool active)
    {
        if (idx < 0 || idx >= levers.Length)
            return;

        levers[idx].SetState(!active);   // 활성(터치 가능) = isNotTouch false
        levers[idx].AnimSwitch();
    }

    public async void MovingStop()
    {
        myAudioSource.Stop();
        SoundManager.Instance.PlaySound(ConstValues.ElevatorHiss);
        elevatorCancellation = new CancellationTokenSource();
        if(await NormalDelay(1.0f, elevatorCancellation).SuppressCancellationThrow())
            return;

        isPlayerTouch = false;
        //GameManager.Instance.SaveGame();
    }

    // 레버: 해당 층으로 직행 호출 (현재 층과 다를 때만 작동). 실제로 작동했는지 반환
    private bool LeverAction(int floorIdx)
    {
        if (isMoving)
            return false;
        if (floorIdx == curIdx)
            return false;

        playWallEffect = false;  // 레버 → 벽 연출 OFF
        return MoveTo(floorIdx);
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

    // private void SetPlatformObject()
    // {
    //     platformObject = new PlatformObject
    //     {
    //         collider = bottomCollider,
    //         height = PlatformHeight(),
    //     };
    // }
    //
    // private void SetHeight()
    // {
    //     if (platformObject != null)
    //         platformObject.height = PlatformHeight();
    // }
    //
    // private float PlatformHeight()
    // {
    //     if (!bottomCollider)
    //         return 0f;
    //
    //     var t = bottomCollider.transform;
    //     return t.position.y + bottomCollider.size.y * 0.5f + bottomCollider.offset.y - 0.2f;
    // }
    
    // 일반 딜레이
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
