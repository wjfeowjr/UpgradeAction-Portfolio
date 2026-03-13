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
    [SerializeField] private Transform changeAttackPos;
    
    [SerializeField] private Transform upperSlashPos;
    
    private int maxPunch = 3;

    public override async void ChangeAttack()
    {
        //Debug.Log("교체 공격 시작");
        CancelMotion();
        
        curGlobalCoolTime = 0;
        stateCancellation = new CancellationTokenSource();
        var finishSuccess = await BerserkerChangeAttack();
        
        // 성공여부와 상관없이 바디타입 원상복구
        ResetBodyType();
        if (!finishSuccess)
        {
            Debug.Log($"교체 공격 캔슬");
            return;
        }
        
        //Debug.Log($"교체공격 끝");
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

    private async UniTask<bool> FighterJumpAttack()
    {
        ResetTriggerAnimator(ConstValues.JumpDown);
        
        float jumpAttackDelay1 = 0.16f;
        float jumpAttackDelay2 = 0.25f;
        
        MotionFlip();
        StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack, ConstValues.JumpAttack);
        if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow()) 
            return false;

        SpawnAttackObject(ConstValues.BerserkerJumpAttack, jumpAttackPos);

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

        SkillSpeedAndArmorCheck(skillId);
        if (skillId == ConstValues.FighterLightningKick)
        {
            finishSuccess = await LightningKick();
        }
        else if (skillId == ConstValues.FighterLightningGrab)
        {
            finishSuccess = await LightningGrab();
        }
        else if (skillId == ConstValues.FighterLightningPunch)
        {
            finishSuccess = await LightningPunch();
        }
        else if (skillId == ConstValues.FighterStrongPunch)
        {
            finishSuccess = await StrongPunch();
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
    private async UniTask<bool> LightningKick()
    {

        return true;
    }
    
    // 불덩이 날리기
    private async UniTask<bool> LightningGrab()
    {

        return true;
    }
    
    // 반격
    private async UniTask<bool> LightningPunch()
    {
        
        return true;
    }
    
    // 박살내기
    private async UniTask<bool> StrongPunch()
    {

        return true;
    }
}
