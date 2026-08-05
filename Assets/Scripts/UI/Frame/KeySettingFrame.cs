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
                SetText(GameManager.Instance.GetTalk(30035));
                break;
            case ConstValues.RightKey:
                SetText(GameManager.Instance.GetTalk(30036));
                break;
            case ConstValues.UpKey:
                SetText(GameManager.Instance.GetTalk(30037));
                break;
            case ConstValues.DownKey:
                SetText(GameManager.Instance.GetTalk(30038));
                break;
            case ConstValues.MiniMapKey:
                SetText(GameManager.Instance.GetTalk(30039));
                break;
            case ConstValues.CharacterInfoKey:
                SetText(GameManager.Instance.GetTalk(30040));
                break;
            case ConstValues.AttackKey:
                SetText(GameManager.Instance.GetTalk(30041));
                break;
            case ConstValues.JumpKey:
                SetText(GameManager.Instance.GetTalk(30042));
                break;
            case ConstValues.DashKey:
                SetText(GameManager.Instance.GetTalk(30043));
                break;
            case ConstValues.ChangeCharacterKey:
                SetText(GameManager.Instance.GetTalk(30044));
                break;
            case ConstValues.SkillKey1:
                SetText(GameManager.Instance.GetTalk(30045));
                break;
            case ConstValues.SkillKey2:
                SetText(GameManager.Instance.GetTalk(30046));
                break;
            case ConstValues.SkillKey3:
                SetText(GameManager.Instance.GetTalk(30047));
                break;
            case ConstValues.SkillKey4:
                SetText(GameManager.Instance.GetTalk(30048));
                break;
            case ConstValues.PotionKey:
                SetText(GameManager.Instance.GetTalk(30071));
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
            case ConstValues.PotionKey:         GameManager.Instance.potionKey         = changeKeyCode; break;
        }
        // 스킬은 세이브의 keyCode 로 skillId 를 역조회하므로, 바뀐 키를 세이브에도 반영해야 한다
        GameManager.Instance.SyncSkillKeyCode();

        CurrentKeyCode   = changeKeyCode;
        keyText.text     = GameManager.Instance.GetKeyCode(changeKeyCode);
    }
}
