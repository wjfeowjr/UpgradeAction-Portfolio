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

    [SerializeField] private GameFrame[] videoFrames;

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
        if (Input.GetKeyDown(KeyCode.Escape))
            _presenter.HandleEsc();
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
                break;

            case 1: // 전체화면
                int fullScreen = ((GameManager.Instance.fullScreen + dir) % 2 + 2) % 2;
                ApplyFullScreen(fullScreen);
                break;

            case 2: // 수직 동기화
                int vSync = ((GameManager.Instance.vSync + dir) % 2 + 2) % 2;
                ApplyVSync(vSync);
                break;
        }

        _commonActions?.PlayMoveSound?.Invoke();
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
        SettingIntBinding.SaveSetting(ConstValues.ResolutionX, resolution.x);
        SettingIntBinding.SaveSetting(ConstValues.ResolutionY, resolution.y);

        // 선택한 해상도가 모니터보다 크면 모니터가 수용하는 가장 큰 해상도로 보정해서 적용
        Vector2Int applied = ClampToDisplay(resolution.x, resolution.y);
        Screen.SetResolution(applied.x, applied.y, ToFullScreenMode(GameManager.Instance.fullScreen));

        if (videoFrames.Length > 0)
            videoFrames[0].SetData($"{resolution.x} X {resolution.y}");
    }

    // 전체화면 즉시 적용 + 저장 + 표시 갱신
    // SetResolution으로 창을 다시 만들어야 창모드 복귀 시 크기 조절 핸들이 정상 복원된다
    private void ApplyFullScreen(int fullScreen)
    {
        GameManager.Instance.fullScreen = fullScreen;
        SettingIntBinding.SaveSetting(ConstValues.FullScreen, fullScreen);

        Vector2Int applied = ClampToDisplay(GameManager.Instance.resolutionX, GameManager.Instance.resolutionY);
        Screen.SetResolution(applied.x, applied.y, ToFullScreenMode(fullScreen));

        if (videoFrames.Length > 1)
            videoFrames[1].SetData(OnOffToText(fullScreen));
    }

    // 수직 동기화 즉시 적용 + 저장 + 표시 갱신
    private void ApplyVSync(int vSync)
    {
        GameManager.Instance.vSync = vSync;
        SettingIntBinding.SaveSetting(ConstValues.Vsync, vSync);
        QualitySettings.vSyncCount = vSync;

        if (videoFrames.Length > 2)
            videoFrames[2].SetData(OnOffToText(vSync));
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
    }

    private void RefreshVideoData()
    {
        if (videoFrames.Length > 0)
            videoFrames[0].SetData($"{GameManager.Instance.resolutionX} X {GameManager.Instance.resolutionY}");
        if (videoFrames.Length > 1)
            videoFrames[1].SetData(OnOffToText(GameManager.Instance.fullScreen));
        if (videoFrames.Length > 2)
            videoFrames[2].SetData(OnOffToText(GameManager.Instance.vSync));
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
}
