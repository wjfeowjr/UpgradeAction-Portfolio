using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class AttributeFrame_Attribute : AttributeFrame
{
    private Tween selectTween;
    
    public void SetData(string id, bool isActive)
    {
        mainImage.sprite = GameManager.Instance.GetAtlasSprite(id);
        if (isActive)
            frameImage.sprite = frameSprite[2];
        else
            frameImage.sprite = frameSprite[0];
    }

    public void Select()
    {
        SelectObjectActive(true);
        selectTween?.Kill(false);
        selectTween = transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void UnSelect()
    {
        SelectObjectActive(false);
        selectTween?.Kill(false);
        selectTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void OnDisable()
    {
        selectTween?.Kill(false);
        selectTween = null;
    }
}
