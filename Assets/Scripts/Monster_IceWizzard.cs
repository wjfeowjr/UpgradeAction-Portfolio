using UnityEngine;

public class Monster_IceWizzard : Monster
{
    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                TraceFrost();
                break;
        }
    }
    
    // 추적 냉기 폭파
    private async void TraceFrost()
    {
        float delay1 = 0.2f;
        float delay2 = 0.8f;
        float delay3 = 0.4f;
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        while (patternInfo[0].playerInAttackRange)
        {
            var playerPos = GameManager.Instance.CurPlayer.CenterPos.position;
            
            SpawnObject(ConstValues.MonsterMoonAttack3DelayObject, playerPos);
            if(await AttackDelay(delay2).SuppressCancellationThrow())
                return;
            
            SpawnAttack(ConstValues.MonsterIceWizzardAttack, playerPos);
        }
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
}
