using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Player_Gunner : Player
{
    [SerializeField] private Transform dashShotPos;
    [SerializeField] private Transform landingAttackPos;
    [SerializeField] private Transform landingEffectPos;
    [SerializeField] private Transform jumpAttackPos;
    [SerializeField] private Transform jumpEffectPos;
    [SerializeField] private Transform changeAttackPos;
    
    [SerializeField] private Transform grenadePos;
    [SerializeField] private Transform knockBackShotPos;
    [SerializeField] private Transform crazyShotPos;
    [SerializeField] private Transform bigShotPos;
    
    private int maxBullet = 3;

    private void Scream()
    {
        SoundManager.Instance.PlaySound(ConstValues.GunnerLaugh);
    }
    
    public override async void ChangeAttack()
    {
        Debug.Log("교체 공격 시작");
        CancelMotion();
        
        curGlobalCoolTime = 0;
        stateCancellation = new CancellationTokenSource();
        var finishSuccess = await GunnerChangeAttack();
        if (!finishSuccess)
        {
            Debug.Log($"교체 공격 캔슬");
            return;
        }
        
        Debug.Log($"교체 공격 끝");
        // 동작이 끝날때 반환하는 트리거
        StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
    }
    private async UniTask<bool> GunnerChangeAttack()
    {
        var delay1 = 0.05f;
        StateSetting(ENormalState.Attack, ConstValues.ChangeAttack, ConstValues.ChangeAttack);
        for (int i = 0; i < 10; i++)
        {
            SpawnAttack($"{ConstValues.Gunner}_{ConstValues.ChangeAttack}", changeAttackPos);
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;
        }
        
        return true;
    }
    
    public override async UniTask<bool> Attack()
    {
        if (!await base.Attack())
            return false;
        
        // 점프 공격횟수를 다 쓰면 점프공격을 할 수 없다
        // if (landingState == ELandingState.Air && jumpAttackCount > maxBullet)
        //     return false;
        
        bool finishSuccess = true;
        string type = "지상";
        
        switch (landingState)
        {
            // 지상공격
            case ELandingState.Ground:
                CancelMotion(true, true, false);
                stateCancellation = new CancellationTokenSource();
                finishSuccess = await GunnerLandingAttack();
                break;

            // 점프공격
            case ELandingState.Air:
                type = "점프";
                CancelMotion(false, false);
                stateCancellation = new CancellationTokenSource();
                finishSuccess = await GunnerJumpAttack();
                break;
        }

        if (!finishSuccess)
        {
            Debug.Log($"{type}공격 캔슬");
            return false;
        }
        
        if (attackBuffer)
        {
            Attack().Forget();
        }
        else
        {
            StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
            Debug.Log($"{type}공격 끝");
            landingAttackCount = 0;
        }
        return true;
    }

    private async UniTask<bool> GunnerLandingAttack()
    {
        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);

        float afterDelay = 0.35f;
        attackBuffer = false;
        
        if (landingAttackCount < maxBullet)
            landingAttackCount += 1;
        
        float delay1 = 0.066f; // 0.066f
        float delay2 = 0.016f; // 0.016f
        
        int bullet = 0;
        int cycle = 4;
        switch (landingAttackCount)
        {
            case 1:
                AttackChecker(0.1f, (delay1 + delay2) * cycle + afterDelay);
                for (int i = 0; i < cycle; i++)
                {
                    MotionFlip();
                    bullet += 1;
                    if(bullet == 1)
                        StateSetting(ENormalState.Attack, ConstValues.Attack, ConstValues.Attack1);
                    else if(bullet % 2 == 0)
                        StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack1);
                    else if(bullet % 2 == 1)
                        StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack2);
            
                    if (await AttackDelay(delay1).SuppressCancellationThrow())
                        return false;
            
                    SpawnObject(ConstValues.GunnerAttackEffect1, landingEffectPos);
                    SpawnObject(ConstValues.GunnerAttack1Object, landingAttackPos);
                    
                    if (await AttackDelay(delay2).SuppressCancellationThrow())
                        return false;
                }
                if (await BufferDelay(delay2, afterDelay).SuppressCancellationThrow())
                    return false;
                break;
            
            case 2:
                AttackChecker(0.1f, (delay1 + delay2) * cycle + afterDelay);
                for (int i = 0; i < cycle; i++)
                {
                    MotionFlip();
                    bullet += 1;
                    if(bullet == 1)
                        StateSetting(ENormalState.Attack, ConstValues.Attack, ConstValues.Attack1);
                    else if(bullet % 2 == 0)
                        StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack1);
                    else if(bullet % 2 == 1)
                        StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack2);
            
                    if (await AttackDelay(delay1).SuppressCancellationThrow())
                        return false;
            
                    SpawnObject(ConstValues.GunnerAttackEffect1, landingEffectPos);
                    SpawnObject(ConstValues.GunnerAttack1Object, landingAttackPos);
                    
                    if (await AttackDelay(delay2).SuppressCancellationThrow())
                        return false;
                }
                if (await BufferDelay(delay2, afterDelay).SuppressCancellationThrow())
                    return false;
                break;
            
            case 3:
                float delay3 = 0.1f;
                float delay4 = 0.3f;
                
                MotionFlip();
                StateSetting(ENormalState.Attack, ConstValues.FinalAttack, ConstValues.Attack3Ready);
                if (await AttackDelay(delay4).SuppressCancellationThrow())
                    return false;
            
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack3);
                if (await AttackDelay(delay3).SuppressCancellationThrow())
                    return false;
        
                SpawnObject(ConstValues.GunnerAttackEffect2, landingEffectPos);
                SpawnObject(ConstValues.GunnerAttack2Object, landingAttackPos);
                Rebound(2.0f);
            
                if (await AttackDelay(delay4).SuppressCancellationThrow())
                    return false;
                landingAttackCount = 0;
                break;
        }
        canAttack = true;
        return true;
    }
    
    private async UniTask<bool> GunnerJumpAttack()
    {
        ResetTriggerAnimator(ConstValues.JumpDown);
        attackBuffer = false;

        float delay1 = 0.066f; // 0.066f
        float delay2 = 0.016f; // 0.016f

        // 메탈슬러그 버전
        int bullet = 0;
        int cycle = 4;
        
        AttackChecker(0.1f, (delay1 + delay2) * cycle);
        for (int i = 0; i < cycle; i++)
        {
            if(bullet == 0)
                StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
            else if(bullet % 2 == 0)
                StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2);
            else if(bullet % 2 == 1)
                StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack1);

            if (!GetJumpState())
                break;
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;
            
            SpawnObject(ConstValues.GunnerAttackEffect1, landingEffectPos);
            SpawnObject(ConstValues.GunnerAttack1Object, landingAttackPos);
            bullet++;
            
            if (!GetJumpState())
                break;
            if (await AttackDelay(delay2).SuppressCancellationThrow())
                return false;
        }
        canAttack = true;
        return true;

        // 총알이 1개 남을 때 까지 난사
        // while (jumpAttackCount < maxBullet)
        // {
        //     if (firstShot)
        //     {
        //         StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
        //         firstShot = false;
        //     }
        //     else
        //     {
        //         if(jumpAttackCount % 2 == 0)
        //             StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2);
        //         else if(jumpAttackCount % 2 == 1)
        //             StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack1);
        //     }
        //     
        //     nextAttack = false;
        //     if (await AttackDelay(delay1).SuppressCancellationThrow())
        //         return false;
        //
        //     myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, 1.9f);
        //
        //     SpawnObject(ConstValues.GunnerAttackEffect1, landingEffectPos);
        //     SpawnObject(ConstValues.GunnerAttack1Object, landingAttackPos);
        //     jumpAttackCount++;
        //     
        //     if (jumpAttackCount == maxBullet - 1)
        //         break;
        //
        //     if (await NextAttackDelay(delay2, afterDelay).SuppressCancellationThrow())
        //         return false;
        //     
        //     if (!nextAttack)
        //         break;
        // }
        // if (jumpAttackCount == maxBullet)
        // {
        //     jumpAttackCount++;
        //     StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
        //     myRigidbody.linearVelocity = Vector2.zero;
        //     GravityChange(0);
        //     if (await AttackDelay(delay4).SuppressCancellationThrow())
        //         return false;
        //     
        //     StateSetting(ENormalState.Attack, ConstValues.JumpAttack2, ConstValues.JumpAttack3);
        //     if (await AttackDelay(delay3).SuppressCancellationThrow())
        //         return false;
        //
        //     SpawnObject(ConstValues.GunnerAttackEffect2, landingEffectPos);
        //     SpawnObject(ConstValues.GunnerAttack2Object, landingAttackPos);
        //     myRigidbody.linearVelocity = new Vector2(0, 3.0f);
        //     GravityChange(myGravity);
        //
        //     if (await AttackDelay(delay4).SuppressCancellationThrow())
        //         return false;
        // }
        // GravityChange(myGravity);

        // 그냥 계속 쏜다 ㅋㅋ
        // while (GetJumpState())
        // { 
        //     if(bulletCount == 0)
        //         StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
        //     else if(bulletCount % 2 == 0)
        //         StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2);
        //     else if(bulletCount % 2 == 1)
        //         StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack1);
        //     
        //     nextAttack = false;
        //
        //     if (await AttackDelay(delay1).SuppressCancellationThrow())
        //         return false;
        //
        //     myRigidbody.linearVelocity = new Vector2(0, 0.1f);
        //     SpawnObject(ConstValues.GunnerAttackEffect1, jumpEffectPos, -45);
        //     SpawnObject(ConstValues.GunnerAttack1Object, jumpAttackPos, -45);
        //     bulletCount++;
        //     
        //     if (await NextAttackDelay(delay2, afterDelay).SuppressCancellationThrow())
        //         return false;
        //     
        //     if (!nextAttack)
        //         break;
        // }
    }
    
    public override async void Skill(KeyCode skillKey)
    {
        if (Time.timeScale == 0)
            return;

        var skillId = GameManager.Instance.PlayerSkillKey.gunnerSkillKeyList.Find(x => x.keyCode == skillKey).skillId;
        if (!IsCanSkill(skillId))
            return;
        
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }
        
        if (skillKey == GameManager.Instance.dashKey)
        {
            if(!GetDashDelay())
            {
                Debug.Log("대시 딜레이가 지나지 않음");
                return;
            }
        }
        
        // 스킬 사용 후 쿨타임 및 공격캔슬 관리
        UseSkill(skillId);
        curGlobalCoolTime = 0;
        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);
        
        // 대시중이라면 즉시 정지해야함..
        if (normalState == ENormalState.Dash)
            myRigidbody.linearVelocity = Vector2.zero;

        CancelMotion(false, false);
        MotionFlip();
        // 스킬 특성: 슈퍼아머 체크
        if(skillKey != GameManager.Instance.dashKey && GameManager.Instance.PlayerSkill.IsHaveAttribute(skillId, ConstValues.SuperArmor))
            BodyTypeSetting(ConstValues.SuperArmor);
        
        stateCancellation = new CancellationTokenSource();
        bool finishSuccess = true;
        if (skillKey == GameManager.Instance.dashKey)
        {
            SpawnAttack(ConstValues.GunnerDashShot, dashShotPos);
            finishSuccess = await Dash();
        }
        
        if (skillId == ConstValues.GunnerGrenade)
        {
            finishSuccess = await Grenade();
        }
        else if (skillId == ConstValues.GunnerKnockBackShot)
        {
            finishSuccess = await KnockBackShot();
        }
        else if (skillId == ConstValues.GunnerCrazyShot)
        {
            finishSuccess = await CrazyShot();
        }
        else if (skillId == ConstValues.GunnerBigShot)
        {
            finishSuccess = await BigShot();
        }

        if (!finishSuccess)
        {
            Debug.Log($"{skillKey} 스킬 캔슬");
            return;
        }
        
        Debug.Log($"{skillKey} 스킬 끝");
        GravityChange(myGravity);
        // 동작이 끝날때 반환하는 트리거
        StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
    }
    
    // 수류탄
    private async UniTask<bool> Grenade()
    {
        var delay1 = 0.1f;
        var delay2 = 0.15f;
        
        myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocityY);
        
        StateSetting(ENormalState.Skill, ConstValues.GunnerGrenade, ConstValues.GunnerGrenade);

        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        SpawnObject(ConstValues.GunnerGrenadeObject, grenadePos);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
    
    // 넉백샷
    public async UniTask<bool> KnockBackShot()
    {
        var delay1 = 0.1f;
        var delay2 = 0.2f;
        
        StateSetting(ENormalState.Skill, ConstValues.GunnerKnockBackShot, ConstValues.GunnerKnockBackShotReady);
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerKnockBackShot);
        SpawnAttack(ConstValues.GunnerKnockBackShot, knockBackShotPos);
        Rebound(4.0f);
        
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
    // 넉백샷(이벤트)
    public async UniTask<bool> EventKnockBackShot()
    {
        var delay1 = 0.1f;
        var delay2 = 0.2f;
        
        StateSetting(ENormalState.Skill, ConstValues.GunnerKnockBackShot, ConstValues.GunnerKnockBackShotReady);
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerKnockBackShot);
        SpawnAttack($"{ConstValues.GunnerKnockBackShot}_{ConstValues.Event}", knockBackShotPos);

        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
    
    // 개난사
    private async UniTask<bool> CrazyShot()
    {
        int bulletCount = 14;
        float delay1 = 0.1f;

        StateSetting(ENormalState.Skill, ConstValues.GunnerCrazyShot, ConstValues.GunnerCrazyShot);

        Scream();
        SpawnObject(ConstValues.GunnerFlash, centerPos);

        for (int i = 0; i < bulletCount; i++)
        {
            // 총알
            float randPos = Random.Range(-0.5f, 0.5f);
            Vector2 randVector = new Vector2(crazyShotPos.position.x + randPos, crazyShotPos.position.y + randPos);
            SpawnAttack(ConstValues.GunnerCrazyShot, randVector);

            // 이팩트
            SpawnObject(ConstValues.GunnerCrazyShotEffect, randVector);
            Rebound(2.0f);
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;
        }
        return true;
    }
    
    // 거너 빅샷
    private async UniTask<bool> BigShot()
    {
        float delay1 = 0.5f;

        StateSetting(ENormalState.Skill, ConstValues.GunnerBigShot, ConstValues.GunnerBigShotReady);
        
        Scream();
        SpawnObject(ConstValues.GunnerFlash, centerPos);
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerBigShot);
        SpawnAttack(ConstValues.GunnerBigShot, bigShotPos);
        Rebound(6.0f);
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        return true;
    }
}
