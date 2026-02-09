using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Monster_Knife : Monster
{
    [SerializeField] private Transform knifeStabPos;
    [SerializeField] private Transform jumpSlashPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                KnifeStab();
                break;
            case 1:
                JumpBomb();
                break;
            case 2:
                JumpSlash();
                break;
        }
    }
    
    // 패턴 1. 연속 찌르기
    private async void KnifeStab()
    {
        float delay1 = 0.4f;
        float delay2 = 0.15f;
        float delay3 = 0.2f;
        float delay4 = 0.5f;

        // 준비자세 취하기
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        int attackCount = 4;
        for (int i = 0; i < attackCount; i++)
        {
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}1", knifeStabPos);
            SetTriggerAnimator(ConstValues.Pattern);
            
            float chargeLength1 = 0.35f;
            float chargeSpeed1 = 3.5f;
            if(transform.localScale.x > 0)
                chargeVector = new Vector2(transform.position.x + chargeLength1, transform.position.y);
            else
                chargeVector = new Vector2(transform.position.x - chargeLength1, transform.position.y);
            if (await Charge(chargeSpeed1, 0.5f, chargeLength1, 0.5f) == false)
                return;
            
            LookAt(GameManager.Instance.CurPlayer.transform.position.x);
            SetTriggerAnimator($"{ConstValues.Attack}_0");
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        }
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}2", knifeStabPos);
        
        float chargeLength2 = 2.0f;
        float chargeSpeed2 = 10;
        if(transform.localScale.x > 0)
            chargeVector = new Vector2(transform.position.x + chargeLength2, transform.position.y);
        else
            chargeVector = new Vector2(transform.position.x - chargeLength2, transform.position.y);
        if (await Charge(chargeSpeed2, 0.5f, chargeLength2, 0.5f) == false)
            return;
        
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;
        PatternEnd();
    }
    
    // 패턴 2. 폭탄투하
    private async void JumpBomb()
    {
        float delay1 = 0.5f;
        float delay2 = 0.2f;
        float delay3 = 0.5f;
        
        // 준비자세 취하기
        SpawnObject(ConstValues.GreenFlash, CenterPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 15);
        SetTriggerAnimator(ConstValues.Pattern);
        LandingStateSetting(ELandingState.Air);
        
        int bombCount = 6;
        for (int i = 0; i < bombCount; i++)
        {
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}3_{ConstValues.Object}", centerPos);
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        }
        
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY == 0, stateCancellation).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        LandingStateSetting(ELandingState.Ground);
        SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 공중 강습
    private async void JumpSlash()
    {
        float delay1 = 0.5f;
        float delay2 = 0.4f;
        float delay3 = 0.8f;
        float delay4 = 0.75f;

        float arriveY = transform.position.y;
        float jumpLimitY = transform.position.y + 3.5f;
        
        // 준비자세 취하기
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SoundManager.Instance.PlaySound(ConstValues.BerserkerAttack1);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 20);
        SetTriggerAnimator(ConstValues.Pattern);
        LandingStateSetting(ELandingState.Air);
        while (transform.position.y < jumpLimitY)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
        transform.position = new Vector2(transform.position.x, jumpLimitY);
        myRigidbody.linearVelocity = Vector2.zero;
        GravityChange(0);
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        SpawnObject(ConstValues.GreenFlash, CenterPos);
        SetTriggerAnimator(ConstValues.Pattern);
        float chargeSpeed = 25.0f;
        Vector2 targetVector = new Vector2(GameManager.Instance.CurPlayer.transform.position.x, arriveY);
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        var spawnObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}4", jumpSlashPos).GetComponent<Attack>();
        if (await ChargeToTarget(chargeSpeed, 1.0f, 1.0f, targetVector, 0.5f) == false)
            return;
        
        spawnObject.DisActiveCollider();
        GravityChange(myGravity);
        SetTriggerAnimator(ConstValues.Pattern);
        LandingStateSetting(ELandingState.Ground);
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;
        
        spawnObject.gameObject.SetActive(false);
        PatternEnd();
    }

    // 등장(연출 포함)
    public override async void Appear(Action<string> bossProduct)
    {
        stateCancellation = new CancellationTokenSource();
        SoundManager.Instance.PlaySound(ConstValues.BerserkerAttack1);
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        StandHitBox();
        StateSetting(ENormalState.Appear, ConstValues.Jump, ConstValues.Jump);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Air);
        immortal = true;
        GravityChange(myGravity);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 10);
        await UniTask.WaitUntil(() => landingState == ELandingState.Ground);
        SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
        StateSetting(ENormalState.AppearEnd, ConstValues.Landing, ConstValues.Landing);
        if (await NormalDelay(1.0f, stateCancellation).SuppressCancellationThrow())
            return;

        CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
        FirstCoolTimeReduce();
        IdleOrMove();
        immortal = false;
        bossProduct?.Invoke(basicStat.name);
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

    public void DieAirborne()
    {
        dieCancellation?.Cancel();
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        float xVelocity = 6.0f;
        float yVelocity = 8.0f;

        if (transform.localScale.x > 0)
            xVelocity = -6.0f;
        
        Airborne(xVelocity, yVelocity);
        goldAction?.Invoke(myStat.gold, centerPos.position);
        
        isDie = true;
    }
}
