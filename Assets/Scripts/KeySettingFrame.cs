using TMPro;
using UnityEngine;

public class KeySettingFrame : ExpansionUiObject
{
    [SerializeField] private string myKeyCode;
    [SerializeField] private TMP_Text keyText;

    public KeyCode CurrentKeyCode { get; private set; }

    public void SetData(string actionKey, KeyCode actionKeyCode)
    {
        myKeyCode       = actionKey;
        CurrentKeyCode  = actionKeyCode;
        switch (actionKey)
        {
            case ConstValues.LeftKey:
                SetText("왼쪽_");
                break;
            case ConstValues.RightKey:
                SetText("오른쪽_");
                break;
            case ConstValues.UpKey:
                SetText("위쪽_");
                break;
            case ConstValues.DownKey:
                SetText("아래쪽_");
                break;
            case ConstValues.MiniMapKey:
                SetText("지도_");
                break;
            case ConstValues.CharacterInfoKey:
                SetText("캐릭터 정보_");
                break;
            case ConstValues.AttackKey:
                SetText("공격_");
                break;
            case ConstValues.JumpKey:
                SetText("점프_");
                break;
            case ConstValues.DashKey:
                SetText("대시_");
                break;
            case ConstValues.ChangeCharacterKey:
                SetText("캐릭터 교체_");
                break;
            case ConstValues.SkillKey1:
                SetText("스킬1_");
                break;
            case ConstValues.SkillKey2:
                SetText("스킬2_");
                break;
            case ConstValues.SkillKey3:
                SetText("스킬3_");
                break;
            case ConstValues.SkillKey4:
                SetText("스킬4_");
                break;
        }
        keyText.text = GameManager.Instance.GetKeyCode(actionKeyCode);
    }

    public void SetWaiting()
    {
        keyText.text = "...";
    }

    public void KeyChange(KeyCode changeKeyCode)
    {
        KeyBinding.SaveKey(myKeyCode, changeKeyCode);

        switch (myKeyCode)
        {
            case ConstValues.LeftKey:           GameManager.Instance.leftKey           = changeKeyCode; break;
            case ConstValues.RightKey:          GameManager.Instance.rightKey          = changeKeyCode; break;
            case ConstValues.UpKey:             GameManager.Instance.upKey             = changeKeyCode; break;
            case ConstValues.DownKey:           GameManager.Instance.downKey           = changeKeyCode; break;
            case ConstValues.MiniMapKey:        GameManager.Instance.miniMapKey        = changeKeyCode; break;
            case ConstValues.CharacterInfoKey:  GameManager.Instance.characterInfoKey  = changeKeyCode; break;
            case ConstValues.AttackKey:         GameManager.Instance.attackKey         = changeKeyCode; break;
            case ConstValues.JumpKey:           GameManager.Instance.jumpKey           = changeKeyCode; break;
            case ConstValues.DashKey:           GameManager.Instance.dashKey           = changeKeyCode; break;
            case ConstValues.ChangeCharacterKey:GameManager.Instance.changeCharacterKey= changeKeyCode; break;
            case ConstValues.SkillKey1:         GameManager.Instance.skillKey1         = changeKeyCode; break;
            case ConstValues.SkillKey2:         GameManager.Instance.skillKey2         = changeKeyCode; break;
            case ConstValues.SkillKey3:         GameManager.Instance.skillKey3         = changeKeyCode; break;
            case ConstValues.SkillKey4:         GameManager.Instance.skillKey4         = changeKeyCode; break;
        }
        CurrentKeyCode   = changeKeyCode;
        keyText.text     = GameManager.Instance.GetKeyCode(changeKeyCode);
    }
}
