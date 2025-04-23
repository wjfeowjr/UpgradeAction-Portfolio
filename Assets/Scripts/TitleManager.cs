using System;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Start()
    {
        ButtonSetting();
    }

    private void ButtonSetting()
    {
        startButton.onClick.AddListener(()=> {GameManager.Instance.GoScene(ConstValues.BattleScene);});
    }
}
