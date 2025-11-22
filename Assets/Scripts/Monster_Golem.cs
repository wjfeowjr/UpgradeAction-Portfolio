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
        float delay1 = 0.9f;
        float delay2 = 0.5f;

        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);

        // 공격
        SetTriggerAnimator(ConstValues.Pattern);
        var spawnObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}", attackPos).GetComponent<Attack>();
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        spawnObject.gameObject.SetActive(false);
        PatternEnd();
    }
}
