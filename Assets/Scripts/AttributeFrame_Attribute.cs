using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class AttributeFrame_Attribute : AttributeFrame
{
    [SerializeField] private GameObject activeObject;
    [SerializeField] private GameObject iconObject;
    [SerializeField] private GameObject lockObject;
    
    private Vector2 effectPos;
    public string attributeId;

    public Vector2 EffectPos => effectPos;
    
    public void SetData(string id, bool isActive, bool isLock)
    {
        mainImage.sprite = GameManager.Instance.GetAtlasSprite(id);
        if (isActive)
            frameImage.sprite = frameSprite[1];
        else
            frameImage.sprite = frameSprite[0];

        attributeId = id;
        activeObject.SetActive(isActive);
        iconObject.SetActive(!isLock);
        lockObject.SetActive(isLock);
    }
    
    public override void SelectObjectActive(bool active)
    {
        base.SelectObjectActive(active);
        effectPos = transform.position;
    }
}
