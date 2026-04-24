using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupGameModel
{
    public Action closeAction;
    public Action languageChangeAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupGameView
{
    void SetAction(PopupGamePresenter presenter, PopupCommonActions commonActions, Action languageChangeAction);
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

    public void SetAction() => _view.SetAction(this, _model.commonActions, _model.languageChangeAction);
    public void HandleEsc()
    {
        _model.closeAction?.Invoke();
        _model.commonActions.PlayCancelSound?.Invoke();
    }
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupGameView : MonoBehaviour, IPopupGameView
{
    private static readonly string[] LanguageOptions = { ConstValues.Korean, ConstValues.English };

    [SerializeField] private GameFrame[] gameFrames;

    private PopupGamePresenter _presenter;
    private PopupCommonActions _commonActions;
    private Action             _languageChangeAction;
    private int _cursor = 0;

    private void OnEnable()
    {
        _cursor = 0;
        SetTextGameFrames();
        RefreshGameData();
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
        if (Input.GetKeyDown(GameManager.Instance.leftKey))
            HandleOption(-1);
        if (Input.GetKeyDown(GameManager.Instance.rightKey))
            HandleOption(+1);
        if (Input.GetKeyDown(KeyCode.Escape))
            _presenter.HandleEsc();
    }

    private void HandleArrow(int dir)
    {
        if (gameFrames.Length == 0)
            return;

        _cursor = (_cursor + dir + gameFrames.Length) % gameFrames.Length;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void HandleOption(int dir)
    {
        if (gameFrames.Length == 0)
            return;

        switch (_cursor)
        {
            case 0: // 언어
                int langIdx = Array.IndexOf(LanguageOptions, GameManager.Instance.language);
                if (langIdx < 0) langIdx = 0;
                langIdx = (langIdx + dir + LanguageOptions.Length) % LanguageOptions.Length;
                GameManager.Instance.language = LanguageOptions[langIdx];
                SettingStringBinding.SaveSetting(ConstValues.Language, LanguageOptions[langIdx]);
                gameFrames[0].SetData(GameManager.Instance.language);
                _languageChangeAction?.Invoke();
                SetTextGameFrames();
                RefreshGameData();
                break;

            case 1: // 카메라 흔들림
                int shaking = ((GameManager.Instance.cameraShaking + dir) % 2 + 2) % 2;
                GameManager.Instance.cameraShaking = shaking;
                SettingIntBinding.SaveSetting(ConstValues.CameraShaking, shaking);
                gameFrames[1].SetData(CameraShakingToText(shaking));
                break;
        }

        _commonActions?.PlayMoveSound?.Invoke();
    }

    private void SetTextGameFrames()
    {
        if (gameFrames.Length > 0)
            gameFrames[0].SetText(GameManager.Instance.GetTalk(30028));
        if (gameFrames.Length > 1)
            gameFrames[1].SetText(GameManager.Instance.GetTalk(30029));
    }

    private void RefreshGameData()
    {
        if (gameFrames.Length > 0)
            gameFrames[0].SetData(GameManager.Instance.GetTalk(30056));
        if (gameFrames.Length > 1)
            gameFrames[1].SetData(CameraShakingToText(GameManager.Instance.cameraShaking));
    }

    private static string CameraShakingToText(int value) => value == 0 ? GameManager.Instance.GetTalk(30033) : GameManager.Instance.GetTalk(30034);

    private void RefreshCursors()
    {
        for (int i = 0; i < gameFrames.Length; i++)
        {
            if (i == _cursor)
            {
                gameFrames[i].SelectObjectActive(true);
                gameFrames[i].Expansion(1.1f);
            }
            else
            {
                gameFrames[i].SelectObjectActive(false);
                gameFrames[i].Reduction();
            }
        }
    }

    // IPopupGameView
    public void SetAction(PopupGamePresenter presenter, PopupCommonActions commonActions, Action languageChangeAction)
    {
        _presenter            = presenter;
        _commonActions        = commonActions;
        _languageChangeAction = languageChangeAction;
    }
}
