using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AttributeFrame : MonoBehaviour
{
    [SerializeField] protected Image mainImage;  // 메인 이미지
    [SerializeField] protected Image frameImage;  // 프레임 이미지
    [SerializeField] protected Sprite[] frameSprite; // 프레임 스프라이트
    [SerializeField] protected GameObject selectObject;
    [SerializeField] protected GameObject unSelectObject;
    
    private Tween selectTween;
    
    public virtual void SelectObjectActive(bool active)
    {
        selectObject.SetActive(active);
        unSelectObject.SetActive(!active);
    }

    public void Expansion(float scale)
    {
        selectTween?.Kill(false);
        selectTween = transform.DOScale(scale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void Reduction()
    {
        selectTween?.Kill(false);
        selectTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }
    
    protected void OnDisable()
    {
        selectTween?.Kill(false);
        selectTween = null;
    }
}
