using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonData
{
    public string ButtonText;
    public Action ButtonAction;
}

public class UIButtonView : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void SetText(string text)
    {
        buttonText.text = text;
    }

    public void SetClickAction(Action action)
    {
        Utils.AddClickAction(button, action);
    }

    public void SetData(UIButtonData data)
    {
        SetText(data.ButtonText);
        SetClickAction(data.ButtonAction);
    }
}
