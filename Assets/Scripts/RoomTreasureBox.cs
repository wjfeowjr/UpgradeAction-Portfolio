using System;
using UnityEngine;

public class RoomTreasureBox : InteractionController
{
    [SerializeField] private Animator myAnimator;
    [SerializeField] private bool isOpen;
    private Action action;

    public bool IsOpen
    {
        get => isOpen;
        set => isOpen = value;
    }

    private void OnEnable()
    {
        myAnimator.SetTrigger(isOpen ? ConstValues.Open : ConstValues.Close);
    }

    public void OpenProduct()
    {
        if (!isOpen)
        {
            myAnimator.SetTrigger(ConstValues.SwitchOpen);
            isOpen = true;
        }
    }

    public override void SpawnInteractionObject()
    {
        if (isOpen)
            return;

        base.SpawnInteractionObject();
    }
    
    public void SetInteractionAction()
    {
        SetInteractionAction(GetItem, 30001, GameManager.Instance.GetKeyCode(GameManager.Instance.upKey));
    }
    
    private void GetItem()
    {
        action();
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }
}
