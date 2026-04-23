using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupKeyboardModel
{
    public Action closeAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupKeyboardView
{
    void SetAction(PopupKeyboardPresenter presenter, PopupCommonActions commonActions);
}

// ── Presenter ─────────────────────────────────────────────────────────────────
public class PopupKeyboardPresenter
{
    private readonly IPopupKeyboardView _view;
    private readonly PopupKeyboardModel _model;

    public PopupKeyboardPresenter(IPopupKeyboardView view, PopupKeyboardModel model)
    {
        _view  = view;
        _model = model;
    }

    public void SetAction() => _view.SetAction(this, _model.commonActions);
    public void HandleEsc()
    {
        _model.closeAction?.Invoke();
        _model.commonActions.PlayCancelSound?.Invoke();
    }
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupKeyboardView : MonoBehaviour, IPopupKeyboardView
{
    [SerializeField] private ExpansionUiObject[] buttons;

    private PopupKeyboardPresenter _presenter;
    private PopupCommonActions     _commonActions;
    private int _cursor = 0;

    private void OnEnable()
    {
        _cursor = 0;
        RefreshCursors();
    }

    private void Update()
    {
        if (_presenter == null)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            HandleArrow(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            HandleArrow(+1);
        if (Input.GetKeyDown(KeyCode.Escape))
            _presenter.HandleEsc();
    }

    private void HandleArrow(int dir)
    {
        if (buttons.Length == 0)
            return;

        _cursor = (_cursor + dir + buttons.Length) % buttons.Length;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void RefreshCursors()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
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

    // IPopupKeyboardView
    public void SetAction(PopupKeyboardPresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
    }
}
