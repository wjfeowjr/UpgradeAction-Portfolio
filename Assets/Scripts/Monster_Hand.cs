using UnityEngine;

public class Monster_Hand : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                DropAttack();
                break;
        }
    }

    // 패턴1. 내려찍기
    private async void DropAttack()
    {
        float delay1 = 0.5f;
        float delay2 = 0.5f;
        
        var target = GameManager.Instance.CurPlayer.transform.position;
        LookAt(target.x);
        basicStat.bodyType = EBodyType.SuperArmor;
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        LandingStateSetting(ELandingState.Air);
        
        float upperForce = 15.0f;
        float dropForce = 30.0f;

        myRigidbody.gravityScale = ConstValues.BasicGravity;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, upperForce);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY < -0.1f, stateCancellation).SuppressCancellationThrow())
            return;

        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        SpawnAttackObject($"{basicStat.id}_DropEffect", transform);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY == 0, stateCancellation).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}", transform.position);
        LandingStateSetting(ELandingState.Ground);

        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    protected override void PatternEnd(bool movingStart = true)
    {
        base.PatternEnd(true);
        HoveringLeap();
        basicStat.bodyType = originStat.bodyType;
    }
}
