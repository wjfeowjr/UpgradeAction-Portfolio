using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Monster_BigCharge : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform upperCutPos;
    [SerializeField] private Transform readyEffectPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                ChargePunch();
                break;
            case 1:
                GroundCrash();
                break;
            case 2:
                UpperCut();
                break;
        }
    }
    
    // 돌진
    private async void ChargePunch()
    {
        float delay1 = 0.9f;
        float delay2 = 0.3f;
        float chargeSpeed = 20;
        float chargeLength = 10.0f;

        if(transform.localScale.x > 0)
            chargeVector = new Vector2(transform.position.x + chargeLength, transform.position.y);
        else
            chargeVector = new Vector2(transform.position.x - chargeLength, transform.position.y);
        
        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);

        // 돌진
        SetTriggerAnimator(ConstValues.Pattern);
        var spawnObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}1", attackPos).GetComponent<Attack>();
        if (await Charge(chargeSpeed, 0.5f, chargeLength, 0.5f) == false)
            return;
        
        spawnObject.DisActiveCollider();
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        spawnObject.gameObject.SetActive(false);
        PatternEnd();
    }

    // 대지분쇄
    private async void GroundCrash()
    {
        float delay1 = 0.7f;
        float delay2 = 0.1f;
        float delay3 = 0.4f;

        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);
        
        // 대지분쇄
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}3", transform);
        FireballSpawn(new Vector2(transform.position.x, transform.position.y + 1.0f), 4);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
    
    // 어퍼컷
    private async void UpperCut()
    {
        float delay1 = 0.6f;
        float delay2 = 0.1f;
        float delay3 = 0.5f;

        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        // 어퍼컷
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        readyObject.gameObject.SetActive(false);
        
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}4", upperCutPos);
        FireballSpawn(upperCutPos.transform.position, 4);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        // 종료
        PatternEnd();;
    }
    
    private async void FireballSpawn(Vector2 startVector, int fireCount)
    {
        for (int i = 0; i < fireCount; i++)
        {
            SpawnObject($"{basicStat.id}_{ConstValues.Attack}2_Object", startVector);
            await UniTask.Yield();
        }
    }
    
    // 등장(연출 포함)
    public override async void Appear(Action<string> bossProduct)
    {
        if (GameManager.Instance.EpisodeName == ConstValues.Episode2)
        {
            stateCancellation = new CancellationTokenSource();
            await UniTask.WaitUntil(() => TableManager.Instance.monsterTable.Monster.Count > 0);
            await UniTask.WaitUntil(() => basicStat.id != default);

            StandHitBox();
            StateSetting(ENormalState.Appear, ConstValues.Appear, ConstValues.Appear);
            MoveStateSetting(EMoveState.Stopping);
            LandingStateSetting(ELandingState.Air);
            myBoxCollider.enabled = false;
            GravityChange(myGravity);

            foreach (var mySpriteRenderer in mySpriteRenderers)
                mySpriteRenderer.enabled = false;

            var meteor = SpawnObject($"{basicStat.id}_{ConstValues.Meteor}", centerPos);
            float dropForce = 20.0f;
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -dropForce);
            await UniTask.WaitUntil(() => landingState == ELandingState.Ground);

            foreach (var mySpriteRenderer in mySpriteRenderers)
                mySpriteRenderer.enabled = true;
            
            meteor.SetActive(false);
            
            LookAt(GameManager.Instance.CurPlayer.transform.position.x);

            SpawnObject($"{basicStat.id}_{ConstValues.Appear}", transform);
            StateSetting(ENormalState.AppearEnd, ConstValues.AppearEnd, ConstValues.AppearEnd);
            if (await NormalDelay(1.0f, stateCancellation).SuppressCancellationThrow())
                return;
                
            CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
            await UniTask.WaitUntil(() => GameManager.Instance.ControlStart);
            FirstCoolTimeReduce();
            IdleOrMove();
            myBoxCollider.enabled = true;
            bossProduct?.Invoke(basicStat.name);
        }
        else
        {
            base.Appear(bossProduct);
        }
    }
    
    public override async void Die()
    {
        base.Die();
        
        if (GameManager.Instance.EpisodeName == ConstValues.Episode2)
        {
            CancelMotion();
            ClearObjectList(buffObject);
            isDie = true;
            //removeAction?.Invoke();
            //GameManager.Instance.RemoveMonster(this);
            
            var delay = 0.12f;
            StateSetting(ENormalState.Die, ConstValues.Die, ConstValues.Die);
            MoveStateSetting(EMoveState.Stopping);
            
            dieCancellation = new CancellationTokenSource();
            while (true)
            {
                SpawnHitEffect(myStat.dyingMiniEffect, 1.0f, 1.5f);
                GameManager.Instance.CameraShake(0.1f, 0.1f, 0.1f);
                if (await NormalDelay(delay, dieCancellation).SuppressCancellationThrow())
                    return;
            }
        }
    }

    public void DieAirborne(Vector2 endPos)
    {
        dieCancellation?.Cancel();
        SpawnObject($"{basicStat.id}_{ConstValues.Die}", diePos);
        //removeAction?.Invoke();
        //GameManager.Instance.RemoveMonster(this);
        Vector2 start = transform.position;
        Vector2 end = endPos;
        float travelTime = 0.6f;
        Vector2 velocity = CalculateLaunchVelocity(start, end, travelTime);
        Airborne(velocity.x, velocity.y);
        //myRigidbody.linearVelocity = velocity;
        goldAction?.Invoke(myStat.gold, centerPos.position);
    }
}
