using System;
using UnityEngine;

public class LockDoor : InteractionController
{
    [SerializeField] private string keyId;
    [SerializeField] private TileFactory tileFactory;
    
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
        SetInteractionAction(OpenAction, GameManager.Instance.GetTalk(30001), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey));
    }

    // 문 열기 연출
    private void OpenAction()
    {
        action();
    }

    public void OpenDoor()
    {
        isOpen = true;
        tileFactory.Crash(false);
        DeleteDoor();
    }
    public void DeleteDoor()
    {
        gameObject.SetActive(false);
    }
    
    // 문 열림
    public async void OpenMessage()
    {
        var getMessage = GameManager.Instance.GetTalk(30209);
        await GameManager.Instance.SpawnWarningPopup(getMessage);
    }
    
    // 잠겨있음
    public async void LockMessage()
    {
        string getMessage = string.Format(GameManager.Instance.GetTalk(30208), GameManager.Instance.GetItemTalk(keyId));
        await GameManager.Instance.SpawnWarningPopup(getMessage);
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }
}
