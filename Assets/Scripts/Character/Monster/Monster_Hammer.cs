using UnityEngine;

public class Monster_Hammer : Monster
{
    [SerializeField] private Transform flashPos;
    [SerializeField] private Transform firePos;
    [SerializeField] private Transform attackPos;
    
    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                Smash();
                break;
        }
    }
    
    // 패턴1.내려치기
    private async void Smash()
    {
        float delay1 = 0.6f;
        float delay2 = 0.15f;
        float delay3 = 0.4f;
        float delay4 = 0.4f;
        
        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{ConstValues.FighterFlash}", flashPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        readyObject.SetActive(false);
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        SpawnAttack($"{basicStat.id}_Attack2", attackPos);
        
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;

        PatternEnd();;
    }
}
