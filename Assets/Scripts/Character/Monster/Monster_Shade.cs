using UnityEngine;

public class Monster_Shade : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform readyPos;
    
    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                FlameThrow();
                break;
        }
    }
    
    // 패턴1. 화염 투척
    private async void FlameThrow()
    {
        float delay1 = 1.0f;
        float delay3 = 0.6f;

        var targetPos = GameManager.Instance.CurPlayer.CenterPos.position;
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        var chargeEffect = SpawnObject($"{basicStat.id}_{ConstValues.Ready}", readyPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        chargeEffect.SetActive(false);
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_{ConstValues.Attack}", attackPos, 0, targetPos);
        if(await AttackDelay(delay3).SuppressCancellationThrow())
            return;
            
        PatternEnd();
    }
}
