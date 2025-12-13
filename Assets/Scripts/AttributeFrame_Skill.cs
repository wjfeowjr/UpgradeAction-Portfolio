using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AttributeFrame_Skill : AttributeFrame
{
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private GameObject skillObject;
    [SerializeField] private GameObject lockObject;

    private bool isHaveSkill;
    private Tween selectTween;

    public bool IsHaveSkill => isHaveSkill;
    
    public void SetData(bool haveSkill, SkillData skill)
    {
        isHaveSkill = haveSkill;
        if (isHaveSkill)
        {
            mainImage.sprite = GameManager.Instance.GetAtlasSprite(skill.id);
            skillName.text = skill.name;
            skillObject.SetActive(true);
            lockObject.SetActive(false);
        }
        else
        {
            skillName.text = "???";
            skillObject.SetActive(false);
            lockObject.SetActive(true);
        }
    }
    
    public void Select()
    {
        SelectObjectActive(true);
        frameImage.sprite = frameSprite[2];
        selectTween?.Kill(false);
        selectTween = gameObject.GetComponent<RectTransform>().DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void UnSelect()
    {
        SelectObjectActive(false);
        if(isHaveSkill)
            frameImage.sprite = frameSprite[1];
        else
            frameImage.sprite = frameSprite[0];
        selectTween?.Kill(false);
        selectTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void OnDisable()
    {
        selectTween?.Kill(false);
        selectTween = null;
    }
}
