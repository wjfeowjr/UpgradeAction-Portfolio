using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public enum ShortcutType
{
    Crush,
    Wall,
    Lever,
    Obstacle,
}

public class ShortcutObject : Lever
{
    [SerializeField] private ShortcutType type;
    [SerializeField] protected Collider2D myCollider;
    [SerializeField] private GameObject[] shortcutBlockers;// 막고 있는 문/벽(콜라이더 포함)
    [SerializeField] protected bool opened;
    
    protected CancellationTokenSource delayCancellation;
    private Action<string, bool> openAction;
    private int startHp;
    private int hp;
    
    public ShortcutType Type => type;
    public string TypeString => type.ToString();

    public void OpenSetting(bool isOpen, Action<string, bool> action)
    {
        if (!isOpen)
        {
            startHp = 0;
            
            if (type == ShortcutType.Lever)
                startHp = 1;
            else if (type is ShortcutType.Crush)
                startHp = 3;

            hp = startHp;
            openAction = action;
            return;
        }
        
        OpenImmediate();
    }

    // 숏컷이 열리는 연출
    public void BreakAndOpen()
    {
        if (startHp > 0)
        {
            hp -= 1;
            if (hp > 0)
            {
                if(GetComponent<IHitProduct>() != null)
                    GetComponent<IHitProduct>().HitProduct();
                return;
            }
        }
        
        OpenProduct();
        openAction(name, true);
    }

    // 열리는 연출
    public virtual void OpenProduct()
    {
        OpenImmediate();
    }

    // 즉시 오픈
    protected virtual void OpenImmediate()
    {
        myCollider.enabled = false;
        opened = true;

        if (shortcutBlockers.Length > 0)
        {
            foreach (var shortcutBlocker in shortcutBlockers)
            {
                shortcutBlocker.SetActive(false);
            }
        }
    }
    
    // 일반 딜레이
    protected async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }

    // 공격판정(Attack 오브젝트)와 충돌했을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened || startHp == 0)
            return;

        // Attack 컴포넌트로 판정 (프로젝트 구조 기준)
        var atk = other.GetComponent<Attack>();
        if (!atk)
            return;

        if (!atk.CastChar.GetComponent<Player>())
            return;

        // 파괴 처리
        BreakAndOpen();
    }
}
