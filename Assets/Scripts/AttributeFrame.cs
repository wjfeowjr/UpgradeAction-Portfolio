using System;
using UnityEngine;
using UnityEngine.UI;

public class AttributeFrame : MonoBehaviour
{
    [SerializeField] protected Image mainImage;  // 메인 이미지
    [SerializeField] protected Image frameImage;  // 프레임 이미지
    [SerializeField] protected Sprite[] frameSprite; // 프레임 스프라이트
    [SerializeField] protected GameObject selectObject;

    public void SelectObjectActive(bool active)
    {
        selectObject.SetActive(active);
    }
}
