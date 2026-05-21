using UnityEngine;

public class Monster_Bazooka : Monster
{
    [SerializeField] private Transform flashPos;
    [SerializeField] private Transform effectPos;
    [SerializeField] private Transform attackPos;

    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                Shot();
                break;
        }
    }
    private async void Shot()
    {
        float delay1 = 0.2f;
        float delay2 = 0.5f;

        if (await AttackDelay(delay1).SuppressCancellationThrow())
            return;

        SpawnObject(ConstValues.GreenFlash, flashPos);
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);

        // 궤도 설정
        int missileDir = 1;
        if (GameManager.Instance.CurPlayer.CenterPos.position.x < transform.position.x)
            missileDir = -1;
        
        // var missileObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}_{ConstValues.Object}", attackPos, 0, missileDir).GetComponent<Missile>();
        // missileObject.LookAtTarget(GameManager.Instance.CurPlayer.CenterPos.position);
        // missileObject.gameObject.SetActive(false);
        
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        SetTriggerAnimator(ConstValues.Pattern);
        
        GameObject effectObject = SpawnObject($"{basicStat.id}_{ConstValues.Attack}_{ConstValues.Effect}", effectPos);
        effectObject.transform.eulerAngles = transform.localScale.x > 0 ? new Vector3(-20, 90, 0) : new Vector3(-20, -90, 0);
        
        var missileObject = SpawnAttackObject($"{basicStat.id}_{ConstValues.Attack}_{ConstValues.Object}", attackPos, 0, missileDir).GetComponent<Missile>();
        var targetPos = GameManager.Instance.CurPlayer.CenterPos.position;
        var myPos = transform.position;
        
        // 오른쪽 보고있음
        if (transform.localScale.x > 0)
        {
            if(targetPos.x > myPos.x)
                missileObject.LookAtTarget(targetPos);
            else
                missileObject.LookAtTarget(new Vector2(centerPos.position.x + 5.0f, centerPos.position.y));
        }
        // 왼쪽 보고있음
        else
        {
            if(targetPos.x > myPos.x)
                missileObject.LookAtTarget(new Vector2(centerPos.position.x - 5.0f, centerPos.position.y));
            else
                missileObject.LookAtTarget(targetPos);
        }
        
        //missileObject.gameObject.SetActive(true);
        
        SetTriggerAnimator(ConstValues.Pattern);
        if (await AttackDelay(delay2).SuppressCancellationThrow())
            return;

        PatternEnd();
    }
}
