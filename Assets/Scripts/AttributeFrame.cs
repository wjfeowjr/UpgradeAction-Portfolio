using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AttributeFrame : MonoBehaviour
{
    public bool isHaveSkill;
    public string id;

    [Header("Upgrade UI Anchors")]
    public RectTransform upgradeFrame; // 이동하는 하이라이트 프레임
    public RectTransform plusButton;
    public RectTransform minusButton;

    [Header("Level / Data")]
    public int level; // 현재 레벨
    public List<SkillAttributeData> attributeData; // 해당 프레임의 기준 데이터(레벨 비교용)
    
    [SerializeField] private GameObject haveSkillObject;
    [SerializeField] private GameObject notHaveSkillObject;

    [SerializeField] private Image skillImage;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text nextCostText;
    [SerializeField] private TMP_Text nextCost;
    [SerializeField] private TMP_Text currentLvText;
    [SerializeField] private GameObject costFrame;
    [SerializeField] private Color startColor;
    [SerializeField] private Color endColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color activeColor;
    
    [SerializeField] private TMP_Text[] attributeInfoArray;

    private void Start()
    {
        EnableSetting();
    }

    private void EnableSetting()
    {
        // upgradeFrame.GetComponent<Image>().color = startColor;
        // upgradeFrame.GetComponent<Image>().DOColor(endColor, 0.75f)
        //     .SetLoops(-1, LoopType.Yoyo)
        //     .SetEase(Ease.Linear)
        //     .SetUpdate(true) // TimeScale 영향을 받지 않게 할 때 유용
        //     .SetLink(gameObject, LinkBehaviour.KillOnDestroy); // 오브젝트 파괴 시 자동 정리
    }
    
    // 외부에서 호출됨: 업그레이드 프레임 On/Off
    public void SetUpgradeFrameActive(bool active)
    {
        if (upgradeFrame != null)
            upgradeFrame.gameObject.SetActive(active);
    }
    
    // 규칙 3 구현: 시작 위치가 minus인가?
    // "attributeData[level]와 같은 레벨이면 minus" 라는 요구를 다음처럼 해석:
    // 현재 level이 기준 데이터의 level과 같다면 minus부터 선택
    public bool ShouldStartOnMinus()
    {
        if (attributeData == null)
            return false;
        
        return level == attributeData[^1].level;
    }

    public void SetSkillInfo(string skillId)
    {
        isHaveSkill = GameManager.Instance.PlayerSkill.IsHaveSkill(skillId);
        id = skillId;
        
        haveSkillObject.SetActive(isHaveSkill);
        notHaveSkillObject.SetActive(!isHaveSkill);

        if (isHaveSkill)
        {
            var skillData = TableManager.Instance.skillTable.Skill.Find(x => x.id == skillId);
            attributeData = TableManager.Instance.skillAttributeTable.SkillAttribute.FindAll(x => x.id == skillId);
            int attributeLv = 0;

            switch (skillData.caster)
            {
                case ConstValues.Berserker:
                    //attributeLv = GameManager.Instance.PlayerSkill.AttributeLv(skillId);
                    attributeLv = 0;
                    break;
            
                case ConstValues.Gunner:
                    //attributeLv = GameManager.Instance.PlayerSkill.AttributeLv(skillId);
                    attributeLv = 0;
                    break;
            }

            skillImage.sprite = GameManager.Instance.GetAtlasSprite(skillData.id);
            skillNameText.text = skillData.name;
        
            foreach (var attributeInfo in attributeInfoArray)
                attributeInfo.gameObject.SetActive(false);
            
            if (attributeData.Count > 0)
            {
                int maxLv = attributeData[^1].level;
                if (attributeLv < maxLv)
                {
                    nextCostText.text = $"다음 Lv{attributeLv + 1}";
                    int nextPoint = attributeData.Find(x => x.level == attributeLv + 1).level;
                    nextCost.text = nextPoint.ToString();
                    costFrame.SetActive(true);
                    
                }
                else
                {
                    nextCostText.text = $"최대 레벨 도달!";
                    costFrame.SetActive(false);
                }
                currentLvText.text = $"현재 레벨: {attributeLv}/{maxLv}";
            
                for (int i = 0; i < attributeData.Count; i++)
                {
                    attributeInfoArray[i].gameObject.SetActive(true);
                    attributeInfoArray[i].text = $"Lv.{attributeData[i].level} {attributeData[i].talk}";
                    attributeInfoArray[i].color = normalColor;
                }

                //var activeLevel = GameManager.Instance.PlayerSkill.AttributeLv(skillId);
                var activeLevel = 0;
                for (int i = 0; i < activeLevel; i++)
                    attributeInfoArray[i].color = activeColor;
            }
        }
    }

    public void AttributeLvUp()
    {
        //GameManager.Instance.PlayerSkill.AttributeLvUp(id);
    }
    public void AttributeLvDown()
    {
        //GameManager.Instance.PlayerSkill.AttributeLvDown(id);
    }
}
