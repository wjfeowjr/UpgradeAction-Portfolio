using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Monster_Moon : Monster
{
    [SerializeField] private Transform attackPos;

    private float pointA;
    private float pointB;
    private Vector2 dir;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                DropFrost();
                break;
            case 1:
                IceBall();
                break;
            case 2:
                TraceFrost();
                break;
        }
    }

    private void PatrolRay()
    {
        var leftRay = Physics2D.Raycast(CenterPos.position, Vector2.left, 20f, wallLayerMask);
        Debug.DrawRay(CenterPos.position, Vector2.left * 20f, ConstValues.RedColor, 0.1f);
        pointA = leftRay.point.x + physicsCollider.size.x;
        
        var rightRay = Physics2D.Raycast(CenterPos.position, Vector2.right, 20f, wallLayerMask);
        Debug.DrawRay(CenterPos.position, Vector2.right * 20f, ConstValues.BlueColor, 0.1f);
        pointB = rightRay.point.x - physicsCollider.size.x;
        
        dir = Vector2.left;
    }
    
    protected override void Move()
    {
        // 움직이기
        if (moveState != EMoveState.Moving)
            return;
        
        float targetSpeedX = basicStat.moveSpeed * dir.x;
        float targetSpeedY = myRigidbody.linearVelocity.y;

        if (dir == Vector2.left)
        {
            if (Vector2.Distance(transform.position, new Vector2(pointA, transform.position.y)) < 0.1f)
            {
                dir = Vector2.right;
                StopVelocity();
            }
        }
        else if (dir == Vector2.right)
        {
            if (Vector2.Distance(transform.position, new Vector2(pointB, transform.position.y)) < 0.1f)
            {
                dir = Vector2.left;
                StopVelocity();
            }
        }
        
        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
    }
    
    // 얼음 낙하
    private async void DropFrost()
    {
        float delay1 = 0.3f;
        float fadeSpeed = 0.4f;
        
        var playerPos = GameManager.Instance.CurPlayer.transform.position;
        var firePos = new Vector2(playerPos.x, playerPos.y + 10.0f);
        var endPos = new Vector2(playerPos.x, GameManager.Instance.GroundPosY);
        Vector3 warningAngle = new Vector3(0, 0, 90);
        var targetCollider = GameManager.Instance.ObjectCollider(ConstValues.MonsterMoonAttack1Object);
        var moonEffect = SpawnObject(ConstValues.MonsterMoonEffect, CenterPos);
        
        await WarningAreaSpawnTrajectory(firePos, endPos, warningAngle, fadeSpeed, ConstValues.RedColor, targetCollider.size.x);
        SpawnAttack(ConstValues.MonsterMoonAttack1Object, firePos);
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        moonEffect.SetActive(false);
        PatternEnd();
    }
    
    // 아이스볼
    private async void IceBall()
    {
        float delay1 = 1.0f;
        float delay2 = 0.2f;
        float delay3 = 1.0f;

        var moonEffect = SpawnObject(ConstValues.MonsterMoonEffect, CenterPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        for (int i = 0; i < 7; i++)
        {
            var attackObject = SpawnAttackObject(ConstValues.MonsterMoonAttack2, attackPos).GetComponent<Missile>();
            attackObject.LookAtTarget(GameManager.Instance.CurPlayer.CenterPos.position);
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
        }
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        moonEffect.gameObject.SetActive(false);
        PatternEnd();
    }
    
    // 추적 냉기 폭파
    private async void TraceFrost()
    {
        float delay1 = 1.0f;
        float delay2 = 0.5f;

        var moonEffect = SpawnObject(ConstValues.MonsterMoonEffect, CenterPos);
        
        for (int i = 0; i < 3; i++)
        {
            var playerPos = GameManager.Instance.CurPlayer.CenterPos.position;
            
            SpawnObject(ConstValues.MonsterMoonAttack3DelayObject, playerPos);
            if(await AttackDelay(delay1).SuppressCancellationThrow())
                return;
            
            SpawnAttack(ConstValues.MonsterMoonAttack3, playerPos);
        }
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        moonEffect.gameObject.SetActive(false);
        PatternEnd();
    }
    
    // 등장
    public override async void Appear(Action bossProduct)
    {
        await UniTask.WaitUntil(() => TableManager.Instance.monsterTable.Monster.Count > 0);
        StandHitBox();
        StateSetting(ENormalState.Appear, ConstValues.Appear, ConstValues.Appear);
        MoveStateSetting(EMoveState.Stopping);
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        
        myBoxCollider.enabled = false;
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
        myBoxCollider.enabled = true;
        PatrolRay();
        bossProduct?.Invoke();
    }

    public override void Die()
    {
        myBoxCollider.enabled = false;

        CancelMotion();
        MoveStateSetting(EMoveState.Stopping);
        isDie = true;
        GameManager.Instance.RemoveMonster(this);
    }

    public async void DieBomb()
    {
        var delay = 0.12f;
        dieCancellation = new CancellationTokenSource();
        while (true)
        {
            SpawnHitEffect(myStat.dyingMiniEffect, 1.0f, 1.5f);
            //GameManager.Instance.CameraShake(0.1f, 0.1f);
            if (await NormalDelay(delay, dieCancellation).SuppressCancellationThrow())
                return;
        }
    }

    public override void DieExplosion()
    {
        dieCancellation?.Cancel();
        base.DieExplosion();
    }
}