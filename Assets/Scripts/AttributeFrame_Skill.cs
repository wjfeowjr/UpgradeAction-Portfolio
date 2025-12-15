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

    public void SetData(bool haveSkill, SkillData skill)
    {
        isHaveSkill = haveSkill;
        if (isHaveSkill)
        {
            mainImage.sprite = GameManager.Instance.GetAtlasSprite(skill.id);
            skillName.text = skill.name;
            skillObject.SetActive(true);
            lockObject.SetActive(false);
            frameImage.sprite = frameSprite[1];
        }
        else
        {
            skillName.text = "???";
            skillObject.SetActive(false);
            lockObject.SetActive(true);
            frameImage.sprite = frameSprite[0];
        }
    }
}
