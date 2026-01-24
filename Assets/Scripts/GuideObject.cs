using System;
using TMPro;
using UnityEngine;

public enum GuideType
{
    Move,
    Jump,
    Dash,
    Attack,
    DownJump,
}

public class GuideObject : MonoBehaviour
{
    [SerializeField] private GuideType type;
    [SerializeField] private TextMeshPro controlText;
    [SerializeField] private TextMeshPro[] keyTexts;

    private void Awake()
    {
        Setting();
    }

    private void Setting()
    {
        switch (type)
        {
            case GuideType.Move:
                controlText.text = GameManager.Instance.GetTalk(30011);
                keyTexts[0].text = GameManager.Instance.GetKeyCode(GameManager.Instance.leftMoveKey);
                keyTexts[1].text = GameManager.Instance.GetKeyCode(GameManager.Instance.rightMoveKey);
                break;
            
            case GuideType.Jump:
                controlText.text = GameManager.Instance.GetTalk(30012);
                keyTexts[0].text = GameManager.Instance.GetKeyCode(GameManager.Instance.jumpKey);
                break;
            
            case GuideType.Dash:
                controlText.text = GameManager.Instance.GetTalk(30013);
                keyTexts[0].text = GameManager.Instance.GetKeyCode(GameManager.Instance.dashKey);
                break;
            
            case GuideType.Attack:
                controlText.text = GameManager.Instance.GetTalk(30014);
                keyTexts[0].text = GameManager.Instance.GetKeyCode(GameManager.Instance.attackKey);
                break;

            case GuideType.DownJump:
                controlText.text = GameManager.Instance.GetTalk(30015);
                keyTexts[0].text = GameManager.Instance.GetKeyCode(GameManager.Instance.downKey);
                keyTexts[1].text = GameManager.Instance.GetKeyCode(GameManager.Instance.jumpKey);
                break;
        }
    }
}
