using System;
using DG.Tweening;
using UnityEngine;

public class RoomItem : InteractionController
{
    [SerializeField] private string itemId;
    [SerializeField] private SpriteRenderer itemSpriteRenderer;
    [SerializeField] private GameObject movingObject;

    private float moveY = 0.5f;
    private float duration = 1.0f;
    
    private Tween moveTween;
    private Action action;
    private bool isGet;

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
        SetInteractionAction(GetItem, 30017, GameManager.Instance.upKey);
    }
    
    private void GetItem()
    {
        action();
        gameObject.SetActive(false);
    }

    public void SetAction(Action getAction)
    {
        itemSpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(itemId);
        action = getAction;
    }
}
