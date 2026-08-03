using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupVideoModel
{
    public Action closeAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupVideoView
{
    void SetAction(PopupVideoPresenter presenter, PopupCommonActions commonActions);
}

// ── Presenter ─────────────────────────────────────────────────────────────────
public class PopupVideoPresenter
{
    private readonly IPopupVideoView _view;
    private readonly PopupVideoModel _model;

    public PopupVideoPresenter(IPopupVideoView view, PopupVideoModel model)
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
public class PopupVideoView : MonoBehaviour, IPopupVideoView
{
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    private PopupVideoPresenter presenter;

    public PopupVideoPresenter Bind(PopupVideoModel model)
    {
        presenter = new PopupVideoPresenter(this, model);
        return presenter;
    }

    // 지원 해상도 목록 (16:9, 4:3 두 종류)
    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(640,  480),  // 4:3
        new Vector2Int(800,  600),  // 4:3
        new Vector2Int(1024, 768),  // 4:3
        new Vector2Int(1280, 720),  // 16:9
        new Vector2Int(1280, 960),  // 4:3
        new Vector2Int(1600, 900),  // 16:9
        new Vector2Int(1600, 1200), // 4:3
        new Vector2Int(1920, 1080), // 16:9
        new Vector2Int(2560, 1440), // 16:9
        new Vector2Int(3840, 2160), // 16:9
    };

    [SerializeField] private ExpansionUiObject[] videoFrames;

    private PopupVideoPresenter _presenter;
    private PopupCommonActions  _commonActions;
    private int _cursor = 0;
    private int _lastFullScreen;

    private void OnEnable()
    {
        _cursor = 0;
        _lastFullScreen = GameManager.Instance.fullScreen;
        SetTextVideoFrames();
        RefreshVideoData();
        RefreshCursors();
    }

    private void Update()
    {
        if (_presenter == null)
            return;

        // Alt+Enter 토글 등 외부에서 전체화면 상태가 바뀐 경우 표시 갱신
        if (_lastFullScreen != GameManager.Instance.fullScreen)
        {
            _lastFullScreen = GameManager.Instance.fullScreen;
            RefreshVideoData();
        }

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

    private void HandleEnter()
    {
        switch (_cursor)
        {
            case 3:
                GameManager.Instance.SetDefaultVideo();
                Vector2Int resolution = new Vector2Int(GameManager.Instance.resolutionX, GameManager.Instance.resolutionY);
                ApplyResolution(resolution);
                ApplyFullScreen(GameManager.Instance.fullScreen);
                ApplyVSync(GameManager.Instance.vSync);
                RefreshVideoData();
                _commonActions?.PlaySelectSound?.Invoke();
                break;

            case 4:
                _presenter.HandleEsc();
                break;
        }
    }

    private void HandleArrow(int dir)
    {
        if (videoFrames.Length == 0)
            return;

        _cursor = (_cursor + dir + videoFrames.Length) % videoFrames.Length;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void HandleOption(int dir)
    {
        if (videoFrames.Length == 0)
            return;

        switch (_cursor)
        {
            case 0: // 해상도
                int resIdx = FindResolutionIndex();
                resIdx = (resIdx + dir + Resolutions.Length) % Resolutions.Length;
                ApplyResolution(Resolutions[resIdx]);
                _commonActions?.PlayMoveSound?.Invoke();
                break;

            case 1: // 전체화면
                int fullScreen = ((GameManager.Instance.fullScreen + dir) % 2 + 2) % 2;
                ApplyFullScreen(fullScreen);
                _commonActions?.PlayMoveSound?.Invoke();
                break;

            case 2: // 수직 동기화
                int vSync = ((GameManager.Instance.vSync + dir) % 2 + 2) % 2;
                ApplyVSync(vSync);
                _commonActions?.PlayMoveSound?.Invoke();
                break;
        }
    }

    // 현재 GameManager 해상도와 일치하는 목록 인덱스 반환
    private int FindResolutionIndex()
    {
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (Resolutions[i].x == GameManager.Instance.resolutionX && Resolutions[i].y == GameManager.Instance.resolutionY)
                return i;
        }

        return 0;
    }

    // 저장된 해상도가 모니터보다 크면 모니터에 맞는 가장 큰 해상도로 보정 (게임 시작 시 사용)
    public static Vector2Int ClampToDisplay(int width, int height)
    {
        int maxWidth  = Display.main.systemWidth;
        int maxHeight = Display.main.systemHeight;

        if (width <= maxWidth && height <= maxHeight)
            return new Vector2Int(width, height);

        for (int i = Resolutions.Length - 1; i >= 0; i--)
        {
            if (Resolutions[i].x <= maxWidth && Resolutions[i].y <= maxHeight)
                return Resolutions[i];
        }

        return new Vector2Int(maxWidth, maxHeight);
    }

    // 해상도 즉시 적용 + 저장 + 표시 갱신
    private void ApplyResolution(Vector2Int resolution)
    {
        GameManager.Instance.resolutionX = resolution.x;
        GameManager.Instance.resolutionY = resolution.y;
        SettingIntBinding.SaveGameSetting(ConstValues.ResolutionX, resolution.x);
        SettingIntBinding.SaveGameSetting(ConstValues.ResolutionY, resolution.y);

        // 선택한 해상도가 모니터보다 크면 모니터가 수용하는 가장 큰 해상도로 보정해서 적용
        Vector2Int applied = ClampToDisplay(resolution.x, resolution.y);
        Screen.SetResolution(applied.x, applied.y, ToFullScreenMode(GameManager.Instance.fullScreen));

        if (videoFrames.Length > 0)
            videoFrames[0].GetComponent<GameFrame>().SetData($"{resolution.x} X {resolution.y}");
    }

    // 전체화면 즉시 적용 + 저장 + 표시 갱신
    // SetResolution으로 창을 다시 만들어야 창모드 복귀 시 크기 조절 핸들이 정상 복원된다
    private void ApplyFullScreen(int fullScreen)
    {
        GameManager.Instance.fullScreen = fullScreen;
        SettingIntBinding.SaveGameSetting(ConstValues.FullScreen, fullScreen);

        Vector2Int applied = ClampToDisplay(GameManager.Instance.resolutionX, GameManager.Instance.resolutionY);
        Screen.SetResolution(applied.x, applied.y, ToFullScreenMode(fullScreen));

        if (videoFrames.Length > 1)
            videoFrames[1].GetComponent<GameFrame>().SetData(OnOffToText(fullScreen));
    }

    // 수직 동기화 즉시 적용 + 저장 + 표시 갱신
    private void ApplyVSync(int vSync)
    {
        GameManager.Instance.vSync = vSync;
        SettingIntBinding.SaveGameSetting(ConstValues.Vsync, vSync);
        QualitySettings.vSyncCount = vSync;

        if (videoFrames.Length > 2)
            videoFrames[2].GetComponent<GameFrame>().SetData(OnOffToText(vSync));
    }

    private static FullScreenMode ToFullScreenMode(int fullScreen) => fullScreen == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

    private static string OnOffToText(int value) => value == 0 ? GameManager.Instance.GetTalk(30033) : GameManager.Instance.GetTalk(30034);

    private void SetTextVideoFrames()
    {
        if (videoFrames.Length > 0)
            videoFrames[0].SetText(GameManager.Instance.GetTalk(30066));
        if (videoFrames.Length > 1)
            videoFrames[1].SetText(GameManager.Instance.GetTalk(30067));
        if (videoFrames.Length > 2)
            videoFrames[2].SetText(GameManager.Instance.GetTalk(30068));
        if (videoFrames.Length > 3)
            videoFrames[3].SetText(GameManager.Instance.GetTalk(30069));
        if (videoFrames.Length > 4)
            videoFrames[4].SetText(GameManager.Instance.GetTalk(30070));
    }

    private void RefreshVideoData()
    {
        if (videoFrames.Length > 0)
            videoFrames[0].GetComponent<GameFrame>().SetData($"{GameManager.Instance.resolutionX} X {GameManager.Instance.resolutionY}");
        if (videoFrames.Length > 1)
            videoFrames[1].GetComponent<GameFrame>().SetData(OnOffToText(GameManager.Instance.fullScreen));
        if (videoFrames.Length > 2)
            videoFrames[2].GetComponent<GameFrame>().SetData(OnOffToText(GameManager.Instance.vSync));
    }

    private void RefreshCursors()
    {
        for (int i = 0; i < videoFrames.Length; i++)
        {
            if (i == _cursor)
            {
                videoFrames[i].SelectObjectActive(true);
                videoFrames[i].Expansion(1.1f);
            }
            else
            {
                videoFrames[i].SelectObjectActive(false);
                videoFrames[i].Reduction();
            }
        }
    }

    // IPopupVideoView
    public void SetAction(PopupVideoPresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
    }

    // ── 마우스 상호작용 (보류) ── 재활성화 시 아래 주석 해제
    /*
    // 항목 호버/클릭과 좌우 화살표 클릭(값 순환) 연결
    private void SetMouseInteraction()
    {
        _ownerPopup = GetComponentInParent<UIBase>();

        for (int i = 0; i < videoFrames.Length; i++)
        {
            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(videoFrames[i],
                onHover: () => MoveCursorTo(index),
                onClick: () =>
                {
                    if (!CanMouseInput())
                        return;

                    MoveCursorTo(index);
                    HandleEnter(); // 버튼 항목만 동작 (옵션 항목은 케이스 없음)
                });

            // 옵션 항목의 좌/우 화살표 클릭 → 값 순환
            var frame = videoFrames[i].GetComponent<GameFrame>();
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
