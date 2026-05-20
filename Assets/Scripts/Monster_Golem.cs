using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Monster_Golem : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform readyEffectPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                GroundAttack();
                break;
        }
    }

    // 땅치기
    private async void GroundAttack()
    {
        float delay1 = 1.0f;
        float delay2 = 0.3f;
        float delay3 = 0.8f;

        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);
        
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        // 공격
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}", attackPos).GetComponent<Attack>();
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 등장(연출 포함)
    public override async void Appear(Action<string, EMonsterType> bossProduct)
    {
        transform.position = new Vector2(transform.position.x, transform.position.y + 10f);
        stateCancellation = new CancellationTokenSource();
        SoundManager.Instance.PlaySound(ConstValues.FighterAttackHit);
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        StandHitBox();
        StateSetting(ENormalState.Appear, ConstValues.Jump, ConstValues.Jump);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Air);
        immortal = true;
        GravityChange(myGravity);
        await UniTask.WaitUntil(() => isGrounded);
        
        SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
        StateSetting(ENormalState.AppearEnd, ConstValues.Landing, ConstValues.Landing);
        if (await NormalDelay(1.0f, stateCancellation).SuppressCancellationThrow())
            return;

        CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
        FirstCoolTimeReduce();
        IdleOrMove();
        immortal = false;
        bossProduct?.Invoke(basicStat.name, monsterType);
    }

    public override async void Die()
    {
        base.Die();
        
        CancelMotion();
        ClearObjectList(buffObject);

        int count = 15;
        var delay = 0.12f;
        StateSetting(ENormalState.Die, ConstValues.Die, ConstValues.Die);
        MoveStateSetting(EMoveState.Stopping);
            
        dieCancellation = new CancellationTokenSource();

        for (int i = 0; i < count; i++)
        {
            SpawnHitEffect(myStat.dyingMiniEffect, 1.0f, 1.5f);
            GameManager.Instance.CameraShake(0.1f, 0.1f, 0.1f);
            if (await NormalDelay(delay, dieCancellation).SuppressCancellationThrow())
                return;
        }
        DieAirborne();
    }

    private void DieAirborne()
    {
        dieCancellation?.Cancel();
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        float xVelocity = 6.0f;
        float yVelocity = 8.0f;

        if (transform.localScale.x > 0)
            xVelocity = -6.0f;
        
        Airborne(xVelocity, yVelocity, true);
        goldAction?.Invoke(myStat.gold, centerPos.position);
        
        isDie = true;
    }
}
