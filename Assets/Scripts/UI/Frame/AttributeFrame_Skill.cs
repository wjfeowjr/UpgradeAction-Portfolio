using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AttributeFrame_Skill : AttributeFrame
{
    [SerializeField] private GameObject skillObject;
    [SerializeField] private GameObject lockObject;

    private bool isHaveSkill;

    public void SetData(bool haveSkill, SkillData skill)
    {
        isHaveSkill = haveSkill;
        if (isHaveSkill)
        {
            mainImage.sprite = GameManager.Instance.GetAtlasSprite(skill.id);
            SetText(GameManager.Instance.GetTalk(skill.talk));
            skillObject.SetActive(true);
            lockObject.SetActive(false);
            frameImage.sprite = frameSprite[1];
        }
        else
        {
            SetText("???");
            skillObject.SetActive(false);
            lockObject.SetActive(true);
            frameImage.sprite = frameSprite[0];
        }
    }
}
