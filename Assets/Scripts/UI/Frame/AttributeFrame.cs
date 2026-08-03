using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AttributeFrame : ExpansionUiObject
{
    [SerializeField] protected Image mainImage;  // 메인 이미지
    [SerializeField] protected Image frameImage;  // 프레임 이미지
    [SerializeField] protected Sprite[] frameSprite; // 프레임 스프라이트
}
