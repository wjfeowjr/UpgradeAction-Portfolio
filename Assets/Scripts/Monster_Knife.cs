using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Monster_Knife : Monster
{
    [SerializeField] private Transform knifeStabPos;
    [SerializeField] private Transform jumpSlashPos;
    [SerializeField] private GameObject auraObject;

    protected override void Update()
    {
        base.Update();

        if (basicStat.hp <= basicStat.maxHp * 0.5f && !isDie)
        {
            if (Math.Abs(basicStat.moveSpeed - originStat.moveSpeed) < 0.1f)
                basicStat.moveSpeed = 7;

            if (auraObject)
            {
                if(!normalObject.Contains(auraObject))
                    auraObject = SpawnObject($"{basicStat.id}_{ConstValues.Aura}", transform);
            }
            else
            {
                auraObject = SpawnObject($"{basicStat.id}_{ConstValues.Aura}", transform);
            }
        }
    }
    
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
            case 3:
                TripleSlash();
                break;
            case 4:
                KnifeGrenade();
                break;
        }
    }
    
    // 패턴 1. 연속 찌르기
    private async void KnifeStab()
    {
        //float delay1 = 0.4f;
        //float delay2 = 0.3f;
        float delay3 = 0.4f;
        float delay4 = 0.5f;

        // 준비자세 취하기
        // LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        // if(await AttackDelay(delay1).SuppressCancellationThrow())
        //     return;
        //
        // int attackCount = 4;
        // for (int i = 0; i < attackCount; i++)
        // {
        //     SpawnAttack($"{basicStat.id}_{ConstValues.Attack}1", knifeStabPos);
        //     SetTriggerAnimator(ConstValues.Pattern);
        //     
        //     float chargeLength1 = 0.35f;
        //     float chargeSpeed1 = 3.5f;
        //     if(transform.localScale.x > 0)
        //         chargeVector = new Vector2(transform.position.x + chargeLength1, transform.position.y);
        //     else
        //         chargeVector = new Vector2(transform.position.x - chargeLength1, transform.position.y);
        //     if (await Charge(chargeSpeed1, 0.5f, chargeLength1, 0.5f) == false)
        //         return;
        //     
        //     LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        //     SetTriggerAnimator($"{ConstValues.Attack}_0");
        //     if(await AttackDelay(delay2).SuppressCancellationThrow())
        //         return;
        // }

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
        
        int bombCount = 3;

        if (basicStat.hp <= basicStat.maxHp * 0.5f)
            bombCount = 6;
        
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
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 공중 강습
    private async void JumpSlash()
    {
        float delay1 = 0.5f;
        float delay2 = 0.4f;
        float delay3 = 0.6f;
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
    
    // 3단베기
    private async void TripleSlash()
    {
        float delay1 = 0.5f;
        float delay2 = 0.3f;
        float delay3 = 0.75f;
        float delay4 = 0.25f;
        
        // 레이로 벽 감지
        var rayVector = CenterPos.position;
        var leftPos = Vector2.zero;
        var rightPos = Vector2.zero;
        
        var leftRay = Physics2D.Raycast(rayVector, Vector2.left, 15.0f, groundLayerMask);
        Debug.DrawRay(rayVector, Vector2.left * 15.0f, ConstValues.BlueColor, 0.025f);
        if (leftRay.collider != null)
            leftPos = new Vector2(leftRay.point.x + myBoxCollider.size.x / 2, transform.position.y);
        
        var rightRay = Physics2D.Raycast(rayVector, Vector2.right, 15.0f, groundLayerMask);
        Debug.DrawRay(rayVector, Vector2.right * 15.0f, ConstValues.BlueColor, 0.025f);
        if (rightRay.collider != null)
            rightPos = new Vector2(rightRay.point.x - myBoxCollider.size.x / 2, transform.position.y);

        int rand = Random.Range(0, 2);

        List<Vector2> posList = new List<Vector2>();

        switch (rand)
        {
            case 0:
                posList.Add(leftPos);
                posList.Add(rightPos);
                break;
            
            case 1:
                posList.Add(rightPos);
                posList.Add(leftPos);
                break;
        }

        // 사라짐
        if (await FadeOut().SuppressCancellationThrow())
            return;
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        immortal = true;
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        transform.position = posList[0];
        immortal = false;

        float chargeLength = 12.0f;
        float chargeSpeed = 25;
        foreach (var pos in posList)
        {
            if (pos == leftPos)
            {
                Flip(1);
            }
            else if (pos == rightPos)
            {
                Flip(-1);
            }
            // 다시 나타남
            await FadeIn();
            
            SpawnObject(ConstValues.GreenFlash, CenterPos);
            SetTriggerAnimator(ConstValues.Pattern);
            if(await AttackDelay(delay1).SuppressCancellationThrow())
                return;
            
            var spawnObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}4", jumpSlashPos).GetComponent<Attack>();
            SetTriggerAnimator(ConstValues.Pattern);
            if(transform.localScale.x > 0)
                chargeVector = new Vector2(transform.position.x + chargeLength, transform.position.y);
            else
                chargeVector = new Vector2(transform.position.x - chargeLength, transform.position.y);
            if (await Charge(chargeSpeed, 0.5f, chargeLength, 0.5f) == false)
                return;
            
            spawnObject.DisActiveCollider();
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
            
            spawnObject.gameObject.SetActive(false);
        }
        
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        // 점프
        var jumpPos = new Vector2(transform.position.x, transform.position.y + 11.0f);
        SetTriggerAnimator(ConstValues.Pattern);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 35);
        LandingStateSetting(ELandingState.Air);
        SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
        
        if (transform.position.y > jumpPos.y)
        {
            transform.position = jumpPos;
            myRigidbody.linearVelocity = Vector2.zero;
            GravityChange(0);
        }
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;

        transform.position = new Vector2(GameManager.Instance.CurPlayer.transform.position.x, transform.position.y);
        var dropPos = new Vector2(GameManager.Instance.CurPlayer.transform.position.x, RoomManager.Instance.GroundPosY);
        SpawnObject($"{basicStat.id}_{ConstValues.Warning}", dropPos);
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        var dropAttackObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}5", transform).GetComponent<Attack>();
        GravityChange(myGravity);
        float dropForce = 30.0f;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        
        if(await WaitUntilDelay(()=> myRigidbody.linearVelocityY == 0, stateCancellation).SuppressCancellationThrow())
            return;
        
        dropAttackObject.DisActiveCollider();
        SpawnObject($"{basicStat.id}_{ConstValues.DropEffect}", transform);
        SetTriggerAnimator(ConstValues.Pattern);

        float length = 0;
        for (int i = 0; i < 3; i++)
        {
            length += 1.25f;
            var leftMissilePos = new Vector2(transform.position.x - length, transform.position.y);
            var rightMissilePos = new Vector2(transform.position.x + length, transform.position.y);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}6", leftMissilePos);
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}6", rightMissilePos);
            if(await AttackDelay(delay4).SuppressCancellationThrow())
                return;
        }
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        dropAttackObject.gameObject.SetActive(false);
        PatternEnd();
    }
    
    // 칼날 수류탄
    private async void KnifeGrenade()
    {
        float delay1 = 0.5f;
        float delay2 = 0.15f;
        float delay3 = 0.3f;
        float delay4 = 0.75f;
        
        // 준비자세 취하기
        SpawnObject(ConstValues.GreenFlash, CenterPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        for (int i = 0; i < 3; i++)
        {
            var targetVector = GameManager.Instance.CurPlayer.CenterPos.position;
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}7", centerPos, 0, targetVector);
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        }
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        SetTriggerAnimator(ConstValues.Pattern2);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        for (int i = 0; i < 3; i++)
        {
            var targetVector = GameManager.Instance.CurPlayer.CenterPos.position;
            SpawnAttack($"{basicStat.id}_{ConstValues.Attack}7", centerPos, 0, targetVector);
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        }
        
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    private async UniTask FadeOut()
    {
        if (stateCancellation == null || stateCancellation.IsCancellationRequested)
            stateCancellation = new CancellationTokenSource();

        float speed = 3.0f;
        while (mySpriteRenderers[0].color.a > 0)
        {
            var alpha = mySpriteRenderers[0].color.a - Time.deltaTime * speed;
            mySpriteRenderers[0].color = new Color(1, 1, 1, alpha);
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
    }
    
    private async UniTask FadeIn()
    {
        if (stateCancellation == null || stateCancellation.IsCancellationRequested)
            stateCancellation = new CancellationTokenSource();

        float speed = 3.0f;
        while (mySpriteRenderers[0].color.a < 1)
        {
            var alpha = mySpriteRenderers[0].color.a + Time.deltaTime * speed;
            mySpriteRenderers[0].color = new Color(1, 1, 1, alpha);
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
        }
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
        SoundManager.Instance.PlaySound(ConstValues.BerserkerAttack1);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -7);
        await UniTask.WaitUntil(() => isGrounded);
        
        // 착지
        SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
        StateSetting(ENormalState.AppearEnd, ConstValues.Landing, ConstValues.Landing);
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
        mySpriteRenderers[0].color = ConstValues.WhiteColor;

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
        DieAirborne(new Vector2(RayCenterVector().x, transform.position.y));
    }

    private void DieAirborne(Vector2 endPos)
    {
        dieCancellation?.Cancel();
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        Vector2 start = transform.position;
        Vector2 end = endPos;
        float travelTime = 0.6f;
        Vector2 velocity = CalculateLaunchVelocity(start, end, travelTime);
        Airborne(velocity.x, velocity.y, true);
        goldAction?.Invoke(myStat.gold, centerPos.position);
    }

    public async UniTask EventExit()
    {
        float delay1 = 0.5f;
        
        StateSetting(ENormalState.AppearEnd, ConstValues.AppearEnd, ConstValues.AppearEnd);
        stateCancellation = new CancellationTokenSource();
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        StateSetting(ENormalState.Appear, ConstValues.Appear, ConstValues.Appear);
        GravityChange(0);
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 20);
        SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
        
        if (await NormalDelay(1.0f, dieCancellation).SuppressCancellationThrow())
            return;
        
        gameObject.SetActive(false);
    }
}
