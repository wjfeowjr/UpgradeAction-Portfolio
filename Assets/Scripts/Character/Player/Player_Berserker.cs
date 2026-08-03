using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Player_Berserker : Player
{
    [SerializeField] private Transform attack1Pos;
    [SerializeField] private Transform attack2Pos;
    [SerializeField] private Transform attack3Pos;
    [SerializeField] private Transform jumpAttackPos;
    [SerializeField] private Transform jumpAttack1Pos;
    [SerializeField] private Transform jumpAttack2Pos;
    [SerializeField] private Transform changeAttackPos;
    
    [SerializeField] private Transform upperSlashPos;
    [SerializeField] private Transform upperSlashComboPos;
    [SerializeField] private Transform crashPos;
    [SerializeField] private Transform crashExplosionPos;
    [SerializeField] private Transform fireStrikePos;
    [SerializeField] private Transform fireStrikeChargePos;
    [SerializeField] private Transform swordCounterPos;
    [SerializeField] private Transform swordCounterEffectPos;
    [SerializeField] private Transform swordCounterChargePos;
    [SerializeField] private Transform swordCounterGuardEffectPos;
    [SerializeField] private Transform chargeCrashSmashPos;
    [SerializeField] private Transform chargeCrashSmashEffectPos;
    
    private int maxSword = 3;

    public override async void ChangeAttack()
    {
        CancelMotion();

        curGlobalCoolTime = 0;

        stateCancellation = new CancellationTokenSource();
        var finishSuccess = await BerserkerChangeAttack();
        // 스킬로 캔슬된 경우, 새 스킬이 설정한 바디타입(예: 반격기의 Counter)을 덮어쓰지 않는다
        if (normalState != ENormalState.Skill)
            ResetBodyType();
        if (!finishSuccess)
        {
            Debug.Log($"교체 공격 캔슬");
            return;
        }

        StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);

    }
    private async UniTask<bool> BerserkerChangeAttack()
    {
        StateSetting(ENormalState.Attack, ConstValues.ChangeAttack, ConstValues.ChangeAttack);
        
        var delay1 = 0.14f;
        for (int i = 0; i < 3; i++)
        {
            SpawnAttack($"{ConstValues.Berserker}_{ConstValues.ChangeAttack}", changeAttackPos);
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;
        }
        return true;
    }
    private async void BerserkerChangeAttackNotMotion()
    {
        stateCancellation = new CancellationTokenSource();
        var delay1 = 0.14f;
        for (int i = 0; i < 3; i++)
        {
            SpawnAttack($"{ConstValues.Berserker}_{ConstValues.ChangeAttack}", changeAttackPos);
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return;
        }
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
            // 스킬로 캔슬된 경우, 새 스킬이 설정한 바디타입(예: 반격기의 Counter)을 덮어쓰지 않는다
            if (normalState != ENormalState.Skill)
                ResetBodyType();
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
        ResetBodyType();
        
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
                
                AttackMotionFlip();
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
                
                AttackMotionFlip();
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
        
        float jumpAttackDelay1 = 0.16f;
        float jumpAttackDelay2 = 0.25f;
        
        StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack, ConstValues.JumpAttack);
        if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow()) 
            return false;

        SpawnAttack(ConstValues.BerserkerJumpAttack, jumpAttackPos);

        float timer = 0.0f;
        while (GetJumpState() && timer < jumpAttackDelay2 && !isGrounded)
        {
            timer += Time.deltaTime;
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        
        if (timer < jumpAttackDelay2)
        {
            myRigidbody.linearVelocityX = 0;
            StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttackEnd);
            float finalTime = jumpAttackDelay2 - timer;
            if (await AttackDelay(finalTime).SuppressCancellationThrow()) 
                return false;
        }
        
        canAttack = true;
        didJumpAttack = true;
        return true;
        
    }

    public override async void Skill(KeyCode skillKey)
    {
        if (Time.timeScale == 0)
            return;

        var playerInfo = GameManager.Instance.PlayerInfoList.Find(x => x.playerId == basicStat.id);
        var skillId = playerInfo.skillKeyList.Find(x => x.keyCode == skillKey).skillId;
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

        SkillSpeedAndArmorCheck(skillId);
        if (skillId == ConstValues.BerserkerUpperSlash)
        {
            finishSuccess = await UpperSlash();
        }
        else if (skillId == ConstValues.BerserkerFireStrike)
        {
            finishSuccess = await FireStrike();
        }
        else if (skillId == ConstValues.BerserkerSwordCounter)
        {
            finishSuccess = await SwordCounter();
            // 시간 제어 스킬
            if (!finishSuccess)
            {
                Time.timeScale = 1.0f;
                GameManager.Instance.TimeProduct = false;
            }
        }
        else if (skillId == ConstValues.BerserkerCrash)
        {
            finishSuccess = await Crash();
        }
        else if (skillId == ConstValues.BerserkerChargeCrash)
        {
            finishSuccess = await ChargeCrash();
        }
        
        // 스킬을 끝마치건 도중 캔슬되던, 스피드는 원상태로 복구됨
        ResetSkillSpeed();
        // 성공여부와 상관없이 바디타입 원상복구
        ResetBodyType();
        
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
        // 특성 체크
        var skillId = ConstValues.BerserkerUpperSlash;
        bool swiftSlash = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.SwiftSlash);
        bool swordBeam = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.SwordBeam);
        bool comboSlash = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.ComboSlash);

        var delay1 = 0.2f;
        var delay2 = 0.2f;
        var delay3 = 0.2f;
        var delay4 = 0.1f;
        

        // 신속한 베기 : 보는 방향으로 감지된 거리만큼 돌진 후 공격
        if (swiftSlash)
        {
            var dashSpeed = 20;
            float dashLength = 4;

            Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var boxSize = new Vector2(0.1f, physicsCollider.size.y);
            var boxHit = Physics2D.BoxCast(physicCenterPos.position, boxSize, 0f, dir, dashLength, playerChargeLayerMask);
            if (boxHit.collider)
                dashLength = boxHit.distance;

            chargeVector = new Vector2(transform.position.x + dir.x * dashLength, transform.position.y);

            if (dashLength > 1)
            {
                PlaySound(ConstValues.PlayerDash);
                StateSetting(ENormalState.Skill, ConstValues.BerserkerUpperSlashSwift, ConstValues.BerserkerUpperSlash);
                if (await Charge(dashSpeed, 1.0f, dashLength, 1.0f) == false)
                    return false;
            }
        }

        StateSetting(ENormalState.Skill, ConstValues.BerserkerUpperSlash, ConstValues.BerserkerUpperSlash);
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        
        SpawnAttackObject(ConstValues.BerserkerUpperSlash, upperSlashPos).GetComponent<Attack>();
        
        // 후딜동안 스킬버튼 한번 더 누르면 추가타격
        float addTime = 0;
        bool inputCombo = false;
        while (addTime < delay3)
        {
            addTime += Time.deltaTime * basicStat.attackSpeed;
            if (Input.GetKeyDown(GameManager.Instance.GetSkillKey(ConstValues.BerserkerUpperSlash)))
                inputCombo = true;
            
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;

            // 추가 타격
            if (comboSlash && inputCombo && addTime >= delay2)
            {
                comboSlash = false;
                StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerUpperSlashComboAttack);
                if (await AttackDelay(delay1).SuppressCancellationThrow())
                    return false;
            
                SpawnAttackObject(ConstValues.BerserkerUpperSlashComboAttack, upperSlashComboPos);
                Leap(0, 15, 2.0f);
                
                if (await AttackDelay(delay4).SuppressCancellationThrow())
                    return false;
                
                if(swordBeam)
                    SpawnAttackObject(ConstValues.BerserkerUpperSlashSwordBeam, upperSlashComboPos);
            
                if (await WaitUntilDelay(()=> myRigidbody.linearVelocityY < 0.01f, stateCancellation).SuppressCancellationThrow())
                    return false;
            }
        }

        return true;
    }
    
    // 불덩이 날리기
    private async UniTask<bool> FireStrike()
    {
        // 특성 체크
        var skillId = ConstValues.BerserkerFireStrike;
        var objectId = $"{ConstValues.BerserkerFireStrike}_{ConstValues.Object}";
        bool chargingFlame = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.ChargingFire);

        StateSetting(ENormalState.Skill, ConstValues.BerserkerFireStrike, ConstValues.BerserkerFireStrike);

        var delay1 = 0.1f;
        var delay2 = 0.1f;
        var delay3 = 0.4f;

        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;

        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        bool isCharge = false;
        GameObject chargeEffect = null;
        if (chargingFlame)
        {
            float addTime = 0;
            float originChargeTime = 0.35f;
            float plusAttackSpeed = basicStat.attackSpeed - originStat.attackSpeed;
            float chargeTime = originChargeTime - (originChargeTime * plusAttackSpeed);

            bool isSpawnedEffect = false;
            while (addTime < chargeTime && Input.GetKey(GameManager.Instance.GetSkillKey(ConstValues.BerserkerFireStrike)))
            {
                if (!isSpawnedEffect)
                {
                    chargeEffect = SpawnObject(ConstValues.BerserkerFireStrikeChargeEffect, fireStrikeChargePos);
                    isSpawnedEffect = true;
                }
            
                addTime += Time.deltaTime;
                if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                    return false;
            
                ShakeSpritePos(0.05f);
            }

            if (addTime >= chargeTime)
                isCharge = true;

            if (isCharge)
            {
                Debug.Log(addTime);
                SpawnObject(ConstValues.BerserkerFlash, centerPos);
            }
            
            // 충전 완료 뒤에도 잠시 모을시간 주기
            if (isCharge)
            {
                float addTime2 = 0;
                float extraChargeTime = 0.35f;
                while (addTime2 < extraChargeTime && Input.GetKey(GameManager.Instance.GetSkillKey(ConstValues.BerserkerFireStrike)))
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
            objectId = $"{objectId}_{ConstValues.Big}";
        
        if(chargeEffect != null)
            chargeEffect.SetActive(false);

        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerFireStrike);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        var missileObject = SpawnAttackObject(objectId, fireStrikePos).GetComponent<Missile>();
        var addObjectList = GameManager.Instance.GetAttributeAddObject(objectId);
        foreach (var addObject in addObjectList)
        {
            switch (addObject.addObjectId)
            {
                // 폭발 시 오브젝트 생성
                case ConstValues.ExplosionObject:
                    string burnId = addObject.objectId;
                    var objectIdSplit = objectId.Split('_');
                    if (objectIdSplit.Length > 3)
                        burnId = $"{burnId}_{objectIdSplit[3]}";
                    
                    missileObject.AddSpawnObject(burnId);
                    break;
            }
        }

        if (await AttackDelay(delay3).SuppressCancellationThrow())
            return false;

        return true;
    }
    
    // 황소 반격
    private async UniTask<bool> SwordCounter()
    {
        // 특성 체크
        var skillId = ConstValues.BerserkerSwordCounter;
        bool vibratingSteel = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.VibratingSteel);
        bool bullCharge = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.BullCharge);
        
        float delay1 = 0.2f;
        float delay2 = 0.5f;
        float delay3 = 0.2f;
        float justTime = 0.15f;

        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;
        
        StateSetting(ENormalState.Skill, ConstValues.BerserkerSwordCounter, ConstValues.BerserkerSwordCounter);
        BodyTypeSetting(EBodyType.Counter);
        PlaySound(ConstValues.BerserkerSwordCounterGuard);
        
        float addTime = 0;
        float counterTime = 0.5f;
        float originTime = counterTime;
        float durationTime = 0;
        var upgradeList = GameManager.Instance.GetAttributeUpgrade(skillId);
        foreach (var upgrade in upgradeList)
        {
            switch (upgrade.upgradeId)
            {
                // 지속시간 증가
                case ConstValues.DurationUp:
                    durationTime = (originTime * upgrade.upgradeValue * 0.01f); // 수치
                    break;
            }
        }
        counterTime = originTime + durationTime;

        basicStat.bodyType = EBodyType.Counter;
        while (addTime < counterTime && !isCounterAttack)
        {
            addTime += Time.deltaTime;
            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        basicStat.bodyType = originStat.bodyType;
        
        // 반격 성공
        if (isCounterAttack)
        {
            // 황소의 힘
            var attributeBuffList = GameManager.Instance.GetAttributeBuff(skillId);
            foreach (var buff in attributeBuffList)
                AddBuff(buff.buffId, buff.buffValue, buff.buffTime, 0);

            bool isJustGuard = addTime < justTime;
            
            // 깡!
            if(vibratingSteel)
                SpawnAttack(ConstValues.BerserkerSwordCounterStun, swordCounterGuardEffectPos);
            else
                SpawnObject(ConstValues.BerserkerSwordCounterGuardEffect, swordCounterGuardEffectPos);
            
            if(isJustGuard)
                SpawnObject(ConstValues.BerserkerSwordCounterJustEffect, centerPos);
            
            immortal = true;
            GameManager.Instance.TimeProduct = true;
            Time.timeScale = 0.05f;
            var timeDelay = 0.06f;

            Debug.Log($"패링까지 걸린 시간: {addTime}");
            if (await AttackDelayNonAttackSpeed(timeDelay).SuppressCancellationThrow())
                return false;

            // 황소돌진 + 저스트가드 성공
            if (bullCharge && isJustGuard)
            {
                // 자세잡기
                StateSetting(ENormalState.Skill, ConstValues.ComboAttack2, ConstValues.BerserkerSwordCounter);
                if (await IgnoreTimeDelay(delay1).SuppressCancellationThrow())
                    return false;
                
                Time.timeScale = 1.0f;
                GameManager.Instance.TimeProduct = false;
                
                // 돌진반격
                StateSetting(ENormalState.Skill, ConstValues.ComboAttack2, ConstValues.BerserkerSwordCounter);
                var chargeObject = SpawnAttackObject(ConstValues.BerserkerSwordCounterCharge, swordCounterChargePos).GetComponent<Attack>();
                
                // 돌진
                var dashSpeed = 25;
                var dashLength = 10;
                if(transform.localScale.x > 0)
                    chargeVector = new Vector2(transform.position.x + dashLength, transform.position.y);
                else
                    chargeVector = new Vector2(transform.position.x - dashLength, transform.position.y);
                
                if(await Charge(dashSpeed, 1.0f, dashLength, 1.0f) == false)
                    return false;
                
                chargeObject.DisActiveCollider();
                if (await AttackDelay(delay2).SuppressCancellationThrow())
                    return false;
                
                chargeObject.gameObject.SetActive(false);
            }
            else
            {
                // 자세잡기
                StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerSwordCounter);
                if (await IgnoreTimeDelay(delay1).SuppressCancellationThrow())
                    return false;
                
                Time.timeScale = 1.0f;
                GameManager.Instance.TimeProduct = false;
            
                // 반격
                StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerSwordCounter);

                if (isJustGuard)
                {
                    SpawnAttack(ConstValues.BerserkerSwordCounterJust, swordCounterPos);
                    SpawnAttack(ConstValues.BerserkerSwordCounterEffect, swordCounterEffectPos);
                }
                else
                {
                    SpawnAttack(ConstValues.BerserkerSwordCounter, swordCounterPos);
                }
                if (await AttackDelay(delay2).SuppressCancellationThrow())
                    return false;
            }
            
            immortal = false;
            isCounterAttack = false;
        }
        return true;
    }
    
    // 박살내기
    private async UniTask<bool> Crash()
    {
        // 특성 체크
        var skillId = ConstValues.BerserkerCrash;
        bool earthQuake = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.EarthQuake);
        bool magmaEruption = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.MagmaEruption);
        bool furiousStrike = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.FuriousStrike);
        bool secondaryExplosion = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.SecondaryExplosion);
        
        float delay1 = 0.15f;
        float delay2 = 0.5f;
        float delay3 = 0.5f;

        float leapHeight = 8.0f;
        
        StateSetting(ENormalState.Skill, ConstValues.BerserkerCrash, ConstValues.BerserkerCrash);

        // 회전공격
        if (furiousStrike)
        {
            leapHeight = 10.0f;
        }
        else
        {
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocityX, leapHeight);
            for (int i = 0; i < 3; i++)
            {
                SpawnAttack(ConstValues.BerserkerCrashSpinAttack, jumpAttack1Pos);
                if (await AttackDelay(delay1).SuppressCancellationThrow())
                    return false;
            }
        }
        
        // 도움닫기
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocityX, leapHeight);
        SpawnObject(ConstValues.BerserkerFlash, centerPos);
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerCrash);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        float startPosY = transform.position.y;
        float dropForce = 30.0f;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        while (GetJumpState() && myRigidbody.linearVelocity.y < -0.05f && !isGrounded)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.BerserkerCrashSmash);
        SpawnAttack(ConstValues.BerserkerCrash, crashPos);
        SpawnAttack(ConstValues.BerserkerCrashExplosion, crashExplosionPos);
        
        if(earthQuake)
            SpawnAttack(ConstValues.BerserkerCrashEarthQuake, crashExplosionPos);

        if (magmaEruption)
        {
            float endPos = transform.position.y;
            float height = startPosY - endPos;
            Debug.Log(height);
            int count = 1;
            if (height >= 2.5f)
                count += 1;
            if (height >= 4.5f)
                count += 1;
            
            MagmaEruption(count);
        }

        if (secondaryExplosion)
            SecondaryExplosion();
        
        if (await AttackDelay(delay3).SuppressCancellationThrow())
            return false;

        return true;
        
    }

    private async void MagmaEruption(int count)
    {
        delayCancellation = new CancellationTokenSource();
        float delay = 0.3f;
        var objectPos = crashExplosionPos.position;
        for (int i = 0; i < count; i++)
        {
            float randX = Random.Range(-1, 1);
            Vector2 magmaPos = new Vector2(objectPos.x + randX, objectPos.y);
            if (await NormalDelay(delay, delayCancellation).SuppressCancellationThrow())
                return;
            SpawnAttack(ConstValues.BerserkerCrashMagmaEruption, magmaPos);
        }
    }

    private async void SecondaryExplosion()
    {
        delayCancellation = new CancellationTokenSource();
        float delay = 2.0f;
        Vector2 objectPos = crashExplosionPos.position;
        
        SpawnObject(ConstValues.BerserkerCrashSecondaryExplosionEffect, objectPos);
        if (await NormalDelay(delay, delayCancellation).SuppressCancellationThrow())
            return;
        SpawnAttack(ConstValues.BerserkerCrashSecondaryExplosion, objectPos);
    }
    
    public async UniTask<bool> EventCrash()
    {
        float delay1 = 0.5f;
        float delay2 = 0.05f;

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