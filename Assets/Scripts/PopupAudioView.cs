using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupAudioModel
{
    public Action closeAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupAudioView
{
    void SetAction(PopupAudioPresenter presenter, PopupCommonActions commonActions);
}

// ── Presenter ─────────────────────────────────────────────────────────────────
public class PopupAudioPresenter
{
    private readonly IPopupAudioView _view;
    private readonly PopupAudioModel _model;

    public PopupAudioPresenter(IPopupAudioView view, PopupAudioModel model)
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
public class PopupAudioView : MonoBehaviour, IPopupAudioView
{
    [SerializeField] private ExpansionUiObject[] buttons;

    private PopupAudioPresenter _presenter;
    private PopupCommonActions  _commonActions;
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

    // IPopupAudioView
    public void SetAction(PopupAudioPresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
    }
}
