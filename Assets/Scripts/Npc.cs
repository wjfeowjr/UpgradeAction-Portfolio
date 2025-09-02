using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Npc : Character
{
    [SerializeField] private Transform speechPos;

    public Transform SpeechPos => speechPos;

    protected override void Update()
    {
        if (isDie || basicStat.hp <= 0 || !GameManager.Instance.ControlStart)
            return;

        base.Update();
    }
    
    protected override void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
    {
        normalState = changeNormalState;
        SetTriggerAnimator(triggerName);
    }
    
    protected override void StateCheck()
    {
        
    }
    protected override void StateRecovery()
    {
        
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
    
    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        // 착지
        if ((col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform)) && landingState == ELandingState.Air)
        {
            if (myRigidbody.gravityScale == 0 || myRigidbody.linearVelocityY is >= 0.05f or <= -0.05f)
                return;
            
            LandingStateSetting(ELandingState.Ground);
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;
            groundObject = col.gameObject;
            
            // 점프도중, 또는 에어본 도중 지면에 닿았을 경우의 애니메이션 처리
            switch (normalState)
            {
                case ENormalState.Airborne:
                    DownAndStand();
                    break;
            }
        }
    }
    
    protected void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform))
        {
            LandingStateSetting(ELandingState.Air);
        }
    }
}
