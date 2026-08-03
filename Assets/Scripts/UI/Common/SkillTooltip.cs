using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTooltip : MonoBehaviour
{
    [SerializeField] private Image skillImage;
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private TMP_Text skillCoolTime;
    [SerializeField] private TMP_Text skillStack;
    [SerializeField] private TMP_Text skillArmor;
    [SerializeField] private TMP_Text skillExplain;

    public void SetTooltip(PlayerSkill playerSkill)
    {
        skillImage.sprite = GameManager.Instance.GetAtlasSprite(playerSkill.id);
        skillName.text = playerSkill.talk;
        
        skillCoolTime.text = string.Format(GameManager.Instance.GetTalk(30107), playerSkill.maxCoolTime[0]);
        if(playerSkill.maxCoolTime.Count > 1)
            skillCoolTime.text = string.Format(GameManager.Instance.GetTalk(30107), playerSkill.maxCoolTime[1]);

        skillStack.gameObject.SetActive(playerSkill.maxCoolTime.Count > 1);
        if (playerSkill.maxCoolTime.Count > 1)
            skillStack.text = string.Format(GameManager.Instance.GetTalk(30108), playerSkill.maxCoolTime[2]);

        if (playerSkill.skillArmor == ConstValues.SuperArmor)
            skillArmor.text = GameManager.Instance.GetTalk(30109);
        else
            skillArmor.gameObject.SetActive(false);
        
        var attackData = TableManager.Instance.GetAttack(playerSkill.id);
        if (attackData != null)
        {
            if (string.IsNullOrWhiteSpace(attackData.deBuffTime))
            {
                skillExplain.text = playerSkill.explainTalk;
            }
            else
            {
                var deBuffTimeSplit = attackData.deBuffTime.Split(';');
                skillExplain.text = string.Format(playerSkill.explainTalk, deBuffTimeSplit[0]);
            }
            return;
        }
        var skillData = TableManager.Instance.GetSkill(playerSkill.id);
        if (skillData != null)
        {
            if (string.IsNullOrWhiteSpace(skillData.buffName))
            {
                skillExplain.text = playerSkill.explainTalk;
            }
            else
            {
                List<string> valueList = new List<string>();
                valueList.Add(skillData.buffTime.ToString(CultureInfo.InvariantCulture));
                valueList.Add(skillData.buffCount.ToString(CultureInfo.InvariantCulture));
                var buffValueSplit = skillData.buffValue.Split(';');
                foreach (var buffValue in buffValueSplit)
                    valueList.Add(buffValue);

                object[] finalValue = valueList.ToArray();
                skillExplain.text = string.Format(playerSkill.explainTalk, finalValue);
            }
            return;
        }

        skillExplain.text = "Error!";
    }
}
