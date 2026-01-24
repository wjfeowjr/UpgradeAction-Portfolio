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
        SetInteractionAction(OpenAction, GameManager.Instance.GetTalk(30001), GameManager.Instance.GetKeyCode(GameManager.Instance.interactionKey));
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
    
    // 잠겨있음
    public async void OpenMessage()
    {
        string getMessage = $"문이 열렸다";
        await GameManager.Instance.SpawnWarningPopup(getMessage);
    }
    
    // 잠겨있음
    public async void LockMessage()
    {
        string getMessage = $"{keyId}가 필요합니다";
        await GameManager.Instance.SpawnWarningPopup(getMessage);
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }
}
