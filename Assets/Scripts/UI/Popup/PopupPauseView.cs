using System;
using Cysharp.Threading.Tasks;
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
    void SetSettingOpen(bool isOpen);
    void SetButtonText();
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

    public void ResumeAction()
    {
        _model.resumeAction?.Invoke();
    }
    public void SettingAction()          => _model.settingAction?.Invoke();
    public void ReturnAction()           => _model.returnAction?.Invoke();

    public void HandleEsc()
    {
        _model.resumeAction?.Invoke();
    }
    public void SetSettingOpen(bool isOpen) => _view.SetSettingOpen(isOpen);
    public void SetButtonText() => _view.SetButtonText();
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupPauseView : MonoBehaviour, IPopupPauseView
{
    private const int ButtonCount = 3;

    private PopupPausePresenter _presenter;
    private PopupCommonActions  _commonActions;
    private int  _cursor      = 0;
    
    [SerializeField] private bool _isSettingOpen = false;
    
    [SerializeField] private ExpansionUiObject resumeButton;
    [SerializeField] private ExpansionUiObject settingButton;
    [SerializeField] private ExpansionUiObject returnButton;


    public bool _IsSettingOpen => _isSettingOpen;
    
    // 입력 처리는 소유 Popup_Pause의 Update에서 openComplete일 때만 호출됨
    public void HandleInput()
    {
        if (_presenter == null || _isSettingOpen)
            return;

        if (Input.GetKeyDown(GameManager.Instance.upKey))
            HandleArrow(-1);
        if (Input.GetKeyDown(GameManager.Instance.downKey))
            HandleArrow(+1);
        if (InputHelper.GetEnterDown() || InputHelper.GetKeypadEnterDown())
            HandleEnter();
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
        switch (_cursor)
        {
            case 0:
                _presenter.ResumeAction();
                break;
            case 1:
                _presenter.SettingAction();
                _commonActions?.PlaySelectSound?.Invoke();
                break;
            case 2:
                _presenter.ReturnAction();
                break;
        }
    }

    // IPopupPauseView
    public void SetSettingOpen(bool isOpen)
    {
        _isSettingOpen = isOpen;
    }

    public void SetButtonText()
    {
        resumeButton.SetText(GameManager.Instance.GetTalk(30049));
        settingButton.SetText(GameManager.Instance.GetTalk(30026));
        returnButton.SetText(GameManager.Instance.GetTalk(30050));
    }

    public void SetAction(PopupPausePresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
        _cursor        = 0;

        SetButtonText();
        RefreshCursors();
    }

    // ── 마우스 상호작용 (보류) ── 재활성화 시 아래 주석 해제
    /*
    // 일시정지 버튼에 마우스 호버/클릭 연결
    private void SetMouseInteraction()
    {
        _ownerPopup = GetComponentInParent<UIBase>();

        var buttons = new ExpansionUiObject[] { resumeButton, settingButton, returnButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (!buttons[i])
                continue;

            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(buttons[i],
                onHover: () => MoveCursorTo(index),
                onClick: () =>
                {
                    if (!CanMouseInput())
                        return;

                    MoveCursorTo(index);
                    HandleEnter();
                });
        }
    }

    // 마우스 호버로 커서 이동 (키보드 커서 이동과 동일한 연출)
    private void MoveCursorTo(int index)
    {
        if (!CanMouseInput())
            return;

        if (_cursor == index)
            return;

        _cursor = index;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    // 팝업 열림 연출이 끝나고 설정 창이 닫혀 있을 때만 마우스 입력 허용
    private bool CanMouseInput() => _presenter != null && !_isSettingOpen && _ownerPopup && _ownerPopup.OpenComplete;
    */
}
