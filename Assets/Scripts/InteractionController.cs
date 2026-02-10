using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private Transform objectPos;
    [SerializeField] private Transform selectPos;
    
    private InteractionObject interactionObject;
    private InteractionSelect interactionSelect;

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
        interactionObject.transform.position = objectPos.position;
        interactionObject.Expansion();
    }
    
    public void ReduceInteractionObject()
    {
        interactionObject.Reduce();
    }

    protected void ActiveInteractionObject(bool active)
    {
        interactionObject.gameObject.SetActive(active);
    }
    
    protected void SetInteractionAction(Action action, string text, string key)
    {
        if (interactionObject == null)
        {
            interactionObject = SpawnInteraction(ConstValues.InteractionUI, objectPos).GetComponent<InteractionObject>();
            interactionObject.SetInteractionAction(action);
            interactionObject.SetText(text, key);
            interactionObject.gameObject.SetActive(false);
        }
    }
    
    private GameObject SpawnInteraction(string id, Transform pos)
    {
        var obj = GameManager.Instance.SpawnToUIObjectPoolInstantiate(id, pos);
        
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
            
            trace.SetTarget(pos);
        }

        return obj;
    }
    
    // 선택지
    
    // 대화 선택지 및 선택 액션
    protected void SetActionInteractionSelect(Action<string> dialogueAction, Action closeAction)
    {
        ActiveInteractionSelect(true);
        interactionSelect.SetAction(dialogueAction, closeAction);
        interactionSelect.SetDelay();
    }
    
    protected void SpawnInteractionSelect(NpcData npcData)
    {
        var selectList = TableManager.Instance.dialogueChoiceTable.DialogueChoice.FindAll(x => x.npc == npcData.id);
        
        if (selectList.Count > 0 && interactionSelect == null)
        {
            interactionSelect = SpawnInteraction(ConstValues.InteractionSelectUI, selectPos).GetComponent<InteractionSelect>();
            
            List<string> choiceList = new List<string>();
            foreach (var select in selectList)
                choiceList.Add(GameManager.Instance.GetTalk(select.talk));
            
            List<string> idList = new List<string>();
            foreach (var select in selectList)
                idList.Add(select.id);
            
            interactionSelect.StartSetting(choiceList, idList);
            interactionSelect.gameObject.SetActive(false);
        }
    }

    protected void ActiveInteractionSelect(bool active)
    {
        if (interactionSelect)
        {
            interactionSelect.transform.position = selectPos.position;
            interactionSelect.gameObject.SetActive(active);
        }
    }
}
