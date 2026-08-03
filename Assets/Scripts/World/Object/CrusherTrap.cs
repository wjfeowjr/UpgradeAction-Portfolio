using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CrusherTrap : MonoBehaviour
{
    [Header("오브젝트")]
    [SerializeField] private Transform crusher;   // 실제로 내려찍는 오브젝트
    [SerializeField] private Transform topPoint;  // 크러셔가 올라가는 높이
    [SerializeField] private Transform rayPoint;  // 레이캐스트를 발사하는 포인트

    [Header("딜레이")]
    [SerializeField] private float startDelay = 1f;       // 최초 딜레이(시작 시 1회)
    [SerializeField] private float waitDelay = 2f;        // 대기 딜레이(내려찍기 전 대기)
    [SerializeField] private float attackDelay = 0.1f;  // 내려찍고 다시 올라가기까지 대기
    [SerializeField] private float downWaitDelay = 0.4f;  // 내려찍고 다시 올라가기까지 대기

    [Header("속도")]
    [SerializeField] private float downSpeed = 40f;  // 내려찍는 속도
    [SerializeField] private float upSpeed = 5f;     // 올라가는 속도

    [Header("레이캐스트")]
    private int groundAndPlatformLayerMask;
    [SerializeField] private float rayDistance = 50f;   // 레이 최대 거리

    private Attack attack;
    
    private float topY;   // 올라간 상태의 Y
    private float downY;  // 내려찍었을 때의 Y(레이로 찾은 바닥 지점)
    
    private CancellationTokenSource crusherCancellation;

    private void Awake()
    {
        groundAndPlatformLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform));
    }

    private void Start()
    {
        topY = topPoint.position.y;

        // rayPoint에서 아래로 레이캐스트 → Ground 레이어 바닥 지점 탐지
        RaycastHit2D hit = Physics2D.Raycast(rayPoint.position, Vector2.down, rayDistance, groundAndPlatformLayerMask);
        if (hit)
        {
            downY = hit.point.y - 0.15f;
        }
        else
        {
            downY = topY; // 바닥을 못 찾으면 제자리(이동 없음)
            Debug.LogWarning($"[CrusherTrap] {name}: Ground를 찾지 못했습니다. 레이 거리/레이어를 확인하세요.");
        }

        // 시작 위치를 올라간 높이로 맞춤
        SetCrusherY(topY);
    }

    private void OnEnable()
    {
        SetCrusherY(topY);
        crusherCancellation = new CancellationTokenSource();
        CrushLoop().Forget();
    }

    private void OnDisable()
    {
        crusherCancellation?.Cancel();
        crusherCancellation?.Dispose();
        crusherCancellation = null;
    }

    private void Update()
    {
        if (attack == null)
            attack = crusher.GetComponent<Attack>();
    }

    private void OnDestroy()
    {
        crusherCancellation?.Cancel();
        crusherCancellation?.Dispose();
    }

    private async UniTaskVoid CrushLoop()
    {
        // 최초 딜레이는 시작 시 1회만 적용
        if (await NormalDelay(startDelay, crusherCancellation).SuppressCancellationThrow())
            return;
        
        if (attack)
            attack.ColliderActive(false);

        while (true)
        {
            // 대기 딜레이
            if (await NormalDelay(waitDelay, crusherCancellation).SuppressCancellationThrow())
                return;

            if (attack)
                attack.ColliderActive(true);
            
            // 바닥(레이 포인트)까지 내려찍기
            if (await MoveY(downY, downSpeed).SuppressCancellationThrow())
                return;
            
            // 쾅
            Vector2 effectPos = new Vector2(transform.position.x, downY);
            SpawnEffect(ConstValues.TrapCrusherEffect, effectPos);
            
            // 살짝 남아있는 공격판정
            if (await NormalDelay(attackDelay, crusherCancellation).SuppressCancellationThrow())
                return;

            if (attack)
                attack.ColliderActive(false);
            
            // 내려찍은 채로 대기
            if (await NormalDelay(downWaitDelay, crusherCancellation).SuppressCancellationThrow())
                return;

            // 다시 올라가기
            if (await MoveY(topY, upSpeed).SuppressCancellationThrow())
                return;
        }
    }

    // 크러셔를 지정한 Y까지 일정 속도로 이동
    private async UniTask MoveY(float targetY, float speed)
    {
        while (Mathf.Abs(crusher.position.y - targetY) > 0.001f)
        {
            float y = Mathf.MoveTowards(crusher.position.y, targetY, speed * Time.deltaTime);
            SetCrusherY(y);
            await YieldDelay(crusherCancellation);
        }
        SetCrusherY(targetY);
    }

    // X/Z는 유지하고 Y만 변경
    private void SetCrusherY(float y)
    {
        var p = crusher.position;
        p.y = y;
        crusher.position = p;
    }
    
    // 오브젝트 소환
    public void SpawnEffect(string effectId, Vector2 pos)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(effectId, pos);
        
        var objectData = TableManager.Instance.GetSpawnedObject(effectId);
        if (objectData != null)
        {
            var spawnedObject = obj.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = obj.AddComponent<SpawnedObject>();

            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
        }
    }
    
    // 일반 딜레이
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    private async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }
}
