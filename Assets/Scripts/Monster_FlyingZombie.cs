using UnityEngine;

public class Monster_FlyingZombie : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                ZombieExplosion();
                break;
        }
    }
    
    // 패턴1. 음파
    private async void ZombieExplosion()
    {
        float delay1 = 0.7f;
        float delay2 = 0.6f;

        PlaySound($"{basicStat.id}_Voice1");
        Vector2 targetPos = GameManager.Instance.CurPlayer.CenterPos.position;
        int missileDir = 1;
        if (targetPos.x < transform.position.x)
            missileDir = -1;
        
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        
        var attackObject = SpawnAttackObject($"{basicStat.id}_Attack", attackPos, 0, missileDir).GetComponent<Missile>();
        SetTriggerAnimator(ConstValues.Pattern);
        attackObject.LookAtTarget(targetPos);
        
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }
}
