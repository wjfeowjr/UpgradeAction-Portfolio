using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Monster_Sun : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void OnEnable()
    {
        base.OnEnable();
        moveState = EMoveState.Moving;
    }
    
    // 등장
    protected override async void AppearProduction()
    {
        myBoxCollider.enabled = false;
        
        if(!myStat.hovering) 
            GravityChange(ConstValues.BasicGravity);
        
        PlaySound(ConstValues.RewardPage);
        var movePos = new Vector2(transform.position.x, transform.position.y - 3.5f);
        
        stateCancellation = new CancellationTokenSource();
        while (Math.Abs(transform.position.y - movePos.y) > 0.1f)
        {
            EpisodeMoveVertical(movePos.y);
            await FixedYieldDelay(stateCancellation);
        }
        ZeroVelocity();
        
        await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
        MoveStateSetting(EMoveState.Moving);
        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        FirstCoolTimeReduce();
        myBoxCollider.enabled = true;
    }

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                ShotFire();
                break;
        }
    }
    
    protected override void Move()
    {
        // 움직이기
        if (moveState != EMoveState.Moving)
            return;
        
        
    }
    
    // 불꽃발사
    private async void ShotFire()
    {
        Debug.Log("불꽃발사!");
    }
}
