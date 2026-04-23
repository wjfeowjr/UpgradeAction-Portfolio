using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupGameModel
{
    public Action closeAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupGameView
{
    void SetAction(PopupGamePresenter presenter, PopupCommonActions commonActions);
}

// ── Presenter ─────────────────────────────────────────────────────────────────
public class PopupGamePresenter
{
    private readonly IPopupGameView _view;
    private readonly PopupGameModel _model;

    public PopupGamePresenter(IPopupGameView view, PopupGameModel model)
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
public class PopupGameView : MonoBehaviour, IPopupGameView
{
    [SerializeField] private ExpansionUiObject[] buttons;

    private PopupGamePresenter _presenter;
    private PopupCommonActions _commonActions;
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

        if (Input.GetKeyDown(GameManager.Instance.upKey))
            HandleArrow(-1);
        if (Input.GetKeyDown(GameManager.Instance.downKey))
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

    // IPopupGameView
    public void SetAction(PopupGamePresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
    }
}
