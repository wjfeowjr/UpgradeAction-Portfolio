using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Monster_Charge : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform readyEffectPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                ChargePunch();
                break;
        }
    }

    // 돌진
    private async void ChargePunch()
    {
        float delay1 = 0.9f;
        float delay2 = 0.5f;
        float chargeSpeed = 13;
        float chargeLength = 6.0f;
        
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
        var spawnObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}", attackPos).GetComponent<Attack>();
        if (await Charge(chargeSpeed, 0.5f, chargeLength, 0.5f) == false)
            return;
        
        spawnObject.DisActiveCollider();
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        spawnObject.gameObject.SetActive(false);
        PatternEnd();
    }

    public async UniTask EventCharge(float chargeLength, float upperX = 0, float upperY = 0)
    {
        float delay1 = 0.9f;
        float delay2 = 0.5f;
        float chargeSpeed = 13;

        if(transform.localScale.x > 0)
            chargeVector = new Vector2(transform.position.x + chargeLength, transform.position.y);
        else
            chargeVector = new Vector2(transform.position.x - chargeLength, transform.position.y);
        
        stateCancellation = new CancellationTokenSource();
        SetTriggerAnimator($"{ConstValues.Attack}_0");
        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);

        // 돌진
        SetTriggerAnimator(ConstValues.Pattern);
        var spawnObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}_{ConstValues.Event}", attackPos).GetComponent<Attack>();
        if (upperX > 0 && upperY > 0)
            spawnObject.SetUpperPower(new Vector2(upperX, upperY));
        
        await Charge(chargeSpeed, 0.5f, chargeLength, 0.5f);
        spawnObject.DisActiveCollider();
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        spawnObject.gameObject.SetActive(false);
    }
}
