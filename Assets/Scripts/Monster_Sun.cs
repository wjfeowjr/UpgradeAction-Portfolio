using UnityEngine;

public class Monster_Sun : Monster
{
    [SerializeField] private Transform attackPos;

    protected override void OnEnable()
    {
        base.OnEnable();
        moveState = EMoveState.Moving;
    }
    
    protected override void MonsterPattern(int idx)
    {
        base.MonsterPattern(idx);
        switch (idx)
        {
            case 0:
                ShotFire();
                break;
        }
    }
    
    protected override void Move()
    {
        // 움직이기
        if (moveState != EMoveState.Moving)
            return;
        
        
    }
    
    // 불꽃발사
    private async void ShotFire()
    {
        Debug.Log("불꽃발사!");
    }
}
