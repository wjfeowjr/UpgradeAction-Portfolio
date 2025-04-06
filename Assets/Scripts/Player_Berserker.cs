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
    [SerializeField] private Transform upperSlashPos;
    [SerializeField] private Transform fireStrikePos;
    
    public override async void Attack()
    {
        if (state is EState.Attack or EState.JumpAttack or EState.Skill)
            return;

        stateCancellation = new CancellationTokenSource();
        
        switch (jumpState)
        {
            // 지상공격
            case EJumpState.Landing:
                if (moveState == EMoveState.Moving)
                    MoveStateSetting(EMoveState.Stopping);
                
                var delay1 = 0.16f;
                var delay2 = 0.2f;
                var delay3 = 0.12f;
                var delay4 = 0.2f;
                var delay5 = 0.16f;
                var delay6 = 0.5f;
                var afterDelay = 0.35f;

                StateSetting(EState.Attack, ConstValues.Attack, ConstValues.Attack1);
                AttackAdvance(2.0f);
                nextAttack = false;

                if (await AttackDelay(delay1).SuppressCancellationThrow())
                    return;

                SpawnAttack(ConstValues.BerserkerAttack1, attack1Pos);

                if (await NextAttackDelay(delay2, afterDelay).SuppressCancellationThrow())
                    return;

                if (nextAttack)
                {
                    nextAttack = false;
                    MotionFlip();
                    StateSetting(EState.Attack, ConstValues.ComboAttack, ConstValues.Attack2);
                    AttackAdvance(2.0f);

                    if (await AttackDelay(delay3).SuppressCancellationThrow())
                        return;

                    SpawnAttack(ConstValues.BerserkerAttack2, attack2Pos);
                    if (await NextAttackDelay(delay4, afterDelay).SuppressCancellationThrow())
                        return;

                    if (nextAttack)
                    {
                        MotionFlip();
                        StateSetting(EState.Attack, ConstValues.ComboAttack, ConstValues.Attack3);
                        AttackAdvance(3.0f);

                        if (await AttackDelay(delay5).SuppressCancellationThrow())
                            return;

                        SpawnAttack(ConstValues.BerserkerAttack3, attack3Pos);

                        if (await AttackDelay(delay6).SuppressCancellationThrow())
                            return;
                    }
                }

                Debug.Log("공격 끝");
                //StateSetting(EState.Idle, ConstValues.Idle, ConstValues.Idle);
                break;

            // 점프공격
            case EJumpState.Jumping:

                switch (jumpAttackCount)
                {
                    case 0:
                        jumpAttackCount += 1;
                        float jumpAttackDelay1 = 0.16f;
                        float jumpAttackDelay2 = 0.2f;
                        float jumpAttackForce = 6;
                        StateSetting(EState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
                        if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow())
                            return;
                        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpAttackForce);
                        SpawnAttack(ConstValues.BerserkerJumpAttack1, jumpAttack1Pos);
                        if (await AttackDelay(jumpAttackDelay2).SuppressCancellationThrow())
                            return;
                        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpAttackForce);
                        SpawnAttack(ConstValues.BerserkerJumpAttack1, jumpAttack1Pos);
                        if (await AttackDelay(jumpAttackDelay1).SuppressCancellationThrow())
                            return;
                        break;
                    
                    case 1:
                        jumpAttackCount += 1;
                        float jumpAttackDelay3 = 0.12f;
                        float jumpAttackDelay4 = 0.6f;
                        
                        MotionFlip();
                        GravityChange(ConstValues.BasicGravity);
                        StateSetting(EState.JumpAttack, ConstValues.JumpAttack2, ConstValues.JumpAttack2Start);
                        if (await AttackDelay(jumpAttackDelay3).SuppressCancellationThrow())
                            return;
                        
                        StateSetting(EState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2Drop);
                        SpawnAttack(ConstValues.BerserkerJumpAttack2, jumpAttack2Pos);
                        float dropForce = 30.0f;
                        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
                        while (myRigidbody.linearVelocity.y < 0)
                        {
                            if (await YieldDelay(stateCancellation).SuppressCancellationThrow())
                                return;
                        }
        
                        StateSetting(EState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2End);
                        SpawnAttack(ConstValues.BerserkerJumpAttack2Effect, jumpAttack2Pos);
                        //SpawnSwordWave(attackPos[2]);
                        //GameManager.Instance.playerShare.currentJumpAttack = 0;
                        if (await AttackDelay(jumpAttackDelay4).SuppressCancellationThrow())
                            return;
                        ControlObjectClear();
                        break;
                }
                // StateSetting(EState.Jump, ConstValues.Jump, ConstValues.Jump);
                break;
        }
        
        // 동작이 끝날때 반환하는 트리거
        StateSetting(ParseState(FinishTrigger()), FinishTrigger(), FinishTrigger());
    }

    public override async void Skill(KeyCode skillKey)
    {
        if (!IsCanSkill(skillKey))
            return;

        CancelMotion();

        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);

        CancelMotion(EState.Attack);
        Debug.Log("스킬 시작");

        stateCancellation = new CancellationTokenSource();
        if (skillKey == GameManager.Instance.skillKey2)
        {
            StateSetting(EState.Skill, ConstValues.BerserkerUpperSlash, ConstValues.BerserkerUpperSlash);

            var delay1 = 0.16f;
            var delay2 = 0.32f;

            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return;

            SpawnAttack(ConstValues.BerserkerUpperSlash, upperSlashPos);
            Leap(6, 20, 1.6f);

            if (await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        }
        else if (skillKey == GameManager.Instance.skillKey4)
        {
            StateSetting(EState.Skill, ConstValues.BerserkerFireStrike, ConstValues.BerserkerFireStrike);

            var delay1 = 0.1f;
            var delay2 = 0.2f;
            
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return;
            
            //PlaySound("Berserker_Attack1");
            
            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return;
            
            SpawnAttack(ConstValues.BerserkerFireStrike, fireStrikePos);
            //SpawnObject($"{skillId}_Effect", attackPos[4]);
            if (await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        }

        Debug.Log("스킬 끝");
        // 동작이 끝날때 반환하는 트리거
        StateSetting(ParseState(FinishTrigger()), FinishTrigger(), FinishTrigger());
    }
}