using System;
using UnityEngine;

public class Monster_Golem : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform readyEffectPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                GroundAttack();
                break;
        }
    }

    // 땅치기
    private async void GroundAttack()
    {
        float delay1 = 1.0f;
        float delay2 = 0.3f;
        float delay3 = 0.8f;

        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);
        
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        // 공격
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}", attackPos).GetComponent<Attack>();
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
}
