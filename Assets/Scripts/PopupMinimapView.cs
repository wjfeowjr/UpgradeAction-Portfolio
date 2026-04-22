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
    void OpenAction();
    void SetAction(Action closeAction);
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

    public void OpenAction()
    {
        _view.OpenAction();
    }

    public void SetAction()
    {
        _view.SetAction(_model.closeAction);
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
    
    [SerializeField] private FadeSystem fadeSystem;

    private Action closeAction;

    private bool isClosing;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
            CloseAction();
    }

    public void SetMinimapText(string check, string cancel)
    {
        checkText.text = check;
        cancelText.text = cancel;
    }

    public void SetAction(Action close)
    {
        closeAction = close;
    }

    public void LimitAction(bool[] boolArray)
    {
        for (int i = 0; i < boolArray.Length; i++)
        {
            arrowArray[i].SetActive(!boolArray[i]);
        }
    }

    public async void OpenAction()
    {
        Time.timeScale = 0;
        miniMapObject.SetActive(true);
        miniMapLayout.SetActive(true);
        isClosing = false;
        
        // 플레이어 위치
        fadeSystem.SetParameter(1, 0, 0.25f, false);
        await fadeSystem.Fade();
    }
    
    public async void CloseAction()
    {
        if (isClosing)
            return;
        
        isClosing = true;
        miniMapObject.SetActive(false);
        miniMapLayout.SetActive(false);
        
        // 플레이어 위치
        fadeSystem.SetParameter(1, 0, 0.25f, false);
        await fadeSystem.Fade();
        gameObject.SetActive(false);
        Time.timeScale = 1.0f;
        closeAction?.Invoke();
    }
}
