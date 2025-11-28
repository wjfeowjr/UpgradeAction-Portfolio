using System.Threading;
using UnityEngine;

public class Monster_Bat : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                SonicWave();
                break;
        }
    }
    
    // 패턴1. 음파
    private async void SonicWave()
    {
        float delay1 = 0.9f;
        float delay2 = 0.6f;

        PlaySound($"{basicStat.id}_Voice1");
        if(await AttackDelay(delay1, true).SuppressCancellationThrow())
            return;

        int missileDir = 1;
        if (GameManager.Instance.CurPlayer.CenterPos.position.x < transform.position.x)
            missileDir = -1;
        
        SetTriggerAnimator(ConstValues.Pattern);
        var attackObject = SpawnAttackObject($"{basicStat.id}_Attack", attackPos, 0, missileDir).GetComponent<Missile>();
        attackObject.LookAtTarget(GameManager.Instance.CurPlayer.CenterPos.position);
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
}
