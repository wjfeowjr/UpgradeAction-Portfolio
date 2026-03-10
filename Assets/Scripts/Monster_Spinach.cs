using System;
using UnityEngine;

public class Monster_Spinach : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                Punch();
                break;
        }
    }
    
    // 패턴1. 주먹질
    private async void Punch()
    {
        float delay1 = 0.45f;
        float delay2 = 0.5f;

        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_Attack", attackPos);
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        PatternEnd();
    }
}
