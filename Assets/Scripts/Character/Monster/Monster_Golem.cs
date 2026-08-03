using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Monster_Golem : Monster
{
    [SerializeField] private Transform groundPunchPos;
    [SerializeField] private Transform[] golemPunchPos;
    [SerializeField] private Transform[] explosionPos;
    [SerializeField] private Transform readyEffectPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                GroundAttack();
                break;
            case 1:
                GolemPunch();
                break;
            case 2:
                GolemDrop();
                break;
            case 3:
                GolemCrumble();
                break;
        }
    }

    // 땅치기
    private async void GroundAttack()
    {
        float delay1 = 0.15f;
        float delay2 = 0.85f;
        float delay3 = 0.3f;
        float delay4 = 0.8f;

        // 준비자세 취하기
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;

        // 공격
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}1", transform);
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 골렘펀치
    private async void GolemPunch()
    {
        float delay1 = 1.0f;
        float delay2 = 0.3f;
        float delay3 = 0.5f;
        
        // 준비자세 취하기
        SpawnObject(ConstValues.GreenFlash, CenterPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        // 공격
        float chargeLength = 1.5f;
        float chargeSpeed = 7.5f;

        for (int i = 0; i < 2; i++)
        {
            if(transform.localScale.x > 0)
                chargeVector = new Vector2(transform.position.x + chargeLength, transform.position.y);
            else
                chargeVector = new Vector2(transform.position.x - chargeLength, transform.position.y);

            // 공격
            SetTriggerAnimator(ConstValues.Pattern);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}2", golemPunchPos[i]);
            SpawnObject($"{basicStat.id}_{ConstValues.Explosion}", explosionPos[i]);
            if (await Charge(chargeSpeed, 0.5f, chargeLength, 0.5f) == false)
                return;

            if (i == 0)
            {
                if(await AttackDelay(delay2).SuppressCancellationThrow())
                    return;
                
                LookAt(GameManager.Instance.CurPlayer.transform.position.x);
                SetTriggerAnimator(ConstValues.Pattern);
                if(await AttackDelay(delay2).SuppressCancellationThrow())
                    return;
            }
        }
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 골렘쿵쿵쿵
    private async void GolemDrop()
    {
        float delay1 = 1.0f;
        float delay2 = 0.5f;
        float delay3 = 1.0f;
        float delay4 = 0.1f;
        float delay5 = 1.0f;
        float jumpHeight = 3.5f;
        float rockInterval = 1.5f;
        
        var centerVector = RayCenterVector();
        var arrivePos = new Vector2(centerVector.x, transform.position.y + jumpHeight);
        var jumpPosY = transform.position.y + jumpHeight;
        float dropForce = 15.0f;
        LookAt(arrivePos.x);
        
        // 준비자세 취하기
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnObject($"{basicStat.id}_{ConstValues.Jump}{ConstValues.Effect}", transform);
        myRigidbody.linearVelocity = CalculateAirVelocity(transform.position, arrivePos, 0);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY < -0.1f, stateCancellation).SuppressCancellationThrow())
            return;

        transform.position = arrivePos;
        SetTriggerAnimator(ConstValues.Pattern);
        
        for (int i = 0; i < 2; i++)
        {
            myRigidbody.linearVelocity = Vector2.zero;
            GravityChange(0);

            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
            
            SpawnObject(ConstValues.GreenFlash, CenterPos);
            if(await AttackDelay(delay3).SuppressCancellationThrow())
                return;
        
            // 낙하
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
            GravityChange(myGravity);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}3", transform);
            if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY >= 0, stateCancellation).SuppressCancellationThrow())
                return;
        
            // 쿵
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}4", transform);
            SpawnObject($"{basicStat.id}_{ConstValues.Explosion}", transform);
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;

            // 점프
            SpawnObject($"{basicStat.id}_{ConstValues.Jump}{ConstValues.Effect}", transform);
            myRigidbody.linearVelocity = new Vector2(0, 15);
            if(await WaitUntilDelay(()=> transform.position.y > jumpPosY, stateCancellation).SuppressCancellationThrow())
                return;

            transform.position = new Vector2(transform.position.x, jumpPosY);
        }

        myRigidbody.linearVelocity = Vector2.zero;
        GravityChange(0);
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnObject(ConstValues.GreenFlash, CenterPos);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        // 낙하
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        GravityChange(myGravity);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}3", transform);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY >= 0, stateCancellation).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        
        // 쿵
        var leftPos = new Vector2(transform.position.x - rockInterval, transform.position.y);
        var rightPos = new Vector2(transform.position.x + rockInterval, transform.position.y);
        
        for (int i = 0; i < 7; i++)
        {
            var rockPosLeft = new Vector2(leftPos.x - rockInterval * i, leftPos.y);
            var rockPosRight = new Vector2(rightPos.x + rockInterval * i, rightPos.y);
            
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}5", rockPosLeft);
            SpawnObject($"{basicStat.id}_{ConstValues.Explosion}", rockPosLeft);
            
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}5", rockPosRight);
            SpawnObject($"{basicStat.id}_{ConstValues.Explosion}", rockPosRight);
            if(await AttackDelay(delay4).SuppressCancellationThrow())
                return;
        }
        if(await AttackDelay(delay5).SuppressCancellationThrow())
            return;

        PatternEnd();
    }
    
    // 골렘 크럼블
    private async void GolemCrumble()
    {
        float delay1 = 1.5f;
        float delay2 = 0.2f;
        float delay3 = 0.2f;
        float delay4 = 0.8f;

        // 준비자세 취하기
        GameManager.Instance.CameraShake(0.1f, 0.1f, 1.0f);
        SpawnObject(ConstValues.GreenFlash, CenterPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        // 공격
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}6", transform);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        for (int i = 0; i < 8; i++)
        {
            SpawnRock();
            if(await AttackDelay(delay3).SuppressCancellationThrow())
                return;
        }

        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }

    private void SpawnRock()
    {
        Vector2 rockVector = new Vector2(RayRandomPosX(0.75f), transform.position.y + 10);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}7", rockVector);
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
        // 화상 틱 등으로 Die가 재호출될 수 있어 base.Die()가 isDie를 세우기 전에 1회만 집계
        if (!isDie)
            SteamWorksManager.Instance.AddStat(ConstValues.StatKilledGolem);

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
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", centerPos);
        float xVelocity = 6.0f;
        float yVelocity = 8.0f;
        
        if (transform.localScale.x > 0)
            xVelocity = -6.0f;
        
        Airborne(xVelocity, yVelocity, true);
        
        goldAction?.Invoke(myStat.gold, centerPos.position);
        isDie = true;
    }
}
