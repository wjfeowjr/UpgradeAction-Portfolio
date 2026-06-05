using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SaveObject : InteractionController
{
    [SerializeField] private Transform savePointPos;
    [SerializeField] private GameObject minimapObject;

    public Transform SavePointPos => savePointPos;
    public Vector2 ColSize => GetComponent<BoxCollider2D>().size;
    public GameObject MinimapObject => minimapObject;

    public void SetSaveAction(Action action)
    {
        SetInteractionAction(action, 30002, GameManager.Instance.upKey);
    }
    
    public void SetFastTravelAction()
    {
        ActiveInteractionObject(false);
        SetActionInteractionSelect(SetAction, SetInteractionSelectCloseAction);
    }

    private void SetAction(int idx)
    {
        switch (idx)
        {
            case 0:
                Debug.Log("미니맵 소환");
                break;
            
            case 1:
                SetInteractionSelectCloseAction();
                break;
        }
    }

    public void SetSelectAction()
    {
        List<int> selectIdx = new List<int>
        {
            20011,
            20012
        };
        SpawnInteractionSelect(selectIdx);
    }

    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }
}
