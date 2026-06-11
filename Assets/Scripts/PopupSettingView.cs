using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupSettingModel
{
    public Action openGameAction;
    public Action openAudioAction;
    public Action openVideoAction;
    public Action openKeyboardAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupSettingView
{
    void SetAction(PopupSettingPresenter presenter, PopupCommonActions commonActions);
}

// ── Presenter ─────────────────────────────────────────────────────────────────
public class PopupSettingPresenter
{
    private readonly IPopupSettingView _view;
    private readonly PopupSettingModel _model;

    public PopupSettingPresenter(IPopupSettingView view, PopupSettingModel model)
    {
        _view  = view;
        _model = model;
    }

    public void SetAction()    => _view.SetAction(this, _model.commonActions);
    public void OpenGame()     => _model.openGameAction?.Invoke();
    public void OpenAudio()    => _model.openAudioAction?.Invoke();
    public void OpenVideo()    => _model.openVideoAction?.Invoke();
    public void OpenKeyboard() => _model.openKeyboardAction?.Invoke();
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupSettingView : MonoBehaviour, IPopupSettingView
{
    private const int ButtonCount = 4;

    [SerializeField] private ExpansionUiObject[] settingButtons;

    private PopupSettingPresenter _presenter;
    private PopupCommonActions    _commonActions;
    private int _cursor = 0;

    private void OnEnable()
    {
        RefreshCursors();
        SetTextGameFrames();
    }

    private void Update()
    {
        if (_presenter == null)
            return;

        if (Input.GetKeyDown(GameManager.Instance.upKey))
            HandleArrow(-1);
        if (Input.GetKeyDown(GameManager.Instance.downKey))
            HandleArrow(+1);
        if (InputHelper.GetEnterDown() || InputHelper.GetKeypadEnterDown())
            HandleEnter();
    }
    
    private void SetTextGameFrames()
    {
        if (settingButtons.Length > 0)
            settingButtons[0].SetText(GameManager.Instance.GetTalk(30053));
        if (settingButtons.Length > 1)
            settingButtons[1].SetText(GameManager.Instance.GetTalk(30054));
        if (settingButtons.Length > 2)
            settingButtons[2].SetText(GameManager.Instance.GetTalk(30055));
        if (settingButtons.Length > 3)
            settingButtons[3].SetText(GameManager.Instance.GetTalk(30056));
    }

    private void HandleArrow(int dir)
    {
        _cursor = (_cursor + dir + ButtonCount) % ButtonCount;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void HandleEnter()
    {
        _commonActions?.PlaySelectSound?.Invoke();
        switch (_cursor)
        {
            case 0: _presenter.OpenGame();
                break;
            case 1: _presenter.OpenAudio();
                break;
            case 2: _presenter.OpenVideo();
                break;
            case 3: _presenter.OpenKeyboard();
                break;
        }
    }

    private void RefreshCursors()
    {
        for (int i = 0; i < settingButtons.Length; i++)
        {
            if (i == _cursor)
            {
                settingButtons[i].SelectObjectActive(true);
                settingButtons[i].Expansion(1.1f);
            }
            else
            {
                settingButtons[i].SelectObjectActive(false);
                settingButtons[i].Reduction();
            }
        }
    }

    // IPopupSettingView
    public void SetAction(PopupSettingPresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
        _cursor        = 0;
        RefreshCursors();
    }
}
