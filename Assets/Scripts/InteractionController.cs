using System;
using UnityEngine;
using UnityEngine.Serialization;

public class InteractionController : MonoBehaviour
{
    [SerializeField] protected Transform interactionPos;
    private InteractionObject interactionObject;

    protected bool isPlayerTouch;

    public InteractionObject InteractionObject => interactionObject;

    public bool IsPlayerTouch
    {
        get => isPlayerTouch;
        set => isPlayerTouch = value;
    }

    public virtual void SpawnInteractionObject()
    {
        interactionObject.gameObject.SetActive(true);
        interactionObject.transform.position = interactionPos.position;
        interactionObject.Expansion();
    }
    
    public void ReduceInteractionObject()
    {
        interactionObject.Reduce();
    }
    
    protected void SetInteractionAction(Action action, string text, string key)
    {
        if (interactionObject == null)
        {
            interactionObject = SpawnInteraction(ConstValues.InteractionUI, interactionPos).GetComponent<InteractionObject>();
            interactionObject.SetInteractionAction(action);
            interactionObject.SetText(text, key);
            interactionObject.gameObject.SetActive(false);
        }
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
}
