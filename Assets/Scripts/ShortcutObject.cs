using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum ShortcutType
{
    Crush,
    Wall,
    Lever,
    Platform,
}

public class ShortcutObject : Lever
{
    [SerializeField] private ShortcutType type;
    [SerializeField] protected Collider2D myCollider;
    [SerializeField] private GameObject[] shortcutBlockers;// 막고 있는 문/벽(콜라이더 포함)

    private Action<string> openAction;
    protected bool opened;
    private int hp;
    
    public ShortcutType Type => type;
    public string TypeString => type.ToString();

    public void OpenSetting(bool isOpen, Action<string> action)
    {
        if (!isOpen)
        {
            hp = 0;
            
            if (type == ShortcutType.Lever)
                hp = 1;
            else if (type is ShortcutType.Crush)
                hp = 3;

            openAction = action;
            return;
        }
        
        OpenImmediate();
    }

    // 숏컷이 열리는 연출
    private void BreakAndOpen()
    {
        hp -= 1;
        if (hp > 0)
        {
            HitProduct();
            return;
        }
        
        openAction(name);
        OpenProduct();
    }

    protected virtual void HitProduct()
    {
        
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
    
    protected GameObject SpawnObject(string id, Vector2 pos)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if(objectData == null)
            return obj;

        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();

        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();

        return obj;
    }
    
    // 공격판정(Attack 오브젝트)와 충돌했을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened || hp == 0)
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
