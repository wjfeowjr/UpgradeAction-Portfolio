using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Npc : Character
{
    protected override void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
    {
        normalState = changeNormalState;
        SetTriggerAnimator(triggerName);
    }
    
    protected override void StateRecovery()
    {
        StandHitBox();
    }
    
    // 커스텀
    public async UniTask EpisodeMove_X(Vector2 movePos, float speed, int finishDir)
    {
        Stop();
        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        
        Vector2 dir = Vector2.left;
        transform.localScale = reverseScale;
        if (transform.position.x < movePos.x)
        {
            dir = Vector2.right;
            transform.localScale = defaultScale;
        }

        stateCancellation = new CancellationTokenSource();
        while (Math.Abs(transform.position.x - movePos.x) > 0.1f)
        {
            // basicStat.moveSpeed
            if(normalState == ENormalState.Idle)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            
            CustomMoving_X(dir, speed);
            await FixedYieldDelay(stateCancellation);
        }

        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        switch (finishDir)
        {
            case -1:
                transform.localScale = reverseScale;
                break;
            case 1:
                transform.localScale = defaultScale;
                break;
        }
        Stop();
        StopVelocity();
    }
    public async UniTask EpisodeMove_Y(Vector2 movePos, float speed)
    {
        Stop();
        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        
        Vector2 dir = Vector2.down;
        if (transform.position.y < movePos.y)
            dir = Vector2.up;

        stateCancellation = new CancellationTokenSource();
        while (Math.Abs(transform.position.y - movePos.y) > 0.1f)
        {
            // basicStat.moveSpeed
            if(normalState == ENormalState.Idle)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            
            CustomMoving_Y(dir, speed);
            await FixedYieldDelay(stateCancellation);
        }

        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        Stop();
        StopVelocity();
    }
}
