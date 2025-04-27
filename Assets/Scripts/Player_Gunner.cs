using System.Threading;
using UnityEngine;

public class Player_Gunner : Player
{
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
                //finishSuccess = await BerserkerLandingAttack();
                break;

            // 점프공격
            case ELandingState.Air:
                type = "점프";
                //finishSuccess = await BerserkerJumpAttack();
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
    
    public override async void Skill(KeyCode skillKey)
    {
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }
        
        var skillId = GameManager.Instance.GetBerserkerSkillKeyList().Find(x => x.keyCode == skillKey).skillId;
        if (!IsCanSkill(skillId))
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
        
        if (skillId == ConstValues.BerserkerUpperSlash)
        {
            //finishSuccess = await UpperSlash();
        }
        else if (skillId == ConstValues.BerserkerCrash)
        {
            //finishSuccess = await Crash();
        }
        else if (skillId == ConstValues.BerserkerFireStrike)
        {
            //finishSuccess = await FireStrike();
        }
        else if (skillId == ConstValues.BerserkerChargeCrash)
        {
            //finishSuccess = await ChargeCrash();
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
}
