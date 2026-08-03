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
    private static readonly string[] LanguageOptions =
    {
        ConstValues.Korean,
        ConstValues.English,
        ConstValues.Japanese,
        ConstValues.ChineseSimplified,
        ConstValues.ChineseTraditional,
        ConstValues.Spanish,
        //ConstValues.Russian,          // 텍스트 길이 문제로 보류
        //ConstValues.PortugueseBrazil, // 보류
    };

    [SerializeField] private ExpansionUiObject[] gameFrames;

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
        if (InputHelper.GetEnterDown() || InputHelper.GetKeypadEnterDown())
            HandleEnter();
        if (Input.GetKeyDown(GameManager.Instance.escKey))
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
                SettingStringBinding.SaveGameSetting(ConstValues.Language, LanguageOptions[langIdx]);
                _languageChangeAction?.Invoke();
                SetTextGameFrames();
                RefreshGameData();
                _commonActions?.PlayMoveSound?.Invoke();
                break;

            case 1: // 카메라 흔들림
                int shaking = ((GameManager.Instance.cameraShaking + dir) % 2 + 2) % 2;
                GameManager.Instance.cameraShaking = shaking;
                SettingIntBinding.SaveGameSetting(ConstValues.CameraShaking, shaking);
                gameFrames[1].GetComponent<GameFrame>().SetData(CameraShakingToText(shaking));
                _commonActions?.PlayMoveSound?.Invoke();
                break;
        }
    }

    private void HandleEnter()
    {
        switch (_cursor)
        {
            case 2:
                GameManager.Instance.SetDefaultGame();
                SetTextGameFrames();
                RefreshGameData();
                _languageChangeAction?.Invoke();
                _commonActions?.PlaySelectSound?.Invoke();
                break;

            case 3:
                _presenter.HandleEsc();
                break;
        }
    }

    private void SetTextGameFrames()
    {
        if (gameFrames.Length > 0)
            gameFrames[0].SetText(GameManager.Instance.GetTalk(30028));
        if (gameFrames.Length > 1)
            gameFrames[1].SetText(GameManager.Instance.GetTalk(30029));
        if (gameFrames.Length > 2)
            gameFrames[2].SetText(GameManager.Instance.GetTalk(30069));
        if (gameFrames.Length > 3)
            gameFrames[3].SetText(GameManager.Instance.GetTalk(30070));
    }

    private void RefreshGameData()
    {
        if (gameFrames.Length > 0)
            gameFrames[0].GetComponent<GameFrame>().SetData(GameManager.Instance.GetTalk(30057));
        if (gameFrames.Length > 1)
            gameFrames[1].GetComponent<GameFrame>().SetData(CameraShakingToText(GameManager.Instance.cameraShaking));
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

    // ── 마우스 상호작용 (보류) ── 재활성화 시 아래 주석 해제
    /*
    // 항목 호버/클릭과 좌우 화살표 클릭(값 순환) 연결
    private void SetMouseInteraction()
    {
        _ownerPopup = GetComponentInParent<UIBase>();

        for (int i = 0; i < gameFrames.Length; i++)
        {
            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(gameFrames[i],
                onHover: () => MoveCursorTo(index),
                onClick: () =>
                {
                    if (!CanMouseInput())
                        return;

                    MoveCursorTo(index);
                    HandleEnter(); // 버튼 항목만 동작 (옵션 항목은 케이스 없음)
                });

            // 옵션 항목의 좌/우 화살표 클릭 → 값 순환
            var frame = gameFrames[i].GetComponent<GameFrame>();
            if (!frame)
                continue;

            MouseSelectable.Attach(frame.LeftArrow,  onHover: null, onClick: () => ClickOption(index, -1));
            MouseSelectable.Attach(frame.RightArrow, onHover: null, onClick: () => ClickOption(index, +1));
        }
    }

    // 화살표 클릭으로 해당 항목의 값 순환
    private void ClickOption(int index, int dir)
    {
        if (!CanMouseInput())
            return;

        MoveCursorTo(index);
        HandleOption(dir);
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

    // 팝업 열림 연출이 끝난 뒤에만 마우스 입력 허용
    private bool CanMouseInput() => _presenter != null && _ownerPopup && _ownerPopup.OpenComplete;
    */
}
