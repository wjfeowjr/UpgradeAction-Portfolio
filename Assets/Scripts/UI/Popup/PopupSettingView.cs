using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupSettingModel
{
    public Action openGameAction;
    public Action openAudioAction;
    public Action openVideoAction;
    public Action openKeyboardAction;
    public Action closeAction;
    public PopupCommonActions commonActions;
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupSettingView : MonoBehaviour
{
    private const int ButtonCount = 5;

    [SerializeField] private ExpansionUiObject[] settingButtons;

    private PopupSettingModel _model;
    private PopupCommonActions    _commonActions;
    private int _cursor = 0;
    private int _enabledFrame = -1;

    private void OnEnable()
    {
        _enabledFrame = Time.frameCount;
        RefreshCursors();
        SetTextGameFrames();
    }

    private void Update()
    {
        if (_model == null)
            return;

        // 다른 뷰에서 전환된 프레임에는 입력 무시 (같은 Enter가 중복 처리되는 것 방지)
        if (Time.frameCount == _enabledFrame)
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
        if (settingButtons.Length > 4)
            settingButtons[4].SetText(GameManager.Instance.GetTalk(30070));
    }

    private void HandleArrow(int dir)
    {
        _cursor = (_cursor + dir + ButtonCount) % ButtonCount;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void HandleEnter()
    {
        switch (_cursor)
        {
            case 0: _model.openGameAction?.Invoke();
                _commonActions?.PlaySelectSound?.Invoke();
                break;
            case 1: _model.openAudioAction?.Invoke();
                _commonActions?.PlaySelectSound?.Invoke();
                break;
            case 2: _model.openVideoAction?.Invoke();
                _commonActions?.PlaySelectSound?.Invoke();
                break;
            case 3: _model.openKeyboardAction?.Invoke();
                _commonActions?.PlaySelectSound?.Invoke();
                break;
            case 4: _model.closeAction?.Invoke();
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

    public void SetAction(PopupSettingModel model)
    {
        _model         = model;
        _commonActions = model.commonActions;
        _cursor        = 0;
        RefreshCursors();
    }

    // ── 마우스 상호작용 (보류) ── 재활성화 시 아래 주석 해제
    /*
    // 선택지 버튼에 마우스 호버/클릭 연결
    private void SetMouseInteraction()
    {
        _ownerPopup = GetComponentInParent<UIBase>();

        for (int i = 0; i < settingButtons.Length; i++)
        {
            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(settingButtons[i],
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

    // 팝업 열림 연출이 끝난 뒤에만 마우스 입력 허용
    private bool CanMouseInput() => _model != null && _ownerPopup && _ownerPopup.OpenComplete;
    */
}
