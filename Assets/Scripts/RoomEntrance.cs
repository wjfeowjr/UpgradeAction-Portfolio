using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RoomEntrance : MonoBehaviour
{
    private Action myAction;
    private Func<UniTask> myAsyncAction;
    private Collider2D myCollider;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    public void SetAction(Action action)
    {
        myAction = action;
    }
    
    public void SetAction(Func<UniTask> asyncAction)
    {
        myAsyncAction = asyncAction;
    }

    public void ResetCollider()
    {
        myCollider.enabled = true;
    }
    
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (GameManager.Instance.ControlStart && col.CompareTag(ConstValues.Player) && !col.isTrigger)
        {
            myAction?.Invoke();
            if (myAsyncAction != null)
                myAsyncAction.Invoke();
            myCollider.enabled = false;
        }
    }
}
  