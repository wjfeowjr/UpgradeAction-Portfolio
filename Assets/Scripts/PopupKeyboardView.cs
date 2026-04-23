using System;
using UnityEngine;
using UnityEngine.Serialization;

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
    private const int Cols = 2;

    [SerializeField] private KeySettingFrame leftKey;
    [SerializeField] private KeySettingFrame rightKey;
    [SerializeField] private KeySettingFrame upKey;
    [SerializeField] private KeySettingFrame downKey;
    [SerializeField] private KeySettingFrame miniMapKey;
    [SerializeField] private KeySettingFrame characterInfoKey;
    [SerializeField] private KeySettingFrame attackKey;
    [SerializeField] private KeySettingFrame jumpKey;
    [SerializeField] private KeySettingFrame dashKey;
    [SerializeField] private KeySettingFrame changeCharacterKey;
    [SerializeField] private KeySettingFrame skillKey1;
    [SerializeField] private KeySettingFrame skillKey2;
    [SerializeField] private KeySettingFrame skillKey3;
    [SerializeField] private KeySettingFrame skillKey4;
    
    private KeySettingFrame[] _grid; // [leftKey, rightKey, upKey, downKey]

    private PopupKeyboardPresenter _presenter;
    private PopupCommonActions     _commonActions;
    private int  _cursor          = 0;
    private bool _isRebinding     = false;
    private int  _rebindDoneFrame = -1;

    private void Awake()
    {
        _grid = new KeySettingFrame[]
        {
            leftKey, 
            rightKey, 
            upKey, 
            downKey,
            miniMapKey,
            characterInfoKey,
            attackKey,
            jumpKey,
            dashKey,
            changeCharacterKey,
            skillKey1,
            skillKey2,
            skillKey3,
            skillKey4,
        };
    }

    private void OnEnable()
    {
        _cursor = 0;
        _isRebinding = false;
        RefreshFrameData();
        RefreshCursors();
    }

    private void Update()
    {
        if (_presenter == null)
            return;

        if (_isRebinding || Time.frameCount == _rebindDoneFrame)
            return;

        if (Input.GetKeyDown(GameManager.Instance.upKey))
            HandleArrow(0, -1);
        if (Input.GetKeyDown(GameManager.Instance.downKey))
            HandleArrow(0, +1);
        if (Input.GetKeyDown(GameManager.Instance.leftKey))
            HandleArrow(-1, 0);
        if (Input.GetKeyDown(GameManager.Instance.rightKey))
            HandleArrow(+1, 0);
        if (Input.GetKeyDown(KeyCode.Escape))
            _presenter.HandleEsc();
    }

    private void OnGUI()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown || e.keyCode == KeyCode.None)
            return;

        if (!_isRebinding)
        {
            if (e.keyCode == KeyCode.Return)
            {
                HandleEnter();
                e.Use();
            }
            return;
        }

        // 대기 중 키 입력 처리
        if (e.keyCode is KeyCode.Escape or KeyCode.Return)
        {
            _isRebinding     = false;
            _rebindDoneFrame = Time.frameCount;
            RefreshFrameData();
            RefreshCursors();
        }
        else
        {
            ApplyKeyWithSwap(e.keyCode);
            _isRebinding     = false;
            _rebindDoneFrame = Time.frameCount;
            _commonActions?.PlaySelectSound?.Invoke();
            RefreshCursors();
        }

        e.Use();
    }

    // 다른 프레임에 같은 키가 있으면 스왑, 없으면 단순 변경
    private void ApplyKeyWithSwap(KeyCode newKeyCode)
    {
        KeyCode prevKeyCode = _grid[_cursor].CurrentKeyCode;

        for (int i = 0; i < _grid.Length; i++)
        {
            if (i == _cursor) continue;
            if (_grid[i].CurrentKeyCode == newKeyCode)
            {
                _grid[i].KeyChange(prevKeyCode);
                break;
            }
        }

        _grid[_cursor].KeyChange(newKeyCode);
    }

    private void HandleEnter()
    {
        _isRebinding = true;
        _grid[_cursor].SetWaiting();
        _commonActions?.PlaySelectSound?.Invoke();
    }

    // dirX: 좌우(-1/+1), dirY: 상하(-1/+1)
    private void HandleArrow(int dirX, int dirY)
    {
        int rows = _grid.Length / Cols;
        int col  = _cursor % Cols;
        int row  = _cursor / Cols;

        col = (col + dirX + Cols) % Cols;
        row = (row + dirY + rows) % rows;

        _cursor = row * Cols + col;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void RefreshFrameData()
    {
        leftKey.SetData(ConstValues.LeftKey, GameManager.Instance.leftKey);
        rightKey.SetData(ConstValues.RightKey, GameManager.Instance.rightKey);
        upKey.SetData(ConstValues.UpKey, GameManager.Instance.upKey);
        downKey.SetData(ConstValues.DownKey, GameManager.Instance.downKey);
        
        miniMapKey.SetData(ConstValues.MiniMapKey, GameManager.Instance.miniMapKey);
        characterInfoKey.SetData(ConstValues.CharacterInfoKey, GameManager.Instance.characterInfoKey);
        attackKey.SetData(ConstValues.AttackKey, GameManager.Instance.attackKey);
        jumpKey.SetData(ConstValues.JumpKey, GameManager.Instance.jumpKey);
        dashKey.SetData(ConstValues.DashKey, GameManager.Instance.dashKey);
        changeCharacterKey.SetData(ConstValues.ChangeCharacterKey, GameManager.Instance.changeCharacterKey);
        skillKey1.SetData(ConstValues.SkillKey1, GameManager.Instance.skillKey1);
        skillKey2.SetData(ConstValues.SkillKey2, GameManager.Instance.skillKey2);
        skillKey3.SetData(ConstValues.SkillKey3, GameManager.Instance.skillKey3);
        skillKey4.SetData(ConstValues.SkillKey4, GameManager.Instance.skillKey4);
    }

    private void RefreshCursors()
    {
        for (int i = 0; i < _grid.Length; i++)
        {
            if (i == _cursor)
            {
                _grid[i].SelectObjectActive(true);
                _grid[i].Expansion(1.1f);
            }
            else
            {
                _grid[i].SelectObjectActive(false);
                _grid[i].Reduction();
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
