using System;
using UnityEngine;

public class LateTrace : MonoBehaviour
{
    [SerializeField] private Transform target;       // 타겟 위치

    private void LateUpdate()
    {
        if (!target)
            return;
        
        Test();
    }

    private void Test()
    {
        var targetPosition = target.position;
        var centerX = RoomManager.Instance.CurrentRoom.SetCenterX();
        var centerY = RoomManager.Instance.CurrentRoom.SetCenterY();
        float magX = 0;
        float magY = 0;
        
        magX = Mathf.Abs(centerX - target.position.x) * 0.05f;
        magY = Mathf.Abs(centerY - target.position.y) * 0.1f;
        
        if (target.position.x < centerX)
            transform.position = new Vector2(targetPosition.x + magX, transform.position.y);
        else if (target.position.x >= centerX)
            transform.position = new Vector2(targetPosition.x - magX, transform.position.y);
        
        if (target.position.y < centerY)
            transform.position = new Vector2(transform.position.x, targetPosition.y + magY);
        else if (target.position.y >= centerY)
            transform.position = new Vector2(transform.position.x, targetPosition.y - magY);
        
        // if (target.position.x < centerX)
        //     transform.position = new Vector2(targetPosition.x + magX, targetPosition.y + 1);
        // else if (target.position.x >= centerX)
        //     transform.position = new Vector2(targetPosition.x - magX, targetPosition.y + 1);
    }
}
