using System;
using DG.Tweening;
using UnityEngine;

public enum ERoomObjectType
{
    Item,
    AttributePoint,
    Relic,
    Potion,
}

public class RoomObject : InteractionController
{
    [SerializeField] private ERoomObjectType roomObjectType;
    [SerializeField] protected GameObject movingObject;
    [SerializeField] protected GameObject minimapObject;

    private float moveY = 0.5f;
    private float duration = 1.0f;
    
    private Tween moveTween;
    private Action action;
    private bool isGet;
    
    [SerializeField] private string itemId;
    [SerializeField] private SpriteRenderer itemSpriteRenderer;

    public ERoomObjectType RoomObjectType => roomObjectType;
    public GameObject MinimapObject => minimapObject;

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
        SetInteractionAction(GetObject, 30017, GameManager.Instance.upKey);
    }
    
    private void GetObject()
    {
        action();
        gameObject.SetActive(false);
        if(minimapObject)
            minimapObject.SetActive(false);
    }
    
    public void SetAction(Action getAction)
    {
        action = getAction;
    }

    public void SetParents(Transform targetTransform)
    {
        if(minimapObject)
            minimapObject.transform.SetParent(targetTransform);
    }
    
    public void SetSprite(string id)
    {
        itemId = id;
        itemSpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(itemId);
    }
}
