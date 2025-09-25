using System;
using UnityEngine;

public class RoomTreasureBox : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mySpriteRenderer;
    [SerializeField] private Transform uiPos;
    
    private InteractionObject interactionObject;
    private bool isOpen;
    private Action action;

    public bool IsOpen => isOpen;

    private void OnEnable()
    {
        OpenSetting();
    }

    private void OpenSetting()
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
    
    public void SpawnInteractionObject()
    {
        interactionObject.gameObject.SetActive(true);
        interactionObject.transform.position = uiPos.position;
        interactionObject.Expansion();
    }

    public void ReduceInteractionObject()
    {
        interactionObject.Reduce();
    }
    
    public void SetInteractionAction()
    {
        if (interactionObject == null)
        {
            interactionObject = SpawnInteraction(ConstValues.InteractionUI, uiPos).GetComponent<InteractionObject>();
            interactionObject.SetInteractionAction(GetItem);
            interactionObject.SetText("열기", "↑");
            interactionObject.gameObject.SetActive(false);
        }
    }
    
    private void GetItem()
    {
        isOpen = true;
        OpenSetting();
        ReduceInteractionObject();
        action();
    }
    
    private GameObject SpawnInteraction(string id, Transform uiTransform)
    {
        var obj = GameManager.Instance.SpawnToUIObjectPoolInstantiate(id, uiTransform);
        
        var uiData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (uiData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
        
        spawnedObject.SetupData(uiData, transform.localScale.x);
        spawnedObject.EnableSetting();
        
        if (spawnedObject.GetTrace())
        {
            var trace = obj.GetComponent<Trace>();
            if(!trace)
                trace = obj.AddComponent<Trace>();
            
            trace.SetTarget(uiTransform);
        }

        return obj;
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }
}
