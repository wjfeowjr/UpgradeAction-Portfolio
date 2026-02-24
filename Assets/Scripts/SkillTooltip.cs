using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTooltip : MonoBehaviour
{
    [SerializeField] private Image skillImage;
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private TMP_Text skillCoolTime;
    [SerializeField] private TMP_Text skillStack;
    [SerializeField] private TMP_Text skillExplain;

    public void SetTooltip(PlayerSkill playerSkill)
    {
        skillImage.sprite = GameManager.Instance.GetAtlasSprite(playerSkill.id);
        skillName.text = playerSkill.talk;
        
        skillCoolTime.text = string.Format(GameManager.Instance.GetTalk(30107), playerSkill.maxCoolTime[0]);

        skillStack.gameObject.SetActive(playerSkill.maxCoolTime.Count > 1);
        if (playerSkill.maxCoolTime.Count > 1)
            skillStack.text = string.Format(GameManager.Instance.GetTalk(30108), playerSkill.maxCoolTime[2]);

        var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == playerSkill.id);
        if (attackData == null)
        {
            skillExplain.text = "Error!";
            return;
        }
        
        if (attackData.deBuffTime == ConstValues.None)
        {
            skillExplain.text = playerSkill.explainTalk;
        }
        else
        {
            var deBuffTimeSplit = attackData.deBuffTime.Split(';');
            skillExplain.text = string.Format(playerSkill.explainTalk, deBuffTimeSplit[0]);
        }
    }
}
