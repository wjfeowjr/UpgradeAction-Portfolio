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
    void CloseAction();
    void CheckerAction(Vector2 checkerPos);
}

public class PopupMinimapModel
{
    public string checkString;
    public string closeString;
    public Action moveAction;
    public Action checkAction;
}

public class PopupMinimapPresenter
{
    private readonly IPopupMinimapView _minimapview;
    private PopupMinimapModel _model;

    public PopupMinimapPresenter(IPopupMinimapView minimapView, PopupMinimapModel model)
    {
        _minimapview = minimapView;
        _model = model;
    }
    
    public void SetMinimapText()
    {
        _minimapview.SetMinimapText(_model.checkString, _model.closeString);
    }

    public void OpenAction()
    {
        _minimapview.OpenAction();
    }

    private void CloseAction()
    {
        _minimapview.CloseAction();
    }

    public void CheckAction()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            _model.checkAction();
    }

    public void CloseMinimap()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
            CloseAction();
    }
    
    public void MoveAction()
    {
        _model.moveAction();
    }

    public void LimitAction(bool[] limitArray)
    {
        _minimapview.LimitAction(limitArray);
    }

    public void SetCheckerPos(Vector2 checkerPos)
    {
        _minimapview.CheckerAction(checkerPos);
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
    [SerializeField] private Texture minimapTexture;

    private bool isClosing;

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
    }

    public void CheckerAction(Vector2 checkerPos)
    {
        
    }
}
