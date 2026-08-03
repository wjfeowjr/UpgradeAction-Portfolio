using System;
using TMPro;
using UnityEngine;

public class ChoiceFrameUI : ExpansionUiObject
{
    [SerializeField] private GameObject selectKeyObject;
    [SerializeField] private TMP_Text selectKeyText;

    private void Awake()
    {
        selectKeyText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.enterKey);
        selectKeyObject.SetActive(false);
    }

    public override void SelectObjectActive(bool active)
    {
        base.SelectObjectActive(active);
    }
}
