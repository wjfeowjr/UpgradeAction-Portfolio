using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Trace : MonoBehaviour
{
    [SerializeField] private Transform target;       // 타겟 위치  
    public bool angleTrace;
    public bool lateTrace;

    public float xPos;
    public float yPos;
    public float zPos;

    private void Update()
    {
        if(lateTrace)
            return;
        
        if (target)
        {
            var targetPosition = target.position;
            transform.position = new Vector3(targetPosition.x + xPos, targetPosition.y + yPos, targetPosition.z + zPos);
        }

        if (angleTrace)
            transform.eulerAngles = target.eulerAngles;
    }

    private void LateUpdate()
    {
        if(!lateTrace)
            return;
        
        if (target)
        {
            var targetPosition = target.position;
            transform.position = new Vector3(targetPosition.x + xPos, targetPosition.y + yPos, targetPosition.z + zPos);
        }

        if (angleTrace)
            transform.eulerAngles = target.eulerAngles;
    }

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        var targetPosition = target.position;
        transform.position = new Vector3(targetPosition.x + xPos, targetPosition.y + yPos, targetPosition.z + zPos);

        if (angleTrace)
            transform.eulerAngles = target.eulerAngles;
    }
}
