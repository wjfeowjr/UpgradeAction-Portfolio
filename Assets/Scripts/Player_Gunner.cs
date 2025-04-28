using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Player_Gunner : Player
{
    [SerializeField] private Transform attack1Pos;
    [SerializeField] private Transform attack2Pos;

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
                finishSuccess = await GunnerLandingAttack();
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
    
    private async UniTask<bool> GunnerLandingAttack()
    {
        if (moveState == EMoveState.Moving)
            MoveStateSetting(EMoveState.Stopping);

        float delay1 = 0.066f;
        float delay2 = 0.1f;
        float delay3 = 0.3f;

        float afterDelay = 0.2f;

        int maxBullet = 9;
        int bullet = maxBullet;
        
        // 총알이 1개 남을 때 까지 난사
        while (bullet > 1)
        {
            if(bullet == maxBullet)
                StateSetting(ENormalState.Attack, ConstValues.Attack, ConstValues.Attack1);
            else if(bullet % 2 == 0)
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack2);
            else if(bullet % 2 == 1)
                StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack1);
            
            nextAttack = false;

            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;

            SpawnObject(ConstValues.GunnerAttackEffect1, attack1Pos);
            SpawnObject(ConstValues.GunnerAttack1Object, attack1Pos);
            bullet--;
            
            if (bullet == 1)
                break;
        
            if (await NextAttackDelay(delay1, afterDelay).SuppressCancellationThrow())
                return false;
            
            if (!nextAttack)
                break;
        }

        // 막타
        if (bullet == 1)
        {
            if (await AttackDelay(delay3).SuppressCancellationThrow())
                return false;
            
            StateSetting(ENormalState.Attack, ConstValues.FinalAttack, ConstValues.Attack3);
            if (await AttackDelay(delay2).SuppressCancellationThrow())
                return false;
        
            SpawnObject(ConstValues.GunnerAttackEffect2, attack1Pos);
            SpawnObject(ConstValues.GunnerAttack2Object, attack1Pos);
            bullet--;
            
            if (await AttackDelay(delay3).SuppressCancellationThrow())
                return false;
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
