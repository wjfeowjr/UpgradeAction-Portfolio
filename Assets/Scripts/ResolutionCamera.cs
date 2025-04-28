using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResolutionCamera : MonoBehaviour
{
    private Camera myCamera;

    private void Awake()
    {
        myCamera = GetComponent<Camera>();
        //ResolutionSetting();
    }

    // private void ResolutionSetting()
    // {
    //     if(!GameManager.Instance)
    //         return;
    //     
    //     if (GameManager.Instance.scaleHeight < 1.0f) // 만약 현재 비율이 16:9보다 세로가 더 길다면 
    //     {  
    //         Rect rect = myCamera.rect;
    //
    //         rect.width = 1.0f;
    //         rect.height = GameManager.Instance.scaleHeight;
    //         rect.x = 0;
    //         rect.y = (1.0f - GameManager.Instance.scaleHeight) / 2.0f;
    //
    //         myCamera.rect = rect;
    //     }
    //     else // 만약 현재 비율이 16:9보다 세로가 더 짧다면, 아무것도 하지 않음
    //     {
    //         myCamera.rect = new Rect(0, 0, 1, 1);
    //     }
    // }
}
