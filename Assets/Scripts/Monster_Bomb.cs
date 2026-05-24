using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Monster_Bomb : Monster
{
    private int fallingBombCount;
    public Transform facePos;
    public Transform rockPunchPos;
    
    public Transform[] crazyBombPos;
    public Transform[] blueFirePos;
    

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                SmashGround();
                break;
            case 1:
                RocketPunch();
                break;
            case 2:
                ThrowBomb();
                break;
            case 3:
                FireBlast();
                break;
        }
    }
    // 패턴1.내려치기
    private async void SmashGround()
    {
        float delay1 = 0.3f;
        float delay2 = 0.5f;
        float delay3 = 0.5f;
        
        PlaySound($"{basicStat.id}_{ConstValues.Pattern}");
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 15);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        float dropForce = 30.0f;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY == 0, stateCancellation).SuppressCancellationThrow())
            return;

        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}1", transform);
        SetTriggerAnimator(ConstValues.Pattern);
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 패턴2.로켓 펀치
    private async void RocketPunch()
    {
        float delay1 = 0.3f;
        float delay2 = 0.5f;
        
        int punchCount = 1;
    
        // 준비자세 취하기
        PlaySound($"{basicStat.id}_{ConstValues.Pattern}");
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
    
        // 플래시 발동
        // SpawnObject($"{basicStat.id}_Flash", rockPunchPos);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
    
        // 주먹질 시작
        for (int i = 0; i < punchCount; i++)
        {
            SetTriggerAnimator(ConstValues.Pattern);
            // 화면흔들기 + 폭발이팩트 + 주먹미사일 날리기
            SpawnObject($"{basicStat.id}_Explosion", rockPunchPos);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}3_{ConstValues.Object}", rockPunchPos);
            
            if (i < punchCount - 1)
            {
                if(await AttackDelay(delay1).SuppressCancellationThrow())
                    return;
                
                LookAt(GameManager.Instance.CurPlayer.transform.position.x);
                SetTriggerAnimator(ConstValues.Pattern2);
                if(await AttackDelay(delay2).SuppressCancellationThrow())
                    return;
            }
        }
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 패턴3.폭탄 던지기
    private async void ThrowBomb()
    {
        float delay1 = 0.2f;
        float delay2 = 0.5f;
        int bombCount = 20;

        PlaySound($"{basicStat.id}_laugh");
        SpawnObject(ConstValues.BlueFlash, facePos);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
    
        SetTriggerAnimator(ConstValues.Pattern);

        for (int i = 0; i < bombCount; i++)
        {
            int randX = Random.Range(0, 2);
            Vector2 bombVector = new Vector2(crazyBombPos[randX].position.x, crazyBombPos[randX].position.y);

            if (i % 3 == 0)
            {
                var targetPos = GameManager.Instance.CurPlayer.CenterPos.position;
                float randomX = Random.Range(-2.0f, 2.0f);
                targetPos.y += randomX;
                SpawnAttack($"{basicStat.id}_{ConstValues.Attack}4_{ConstValues.Object}_Homing", bombVector, 0, targetPos);
            }
            else
            {
                SpawnAttack($"{basicStat.id}_{ConstValues.Attack}4_{ConstValues.Object}", bombVector);
            }

            if(await AttackDelay(delay1).SuppressCancellationThrow())
                return;
        }
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 패턴4.8연발 포격
    private async void FireBlast()
    {
        float delay1 = 1.0f;
        int blueFireBlastCount = 8;
        
        var leftHit = Physics2D.Raycast(centerPos.position, Vector2.left, 100, groundLayerMask);
        var rightHit = Physics2D.Raycast(centerPos.position, Vector2.right, 100, groundLayerMask);

        float maxLeftX = leftHit.point.x + myBoxCollider.size.x * 0.5f + 0.2f;
        float maxRightX = rightHit.point.x - myBoxCollider.size.x * 0.5f - 0.2f;

        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        GravityChange(0);
        SetTriggerAnimator(ConstValues.Pattern);
        
        // 점프
        var jumpPos = new Vector2(transform.position.x, transform.position.y + 11.0f);
        SetTriggerAnimator(ConstValues.Pattern);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 35);
        LandingStateSetting(ELandingState.Air);
        // 공격판정
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}5", transform);
        
        if (transform.position.y > jumpPos.y)
        {
            transform.position = jumpPos;
            myRigidbody.linearVelocity = Vector2.zero;
            GravityChange(0);
        }
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        // 착지위치 = 오른쪽 또는 왼쪽 벽 끝
        int randX = Random.Range(0, 2);
        switch (randX)
        {
            case 0:
                transform.position = new Vector2(maxLeftX, jumpPos.y);
                Flip(1);
                break;
            case 1:
                transform.position = new Vector2(maxRightX, jumpPos.y);
                Flip(-1);
                break;
        }
        
        GravityChange(myGravity);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -15);
        await UniTask.WaitUntil(() => isGrounded);
    
        // 착지
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}6", transform);
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        PlaySound($"{basicStat.id}_laugh");
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SpawnObject(ConstValues.BlueFlash, facePos);
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        for (int i = 0; i < blueFireBlastCount; i++)
        {
            if (await FireSpawn(i).SuppressCancellationThrow())
                return;
        }

        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    private async UniTask FireSpawn(int count)
    {
        float delay = 1.0f;
        // 짝수
        if (count % 2 == 0)
        {
            SetTriggerAnimator(ConstValues.Pattern3);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}6", blueFirePos[0]);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}8_{ConstValues.Object}", blueFirePos[0]);
        }
        // 홀수
        else if (count % 2 == 1)
        {
            SetTriggerAnimator(ConstValues.Pattern2);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}6", blueFirePos[1]);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}8_{ConstValues.Object}", blueFirePos[1]);
        }
        if(await AttackDelay(delay).SuppressCancellationThrow())
            return;
        SetTriggerAnimator(ConstValues.Pattern4);
    }
    
    public override void Stagger()
    {
        PlaySound($"{basicStat.id}_{ConstValues.Stun}");
        base.Stagger();
    }
    
    // 등장(연출 포함)
    public override async void Appear(Action<string, EMonsterType> bossProduct)
    {
        transform.position = new Vector2(transform.position.x, transform.position.y + 10f);
        stateCancellation = new CancellationTokenSource();
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        StandHitBox();
        StateSetting(ENormalState.Appear, ConstValues.Jump, ConstValues.Jump);
        MoveStateSetting(EMoveState.Stopping);
        LandingStateSetting(ELandingState.Air);
        immortal = true;
        GravityChange(myGravity);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -15);
        await UniTask.WaitUntil(() => isGrounded);
        
        PlaySound($"{basicStat.id}_laugh");
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
        PlaySound($"{basicStat.id}_{ConstValues.Die}");
        
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

    private async void DieAirborne()
    {
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        float xVelocity = 0.0f;
        float yVelocity = 30.0f;

        GravityChange(0);
        Airborne(xVelocity, yVelocity, true);
        goldAction?.Invoke(myStat.gold, centerPos.position);
        
        if (await NormalDelay(1.0f, dieCancellation).SuppressCancellationThrow())
            return;
        
        isDie = true;
        gameObject.SetActive(false);
        dieCancellation?.Cancel();
    }
    
    // 이벤트 쿵쿵
    public async void EventSmashGround()
    {
        float delay1 = 0.2f;
        float delay2 = 0.3f;
        float delay3 = 0.2f;
        
        PlaySound($"{basicStat.id}_laugh");

        for (int i = 0; i < 3; i++)
        {
            SetTriggerAnimator(ConstValues.Attack_0);
            
            if(await AttackDelay(delay1).SuppressCancellationThrow())
                return;

            SetTriggerAnimator(ConstValues.Pattern);
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 15);
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        
            float dropForce = 30.0f;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
            if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY == 0, stateCancellation).SuppressCancellationThrow())
                return;

            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}1", transform);
            SetTriggerAnimator(ConstValues.Pattern);
        
            if(await AttackDelay(delay3).SuppressCancellationThrow())
                return;
        }
        SetTriggerAnimator(ConstValues.Idle);
    }
    
    // 이벤트 폭탄 던지기
    public async void EventThrowBomb(Npc[] npc)
    {
        float delay1 = 0.2f;
        float delay2 = 0.5f;
        int bombCount = 15;
        
        SetTriggerAnimator(ConstValues.Attack_2);

        PlaySound($"{basicStat.id}_laugh");
        SpawnObject(ConstValues.BlueFlash, facePos);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
    
        SetTriggerAnimator(ConstValues.Pattern);

        for (int i = 0; i < bombCount; i++)
        {
            int randX = Random.Range(0, 2);
            Vector2 bombVector = new Vector2(crazyBombPos[randX].position.x, crazyBombPos[randX].position.y);
            int randIdx = Random.Range(0, npc.Length);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}4_{ConstValues.Object}_Homing", bombVector, 0, npc[randIdx].CenterPos.position);

            if(await AttackDelay(delay1).SuppressCancellationThrow())
                return;
        }
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Idle);
    }

    public void EventExplosion()
    {
        SpawnObject($"{basicStat.id}_Explosion", centerPos);
    }
}
