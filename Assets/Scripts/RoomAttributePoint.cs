using System;
using DG.Tweening;
using UnityEngine;

public class RoomAttributePoint : InteractionController
{
    [SerializeField] private GameObject movingObject;
    [SerializeField] private GameObject minimapObject;

    private float moveY = 0.5f;
    private float duration = 1.0f;
    
    private Tween moveTween;
    private Action action;
    private bool isGet;
    
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
        SetInteractionAction(GetItem, 30017, GameManager.Instance.upKey);
    }
    
    private void GetItem()
    {
        action();
        gameObject.SetActive(false);
        minimapObject.SetActive(false);
    }
    
    public void SetAction(Action getAction)
    {
        action = getAction;
    }

    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }
}
