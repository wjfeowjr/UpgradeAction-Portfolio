using UnityEngine;

public class Monster_IceWizard : Monster
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
        float delay2 = 0.9f;
        float delay3 = 0.5f;
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        while (patternInfo[0].playerInAttackRange)
        {
            LookAt(GameManager.Instance.CurPlayer.CenterPos.position.x);
            var playerPos = GameManager.Instance.CurPlayer.CenterPos.position;
            
            SpawnObject(ConstValues.MonsterMoonAttack3DelayObject, playerPos);
            if(await AttackDelay(delay2, true).SuppressCancellationThrow())
                return;
            
            SpawnAttack(ConstValues.MonsterIceWizardAttack, playerPos);
        }
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
}
