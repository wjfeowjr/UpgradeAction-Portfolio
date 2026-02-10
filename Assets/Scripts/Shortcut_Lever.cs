using System;
using System.Threading;
using UnityEngine;

public class Shortcut_Lever : ShortcutObject
{
    [SerializeField] private TileFactory tileFactory;
    
    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if(!opened)
            AnimTrigger(ConstValues.Left);
    }

    // 열리는 연출
    public override async void OpenProduct()
    {
        float delay1 = 1.0f;
        
        GameManager.Instance.StopPlayer();
        delayCancellation = new CancellationTokenSource();
        if(await NormalDelay(delay1, delayCancellation).SuppressCancellationThrow())
            return;
        
        AnimTrigger(ConstValues.SwitchRight);
        SoundManager.Instance.PlaySound(ConstValues.Lever);
        
        if(await NormalDelay(delay1, delayCancellation).SuppressCancellationThrow())
            return;
        
        tileFactory.Crash();
        base.OpenImmediate();
        
        if(await NormalDelay(delay1, delayCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.MovePlayer();
    }
    
    // 즉시 오픈
    protected override void OpenImmediate()
    {
        AnimTrigger(ConstValues.Right);
        base.OpenImmediate();
    }
}
