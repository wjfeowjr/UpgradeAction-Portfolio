using System;
using System.Threading;
using DG.Tweening;
using UnityEngine;

public class Shortcut_Lever : ShortcutObject
{
    [SerializeField] private GameObject doorObject;
    [SerializeField] private float doorOpenDistance;
    private float doorHeight;

    private void Awake()
    {
        myAnimator = myCollider.gameObject.GetComponent<Animator>();
        doorHeight = doorObject.transform.position.y + doorOpenDistance;
    }

    private void OnEnable()
    {
        if(opened)
            AnimTrigger(ConstValues.Right);
        else
            AnimTrigger(ConstValues.Left);
    }

    // 열리는 연출
    public override async void OpenProduct()
    {
        float delay1 = 1.5f;

        SpawnEffect(ConstValues.LeverEffect, transform.position);
        AnimTrigger(ConstValues.SwitchRight);
        SoundManager.Instance.PlaySound(ConstValues.Lever);
        // 문이 위로 열리는 연출
        doorObject.transform.DOKill();
        doorObject.transform.DOMoveY(doorHeight, delay1);

        delayCancellation = new CancellationTokenSource();
        if(await NormalDelay(delay1, delayCancellation).SuppressCancellationThrow())
            return;

        base.OpenImmediate();
    }
    
    // 즉시 오픈
    protected override void OpenImmediate()
    {
        AnimTrigger(ConstValues.Right);
        doorObject.transform.position = new Vector2(doorObject.transform.position.x, doorHeight);
        base.OpenImmediate();
    }

    private void OnDestroy()
    {
        if (doorObject)
            doorObject.transform.DOKill();
    }
}
