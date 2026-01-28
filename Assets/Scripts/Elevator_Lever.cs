using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Elevator_Lever : Lever
{
    private bool isNotTouch;

    private Elevator elevator;
    private Action elevatorActon;
    
    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        AnimTrigger(isNotTouch ? ConstValues.Right : ConstValues.Left);
    }

    public void SetState(bool touch)
    {
        isNotTouch = touch;
    }

    public void AnimSwitch()
    {
        AnimTrigger(isNotTouch ? ConstValues.SwitchRight : ConstValues.SwitchLeft);
    }

    public void SetAction(Action action)
    {
        elevatorActon = action;
    }
    
    public override void SpawnInteractionObject()
    {
        if (isNotTouch)
            return;

        base.SpawnInteractionObject();
    }

    public void SetInteractionAction()
    {
        SetInteractionAction(InteractionAction, GameManager.Instance.GetTalk(30003), GameManager.Instance.GetKeyCode(GameManager.Instance.interactionKey));
    }

    private void InteractionAction()
    {
        if (isNotTouch)
            return;
        
        elevatorActon();
        ReduceInteractionObject();
    }
}
