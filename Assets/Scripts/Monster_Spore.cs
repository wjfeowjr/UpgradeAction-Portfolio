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
        float delay1 = 1.0f;
        float delay3 = 0.5f;

        var targetPos = GameManager.Instance.CurPlayer.CenterPos.position;
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}", attackPos, 0, targetPos);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
            
        PatternEnd();
    }
}
