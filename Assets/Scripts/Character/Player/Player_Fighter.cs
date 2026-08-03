using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Player_Fighter : Player
{
    [SerializeField] private Transform attack1Pos;
    [SerializeField] private Transform attack2Pos;
    [SerializeField] private Transform attack3Pos;
    [SerializeField] private Transform jumpAttackPos;
    [SerializeField] private Transform lightningKickPos;
    [SerializeField] private Transform lightningPunchPos;
    [SerializeField] private Transform lightningPunchFinishPos;
    [SerializeField] private Transform lightningPunchMissilePos;
    [SerializeField] private Transform lightningPunchMissileFinishPos;
    [SerializeField] private Transform lightningSmashPos;
    [SerializeField] private Transform strongPunchPos;
    [SerializeField] private Transform punchTrailPos;
    
    private int maxPunch = 3;

    public override async void ChangeAttack()
    {
        //Debug.Log("교체 공격 시작");
        CancelMotion();
        
        curGlobalCoolTime = 0;
        
        stateCancellation = new CancellationTokenSource();
        var finishSuccess = await FighterChangeAttack();
        // 스킬로 캔슬된 경우, 새 스킬이 설정한 바디타입(예: 반격기의 Counter)을 덮어쓰지 않는다
        if (normalState != ENormalState.Skill)
            ResetBodyType();
        if (!finishSuccess)
        {
            Debug.Log($"교체 공격 캔슬");
            return;
        }
        StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
        
        // StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
        // FighterChangeAttackNotMotion();
    }
    private async UniTask<bool> FighterChangeAttack()
    {
        SpawnAttack($"{ConstValues.Fighter}_{ConstValues.ChangeAttack}", centerPos);
        return true;
    }
    private void FighterChangeAttackNotMotion()
    {
        SpawnAttack($"{ConstValues.Fighter}_{ConstValues.ChangeAttack}", centerPos);
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
                finishSuccess = await FighterLandingAttack();
                break;

            // 점프공격
            case ELandingState.Air:
                type = "점프";
                CancelMotion(false, false);
                stateCancellation = new CancellationTokenSource();
                finishSuccess = await FighterJumpAttack();
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

    private async UniTask<bool> FighterLandingAttack()
    {
        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);
        
        var afterDelay = 0.35f;
        attackBuffer = false;
        
        if (landingAttackCount < maxPunch)
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

                GameObject obj = SpawnAttackObject(ConstValues.FighterAttack1, attack1Pos);
                SpawnObject(ConstValues.FighterLightningEffect, attack1Pos);

                if (await BufferDelay(delay2, afterDelay).SuppressCancellationThrow())
                {
                    obj.SetActive(false);
                    return false;
                }
                break;
            
            case 2:
                var delay3 = 0.2f;
                var delay4 = 0.2f;
                var checkDelay2 = delay3 + delay4 + afterDelay;
                
                AttackMotionFlip();
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack2);
                AttackAdvance(2.0f);

                AttackChecker(0.1f, checkDelay2);
                if (await AttackDelay(delay3).SuppressCancellationThrow())
                    return false;

                GameObject obj2 = SpawnAttackObject(ConstValues.FighterAttack2, attack2Pos);
                SpawnObject(ConstValues.FighterLightningEffect, attack2Pos);
                if (await BufferDelay(delay4, afterDelay).SuppressCancellationThrow())
                {
                    obj2.SetActive(false);
                    return false;
                }
                break;
            
            case 3:
                var delay5 = 0.22f;
                var delay6 = 0.5f;
                
                AttackMotionFlip();
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack3);
                AttackAdvance(3.0f);
                
                if (await AttackDelay(delay5).SuppressCancellationThrow())
                    return false;
                
                var punchTrail = SpawnObject(ConstValues.FighterPunchTrail, punchTrailPos).GetComponent<VectorMove>();
                punchTrail.StartMove(attack3Pos.position, 0.1f);

                GameObject obj3 = SpawnAttackObject(ConstValues.FighterAttack3, attack3Pos);
                SpawnObject(ConstValues.FighterLightningEffect, attack3Pos);

                if (await AttackDelay(delay6).SuppressCancellationThrow())
                {
                    obj3.SetActive(false);
                    punchTrail.gameObject.SetActive(false);
                    return false;
                }
                
                landingAttackCount = 0;
                break;
        }
        canAttack = true;
        return true;
    }

    private async UniTask<bool> FighterJumpAttack()
    {
        ResetTriggerAnimator(ConstValues.JumpDown);
        
        float jumpAttackDelay1 = 0.2f;
        float jumpAttackDelay2 = 0.2f;
        float jumpAttackDelay3 = 0.2f;
        
        MotionFlip();
        StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack, ConstValues.JumpAttack);
        
        if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow()) 
            return false;
        
        float dropForceX = 12;
        float dropForceY = 15;
        myRigidbody.linearVelocity = new Vector2(transform.localScale.x * dropForceX, -dropForceY);
        var jumpAttackObject = SpawnAttackObject(ConstValues.FighterJumpAttack, jumpAttackPos).GetComponent<Trace>();
        var trailObject = SpawnObject(ConstValues.FighterJumpAttackTrail, jumpAttackPos).GetComponent<Trace>();
        StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack);

        float timer = 0;
        while (GetJumpState() && myRigidbody.linearVelocity.y < -0.05f && !isGrounded && timer < jumpAttackDelay2)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
            timer += Time.deltaTime;
        }
        
        while (GetJumpState())
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        myRigidbody.linearVelocity = Vector2.zero;
        
        //jumpAttackObject.GetComponent<Attack>().DisActiveCollider();
        trailObject.SetTarget(null);
        if (await AttackDelay(jumpAttackDelay3).SuppressCancellationThrow()) 
            return false;
        
        jumpAttackObject.gameObject.SetActive(false);
        trailObject.gameObject.SetActive(false);
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
        if (skillId == ConstValues.FighterLightningKick)
        {
            finishSuccess = await LightningKick();
        }
        else if (skillId == ConstValues.FighterLightningSmash)
        {
            finishSuccess = await LightningSmash();
        }
        else if (skillId == ConstValues.FighterLightningPunch)
        {
            finishSuccess = await LightningPunch();
        }
        else if (skillId == ConstValues.FighterStrongPunch)
        {
            finishSuccess = await StrongPunch();
            // 시간 제어 스킬
            if (!finishSuccess)
            {
                Time.timeScale = 1.0f;
                GameManager.Instance.TimeProduct = false;
            }
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

    // 공중제비 차기
    private async UniTask<bool> LightningKick()
    {
        // 특성 체크
        var skillId = ConstValues.FighterLightningKick;
        
        bool kickWave = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.KickWave);
        
        StateSetting(ENormalState.Skill, skillId, skillId);
        
        float delay1 = 0.2f;
        float delay2 = 0.3f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;
        
        float leapX = transform.localScale.x * 3;
        Leap(leapX, 6, 2.0f);

        if (await AttackDelay(delay1).SuppressCancellationThrow()) 
            return false;
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, skillId);
        SpawnAttack(skillId, lightningKickPos);
        
        if (kickWave)
            SpawnAttackObject(ConstValues.FighterLightningKickWave, lightningKickPos);
        
        if (await AttackDelay(delay2).SuppressCancellationThrow()) 
            return false;
        
        return true;
    }
    
    // 연타주먹
    private async UniTask<bool> LightningPunch()
    {
        // 특성 체크
        var skillId = ConstValues.FighterLightningPunch;
        bool movingPunch = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.MovingPunch);
        
        StateSetting(ENormalState.Skill, skillId, skillId);
        
        float delay1 = 0.1f;
        float delay2 = 0.2f;
        float delay3 = 0.3f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;

        int punchCount = 10;
        var upgradeList = GameManager.Instance.GetAttributeUpgrade(skillId);
        foreach (var upgrade in upgradeList)
        {
            switch (upgrade.upgradeId)
            {
                // 지속시간 증가
                case ConstValues.CountUp:
                    punchCount += upgrade.upgradeValue;
                    break;
            }
        }
        
        for (int i = 0; i < punchCount; i++)
        {
            float randPos1 = Random.Range(0.0f, 0.75f);
            float randPos2 = Random.Range(-0.75f, 0.75f);

            if (movingPunch)
                SpawnAttack(ConstValues.FighterLightningPunchMissile, lightningPunchMissilePos);
            else
                SpawnAttack(skillId, lightningPunchPos);
            
            var effectVector = new Vector2(lightningPunchPos.position.x + randPos1, lightningPunchPos.position.y + randPos2);
            SpawnObject(ConstValues.FighterLightningPunchEffect, effectVector);
            SpawnObject(ConstValues.FighterLightningEffect, effectVector);
            if (await AttackDelay(delay1).SuppressCancellationThrow()) 
                return false;
        }
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, ConstValues.FighterLightningPunchFinish);
        if (await AttackDelay(delay2).SuppressCancellationThrow()) 
            return false;

        if (movingPunch)
        {
            SpawnAttack(ConstValues.FighterLightningPunchFinishMissile, lightningPunchMissileFinishPos);
        }
        else
        {
            SpawnAttack(ConstValues.FighterLightningPunchFinish, lightningPunchFinishPos);
            SpawnObject(ConstValues.FighterLightningEffect, lightningPunchFinishPos);
        }
        
        if (await AttackDelay(delay3).SuppressCancellationThrow()) 
            return false;
        
        return true;
    }
    
    // 번개 강타
    private async UniTask<bool> LightningSmash()
    {
        // 특성 체크
        var skillId = ConstValues.FighterLightningSmash;
        
        bool lightningStrike = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.LightningStrike);
        bool shockSmash = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.ShockSmash);
        
        StateSetting(ENormalState.Skill, skillId, skillId);
        
        float delay1 = 0.2f;
        float delay2 = 0.3f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;
        
        SpawnObject(ConstValues.FighterFlash, centerPos);
        // if (await AttackDelay(delay1).SuppressCancellationThrow()) 
        //     return false;
        // StateSetting(ENormalState.Skill, ConstValues.ComboAttack, skillId);
        
        // 도움닫기
        var leapHeight = 8.0f; // transform.localScale.x * leapHeight 12.0f;
        myRigidbody.linearVelocity = new Vector2(0, leapHeight);
        var trailObject = SpawnObject(ConstValues.FighterLightningTrail, centerPos);
        SpawnObject(ConstValues.FighterLightningSmashWave, transform);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;
        
        float dropForce = 15.0f;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        while (GetJumpState() && myRigidbody.linearVelocity.y < -0.05f && !isGrounded)
        {
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return false;
        }
        
        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, skillId);
        SpawnAttack(ConstValues.FighterLightningSmash, lightningSmashPos);

        if(lightningStrike)
            LightningStrike( lightningSmashPos, ConstValues.FighterLightningSmashLightning,3);
        
        if(shockSmash)
            SpawnAttack(ConstValues.FighterLightningSmashLightningField, lightningSmashPos);
        
        if (await AttackDelay(delay2).SuppressCancellationThrow()) 
            return false;
        
        trailObject.SetActive(false);
        return true;
    }

    // 메가톤 강철주먹
    private async UniTask<bool> StrongPunch()
    {
        // 특성 체크
        var skillId = ConstValues.FighterStrongPunch;
        StateSetting(ENormalState.Skill, skillId, skillId);
        
        bool lightningIron = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.LightningIron);
        bool counterPunch = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.CounterPunch);
        
        float delay1 = 0.15f;
        float delay2 = 0.5f;
        float delay3 = 0.05f;
        float delay4 = 0.5f;
        
        if(landingState == ELandingState.Ground)
            myRigidbody.linearVelocity = Vector2.zero;
        
        if (await AttackDelay(delay1).SuppressCancellationThrow()) 
            return false;

        SpawnObject(ConstValues.FighterFlash, centerPos);
        SpawnObject(ConstValues.FighterStrongPunchReady, transform);

        if (counterPunch)
        {
            float addTime = 0;
            BodyTypeSetting(EBodyType.Counter);
            while (addTime < delay2 && !isCounterAttack)
            {
                addTime += Time.deltaTime;
                if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                    return false;
            }
            basicStat.bodyType = originStat.bodyType;
        }
        else
        {
            if (await AttackDelay(delay2).SuppressCancellationThrow()) 
                return false;
        }
        
        // 반격 성공
        if (isCounterAttack)
        {
            SpawnObject(ConstValues.BerserkerSwordCounterGuardEffect, centerPos);
            
            immortal = true;
            GameManager.Instance.TimeProduct = true;
            Time.timeScale = 0.05f;
            var timeDelay = 0.06f;
            
            if (await AttackDelayNonAttackSpeed(timeDelay).SuppressCancellationThrow())
                return false;
            
            Time.timeScale = 1.0f;
        }

        StateSetting(ENormalState.Skill, ConstValues.ComboAttack, skillId);
        SpawnObject(ConstValues.FighterStrongPunchWave, transform);
        if (await AttackDelay(delay3).SuppressCancellationThrow()) 
            return false;
        
        if(isCounterAttack)
            SpawnAttack(ConstValues.FighterStrongPunchJust, strongPunchPos);
        else
            SpawnAttack(ConstValues.FighterStrongPunch, strongPunchPos);
        
        if(lightningIron)
            LightningStrike( lightningSmashPos, ConstValues.FighterLightningSmashLightning,5);
        
        if (await AttackDelay(delay4).SuppressCancellationThrow()) 
            return false;
        
        return true;
    }
    
    private async void LightningStrike(Transform pos, string id, int count)
    {
        delayCancellation = new CancellationTokenSource();
        float delay = 0.3f;
        var objectPos = pos.position;
        for (int i = 0; i < count; i++)
        {
            float randX = Random.Range(-1.0f, 1.0f);
            Vector2 lightningPos = new Vector2(objectPos.x + randX, objectPos.y);
            if (await NormalDelay(delay, delayCancellation).SuppressCancellationThrow())
                return;
            SpawnAttack(id, lightningPos);
        }
    }
}
