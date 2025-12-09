using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Monster_Sun : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Spin faceSpin;
    [SerializeField] private Reduction faceReduction;
    
    private Vector2 dir = Vector2.left;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                RisingFire();
                break;
            case 1:
                FireBall();
                break;
        }
    }

    protected override void Move()
    {
        // 움직이기
        if (moveState != EMoveState.Moving)
            return;

        var rayVector = CenterPos.transform.position;
        var distance = myBoxCollider.size.x * 0.5f + 1f;
        
        // 왼쪽
        if (dir == Vector2.left)
        {
            RaycastHit2D leftRay = Physics2D.Raycast(rayVector, Vector2.left, distance, groundLayerMask);
            Debug.DrawRay(rayVector, Vector2.left * distance, ConstValues.CyanColor, 0.02f);
            if (leftRay.collider != null)
                dir = Vector2.right;
        }
        // 오른쪽
        if (dir == Vector2.right)
        {
            
            RaycastHit2D rightRay = Physics2D.Raycast(rayVector, Vector2.right, distance, groundLayerMask);
            Debug.DrawRay(rayVector, Vector2.right * distance, ConstValues.CyanColor, 0.02f);
            if (rightRay.collider != null)
                dir = Vector2.left;
        }

        myRigidbody.linearVelocity = dir * basicStat.moveSpeed;
    }
    
    // 불기둥
    private async void RisingFire()
    {
        float delay1 = 0.8f; // 0.2f
        float delay2 = 0.5f;
        float fadeSpeed = 0.4f;
        
        var firePos = new Vector2(GameManager.Instance.CurPlayer.transform.position.x, RoomManager.Instance.GroundPosY);
        var targetCollider = GameManager.Instance.ObjectCollider(ConstValues.MonsterSunAttack1);
        SpawnObject(ConstValues.FireFlash, CenterPos);
        
        faceSpin.SpinSwitchOn(true);

        // 예전꺼
        // await WarningAreaSpawnCollider(firePos, Vector3.zero, targetCollider, fadeSpeed, ConstValues.RedColor);
        // if(await AttackDelay(delay1).SuppressCancellationThrow())
        //     return;
        
        // 지금꺼
        SpawnObject($"{basicStat.id}_{ConstValues.Warning}", firePos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SpawnAttack(ConstValues.MonsterSunAttack1, firePos);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        faceSpin.StopAndReset();
        PatternEnd();
    }
    
    // 파이어볼
    private async void FireBall()
    {
        float delay1 = 1.0f;
        float delay2 = 0.25f;
        float delay3 = 0.1f;
        float delay4 = 1.0f;
        
        var spinObject = SpawnObject(ConstValues.MonsterSunAttack2SpinObject, CenterPos).GetComponent<Spin>();
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        faceReduction.PlayReduction();
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        int count = 3;
        for (int i = 0; i < count; i++)
        {
            faceReduction.StopAndReset();
            spinObject.DeleteSpinObject(i);
            
            int missileDir = 1;
            if (GameManager.Instance.CurPlayer.CenterPos.position.x < transform.position.x)
                missileDir = -1;
            var attackObject = SpawnAttackObject(ConstValues.MonsterSunAttack2, attackPos, 0, missileDir).GetComponent<Missile>();
            attackObject.LookAtTarget(GameManager.Instance.CurPlayer.CenterPos.position);

            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
            
            if(i < count - 1)
                faceReduction.PlayReduction();
            
            if(await AttackDelay(delay3).SuppressCancellationThrow())
                return;
        }
        
        faceReduction.StopAndReset();
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;
        
        spinObject.gameObject.SetActive(false);
        PatternEnd();
    }
    
    // 등장
    public override async void Appear(Action<string> bossProduct)
    {
        PlaySound($"{ConstValues.Laugh}2");
        faceSpin.enabled = true;
        faceSpin.StopAndReset();
        faceReduction.enabled = true;

        StandHitBox();
        StateSetting(ENormalState.Appear, ConstValues.Appear, ConstValues.Appear);
        MoveStateSetting(EMoveState.Stopping);
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        
        immortal = true;
        MoveStateSetting(EMoveState.Stopping);
        GravityChange(myGravity);
        PlaySound(ConstValues.RewardPage);
        var movePos = new Vector2(transform.position.x, transform.position.y - 3.5f);
        
        stateCancellation = new CancellationTokenSource();
        await EpisodeMove_Y(movePos, basicStat.moveSpeed, -1);
        ZeroVelocity();
        await UniTask.WaitUntil(() => GameManager.Instance.ControlStart && Time.timeScale > 0);
        
        IdleOrMove();
        FirstCoolTimeReduce();
        immortal = false;
        bossProduct?.Invoke(basicStat.name);
        startPos = transform.position;
    }
    
    public override async void Die()
    {
        base.Die();
        
        immortal = true;
        faceSpin.StopAndReset();
        faceReduction.StopAndReset();
        
        CancelMotion();
        MoveStateSetting(EMoveState.Stopping);
        isDie = true;
    }

    public async void SunDie()
    {
        PlaySound($"{ConstValues.Scream}10");
        CancelMotion();
        var delay = 0.12f;
        dieCancellation = new CancellationTokenSource();
        for (int i = 0; i < 15; i++)
        {
            BombEffect();
            if (await NormalDelay(delay, dieCancellation).SuppressCancellationThrow())
                return;
        }
        DieExplosion();
    }
    
    public async UniTask DieBomb(int bombCount, float slashInterval)
    {
        dieCancellation = new CancellationTokenSource();
        for (int i = 0; i < bombCount; i++)
        {
            BombEffect();
            if (await NormalDelay(slashInterval, dieCancellation).SuppressCancellationThrow())
                return;
        }
    }

    public override void DieExplosion()
    {
        base.DieExplosion();
        faceSpin.enabled = false;
        faceReduction.enabled = false;
        goldAction?.Invoke(myStat.gold, centerPos.position);
    }

    private void BombEffect()
    {
        HitMaterial();
        SpawnHitEffect(myStat.dyingMiniEffect, 1.0f, 1.5f);
        GameManager.Instance.CameraShake(0.1f, 0.1f, 0.1f);
    }

    public override void Airborne(float xVelocity, float yVelocity)
    {
        base.Airborne(xVelocity, yVelocity);
        PlaySound($"{ConstValues.Scream}7");
    }

    protected override void Bound(float xVelocity, float yVelocity)
    {
        base.Bound(xVelocity, yVelocity);
        faceSpin.SpinSwitchOn(true);
    }

    protected override async void DownAndStand()
    {
        base.DownAndStand();
        faceSpin.Stop();
    }
    
    protected override void HoveringLeap()
    {
        base.HoveringLeap();
        arriveHeight = startPos.y;
        faceSpin.StopAndReset();
    }
    
    public override void CancelMotion(bool cancelJump = true, bool velocity0 = true, bool zeroLandingAttack = true)
    {
        base.CancelMotion(cancelJump, velocity0, zeroLandingAttack);
        faceSpin.StopAndReset();
    }
}
