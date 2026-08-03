using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Trace : MonoBehaviour
{
    [SerializeField] private Transform target;       // 타겟 위치  
    public bool angleTrace;

    public float xPos;
    public float yPos;
    public float zPos;

    private void Update()
    {
        if (!target)
            return;
        
        var targetPosition = target.position;
        transform.position = new Vector3(targetPosition.x + xPos, targetPosition.y + yPos, targetPosition.z + zPos);

        if (angleTrace)
            transform.eulerAngles = target.eulerAngles;
    }

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        if (!target)
            return;

        var targetPosition = target.position;
        transform.position = new Vector3(targetPosition.x + xPos, targetPosition.y + yPos, targetPosition.z + zPos);

        if (angleTrace)
            transform.eulerAngles = target.eulerAngles;
    }

    public bool IsTargetNull()
    {
        return target == null;
    }
}
