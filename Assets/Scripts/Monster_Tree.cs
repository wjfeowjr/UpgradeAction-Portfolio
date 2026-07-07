using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Monster_Tree : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform upperCutPos;
    [SerializeField] private Transform readyEffectPos;
    
    private CancellationTokenSource rootCancellation;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                Step();
                break;
            case 1:
                BigRoot();
                break;
            case 2:
                GroundRoot();
                break;
            case 3:
                RollingSpike();
                break;
        }
    }

    // 스탭 이동(살짝 점프해서 앞/뒤로 이동)
    private async void Step()
    {
        float delay1 = 0.15f;
        float delay2 = 0.2f;
        
        float stepDistance = 3.0f;
        float travelTime = 0.2f;
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        
        int rand = Random.Range(0, 2);
        if (Vector2.Distance(transform.position, RayCenterVector()) > 3)
            rand = Random.Range(0, 3);
        
        if (rand is 0 or 1)
        {
            bool isFrontStep = rand == 0;
            float forward = transform.localScale.x > 0 ? 1f : -1f;
            float stepDirection;
        
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return;
        
            if (isFrontStep)
                stepDirection = forward;
            else
                stepDirection = -forward;
        
            // 스탭 방향으로 벽(Ground) 감지
            RaycastHit2D wallHit = Physics2D.Raycast(centerPos.position, Vector2.right * stepDirection, stepDistance, groundLayerMask);

            // 살짝 점프하며 스탭 이동
            LandingStateSetting(ELandingState.Air);
            Vector2 start = transform.position;
            Vector2 end = new Vector2(start.x + stepDirection * stepDistance, start.y);
            SpawnObject($"{basicStat.id}_{ConstValues.Jump}_{ConstValues.Effect}", transform.position);
            
            // 벽이 감지되면 방 중앙으로 스탭(체공시간을 늘려서 크게 점프)
            if (wallHit.collider)
            {
                end = new Vector2(RayCenterVector().x, start.y);
                SetTriggerAnimator(ConstValues.Pattern);
                LookAt(RayCenterVector().x);
            
                float jumpHeight = 2.0f;  // travelTime 대신 높이로 제어
                myRigidbody.linearVelocity = CalculateLaunchVelocityByHeight(start, end, jumpHeight);
            }
            else
            {
                if (isFrontStep)
                    SetTriggerAnimator(ConstValues.Pattern);
                else
                    SetTriggerAnimator(ConstValues.Pattern2);
            
                myRigidbody.linearVelocity = CalculateLaunchVelocity(start, end, travelTime);
            }
            
            // 지면에서 떨어진 뒤 다시 착지할 때까지 대기
            if (await WaitUntilDelay(() => !isGrounded, stateCancellation).SuppressCancellationThrow())
                return;
        
            if (await WaitUntilDelay(() => isGrounded, stateCancellation).SuppressCancellationThrow())
                return;

            // 착지
            if (wallHit.collider)
            {
                SetTriggerAnimator(ConstValues.Pattern);
            }
            else
            {
                if (isFrontStep)
                    SetTriggerAnimator(ConstValues.Pattern);
                else
                    SetTriggerAnimator(ConstValues.Pattern2);
            }
        }
        else if (rand == 2)
        {
            Vector2 start = transform.position;
            Vector2 end = new Vector2(RayCenterVector().x, start.y);
            
            SetTriggerAnimator(ConstValues.Pattern);
            LookAt(RayCenterVector().x);
            
            float jumpHeight = 2.0f;  // travelTime 대신 높이로 제어
            SpawnObject($"{basicStat.id}_{ConstValues.Jump}_{ConstValues.Effect}", transform.position);
            myRigidbody.linearVelocity = CalculateLaunchVelocityByHeight(start, end, jumpHeight);
            
            // 지면에서 떨어진 뒤 다시 착지할 때까지 대기
            if (await WaitUntilDelay(() => !isGrounded, stateCancellation).SuppressCancellationThrow())
                return;
        
            if (await WaitUntilDelay(() => isGrounded, stateCancellation).SuppressCancellationThrow())
                return;

            // 착지
            SetTriggerAnimator(ConstValues.Pattern);
        }

        SpawnObject($"{basicStat.id}_{ConstValues.Landing}_{ConstValues.Effect}", transform.position);
        LandingStateSetting(ELandingState.Ground);
        myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocity.y);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        //LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        PatternEnd();
    }
    
    // 거대 뿌리
    private async void BigRoot()
    {
        float delay1 = 1.0f;
        float delay2 = 1.0f;
        
        // 플레이어를 먼저 바라본 뒤 '뒤쪽'을 계산
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);

        int rand = Random.Range(0, 4);

        switch (rand)
        {
            case 0:
                StraightRoot();
                break;
            case 1:
                StraightRoot();
                break;
            case 2:
                UpperRoot();
                break;
            case 3:
                DropRoot();
                break;
        }

        int voiceRand = Random.Range(0, 2);
        switch (voiceRand)
        {
            case 0:
                PlaySound($"{basicStat.id}_{ConstValues.Voice}2");
                break;
            case 1:
                PlaySound($"{basicStat.id}_{ConstValues.Voice}3");
                break;
        }
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }

    private async void GroundRoot()
    {
        float delay1 = 1.0f;
        float delay2 = 1.0f;

        int rootCount = 6;
        float minGapX = 2.0f;
        int maxRetry = 30;

        // 양옆 벽(Ground) 감지로 x 범위 산출
        RaycastHit2D leftHit = Physics2D.Raycast(centerPos.position, Vector2.left, 100, groundLayerMask);
        RaycastHit2D rightHit = Physics2D.Raycast(centerPos.position, Vector2.right, 100, groundLayerMask);

        // 서로 x거리가 minGapX 이상 떨어진 바닥 랜덤 지점 rootCount개 지정
        Vector2[] rootVectors = new Vector2[rootCount];
        int retry = 0;
        for (int i = 0; i < rootCount; i++)
        {
            float randX = Random.Range(leftHit.point.x, rightHit.point.x);

            // 이미 지정된 지점과 x거리가 minGapX 미만이면 다시 뽑는다
            bool tooClose = false;
            for (int j = 0; j < i; j++)
            {
                if (Mathf.Abs(randX - rootVectors[j].x) < minGapX)
                {
                    tooClose = true;
                    break;
                }
            }

            // 방이 좁아 자리를 못 찾는 경우 무한루프가 되지 않도록 재시도 횟수 제한
            if (tooClose && retry < maxRetry)
            {
                retry++;
                i--;
                continue;
            }

            rootVectors[i] = new Vector2(randX, transform.position.y);
        }

        foreach (var rootVector in rootVectors)
            SpawnObject($"{basicStat.id}_{ConstValues.Attack2}_{ConstValues.Warning}", rootVector);
        
        GameManager.Instance.CameraShake(0.1f, 0.1f, 0.2f);
        PlaySound($"{basicStat.id}_{ConstValues.Voice}3");
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        foreach (var rootVector in rootVectors)
            GroundRootAttack(rootVector);
        
        GameManager.Instance.CameraShake(0.4f, 0.3f, 0.3f);
        PlaySound($"{basicStat.id}_{ConstValues.Attack2}");

        SetTriggerAnimator(ConstValues.Pattern);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        PatternEnd();
    }

    private void StraightRoot()
    {
        // 스폰 파이프라인이 localScale에 따라 각도를 자동 반전시키므로 오른쪽 기준 각도만 넘긴다
        int angleZ = -90;
        Vector2 dir = transform.localScale.x > 0 ? Vector2.left : Vector2.right;
        Vector2 rayPos = new Vector2(centerPos.position.x, centerPos.position.y - 0.5f);
        
        RaycastHit2D wallHit = Physics2D.Raycast(rayPos, dir, 20, groundLayerMask);
        BigRootAttack(wallHit.point, angleZ);
    }
    
    private void UpperRoot()
    {
        int angleZ = Random.Range(-15, -45);
        var playerVector = new Vector2(GameManager.Instance.CurPlayer.transform.position.x, transform.position.y);
        BigRootAttack(playerVector, angleZ,14.5f);
    }
    
    private void DropRoot()
    {
        // 양옆 벽(Ground) 감지로 x 범위 산출
        RaycastHit2D leftHit = Physics2D.Raycast(centerPos.position, Vector2.left, 100, groundLayerMask);
        RaycastHit2D rightHit = Physics2D.Raycast(centerPos.position, Vector2.right, 100, groundLayerMask);

        // 위쪽 천장(Ground) 감지로 y 산출
        RaycastHit2D upHit = Physics2D.Raycast(centerPos.position, Vector2.up, 100, groundLayerMask);

        // 천장의 랜덤 지점
        Vector2 rootVector = new Vector2(Random.Range(leftHit.point.x, rightHit.point.x), upHit.point.y);

        // 뿌리(기본값 위쪽 수직)가 플레이어를 향하도록 각도 계산
        Vector2 dir = (Vector2)GameManager.Instance.CurPlayer.CenterPos.position - rootVector;
        int angleZ = Mathf.RoundToInt(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90);

        // 스폰 파이프라인이 localScale에 따라 각도를 반전시키므로 미리 보정
        if (transform.localScale.x < 0)
            angleZ = -angleZ;

        BigRootAttack(rootVector, angleZ,14.5f);
    }

    private async void RollingSpike()
    {
        float delay1 = 0.75f;
        float delay2 = 1.0f;
        float delay3 = 0.15f;
        float delay4 = 0.5f;
        float jumpHeight = 3.5f;
        float dropForce = 5.0f;

        var centerVector = RayCenterVector();
        var arrivePos = new Vector2(centerVector.x, transform.position.y + jumpHeight);
        LookAt(arrivePos.x);
        
        // 준비자세 취하기
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnObject($"{basicStat.id}_{ConstValues.Jump}_{ConstValues.Effect}", transform);
        myRigidbody.linearVelocity = CalculateAirVelocity(transform.position, arrivePos, 0);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY < -0.1f, stateCancellation).SuppressCancellationThrow())
            return;

        transform.position = arrivePos;
        SetTriggerAnimator(ConstValues.Pattern);
        myRigidbody.linearVelocity = Vector2.zero;
        GravityChange(0);
        SpawnObject(ConstValues.GreenFlash, centerPos);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        // 밑쪽 기준 약 150도(±75도) 범위의 랜덤 각도로 가시 15발 발사
        // (프리팹 기본 방향이 위쪽이므로 180도를 더해 아래쪽 기준으로 만든다)
        int spikeCount = 30;
        for (int i = 0; i < spikeCount; i++)
        {
            int angleZ = 180 + Random.Range(-75, 76);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack5}", centerPos, angleZ);
            SpawnObject($"{basicStat.id}_{ConstValues.Attack}_{ConstValues.Hit}", centerPos);
            if(await AttackDelay(delay3).SuppressCancellationThrow())
                return;
        }
        
        // 낙하
        SetTriggerAnimator(ConstValues.Pattern);
        GravityChange(myGravity);
        //myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY >= 0, stateCancellation).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        SpawnObject($"{basicStat.id}_{ConstValues.Landing}_{ConstValues.Effect}", transform);
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;

        PatternEnd();
    }

    // 등장(연출 포함)
    public override async void Appear(Action<string, EMonsterType> bossProduct)
    {
        stateCancellation = new CancellationTokenSource();
            
        StandHitBox();
        immortal = true;
        GravityChange(myGravity);

        foreach (var mySpriteRenderer in mySpriteRenderers)
            mySpriteRenderer.enabled = false;
        
        BigRootAttack(transform.position, -15, 14.5f, 0.5f);

        if (await NormalDelay(1.15f, stateCancellation).SuppressCancellationThrow())
            return;
        
        foreach (var mySpriteRenderer in mySpriteRenderers)
            mySpriteRenderer.enabled = true;
        
        transform.position = new Vector2(transform.position.x, transform.position.y + 0.5f);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 12);
        StateSetting(ENormalState.Appear, ConstValues.Appear, ConstValues.Appear);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Air);
        
        if(await WaitUntilDelay(()=> isGrounded, stateCancellation).SuppressCancellationThrow())
            return;
        
        StateSetting(ENormalState.AppearEnd, ConstValues.AppearEnd, ConstValues.AppearEnd);
        SpawnObject($"{basicStat.id}_{ConstValues.Appear}", centerPos);
        PlaySound($"{basicStat.id}_{ConstValues.Voice}1");
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        if (await NormalDelay(1.5f, stateCancellation).SuppressCancellationThrow())
            return;
        
        if (await WaitUntilDelay(()=> GameManager.Instance.ControlStart, stateCancellation).SuppressCancellationThrow())
            return;
        
        FirstCoolTimeReduce();
        IdleOrMove();
        immortal = false;
        bossProduct?.Invoke(basicStat.name, monsterType);
    }

    private async void BigRootAttack(Vector2 pos, int angleZ, float height = 17.5f, float duration = 0.4f)
    {
        rootCancellation = new CancellationTokenSource();
        
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.Warning}", pos, angleZ);

        if (await NormalDelay(1.15f, rootCancellation).SuppressCancellationThrow())
            return;
        
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.Effect}", pos, angleZ);
        var attack1Object = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack1}", pos, angleZ).GetComponent<Monster_Tree_Attack1>();
        attack1Object.Grow(height, duration);
        if (await NormalDelay(0.65f, rootCancellation).SuppressCancellationThrow())
        {
            attack1Object.gameObject.SetActive(false);
            return;
        }

        attack1Object.gameObject.SetActive(false);
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.Fragments}1", attack1Object.FragmentsPos[0]);
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.Fragments}2", attack1Object.FragmentsPos[1]);
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.Fragments}3", attack1Object.FragmentsPos[2]);
        
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.End}", attack1Object.FragmentsPos[0]);
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.End}", attack1Object.FragmentsPos[1]);
        SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.End}", attack1Object.FragmentsPos[2]);
    }

    private async void GroundRootAttack(Vector2 pos)
    {
        rootCancellation = new CancellationTokenSource();

        int rand = Random.Range(0, 3);
        Monster_Tree_Attack1 groundRootObject = default;
        switch (rand)
        {
            case 0:
                groundRootObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack2}", pos).GetComponent<Monster_Tree_Attack1>();
                groundRootObject.Grow(6, 0.18f);
                break;
                
            case 1:
                groundRootObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack3}", pos).GetComponent<Monster_Tree_Attack1>();
                groundRootObject.Grow(8, 0.2f);
                break;
                
            case 2:
                groundRootObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack4}", pos).GetComponent<Monster_Tree_Attack1>();
                groundRootObject.Grow(5, 0.15f);
                break;
        }

        SpawnObject($"{basicStat.id}_{ConstValues.Attack2}_{ConstValues.Effect}", pos);
        if (await NormalDelay(0.5f, rootCancellation).SuppressCancellationThrow())
            return;
        
        if (groundRootObject != null)
        {
            groundRootObject.gameObject.SetActive(false);
            SpawnObject($"{basicStat.id}_{ConstValues.Attack1}_{ConstValues.End}", groundRootObject.transform.position);
        }
    }
    
    public override async void Die()
    {
        base.Die();

        rootCancellation?.Cancel();
        CancelMotion();
        ClearObjectList(buffObject);
        isDie = true;

        int count = 15;
        var delay1 = 0.12f;
        var delay2 = 0.25f;
        StateSetting(ENormalState.Die, ConstValues.Die, ConstValues.Die);
        MoveStateSetting(EMoveState.Stopping);

        dieCancellation = new CancellationTokenSource();
        for (int i = 0; i < count; i++)
        {
            SpawnHitEffect(myStat.dyingMiniEffect, 1.0f, 1.5f);
            GameManager.Instance.CameraShake(0.1f, 0.1f, 0.1f);
            if (await NormalDelay(delay1, dieCancellation).SuppressCancellationThrow())
                return;
        }
        
        if (await NormalDelay(delay2, dieCancellation).SuppressCancellationThrow())
            return;
        GameManager.Instance.CameraShake(0.5f, 0.5f, 0.3f);
        DieAirborne(transform.position);
    }

    private void DieAirborne(Vector2 endPos)
    {
        dieCancellation?.Cancel();
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        Vector2 start = transform.position;
        Vector2 end = endPos;
        float travelTime = 0.4f;
        Vector2 velocity = CalculateLaunchVelocity(start, end, travelTime);
        Airborne(velocity.x, velocity.y, true);
        goldAction?.Invoke(myStat.gold, centerPos.position);
    }
    
    // private async void Step()
    // {
    //     float delay1 = 0.2f;
    //     float delay2 = 0.3f;
    //     
    //     float stepDistance = 3.0f;
    //     float travelTime = 0.25f;
    //     float xDistanceLimit = 4.0f;
    //     
    //     LookAt(GameManager.Instance.CurPlayer.transform.position.x);
    //
    //     // 플레이어와 x축 거리가 멀면 프론트스탭, 가까우면 백스탭
    //     float xDistance = Mathf.Abs(GameManager.Instance.CurPlayer.transform.position.x - transform.position.x);
    //     bool isFrontStep = xDistance > xDistanceLimit;
    //     float forward = transform.localScale.x > 0 ? 1f : -1f;
    //     float stepDirection;
    //
    //     if (await AttackDelay(delay1).SuppressCancellationThrow())
    //         return;
    //     
    //     if (isFrontStep)
    //     {
    //         // 프론트스탭: 바라보는 방향(플레이어 쪽)으로 이동
    //         stepDirection = forward;
    //     }
    //     else
    //     {
    //         // 백스탭: 플레이어 반대 방향으로 이동
    //         stepDirection = -forward;
    //     }
    //     
    //     // 스탭 방향으로 벽(Ground) 감지
    //     RaycastHit2D wallHit = Physics2D.Raycast(centerPos.position, Vector2.right * stepDirection, stepDistance, groundLayerMask);
    //
    //     // 살짝 점프하며 스탭 이동
    //     LandingStateSetting(ELandingState.Air);
    //     Vector2 start = transform.position;
    //     Vector2 end = new Vector2(start.x + stepDirection * stepDistance, start.y);
    //
    //     // 벽이 감지되면 방 중앙으로 스탭(체공시간을 늘려서 크게 점프)
    //     if (wallHit.collider)
    //     {
    //         end = new Vector2(RayCenterVector().x, start.y);
    //         SetTriggerAnimator(ConstValues.Pattern);
    //         LookAt(RayCenterVector().x);
    //         
    //         float jumpHeight = 4.0f;  // travelTime 대신 높이로 제어
    //         myRigidbody.linearVelocity = CalculateLaunchVelocityByHeight(start, end, jumpHeight);
    //     }
    //     else
    //     {
    //         if (isFrontStep)
    //             SetTriggerAnimator(ConstValues.Pattern);
    //         else
    //             SetTriggerAnimator(ConstValues.Pattern2);
    //         
    //         myRigidbody.linearVelocity = CalculateLaunchVelocity(start, end, travelTime);
    //     }
    //
    //     // 지면에서 떨어진 뒤 다시 착지할 때까지 대기
    //     if (await WaitUntilDelay(() => !isGrounded, stateCancellation).SuppressCancellationThrow())
    //         return;
    //     
    //     if (await WaitUntilDelay(() => isGrounded, stateCancellation).SuppressCancellationThrow())
    //         return;
    //
    //     // 착지
    //     if (wallHit.collider)
    //     {
    //         SetTriggerAnimator(ConstValues.Pattern);
    //     }
    //     else
    //     {
    //         if (isFrontStep)
    //             SetTriggerAnimator(ConstValues.Pattern);
    //         else
    //             SetTriggerAnimator(ConstValues.Pattern2);
    //     }
    //
    //     LandingStateSetting(ELandingState.Ground);
    //     myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocity.y);
    //     if (await AttackDelay(delay2).SuppressCancellationThrow())
    //         return;
    //
    //     PatternEnd();
    // }
}
