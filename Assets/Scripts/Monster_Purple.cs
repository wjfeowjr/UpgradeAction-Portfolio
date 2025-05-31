using Cysharp.Threading.Tasks;
using UnityEngine;

public class Monster_Purple : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform effectPos;
    
    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                StarShot();
                break;
        }
    }
    
    // 패턴1. 별 발사
    private async void StarShot()
    {
        float delay1 = 1.0f;
        float delay2 = 0.5f;
        
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        var chargeEffect = SpawnObject($"{basicStat.id}_ChargeEffect", effectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        chargeEffect.gameObject.SetActive(false);
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttack($"{basicStat.id}_Attack", attackPos);
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
            
        PatternEnd();
    }
}
