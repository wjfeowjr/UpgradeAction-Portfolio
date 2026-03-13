using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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
    [SerializeField] private Transform elementalInfusionPos;
    [SerializeField] private Transform bigShotPos;
    
    private int maxBullet = 3;

    private void Scream()
    {
        SoundManager.Instance.PlaySound(ConstValues.GunnerLaugh);
    }
    
    public override async void ChangeAttack()
    {
        //Debug.Log("교체 공격 시작");
        CancelMotion();
        
        BodyTypeSetting(EBodyType.SuperArmor);
        curGlobalCoolTime = 0;
        stateCancellation = new CancellationTokenSource();
        var finishSuccess = await GunnerChangeAttack();
        
        ResetBodyType();
        if (!finishSuccess)
        {
            Debug.Log($"교체 공격 캔슬");
            return;
        }
        
        //Debug.Log($"교체 공격 끝");
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

    private async UniTask<bool> GunnerLandingAttack()
    {
        string effectId = ConstValues.GunnerAttack1Effect;
        string objectId = ConstValues.GunnerAttack1Object;
        string finalEffectId = ConstValues.GunnerAttack2Effect;
        string finalObjectId = ConstValues.GunnerAttack2Object;
        string elemental = BuffElemental(false);

        if (!string.IsNullOrWhiteSpace(elemental))
        {
            effectId = $"{effectId}_{elemental}";
            objectId = $"{objectId}_{elemental}";
            finalEffectId = $"{finalEffectId}_{elemental}";
            finalObjectId = $"{finalObjectId}_{elemental}";
        }

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
            
                    SpawnObject(effectId, landingEffectPos);
                    SpawnObject(objectId, landingAttackPos);
                    
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
            
                    SpawnObject(effectId, landingEffectPos);
                    SpawnObject(objectId, landingAttackPos);
                    
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
        
                SpawnObject(finalEffectId, landingEffectPos);
                SpawnObject(finalObjectId, landingAttackPos);
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
        string effectId = ConstValues.GunnerAttack1Effect;
        string objectId = ConstValues.GunnerAttack1Object;
        string elemental = BuffElemental(false);
        
        if (!string.IsNullOrWhiteSpace(elemental))
        {
            effectId = $"{effectId}_{elemental}";
            objectId = $"{objectId}_{elemental}";
        }

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
            
            SpawnObject(effectId, landingEffectPos);
            SpawnObject(objectId, landingAttackPos);
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

        stateCancellation = new CancellationTokenSource();
        bool finishSuccess = true;
        if (skillKey == GameManager.Instance.dashKey)
        {
            SpawnAttack(ConstValues.GunnerDashShot, dashShotPos);
            finishSuccess = await Dash();
        }

        SkillSpeedAndArmorCheck(skillId);
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
        else if (skillId == ConstValues.GunnerElementalInfusion)
        {
            finishSuccess = await ElementalInfusion();
        }
        else if (skillId == ConstValues.GunnerBigShot)
        {
            finishSuccess = await BigShot();
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
    
    // 수류탄
    private async UniTask<bool> Grenade()
    {
        // 특성 체크
        var skillId = ConstValues.GunnerGrenade;
        var objectId = ConstValues.GunnerGrenadeObject;
        var skill = GetSkill(skillId);
        
        bool madBomber = GameManager.Instance.PlayerSkill.IsHaveAttribute(skillId, ConstValues.MadBomber);
        
        StateSetting(ENormalState.Skill, ConstValues.GunnerGrenade, ConstValues.GunnerGrenade);
        
        var delay1 = 0.1f;
        var delay2 = 0.15f;
        
        myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocityY);
        
        string elemental = BuffElemental();
        string flashId = ConstValues.GunnerFlash;
        switch (elemental)
        {
            case ConstValues.Fire:
                flashId = ConstValues.FireFlash;
                break;
            case ConstValues.Lightning:
                flashId = ConstValues.LightningFlash;
                break;
            case ConstValues.Ice:
                flashId = ConstValues.IceFlash;
                break;
        }

        int count = 1;
        if (madBomber)
        {
            Debug.Log($"정신나간 폭탄광 발동!");
            count = (int)skill.curCoolTime[2];
            if (count == (int)skill.maxCoolTime[2])
            {
                Scream();
                SpawnObject(flashId, centerPos);
            }
            for (int i = 0; i < count - 1; i++)
                delay1 += 0.1f;
        }
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        if (!string.IsNullOrWhiteSpace(elemental))
            objectId = $"{objectId}_{elemental}";
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerGrenade);
        for (int i = 0; i < count; i++)
        {
            var grenadeObject = SpawnAttackObject(objectId, grenadePos).GetComponent<Grenade>();
            var addObjectList = GameManager.Instance.PlayerSkill.GetAttributeAddObject(objectId);
            foreach (var addObject in addObjectList)
            {
                switch (addObject.addObjectId)
                {
                    // 폭발 시 오브젝트 생성
                    case ConstValues.ExplosionObject:
                        string fragmentsId = addObject.objectId;
                        if (!string.IsNullOrWhiteSpace(elemental))
                            fragmentsId = $"{fragmentsId}_{elemental}";
                        for (int j = 0; j < addObject.objectCount; j++)
                            grenadeObject.AddSpawnObject(fragmentsId);
                        break;
                }
            }
            
            if (madBomber && count > 1 && i > 1)
                grenadeObject.RandomForceThrow(4.0f, 2.0f);
        }
        if (madBomber)
        {
            skill.curCoolTime[1] = 0;
            skill.curCoolTime[2] = 0;
        }
        

        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
    
    // 넉백샷
    public async UniTask<bool> KnockBackShot()
    {
        // 특성 체크
        var skillId = ConstValues.GunnerKnockBackShot;
        var attackId = ConstValues.GunnerKnockBackShot;
        var objectId = $"{ConstValues.GunnerKnockBackShot}_{ConstValues.Object}";
        bool powerfulGunpowder = GameManager.Instance.PlayerSkill.IsHaveAttribute(skillId, ConstValues.PowerfulGunpowder);
        
        StateSetting(ENormalState.Skill, ConstValues.GunnerKnockBackShot, ConstValues.GunnerKnockBackShotReady);
        
        var delay1 = 0.1f;
        var delay2 = 0.2f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        bool isCharge = false;
        string elemental = BuffElemental();
        GameObject chargeEffect = null;
        if (powerfulGunpowder)
        {
            float addTime = 0;
            float chargeTime = 1.0f - Mathf.Abs(1.0f - basicStat.attackSpeed);

            bool isSpawnedEffect = false;
            while (addTime < chargeTime && Input.GetKey(GameManager.Instance.BerserkerSkillKey(ConstValues.BerserkerFireStrike)))
            {
                if (!isSpawnedEffect)
                {
                    string effectId = ConstValues.GunnerChargeEffect;
                    switch (elemental)
                    {
                        case ConstValues.Fire:
                            effectId = ConstValues.FireChargeEffect;
                            break;
                        case ConstValues.Lightning:
                            effectId = ConstValues.LightningChargeEffect;
                            break;
                        case ConstValues.Ice:
                            effectId = ConstValues.IceChargeEffect;
                            break;
                    }
                    chargeEffect = SpawnObject(effectId, centerPos);
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
                string flashId = ConstValues.GunnerFlash;
                switch (elemental)
                {
                    case ConstValues.Fire:
                        flashId = ConstValues.FireFlash;
                        break;
                    case ConstValues.Lightning:
                        flashId = ConstValues.LightningFlash;
                        break;
                    case ConstValues.Ice:
                        flashId = ConstValues.IceFlash;
                        break;
                }
                SpawnObject(flashId, centerPos);
            }
            
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
            attackId = $"{attackId}_{ConstValues.Big}";
        
        if(chargeEffect != null)
            chargeEffect.SetActive(false);
        
        if (!string.IsNullOrWhiteSpace(elemental))
        {
            attackId = $"{attackId}_{elemental}";
            objectId = $"{objectId}_{elemental}";
        }

        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerKnockBackShot);
        SpawnAttackObject(attackId, knockBackShotPos);
        int angleZ = 10;
        for (int i = 0; i < 5; i++)
        {
            SpawnObject(objectId, knockBackShotPos, angleZ);
            angleZ -= 5;
        }

        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
    // 넉백샷(이벤트)
    public async UniTask<bool> EventKnockBackShot()
    {
        var delay1 = 0.1f;
        var delay2 = 0.2f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;
        
        StateSetting(ENormalState.Skill, ConstValues.GunnerKnockBackShot, ConstValues.GunnerKnockBackShotReady);
        
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerKnockBackShot);
        SpawnAttack($"{ConstValues.GunnerKnockBackShot}_{ConstValues.Event}", knockBackShotPos);

        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
    
    // 정신나간 난사
    private async UniTask<bool> CrazyShot()
    {
        // 특성 체크
        var skillId = ConstValues.GunnerCrazyShot;
        var objectId = ConstValues.GunnerCrazyShot;
        var effectId = ConstValues.GunnerCrazyShotEffect;

        bool longShot = GameManager.Instance.PlayerSkill.IsHaveAttribute(skillId, ConstValues.LongShot);
        bool piercingStreak = GameManager.Instance.PlayerSkill.IsHaveAttribute(skillId, ConstValues.PiercingStreak);
        bool finishShot = GameManager.Instance.PlayerSkill.IsHaveAttribute(skillId, ConstValues.FinishShot);

        var delay1 = 0.5f;
        var delay2 = 0.5f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;
        
        string elemental = BuffElemental();
        string flashId = ConstValues.GunnerFlash;
        switch (elemental)
        {
            case ConstValues.Fire:
                flashId = ConstValues.FireFlash;
                break;
            case ConstValues.Lightning:
                flashId = ConstValues.LightningFlash;
                break;
            case ConstValues.Ice:
                flashId = ConstValues.IceFlash;
                break;
        }
        SpawnObject(flashId, centerPos);

        if (longShot)
        {
            StateSetting(ENormalState.Skill, ConstValues.GunnerCrazyShot2, ConstValues.GunnerCrazyShot);
        }
        else
        {
            StateSetting(ENormalState.Skill, ConstValues.GunnerCrazyShot, ConstValues.GunnerCrazyShot);
            // 딜레이가 만약 있다면 여기다가
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;
            StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerCrazyShot);
        }
        Scream();

        // 난사 시작
        float shotTime = 0.1f;

        int bulletCount = 14;
        var upgradeList = GameManager.Instance.PlayerSkill.GetAttributeUpgrade(skillId);
        foreach (var upgrade in upgradeList)
        {
            switch (upgrade.upgradeId)
            {
                // 지속시간 증가
                case ConstValues.CountUp:
                    bulletCount += upgrade.upgradeValue;
                    break;
            }
        }
        
        if (!string.IsNullOrWhiteSpace(elemental))
        {
            objectId = $"{objectId}_{elemental}";
            effectId = $"{effectId}_{elemental}";
        }
        
        for (int i = 0; i < bulletCount; i++)
        {
            // 총알
            Vector2 effectPos = crazyShotPos.position;
            float randPos = Random.Range(-0.3f, 0.3f);
            int randAngleZ = Random.Range(-3, 3);
            Vector2 randVector = new Vector2(effectPos.x + randPos, effectPos.y + randPos);
            var shotObject = SpawnAttackObject(objectId, randVector);
            shotObject.transform.eulerAngles = new Vector3(0, 0, randAngleZ);
            
            // 이팩트
            SpawnObject(effectId, randVector);
            if (await AttackDelay(shotTime).SuppressCancellationThrow())
                return false;
        }

        if (finishShot)
        {
            var delay3 = 0.5f;
            var delay4 = 0.2f;
            string finishId = piercingStreak ? ConstValues.GunnerCrazyShotFinishPierce : ConstValues.GunnerCrazyShotFinishObject;
            if (!string.IsNullOrWhiteSpace(elemental))
                finishId = $"{finishId}_{elemental}";
            
            StateSetting(ENormalState.Skill, ConstValues.ComboAttack2, ConstValues.GunnerCrazyShot);
            SpawnObject(flashId, centerPos);
            if (await AttackDelay(delay3).SuppressCancellationThrow())
                return false;
            
            Scream();
            StateSetting(ENormalState.Skill, ConstValues.ComboAttack2, ConstValues.GunnerCrazyShot);
            SpawnAttack(finishId, bigShotPos);
            if (await AttackDelay(delay4).SuppressCancellationThrow())
                return false;
        }
        else
        {
            StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerCrazyShot);
            if (await AttackDelay(delay2).SuppressCancellationThrow())
                return false;
        }

        return true;
    }
    
    // 거너 속성 주입
    private async UniTask<bool> ElementalInfusion()
    {
        var skillData = skillList.Find(x => x.id == ConstValues.GunnerElementalInfusion);
        if (skillData != null)
        {
            string skillId = ConstValues.GunnerElementalInfusion;
            bool finishingExplosion = GameManager.Instance.PlayerSkill.IsHaveAttribute(skillId, ConstValues.FinishingExplosion);
            
            if(landingState == ELandingState.Ground)
                myRigidbody.linearVelocity = Vector2.zero;
            
            var delay1 = 0.3f;
            
            StateSetting(ENormalState.Skill, ConstValues.GunnerElementalInfusion, ConstValues.GunnerElementalInfusion);
            SpawnObject(ConstValues.GunnerFlash, centerPos);
            Scream();
            var selectObject = SpawnObject(ConstValues.GunnerElementalInfusionSelect, elementalInfusionPos).GetComponent<Gunner_ElementalInfusionSelect>();
            selectObject.SetText(GameManager.Instance.GetKeyCode(GameManager.Instance.leftMoveKey), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey), GameManager.Instance.GetKeyCode(GameManager.Instance.rightMoveKey));
            
            bool isChoice = false;
            string elemental = default;
            float limitTime = 3.0f;
            float timer = 0;
            while (!isChoice && timer < limitTime)
            {
                if (Input.GetKeyDown(GameManager.Instance.leftMoveKey))
                {
                    isChoice = true;
                    elemental = ConstValues.Ice;
                }
                if (Input.GetKeyDown(GameManager.Instance.upKey))
                {
                    isChoice = true;
                    elemental = ConstValues.Lightning;
                }
                if (Input.GetKeyDown(GameManager.Instance.rightMoveKey))
                {
                    isChoice = true;
                    elemental = ConstValues.Fire;
                }
                if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                    return false;

                timer += Time.deltaTime;
                if (timer >= limitTime)
                {
                    isChoice = true;
                    int rand = Random.Range(0, 3);
                    switch (rand)
                    {
                        case 0:
                            elemental = ConstValues.Ice;
                            break;
                        
                        case 1:
                            elemental = ConstValues.Lightning;
                            break;
                        
                        case 2:
                            elemental = ConstValues.Fire;
                            break;
                    }
                }
            }

            Action endAction = null;
            if (finishingExplosion)
                endAction = () => SpawnAttack($"{ConstValues.GunnerElementalInfusion}_{elemental}", centerPos);
            
            switch (elemental)
            {
                case ConstValues.Ice:
                    AddBuff(skillData.buffName[0], skillData.buffValue[0], skillData.buffTime, skillData.buffCount, endAction);
                    break;
                case ConstValues.Lightning:
                    AddBuff(skillData.buffName[1], skillData.buffValue[1], skillData.buffTime, skillData.buffCount, endAction);
                    break;
                case ConstValues.Fire:
                    AddBuff(skillData.buffName[2], skillData.buffValue[2], skillData.buffTime, skillData.buffCount, endAction);
                    break;
            }
            selectObject.gameObject.SetActive(false);
            SpawnAttack($"{ConstValues.GunnerElementalInfusion}_{elemental}", centerPos);
            StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.GunnerElementalInfusion);
            
            // 강한 원소
            var attributeBuffList = GameManager.Instance.PlayerSkill.GetAttributeBuff(skillId);
            foreach (var buff in attributeBuffList)
                AddBuff(buff.buffId, buff.buffValue, buff.buffTime, 0);
            
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;
        }
        return true;
    }

    private string BuffElemental(bool isSkill = true)
    {
        string elemental = default;
        var iceBuff = TargetBuff(EBuffType.ElementalIce);
        var lightningBuff = TargetBuff(EBuffType.ElementalLightning);
        var fireBuff = TargetBuff(EBuffType.ElementalFire);
        
        if (iceBuff != null)
        {
            if (isSkill)
            {
                if (iceBuff.currentCount > 0)
                {
                    iceBuff.currentCount -= 1;
                }
                else
                {
                    return null;
                }
            }
            elemental = ConstValues.Ice;
        }
        else if (lightningBuff != null)
        {
            if (isSkill)
            { 
                if (lightningBuff.currentCount > 0)
                {
                    lightningBuff.currentCount -= 1;
                }
                else
                {
                    return null;
                }
            }
            elemental = ConstValues.Lightning;
        }
        else if (fireBuff != null)
        {
            if (isSkill)
            { 
                if (fireBuff.currentCount > 0)
                {
                    fireBuff.currentCount -= 1;
                }
                else
                {
                    return null;
                }
            }
            elemental = ConstValues.Fire;
        }

        return elemental;
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
