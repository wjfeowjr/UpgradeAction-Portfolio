using System;
using UnityEngine;

public class RoomTreasureBox : InteractionController
{
    [SerializeField] private SpriteRenderer mySpriteRenderer;

    private bool isOpen;
    private Action action;

    public bool IsOpen
    {
        get => isOpen;
        set => isOpen = value;
    }

    private void OnEnable()
    {
        OpenSetting();
    }

    public void OpenSetting()
    {
        if (isOpen)
        {
            mySpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(ConstValues.TreasureBoxOpen);
        }
        else
        {
            mySpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(ConstValues.TreasureBoxClose);
        }
    }

    public void SetSprite(bool alreadyGet)
    {
        if (alreadyGet)
        {
            mySpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(ConstValues.TreasureBoxOpen);
            isOpen = true;
        }
        else
        {
            mySpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(ConstValues.TreasureBoxClose);
            isOpen = false;
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
        SetInteractionAction(GetItem, GameManager.Instance.GetTalk(30001), GameManager.Instance.GetKeyCode(GameManager.Instance.interactionKey));
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
