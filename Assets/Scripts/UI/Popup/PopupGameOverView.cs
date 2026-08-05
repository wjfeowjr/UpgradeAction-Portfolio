using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupGameOverModel
{
    public string title;
    public string message;
    public Action replayAction;
}

public class PopupGameOverView : MonoBehaviour
{
    private PopupGameOverModel _model;
    private bool _isRestarted;

    public void SetData(PopupGameOverModel model)
    {
        _model = model;
        _isRestarted = false;
        SetModel(_model.title, _model.message);
    }

    // 컨테이너(Popup_GameOver)가 열림 완료 후 매 프레임 호출한다
    public void Restart()
    {
        if (_model == null || _isRestarted)
            return;

        if (Input.GetKeyDown(GameManager.Instance.enterKey))
        {
            _isRestarted = true;
            _model.replayAction?.Invoke();
        }
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject bgObject;
    [SerializeField] private GameObject[] playerObjects;

    public void SetModel(string title, string message)
    {
        titleText.text = title;
        messageText.text = message;
        
        for (int i = 0; i < playerObjects.Length; i++)
            playerObjects[i].SetActive(i < GameManager.Instance.PlayerList.Count);
    }

    public void BgActive(bool active)
    {
        bgObject.SetActive(active);
    }
}
