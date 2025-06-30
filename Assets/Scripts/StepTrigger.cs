using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class StepTrigger : MonoBehaviour
{
    private BoxCollider2D triggerCollider;
    private Action myAction;
    private Func<UniTask> myAsyncAction;
    
    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
    }

    public void SetAction(Action action)
    {
        myAction = action;
    }
    
    public void SetAction(Func<UniTask> asyncAction)
    {
        myAsyncAction = asyncAction;
    }

    private async void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.Player))
        {
            Debug.Log("귀신");
            triggerCollider.enabled = false;
            myAction?.Invoke();
            
            if (myAsyncAction != null)
                await myAsyncAction.Invoke();
        }
    }
}
