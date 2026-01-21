using System;
using UnityEngine;

public class LockDoor : InteractionController
{
    [SerializeField] private string keyId;
    
    private bool isOpen;
    private Action action;

    public string KeyId => keyId;
    
    public bool IsOpen
    {
        get => isOpen;
        set => isOpen = value;
    }

    public void SetOpen(bool value)
    {
        isOpen = value;
    }

    public override void SpawnInteractionObject()
    {
        if (isOpen)
            return;

        base.SpawnInteractionObject();
    }
    
    public void SetInteractionAction()
    {
        SetInteractionAction(OpenAction, "열기", "↑");
    }

    // 문 열기 연출
    private void OpenAction()
    {
        action();
    }

    public void OpenDoor()
    {
        isOpen = true;
        Debug.Log("문 열리는 연출");
        DeleteDoor();
    }
    public void DeleteDoor()
    {
        gameObject.SetActive(false);
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }
}
