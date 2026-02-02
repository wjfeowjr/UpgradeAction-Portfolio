using UnityEngine;

public class Monster_FireWizard : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                Fireball();
                break;
        }
    }
    
    // 연속 파이어볼
    private async void Fireball()
    {
        float delay1 = 0.2f;
        float delay2 = 0.5f;
        float delay3 = 1.2f;
        float delay4 = 0.4f;

        var auraObject = SpawnObject($"{basicStat.id}_Aura", attackPos);
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        SetTriggerAnimator(ConstValues.Pattern);
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        while (patternInfo[0].playerInAttackRange)
        {
            SpawnObject($"{basicStat.id}_{ConstValues.Attack}_Hit", attackPos);
            
            int missileDir = 1;
            if (GameManager.Instance.CurPlayer.CenterPos.position.x < transform.position.x)
                missileDir = -1;
            var attackObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}", attackPos, 0, missileDir).GetComponent<Missile>();
            attackObject.LookAtTarget(GameManager.Instance.CurPlayer.CenterPos.position);
            
            if(await AttackDelay(delay3, true).SuppressCancellationThrow())
                return;
            
        }
        if(await AttackDelay(delay4).SuppressCancellationThrow())
            return;
        
        auraObject.SetActive(false);
        PatternEnd();
    }
}
