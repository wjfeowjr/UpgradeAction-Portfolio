using System;
using DG.Tweening;
using UnityEngine;

public class RoomItem : InteractionController
{
    [SerializeField] private GameObject movingObject;

    private float moveY = 0.5f;
    private float duration = 1.0f;

    private string id;
    private Tween moveTween;
    private bool isGet;
    private Action action;

    public string Id
    {
        get => id;
        set => id = value;
    }
    public bool IsGet
    {
        get => isGet;
        set => isGet = value;
    }

    private void Start()
    {
        movingObject.transform.DOMoveY(movingObject.transform.position.y + moveY, duration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    public override void SpawnInteractionObject()
    {
        if (isGet)
            return;

        base.SpawnInteractionObject();
    }
    
    public void SetInteractionAction()
    {
        SetInteractionAction(GetItem, GameManager.Instance.GetTalk(30017), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey));
    }
    
    private void GetItem()
    {
        action();
        SpawnObject(ConstValues.BangEffect, movingObject.transform.position);
        gameObject.SetActive(false);
    }
    
    // 오브젝트 소환
    private void SpawnObject(string objectId, Vector2 pos)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(objectId, pos);
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == objectId);
        if (objectData != null)
        {
            var spawnedObject = obj.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = obj.AddComponent<SpawnedObject>();

            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
        }
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }
}
