using UnityEngine;

public class Monster_Spore : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                SporeShot();
                break;
        }
    }
    
    // 패턴1. 포물선
    private async void SporeShot()
    {
        float delay1 = 0.9f;
        float delay2 = 0.1f;
        float delay3 = 0.5f;
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_Attack", attackPos, 0, GameManager.Instance.CurPlayer.CenterPos.position);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
            
        PatternEnd();
    }
}
