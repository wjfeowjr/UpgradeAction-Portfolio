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

    private void Scream()
    {
        SoundManager.Instance.PlaySound(ConstValues.GunnerLaugh);
    }
    
    public override async void ChangeAttack()
    {
        Debug.Log("교체 공격 시작");
        CancelMotion();
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
    
    public override async void Attack()
    {
        if (normalState is ENormalState.Attack or ENormalState.JumpAttack or ENormalState.Skill || IsDamaged() || downJumping)
            return;
        
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }

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
                finishSuccess = await GunnerJumpAttack();
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
        float delay2 = 0.016f;
        float delay3 = 0.1f;
        float delay4 = 0.3f;

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

            SpawnObject(ConstValues.GunnerAttackEffect1, landingEffectPos);
            SpawnObject(ConstValues.GunnerAttack1Object, landingAttackPos);
            bullet--;
            
            if (bullet == 1)
                break;
        
            if (await NextAttackDelay(delay2, afterDelay).SuppressCancellationThrow())
                return false;
            
            if (!nextAttack)
                break;
        }

        // 막타
        if (bullet == 1)
        {
            StateSetting(ENormalState.Attack, ConstValues.FinalAttack, ConstValues.Attack3Ready);
            if (await AttackDelay(delay4).SuppressCancellationThrow())
                return false;
            
            StateSetting(ENormalState.Attack, ConstValues.ComboAttack, ConstValues.Attack3);
            if (await AttackDelay(delay3).SuppressCancellationThrow())
                return false;
        
            SpawnObject(ConstValues.GunnerAttackEffect2, landingEffectPos);
            SpawnObject(ConstValues.GunnerAttack2Object, landingAttackPos);
            bullet--;
            Rebound(4.0f);
            
            if (await AttackDelay(delay4).SuppressCancellationThrow())
                return false;
        }

        return true;
    }
    
    private async UniTask<bool> GunnerJumpAttack()
    {
        ResetTriggerAnimator(ConstValues.JumpDown);

        float delay1 = 0.066f;
        float delay2 = 0.016f;
        float delay3 = 0.1f;
        float delay4 = 0.3f;

        float afterDelay = 0.2f;

        int bulletCount = 0;
        
        // 그냥 계속 쏜다 ㅋㅋ
        while (GetJumpState())
        { 
            if(bulletCount == 0)
                StateSetting(ENormalState.JumpAttack, ConstValues.JumpAttack1, ConstValues.JumpAttack1);
            else if(bulletCount % 2 == 0)
                StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack2);
            else if(bulletCount % 2 == 1)
                StateSetting(ENormalState.JumpAttack, ConstValues.ComboAttack, ConstValues.JumpAttack1);
            
            nextAttack = false;

            if (await AttackDelay(delay1).SuppressCancellationThrow())
                return false;

            myRigidbody.linearVelocity = new Vector2(0, 0.1f);
            SpawnObject(ConstValues.GunnerAttackEffect1, jumpEffectPos, -45);
            SpawnObject(ConstValues.GunnerAttack1Object, jumpAttackPos, -45);
            bulletCount++;
            
            if (await NextAttackDelay(delay2, afterDelay).SuppressCancellationThrow())
                return false;
            
            if (!nextAttack)
                break;
        }

        return true;
    }
    
    public override async void Skill(KeyCode skillKey)
    {
        if (Time.timeScale == 0)
            return;

        var skillId = GameManager.Instance.PlayerSkillKeyCollection.gunnerSkillKeyList.Find(x => x.keyCode == skillKey).skillId;
        if (!IsCanSkill(skillId))
            return;
        
        if(!GetGlobalCoolTime())
        {
            Debug.Log("글로벌 쿨타임이 지나지 않음");
            return;
        }
        
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
        var delay1 = 0.12f;
        var delay2 = 0.08f;
        
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
