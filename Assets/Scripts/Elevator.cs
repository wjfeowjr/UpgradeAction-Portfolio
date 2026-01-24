using System;
using UnityEngine;

public class Elevator : InteractionController
{
    [SerializeField] private MovingPlatform movingPlatform;
    [SerializeField] private Elevator_Lever[] levers;
    
    private Action startAction;

    public int TargetIdx
    {
        get => movingPlatform.TargetIdx;
        set => movingPlatform.TargetIdx = value;
    }

    public void SetUpDown(int idx)
    {
        TargetIdx = idx;
        for (var i = 0; i < levers.Length; i++)
        {
            levers[i].SetState(i != TargetIdx);
            levers[i].SetInteractionAction();
        }
    }
    
    // 엘리베이터 최초 위치 설정
    public void PosSetting()
    {
        int idx = 0;
        if (TargetIdx == 0)
            idx = 1;
        
        movingPlatform.transform.position = movingPlatform.Points[idx].position;
    }
    
    public override void SpawnInteractionObject()
    {
        if (movingPlatform.IsMoving)
            return;

        base.SpawnInteractionObject();
    }

    public void SetInteractionAction()
    {
        SetInteractionAction(Operation, GameManager.Instance.GetTalk(30003), GameManager.Instance.GetKeyCode(GameManager.Instance.interactionKey));
    }

    public void SetLeverAction()
    {
        foreach (var lever in levers)
            lever.SetAction(LeverAction);
    }

    // 엘베 작동
    private void Operation()
    {
        startAction();
        MovingStart();
    }

    // 위 아래로 이동
    private void MovingStart()
    {
        movingPlatform.IsMoving = true;
        for (var i = 0; i < levers.Length; i++)
        {
            levers[i].SetState(i == TargetIdx);
            levers[i].AnimSwitch();
        }
        GameManager.Instance.ControlStart = false;
    }

    // 레버를 건드릴 때 나오는 액션
    private void LeverAction()
    {
        if (movingPlatform.IsMoving)
            return;
        
        MovingStart();
    }

    public void SetAction(Action getAction)
    {
        startAction = getAction;
    }

    public void SetSaveAction(Action getAction)
    {
        movingPlatform.SetSaveAction(() =>
        {
            getAction();
            if (isPlayerTouch)
            {
                SpawnInteractionObject();
            }
        });
    }
}
