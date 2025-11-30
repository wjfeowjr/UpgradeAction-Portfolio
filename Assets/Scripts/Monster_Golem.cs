using System;
using UnityEngine;

public class Monster_Golem : Monster
{
    [SerializeField] private Transform attackPos;
    [SerializeField] private Transform readyEffectPos;

    protected override void OnEnable()
    {
        base.OnEnable();
        myRigidbody.bodyType = RigidbodyType2D.Dynamic;
        Invoke(nameof(DelayBodyType), 0.1f);
    }

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
        float delay1 = 1.2f;
        float delay2 = 0.8f;

        // 준비자세 취하기
        GameObject readyObject = SpawnObject($"{basicStat.id}_Ready", readyEffectPos);
        if(await AttackDelay(delay1).SuppressCancellationThrow())
            return;
        readyObject.SetActive(false);

        // 공격
        SetTriggerAnimator(ConstValues.Pattern);
        SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}", attackPos).GetComponent<Attack>();
        if(await AttackDelay(delay2).SuppressCancellationThrow())
            return;
        
        PatternEnd();
    }

    private void DelayBodyType()
    {
        myRigidbody.bodyType = RigidbodyType2D.Kinematic;
    }
}
