using System;
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

    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }
}
