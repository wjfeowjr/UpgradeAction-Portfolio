using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProductTrigger : MonoBehaviour
{
    [SerializeField] private Collider2D triggerCollider;
    
    private Action myAction;
    private Func<UniTask> myAsyncAction;

    public void SetAction(Action action)
    {
        myAction = action;
    }
    
    public void SetAction(Func<UniTask> asyncAction)
    {
        myAsyncAction = asyncAction;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.Player) && !col.isTrigger)
        {
            myAction?.Invoke();
            if (myAsyncAction != null)
                myAsyncAction.Invoke();
            
            triggerCollider.enabled = false;
        }
    }
}
