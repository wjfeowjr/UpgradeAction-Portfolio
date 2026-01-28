using System;
using UnityEngine;

public class Shortcut_Lever : ShortcutObject
{
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
    public override void OpenProduct()
    {
        AnimTrigger(ConstValues.SwitchRight);
        base.OpenImmediate();
    }
    
    // 즉시 오픈
    protected override void OpenImmediate()
    {
        AnimTrigger(ConstValues.Right);
        base.OpenImmediate();
    }
}
