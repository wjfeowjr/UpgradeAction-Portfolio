using UnityEngine;

public class Monster_Bull : Monster
{
    [SerializeField] private Transform attackPos;
    private float fallGravityMultiplier = 4.5f;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                Punch();
                break;
            case 1:
                JumpDrop();
                break;
        }
    }
    
    // 패턴1. 주먹질
    private async void Punch()
    {
        float delay1 = 0.45f;
        float delay2 = 0.5f;

        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}1", attackPos);
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        PatternEnd();
    }
    
    // 패턴2. 천근추
    private async void JumpDrop()
    {
        float delay1 = 0.5f;
        float delay2 = 0.5f;

        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        var target = GameManager.Instance.CurPlayer.transform.position;
        LookAt(target.x);
        SetTriggerAnimator(ConstValues.Pattern);
        LandingStateSetting(ELandingState.Air);
        SpawnAttackObject($"{basicStat.id}_{ConstValues.Jump}{ConstValues.Effect}", transform);
        myRigidbody.linearVelocity = CalculateVelocity(transform.position, target, 4);
        
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY < -0.1f, stateCancellation).SuppressCancellationThrow())
            return;
        
        myRigidbody.gravityScale = myGravity * fallGravityMultiplier;
        var dropAttack = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}2", transform);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY == 0, stateCancellation).SuppressCancellationThrow())
            return;
        
        dropAttack.SetActive(false);
        
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}3", transform.position);
        LandingStateSetting(ELandingState.Ground);
        myRigidbody.gravityScale = myGravity;
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        PatternEnd();
    }
    
    private Vector2 CalculateVelocity(Vector2 start, Vector2 target, float height)
    {
        // 1. 올라갈 때의 물리 정보 (기본 중력)
        float gravityUp = Physics2D.gravity.y * myGravity;
        
        // 2. 내려갈 때의 물리 정보 (강한 중력)
        float gravityDown = Physics2D.gravity.y * myGravity * fallGravityMultiplier;

        float maxY = Mathf.Max(start.y, target.y) + height;

        // [상승 구간]
        // 올라가는 변위
        float displacementY_up = maxY - start.y;
        
        // 수직 상승 속도 (Vy) 계산 (기본 중력 기준)
        Vector2 velocityY = Vector2.up * Mathf.Sqrt(-2 * gravityUp * displacementY_up);
        
        // 상승 소요 시간 (t_up)
        float timeUp = Mathf.Sqrt(-2 * displacementY_up / gravityUp);


        // [하강 구간]
        // 내려가는 변위
        float displacementY_down = maxY - target.y;

        // 하강 소요 시간 (t_down) -> *중요* 여기서 강한 중력(gravityDown)을 사용해서 계산
        float timeDown = Mathf.Sqrt(-2 * displacementY_down / gravityDown);

        // [수평 이동]
        // 총 소요 시간 = 상승 시간 + 하강 시간(짧음)
        float totalTime = timeUp + timeDown;

        // 수평 속도 (Vx) = 수평 거리 / 총 시간
        Vector2 displacementX = new Vector2(target.x - start.x, 0);
        Vector2 velocityX = displacementX / totalTime;

        return velocityX + velocityY;
    }
}
