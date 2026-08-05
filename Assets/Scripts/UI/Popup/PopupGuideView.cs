using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupGuideModel
{
    public string guideTitle;
    public string guideMessage;
    public List<string> imgNameList = new List<string>();
    public Action closeAction;
}

public class PopupGuideView : MonoBehaviour
{
    private PopupGuideModel _model;

    public void SetData(PopupGuideModel model)
    {
        _model = model;
        SetModel(_model.guideTitle, _model.guideMessage, _model.imgNameList);
    }

    // 컨테이너(Popup_Guide)가 열림 완료 후 매 프레임 호출한다
    public void Close()
    {
        if (_model != null && Input.GetKeyDown(GameManager.Instance.escKey))
            _model.closeAction?.Invoke();
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text explainText;
    [SerializeField] private TMP_Text closeText;
    [SerializeField] private Image[] guideImages;

    public void SetModel(string guideTitle, string guideMessage, List<string> imgNameList)
    {
        titleText.text = guideTitle;
        explainText.text = guideMessage;
        closeText.text = string.Format(GameManager.Instance.GetTalk(30102), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey));
        
        for (int i = 0; i < guideImages.Length; i++)
        {
            if (i < imgNameList.Count)
            {
                guideImages[i].transform.parent.parent.gameObject.SetActive(true);
                guideImages[i].sprite = GameManager.Instance.GetAtlasSprite(imgNameList[i]);
            }
            else
            {
                guideImages[i].transform.parent.parent.gameObject.SetActive(false);
            }
        }
    }
}
