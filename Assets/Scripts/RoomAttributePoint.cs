using System;
using DG.Tweening;
using UnityEngine;

public class RoomAttributePoint : MonoBehaviour
{
    [SerializeField] private GameObject movingObject;
    [SerializeField] private GameObject minimapObject;

    private float moveY = 0.5f;
    private float duration = 1.0f;
    
    private Tween moveTween;
    private Action action;
    private bool alreadyGet;
    
    public GameObject MinimapObject => minimapObject;

    public bool AlreadyGet
    {
        get => alreadyGet;
        set => alreadyGet = value;
    }

    private void Start()
    {
        movingObject.transform.DOMoveY(movingObject.transform.position.y + moveY, duration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }

    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.Player) && !col.isTrigger)
        {
            action();
            gameObject.SetActive(false);
        }
    }
}
