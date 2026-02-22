using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Player_Berserker : Player
{
    [SerializeField] private Transform attack1Pos;
    [SerializeField] private Transform attack2Pos;
    [SerializeField] private Transform attack3Pos;
    [SerializeField] private Transform jumpAttack1Pos;
    [SerializeField] private Transform jumpAttack2Pos;
    [SerializeField] private Transform changeAttackPos;
    
    [SerializeField] private Transform upperSlashPos;
    [SerializeField] private Transform crashPos;
    [SerializeField] private Transform crashExplosionPos;
    [SerializeField] private Transform fireStrikePos;
    [SerializeField] private Transform fireStrikeChargePos;
    [SerializeField] private Transform chargeCrashSmashPos;
    [SerializeField] private Transform chargeCrashSmashEffectPos;
    
    private int maxSword = 3;
    
    public override async void ChangeAttack()
    {
        Debug.Log("교체 공격 시작");
        CancelMotion();

        curGlobalCoolTime = 0;
        stateCancellation = new CancellationTokenSource();
        var finishSuccess = await BerserkerChangeAttack();
        if (!finishSuccess)
        {
            Debug.Log($"교체 공격 캔슬");
            return;
        }
        
        Debug.Log($"교체공격 끝");
        // 동작이 끝날때 반환하는 트리거
        StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
    }
    private async UniTask<bool> BerserkerChangeAttack()
    {
        var delay1 = 0.14f;
        StateSetting(ENormalState.Attack, ConstValues.ChangeAttack, ConstValues.ChangeAttack);

        for (int i = 0; i < 3; i++)
        {
            SpawnAttack($"{ConstValues.Berserker}_{ConstValues.ChangeAttack}", changeAttackPos);
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;
        }
        return true;
    }
    
    public override async UniTask<bool> Attack()
    {
        if (!await base.Attack())
            return false;
        
        bool finishSuccess = true;
        string type = "지상";
        
        switch (landingState)
        {
            // 지상공격
            case ELandingState.Ground:
                CancelMotion(true, true, false);
                stateCancellation = new CancellationTokenSource();
                finishSuccess = await BerserkerLandingAttack();
                break;

            // 점프공격
            case ELandingState.Air:
                type = "점프";
                CancelMotion(false, false);
                stateCancellation = new CancellationTokenSource();
                finishSuccess = await BerserkerJumpAttack();
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

    private async UniTask<bool> BerserkerLandingAttack()
    {
        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);
        
        var afterDelay = 0.35f;
        attackBuffer = false;
        
        if (landingAttackCount < maxSword)
            landingAttackCount += 1;

        switch (landingAttackCount)
        {
            case 1:
                var delay1 = 0.16f;
                var delay2 = 0.2f;
                var checkDelay1 = delay1 + delay2 + afterDelay;
                StateSetting(ENormalState.Attack, ConstValues.Attack, ConstValues.Attack1);
                AttackAdvance(2.0f);

                AttackChecker(0.1f, checkDelay1);
                if (await AttackDelay(delay1).SuppressCancellationThrow())
                    return false;

                GameObject obj = SpawnAttackObject(ConstValues.BerserkerAttack1, attack1Pos);

                if (await BufferDelay(delay2, afterDelay).SuppressCancellationThrow())
                {
                    obj.SetActive(false);
                    return false;
                }
                break;
            
            case 2:
                var delay3 = 0.12f;
                var delay4 = 0.2f;
                var checkDelay2 = delay3 + delay4 + afterDelay;
                
                MotionFlip();
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack2);
                AttackAdvance(2.0f);

                AttackChecker(0.1f, checkDelay2);
                if (await AttackDelay(delay3).SuppressCancellationThrow())
                    return false;

                GameObject obj2 = SpawnAttackObject(ConstValues.BerserkerAttack2, attack2Pos);
                if (await BufferDelay(delay4, afterDelay).SuppressCancellationThrow())
                {
                    obj2.SetActive(false);
                    return false;
                }
                break;
            
            case 3:
                var delay5 = 0.16f;
                var delay6 = 0.5f;
                
                MotionFlip();
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack3);
                AttackAdvance(3.0f);

                if (await AttackDelay(delay5).SuppressCancellationThrow())
                    return false;

                GameObject obj3 = SpawnAttackObject(ConstValues.BerserkerAttack3, attack3Pos);
                if (await AttackDelay(delay6).SuppressCancellationThrow())
                {
                    obj3.SetActive(false);
                    return false;
                }
                
                landingAttackCount = 0;
                break;
        }
        canAttack = true;
        return true;
    }

    private async UniTask<bool> BerserkerJumpAttack()
    {
        ResetTriggerAnimator(ConstValues.JumpDown);
        attackBuffer = false;
        
        if (jumpAttackCount <= 0)
        {
            float jumpAttackDelay1 = 0.16f;
            float jumpAttackDelay2 = 0.2f;
            float jumpAttackForce = 6;

            float checkDelay1 = jumpAttackDelay1 + (jumpAttackDelay2 * 2);

            AttackChecker(0.1f, checkDelay1);
            StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
            if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow())
                return false;
            
            jumpAttackCount += 1;
            
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpAttackForce);
            SpawnAttack(ConstValues.BerserkerJumpAttack1, jumpAttack1Pos);
            if (await AttackDelay(jumpAttackDelay2).SuppressCancellationThrow())
                return false;
            
            SpawnAttack(ConstValues.BerserkerJumpAttack1, jumpAttack1Pos);
            if (await AttackDelay(jumpAttackDelay2).SuppressCancellationThrow())
                return false;
        }
        else
        {
            jumpAttackCount += 1;

            float jumpAttackDelay3 = 0.12f;
            float jumpAttackDelay4 = 0.6f;
            float jumpAttackForce = 6;

            MotionFlip();
            GravityChange(myGravity);
            StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack2, ConstValues.JumpAttack2Start);
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpAttackForce);
            if (await AttackDelay(jumpAttackDelay3).SuppressCancellationThrow())
                return false;

            StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2Drop);
            SpawnAttack(ConstValues.BerserkerJumpAttack2, jumpAttack2Pos);
            float dropForce = 30.0f;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
            while (GetJumpState() && myRigidbody.linearVelocity.y < -0.05f && !isGrounded)
            {
                if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                    return false;
            }
            jumpAttackCount = 0;
            StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2End);
            SpawnAttack(ConstValues.BerserkerJumpAttack2Effect, jumpAttack2Pos);

            //SpawnSwordWave(attackPos[2]);
            //GameManager.Instance.playerShare.currentJumpAttack = 0;
            if (await AttackDelay(jumpAttackDelay4).SuppressCancellationThrow())
                return false;
            ClearObjectList(controlObject);
        }
        canAttack = true;
        return true;
    }

    public override async void Skill(KeyCode skillKey)
    {
        if (Time.timeScale == 0)
            return;

        var skillId = GameManager.Instance.PlayerSkillKey.berserkerSkillKeyList.Find(x => x.keyCode == skillKey).skillId;
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

        stateCancellation = new CancellationTokenSource();
        bool finishSuccess = true;
        if (skillKey == GameManager.Instance.dashKey)
        {
            finishSuccess = await Dash();
        }
        
        if (skillId == ConstValues.BerserkerUpperSlash)
        {
            finishSuccess = await UpperSlash();
        }
        else if (skillId == ConstValues.BerserkerFireStrike)
        {
            finishSuccess = await FireStrike();
        }
        else if (skillId == ConstValues.BerserkerCrash)
        {
            finishSuccess = await Crash();
        }
        else if (skillId == ConstValues.BerserkerChargeCrash)
        {
            finishSuccess = await ChargeCrash();
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

    // 올려베기
    private async UniTask<bool> UpperSlash()
    {
        StateSetting(ENormalState.Skill, ConstValues.BerserkerUpperSlash, ConstValues.BerserkerUpperSlash);

        var delay1 = 0.16f;
        var delay2 = 0.2f;
        var delay3 = 0.3f;
        var delay4 = 0.1f;
        
        // if(landingState == ELandingState.Ground)
        //     myRigidbody.linearVelocity = Vector2.zero;
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        //Leap(6, 20, 1.6f);
        //Leap(0, 20, 0.8f);

        bool swordBeam = false;
        bool comboAttack = false;
        foreach (var attribute in GameManager.Instance.PlayerSkill.GetSkillAttribute(ConstValues.BerserkerUpperSlash))
        {
            switch (attribute.attributeId)
            {
                case ConstValues.SwordBeam:
                    swordBeam = true;
                    break;
                case ConstValues.ComboAttack:
                    comboAttack = true;
                    break;
            }
        }
        SpawnAttackObject(ConstValues.BerserkerUpperSlash, upperSlashPos);

        float addTime = 0;
        bool firstSword = false;
        bool inputCombo = false;
        while (addTime < delay3)
        {
            addTime += Time.deltaTime * basicStat.attackSpeed;
            if (Input.GetKeyDown(GameManager.Instance.BerserkerSkillKey(ConstValues.BerserkerUpperSlash)))
                inputCombo = true;
            
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;

            if (swordBeam && addTime > delay4 && !firstSword)
            {
                SpawnAttackObject(ConstValues.BerserkerSwordBeam, upperSlashPos);
                firstSword = true;
            }
            
            // 추가 타격
            if (comboAttack && inputCombo && addTime >= delay2)
            {
                comboAttack = false;
                StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerUpperSlashComboAttack);
                if (await AttackDelay(delay1).SuppressCancellationThrow())
                    return false;
            
                SpawnAttackObject(ConstValues.BerserkerUpperSlashComboAttack, upperSlashPos);
                Leap(0, 15, 2.0f);
                
                if (await AttackDelay(delay4).SuppressCancellationThrow())
                    return false;
                
                if(swordBeam)
                    SpawnAttackObject(ConstValues.BerserkerSwordBeam, upperSlashPos);
            
                if (await WaitUntilDelay(()=> myRigidbody.linearVelocityY < 0.01f, stateCancellation).SuppressCancellationThrow())
                    return false;
            }
        }

        return true;
    }
    
    // 불덩이 날리기
    private async UniTask<bool> FireStrike()
    {
        StateSetting(ENormalState.Skill, ConstValues.BerserkerFireStrike, ConstValues.BerserkerFireStrike);

        var delay1 = 0.1f;
        var delay2 = 0.1f;
        var delay3 = 0.4f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;

        bool afterBurn = false;
        bool chargingFlame = false;
        foreach (var attribute in GameManager.Instance.PlayerSkill.GetSkillAttribute(ConstValues.BerserkerFireStrike))
        {
            switch (attribute.attributeId)
            {
                case ConstValues.AfterBurn:
                    afterBurn = true;
                    break;
                
                case ConstValues.ChargingFlame:
                    chargingFlame = true;
                    BodyTypeSetting(ConstValues.SuperArmor);
                    break;
            }
        }
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        string objectId = ConstValues.BerserkerFireStrike;
        
        bool isCharge = false;
        GameObject chargeEffect = null;
        if (chargingFlame)
        {
            float addTime = 0;
            float chargeTime = 1.0f;
            bool isSpawnedEffect = false;
            while (addTime < chargeTime && Input.GetKey(GameManager.Instance.BerserkerSkillKey(ConstValues.BerserkerFireStrike)))
            {
                if (!isSpawnedEffect)
                {
                    chargeEffect = SpawnObject(ConstValues.BerserkerFireStrikeChargeEffect, fireStrikeChargePos);
                    isSpawnedEffect = true;
                }
            
                addTime += Time.deltaTime * basicStat.attackSpeed;
                if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                    return false;
            
                ShakeSpritePos(0.05f);
            }

            if (addTime >= chargeTime)
                isCharge = true;
            
            if(isCharge)
                SpawnObject(ConstValues.BerserkerFlash, centerPos);
            
            // 충전 완료 뒤에도 잠시 모을시간 주기
            if (isCharge)
            {
                float addTime2 = 0;
                float extraChargeTime = 0.5f;
                while (addTime2 < extraChargeTime && Input.GetKey(GameManager.Instance.BerserkerSkillKey(ConstValues.BerserkerFireStrike)))
                {
                    addTime2 += Time.deltaTime * basicStat.attackSpeed;
                    if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                        return false;
                    ShakeSpritePos(0.05f);
                }
            }
        }
        ResetSpritePos();
        if (isCharge)
            objectId = $"{ConstValues.BerserkerFireStrike}_{ConstValues.Big}";
        
        if(chargeEffect != null)
            chargeEffect.SetActive(false);

        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerFireStrike);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        var missileObject = SpawnAttackObject(objectId, fireStrikePos).GetComponent<Missile>();
        // 후속화염 추가
        if (afterBurn)
        {
            string burnId = ConstValues.BerserkerFireStrikeAfterBurn;
            if (isCharge)
                burnId = $"{ConstValues.BerserkerFireStrikeAfterBurn}_{ConstValues.Big}";
            
            missileObject.AddSpawnObject(burnId);
        }
        
        if (await AttackDelay(delay3).SuppressCancellationThrow())
            return false;

        return true;
    }
    
    // 박살내기
    private async UniTask<bool> Crash()
    {
        float delay1 = 0.5f;
        float delay2 = 0.05f;
        float delay3 = 0.32f;

        SpawnAttack(ConstValues.BerserkerFlash, centerPos);
        StateSetting(ENormalState.Skill, ConstValues.BerserkerCrash, ConstValues.BerserkerCrash);
        
        // 도움닫기
        myRigidbody.linearVelocity = new Vector2(0, 8.0f);
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        float dropForce = 20.0f;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        while (GetJumpState())
        {
            if(await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerCrashSmash);
        
        //IsJumping = false;
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;
        
        SpawnAttack(ConstValues.BerserkerCrash, crashPos);
        SpawnAttack(ConstValues.BerserkerCrashExplosion, crashExplosionPos);
        
        if (await AttackDelay(delay3).SuppressCancellationThrow())
            return false;

        return true;
    }
    public async UniTask<bool> EventCrash()
    {
        float delay1 = 0.5f;
        float delay2 = 0.05f;
        float delay3 = 0.32f;

        SpawnAttack(ConstValues.BerserkerFlash, centerPos);
        StateSetting(ENormalState.Skill, ConstValues.BerserkerCrash, ConstValues.BerserkerCrash);
        
        // 도움닫기
        myRigidbody.linearVelocity = new Vector2(0, 8.0f);
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        float dropForce = 20.0f;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        while (GetJumpState())
        {
            if(await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerCrashSmash);
        
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;
        
        SpawnAttack(ConstValues.BerserkerCrash, crashPos);
        SpawnAttack(ConstValues.BerserkerCrashExplosion, crashExplosionPos);
        return true;
    }

    // 광전사 차지크래시
    private async UniTask<bool> ChargeCrash()
    {
        float delay1 = 0.35f;
        float delay2 = 0.25f;
        float delay3 = 0.4f;

        GravityChange(0);
        myRigidbody.linearVelocity = Vector2.zero;
        
        StateSetting(ENormalState.Skill, ConstValues.BerserkerChargeCrash, ConstValues.BerserkerChargeCrash);
        
        SpawnAttack(ConstValues.BerserkerChargeCrash, centerPos);        
        
        var dashSpeed = 16;
        var dashLength = 5;
        // 대시 레이캐스트 체크
        //chargeVector = RayCheckLength(dashLength, 0);
        if(transform.localScale.x > 0)
            chargeVector = new Vector2(transform.position.x + dashLength, transform.position.y);
        else
            chargeVector = new Vector2(transform.position.x - dashLength, transform.position.y);
        
        // 돌진
        if(await Charge(dashSpeed, 1.0f, dashLength, 1.0f) == false)
            return false;
        
        SetTriggerAnimator(ConstValues.ComboAttack);
        SpawnAttack(ConstValues.BerserkerChargeCrashSlash, centerPos);       
        SpawnObject(ConstValues.BerserkerFlash, centerPos);
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        myRigidbody.linearVelocity = Vector2.zero;
        SetTriggerAnimator(ConstValues.ComboAttack);
        
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;
        
        SpawnAttack(ConstValues.BerserkerChargeCrashSmash, chargeCrashSmashPos);
        SpawnObject(ConstValues.BerserkerChargeCrashSmashEffect, chargeCrashSmashEffectPos);
        GravityChange(myGravity);
        
        if (await AttackDelay(delay3).SuppressCancellationThrow())
            return false;
        
        return true;
    }
}