using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupMinimapView
{
    void SetMinimapText(string check, string cancel);
    void LimitAction(bool[] boolArray);
}

public class PopupMinimapModel
{
    public string checkString;
    public string closeString;
    public Action moveAction;
    public Action checkAction;
    public Action closeAction;
}

public class PopupMinimapPresenter
{
    private readonly IPopupMinimapView _view;
    private PopupMinimapModel _model;

    public PopupMinimapPresenter(IPopupMinimapView minimapView, PopupMinimapModel model)
    {
        _view = minimapView;
        _model = model;
    }
    
    public void SetMinimapText()
    {
        _view.SetMinimapText(_model.checkString, _model.closeString);
    }

    public void CheckAction()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            _model.checkAction();
    }

    public void MoveAction()
    {
        _model.moveAction();
    }

    public void LimitAction(bool[] limitArray)
    {
        _view.LimitAction(limitArray);
    }
}

public class PopupMinimapView : MonoBehaviour, IPopupMinimapView
{
    private CancellationTokenSource minimapCancellation;
    
    [SerializeField] private TMP_Text checkText;
    [SerializeField] private TMP_Text cancelText;

    [SerializeField] private GameObject miniMapObject;
    [SerializeField] private GameObject miniMapLayout;
    [SerializeField] private GameObject[] arrowArray;

    public void SetMinimapText(string check, string cancel)
    {
        checkText.text = check;
        cancelText.text = cancel;
    }

    public void LimitAction(bool[] boolArray)
    {
        for (int i = 0; i < boolArray.Length; i++)
        {
            arrowArray[i].SetActive(!boolArray[i]);
        }
    }

    // 미니맵 콘텐츠 활성/비활성 (열기/닫기 페이드는 Popup_Minimap에서 처리)
    public void SetMinimapActive(bool active)
    {
        miniMapObject.SetActive(active);
        miniMapLayout.SetActive(active);
    }
}
