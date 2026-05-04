using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LockDoor : InteractionController
{
    [SerializeField] private string keyId;
    [SerializeField] private TileFactory tileFactory;
    
    private bool isOpen;
    
    private Action openProduct;
    private Action openAction;

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
        SetInteractionAction(openAction, 30001, GameManager.Instance.upKey);
    }

    public void OpenDoor()
    {
        ReduceInteractionObject();
        
        // 여기에 움직이는 연출 등 넣어야함
        openProduct?.Invoke();
    }
    public void DeleteDoor()
    {
        gameObject.SetActive(false);
    }

    // 잠겨있음
    public async void LockMessage()
    {
        string getMessage = string.Format(GameManager.Instance.GetTalk(30208), GameManager.Instance.GetItemTalk(keyId));
        await GameManager.Instance.SpawnWarningPopup(getMessage);
    }
    
    public void SetOpenProduct(Action product)
    {
        openProduct = product;
    }

    public void SetOpenAction(Action open)
    {
        openAction = open;
    }

    public async UniTask OpenAction()
    {
        isOpen = true;
        tileFactory.Crash(false);
        DeleteDoor();
        await GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30209));
    }
}
