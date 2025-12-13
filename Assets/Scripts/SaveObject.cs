using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SaveObject : MonoBehaviour
{
    [SerializeField] private Transform savePointPos;
    [SerializeField] private Transform uiPos;
    
    private InteractionObject interactionObject;

    public InteractionObject InteractionObject => interactionObject;
    public Transform SavePointPos => savePointPos;

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
    
    public void SetSaveAction(Action action)
    {
        if (interactionObject == null)
        {
            interactionObject = SpawnUIObject(ConstValues.InteractionUI, uiPos).GetComponent<InteractionObject>();
            interactionObject.SetInteractionAction(action);
            interactionObject.SetText("저장", "↑");
            interactionObject.gameObject.SetActive(false);
        }
    }

    private GameObject SpawnUIObject(string id, Transform uiTransform)
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
