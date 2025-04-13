using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Player_Berserker : Player
{
    [SerializeField] private Transform centerPos;
    [SerializeField] private Transform attack1Pos;
    [SerializeField] private Transform attack2Pos;
    [SerializeField] private Transform attack3Pos;
    [SerializeField] private Transform jumpAttack1Pos;
    [SerializeField] private Transform jumpAttack2Pos;
    [SerializeField] private Transform upperSlashPos;
    [SerializeField] private Transform crashPos;
    [SerializeField] private Transform crashExplosionPos;
    [SerializeField] private Transform fireStrikePos;
    
    public override async void Attack()
    {
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }
        
        if (normalState is ENormalState.Attack or ENormalState.JumpAttack or ENormalState.Skill || IsDamaged())
            return;

        Debug.Log("공격 시작");
        curGlobalCoolTime = 0;
        CancelMotion();
        stateCancellation = new CancellationTokenSource();
        bool finishSuccess = true;
        string type = "지상";
        switch (landingState)
        {
            // 지상공격
            case ELandingState.Ground:
                finishSuccess = await BerserkerLandingAttack();
                break;

            // 점프공격
            case ELandingState.Air:
                type = "점프";
                finishSuccess = await BerserkerJumpAttack();
                break;
        }

        if (!finishSuccess)
        {
            Debug.Log($"{type}공격 캔슬");
            return;
        }
            
        Debug.Log($"{type}공격 끝");
        // 동작이 끝날때 반환하는 트리거
        StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
    }

    private async UniTask<bool> BerserkerLandingAttack()
    {
        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);

        var delay1 = 0.16f;
        var delay2 = 0.2f;
        var delay3 = 0.12f;
        var delay4 = 0.2f;
        var delay5 = 0.16f;
        var delay6 = 0.5f;
        var afterDelay = 0.35f;

        StateSetting(ENormalState.Attack, ConstValues.Attack, ConstValues.Attack1);
        AttackAdvance(2.0f);
        nextAttack = false;

        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        SpawnAttack(ConstValues.BerserkerAttack1, attack1Pos);

        if (await NextAttackDelay(delay2, afterDelay).SuppressCancellationThrow())
            return false;

        if (nextAttack)
        {
            nextAttack = false;
            MotionFlip();
            StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack2);
            AttackAdvance(2.0f);

            if (await AttackDelay(delay3).SuppressCancellationThrow())
                return false;

            SpawnAttack(ConstValues.BerserkerAttack2, attack2Pos);
            if (await NextAttackDelay(delay4, afterDelay).SuppressCancellationThrow())
                return false;

            if (nextAttack)
            {
                MotionFlip();
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack3);
                AttackAdvance(3.0f);

                if (await AttackDelay(delay5).SuppressCancellationThrow())
                    return false;

                SpawnAttack(ConstValues.BerserkerAttack3, attack3Pos);

                if (await AttackDelay(delay6).SuppressCancellationThrow())
                    return false;
            }
        }
        
        return true;
    }

    private async UniTask<bool> BerserkerJumpAttack()
    {
        if (jumpAttackCount <= 0)
        {
            jumpAttackCount += 1;
            float jumpAttackDelay1 = 0.16f;
            float jumpAttackDelay2 = 0.2f;
            float jumpAttackForce = 6;
            StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
            if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow())
                return false;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpAttackForce);
            SpawnAttack(ConstValues.BerserkerJumpAttack1, jumpAttack1Pos);
            if (await AttackDelay(jumpAttackDelay2).SuppressCancellationThrow())
                return false;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpAttackForce);
            SpawnAttack(ConstValues.BerserkerJumpAttack1, jumpAttack1Pos);
            if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow())
                return false;
        }
        else
        {
            jumpAttackCount += 1;
            float jumpAttackDelay3 = 0.12f;
            float jumpAttackDelay4 = 0.6f;

            MotionFlip();
            GravityChange(ConstValues.BasicGravity);
            StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack2, ConstValues.JumpAttack2Start);
            if (await AttackDelay(jumpAttackDelay3).SuppressCancellationThrow())
                return false;

            StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2Drop);
            SpawnAttack(ConstValues.BerserkerJumpAttack2, jumpAttack2Pos);
            float dropForce = 30.0f;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
            while (myRigidbody.linearVelocity.y < 0)
            {
                if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
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

        return true;
    }

    public override async void Skill(KeyCode skillKey)
    {
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }
        
        if (!IsCanSkill(skillKey))
            return;
        
        Debug.Log("스킬 시작");
        curGlobalCoolTime = 0;
        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);

        CancelMotion();
        MotionFlip();
        
        stateCancellation = new CancellationTokenSource();
        bool finishSuccess = true;
        if (skillKey == GameManager.Instance.dashKey)
        {
            finishSuccess = await Dash();
        }
        else if (skillKey == GameManager.Instance.skillKey2)
        {
            finishSuccess = await UpperSlash();
        }
        else if (skillKey == GameManager.Instance.skillKey3)
        {
            finishSuccess = await Crash();
        }
        else if (skillKey == GameManager.Instance.skillKey4)
        {
            finishSuccess = await FireStrike();
        }

        if (!finishSuccess)
        {
            Debug.Log($"{skillKey} 스킬 캔슬");
            return;
        }
        
        Debug.Log($"{skillKey} 스킬 끝");
        GravityChange(ConstValues.BasicGravity);
        // 동작이 끝날때 반환하는 트리거
        StateSetting(ENormalState.Normal, ConstValues.Normal, ConstValues.Normal);
    }

    private async UniTask<bool> UpperSlash()
    {
        StateSetting(ENormalState.Skill, ConstValues.BerserkerUpperSlash, ConstValues.BerserkerUpperSlash);

        var delay1 = 0.16f;
        var delay2 = 0.32f;

        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;

        SpawnAttack(ConstValues.BerserkerUpperSlash, upperSlashPos);
        Leap(6, 20, 1.6f);

        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
    
    private async UniTask<bool> Crash()
    {
        float delay1 = 0.5f;
        float delay2 = 0.05f;
        float delay3 = 0.32f;

        SpawnAttack(ConstValues.BerserkerFlash, centerPos);
        StateSetting(ENormalState.Skill, ConstValues.BerserkerCrash, ConstValues.BerserkerCrash);
        
        // 도움닫기
        myRigidbody.linearVelocity = new Vector2(0, 12.0f);
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
        
        float dropForce = 20.0f;
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
        while (myRigidbody.linearVelocity.y < 0)
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

    private async UniTask<bool> FireStrike()
    {
        StateSetting(ENormalState.Skill, ConstValues.BerserkerFireStrike, ConstValues.BerserkerFireStrike);

        var delay1 = 0.1f;
        var delay2 = 0.2f;
            
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
            
        //PlaySound("Berserker_Attack1");
            
        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return false;
            
        SpawnAttack(ConstValues.BerserkerFireStrike, fireStrikePos);
        //SpawnObject($"{skillId}_Effect", attackPos[4]);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return false;

        return true;
    }
}