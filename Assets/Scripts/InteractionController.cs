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
    private int talkIdx;
    private KeyCode keyCode;

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
    
    protected void SetInteractionAction(Action action, int idx, KeyCode key)
    {
        talkIdx = idx;
        keyCode = key;
        if (!interactionObject)
        {
            interactionObject = SpawnInteraction(ConstValues.InteractionUI, objectPos).GetComponent<InteractionObject>();
            interactionObject.SetTalkText(talkIdx);
            interactionObject.SetKeyText(keyCode);
            interactionObject.SetInteractionAction(action);
            interactionObject.gameObject.SetActive(false);
        }
    }

    public virtual void RefreshTalkText()
    {
        if(interactionObject)
            interactionObject.SetTalkText(talkIdx);
        if (interactionSelect)
            interactionSelect.RefreshTalk();
    }

    public void RefreshKeyText(KeyCode key)
    {
        keyCode = key;
        if(interactionObject)
            interactionObject.SetKeyText(keyCode);
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
    
    // 최초 대화
    protected void FirstDialog(Action dialogueAction, Action closeAction)
    {
        
    }
    
    // 대화 선택지 및 선택 액션
    protected void SetActionInteractionSelect(Action<string> dialogueAction, Action closeAction)
    {
        ActiveInteractionSelect(true);
        interactionSelect.SetAction(dialogueAction, closeAction);
        interactionSelect.SetDelay();
    }
    
    protected void SetActionInteractionSelect(Action<int> action, Action closeAction)
    {
        ActiveInteractionSelect(true);
        interactionSelect.SetAction(action, closeAction);
        interactionSelect.SetDelay();
    }
    
    protected void SetInteractionSelectCloseAction()
    {
        ActiveInteractionSelect(false);
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.CurPlayer.MyRigidbody.WakeUp();
        isPlayerTouch = false;
    }
    
    protected void SpawnInteractionSelect(NpcCopy npcCopy, NpcInfo npcInfo)
    {
        var selectList = GameManager.Instance.dialogueChoiceCopyList.FindAll(x =>
        {
            if (x.npc != npcCopy.id)
                return false;

            // checkKey가 비어있는 선택지는 분기 없이 항상 노출
            if (x.checkKey.Count == 0)
                return true;

            if (npcInfo?.dialogKey == null)
                return false;

            // NPC가 가진 키 중 ID가 일치하는 것을 찾아 isUse 값까지 매칭
            bool isSame = true;
            for (int i = 0; i < x.checkKey.Count; i++)
            {
                var key = npcInfo.dialogKey.Find(k => k.id == x.checkKey[i]);
                if (key == null)
                    continue;
                if (key.isUse == x.checkKeyValue[i])
                    continue;
                
                isSame = false;
                break;
            }

            return isSame;
        });

        if (selectList.Count == 0)
            return;

        if (interactionSelect == null)
        {
            interactionSelect = SpawnInteraction(ConstValues.InteractionSelectUI, selectPos).GetComponent<InteractionSelect>();
            interactionSelect.gameObject.SetActive(false);
        }

        List<int> choiceIdxList = new List<int>();
        foreach (var select in selectList)
            choiceIdxList.Add(select.talk);

        List<string> idList = new List<string>();
        foreach (var select in selectList)
            idList.Add(select.id);

        interactionSelect.StartSetting(choiceIdxList, idList);
    }
    
    protected void SpawnInteractionSelect(List<int> choiceIdxList)
    {
        if (interactionSelect == null)
        {
            interactionSelect = SpawnInteraction(ConstValues.InteractionSelectUI, selectPos).GetComponent<InteractionSelect>();
            interactionSelect.gameObject.SetActive(false);
        }

        interactionSelect.StartSetting(choiceIdxList, null);
    }

    protected void ActiveInteractionSelect(bool active)
    {
        if (interactionSelect)
        {
            interactionSelect.transform.position = selectPos.position;
            interactionSelect.gameObject.SetActive(active);
        }
    }
    
    // 오브젝트 소환
    public void SpawnAcquireEffect(string effectId, Vector2 pos)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(effectId, pos);
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == effectId);
        if (objectData != null)
        {
            var spawnedObject = obj.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = obj.AddComponent<SpawnedObject>();

            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
        }
    }
}
