using System;
using UnityEngine;

// ── Model ────────────────────────────────────────────────────────────────────
public class PopupPauseModel
{
    public Action resumeAction;
    public Action settingAction;
    public Action returnAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupPauseView
{
    void SetAction(PopupPausePresenter presenter, PopupCommonActions commonActions);
}

// ── Presenter ─────────────────────────────────────────────────────────────────
public class PopupPausePresenter
{
    private readonly IPopupPauseView _view;
    private readonly PopupPauseModel _model;

    public PopupPausePresenter(IPopupPauseView view, PopupPauseModel model)
    {
        _view  = view;
        _model = model;
    }

    public void SetAction()
    {
        _view.SetAction(this, _model.commonActions);
    }

    public void ResumeAction()  => _model.resumeAction?.Invoke();
    public void SettingAction() => _model.settingAction?.Invoke();
    public void ReturnAction()  => _model.returnAction?.Invoke();
    public void HandleEsc()     => _model.resumeAction?.Invoke();
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupPauseView : MonoBehaviour, IPopupPauseView
{
    private const int ButtonCount = 3;

    private PopupPausePresenter _presenter;
    private PopupCommonActions  _commonActions;
    private int _cursor = 0;
    private bool _isExecuted = false;
    
    [SerializeField] private ExpansionUiObject resumeButton;
    [SerializeField] private ExpansionUiObject settingButton;
    [SerializeField] private ExpansionUiObject returnButton;
    
    private void Update()
    {
        if (_presenter == null || _isExecuted)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            HandleArrow(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            HandleArrow(+1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            HandleEnter();
        if (Input.GetKeyDown(KeyCode.Escape))
            _presenter.HandleEsc();
    }

    private void HandleArrow(int dir)
    {
        _cursor = (_cursor + dir + ButtonCount) % ButtonCount;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void RefreshCursors()
    {
        var buttons = new ExpansionUiObject[] { resumeButton, settingButton, returnButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (!buttons[i])
                continue;
            
            if (i == _cursor)
            {
                buttons[i].SelectObjectActive(true);
                buttons[i].Expansion(1.1f);
            }
            else
            {
                buttons[i].SelectObjectActive(false);
                buttons[i].Reduction();
            }
        }
    }

    private void HandleEnter()
    {
        _commonActions?.PlaySelectSound?.Invoke();
        switch (_cursor)
        {
            case 0:
                _isExecuted = true;
                _presenter.ResumeAction();
                break;
            case 1:
                _presenter.SettingAction();
                break;
            case 2:
                _isExecuted = true;
                _presenter.ReturnAction();
                break;
        }
    }

    // IPopupPauseView
    public void SetAction(PopupPausePresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
        _cursor        = 0;

        resumeButton.SetText("계속_");
        settingButton.SetText("설정_");
        returnButton.SetText("종료하고 메뉴로 이동_");

        RefreshCursors();
    }
}
