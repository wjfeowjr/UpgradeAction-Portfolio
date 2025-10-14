using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttributeFrame : MonoBehaviour
{
    public bool isHaveSkill;
    [SerializeField] private GameObject haveSkillObject;
    [SerializeField] private GameObject notHaveSkillObject;

    [SerializeField] private Image skillImage;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text nextCostText;
    [SerializeField] private TMP_Text nextCost;
    [SerializeField] private TMP_Text currentLvText;
    [SerializeField] private GameObject costFrame;
    [SerializeField] private TMP_Text[] attributeInfoArray;

    public void SetSkillInfo(string skillId, string targetSkill)
    {
        isHaveSkill = GameManager.Instance.PlayerSkill.IsHaveSkill(skillId);
        haveSkillObject.SetActive(isHaveSkill);
        notHaveSkillObject.SetActive(!isHaveSkill);

        if (isHaveSkill)
        {
            var skillData = TableManager.Instance.skillTable.Skill.Find(x => x.id == skillId);
            var attributeData = TableManager.Instance.skillAttributeTable.SkillAttribute.FindAll(x => x.id == skillId);
            int attributeLv = 0;

            switch (skillData.caster)
            {
                case ConstValues.Berserker:
                    attributeLv = GameManager.Instance.PlayerSkill.BerserkerAttributeLv(skillId);
                    break;
            
                case ConstValues.Gunner:
                    attributeLv = GameManager.Instance.PlayerSkill.BerserkerAttributeLv(skillId);
                    break;
            }

            skillImage.sprite = GameManager.Instance.GetAtlasSprite(skillData.id);
            string test = skillId == targetSkill ? "v" : "";
            skillNameText.text = $"{skillData.name}{test}";
        
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
                }
            }
        }
    }
}
