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
        
        skillCoolTime.text = $"쿨타임 : {playerSkill.maxCoolTime[0]}초";

        skillStack.gameObject.SetActive(playerSkill.maxCoolTime.Count > 1);
        if (playerSkill.maxCoolTime.Count > 1)
            skillStack.text = $"충전량 : {playerSkill.maxCoolTime[2]}회";

        skillExplain.text = playerSkill.explainTalk;
    }
}
