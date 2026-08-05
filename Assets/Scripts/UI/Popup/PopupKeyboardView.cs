using System;
using UnityEngine;
using UnityEngine.Serialization;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupKeyboardModel
{
    public Action closeAction;
    public PopupCommonActions commonActions;
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupKeyboardView : MonoBehaviour
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
    [SerializeField] private KeySettingFrame potionKey;
    
    [SerializeField] private ExpansionUiObject[] defaultButtons;
    
    private ExpansionUiObject[] _grid; // [leftKey, rightKey, upKey, downKey]

    private PopupKeyboardModel _model;
    private PopupCommonActions     _commonActions;
    private int  _cursor          = 0;
    private bool _isRebinding     = false;
    private int  _rebindDoneFrame = -1;
    private int  _enabledFrame    = -1;

    private void Awake()
    {
        _grid = new ExpansionUiObject[]
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
            potionKey,
            defaultButtons[0],
            defaultButtons[1]
        };
    }

    private void OnEnable()
    {
        _cursor = 0;
        _isRebinding = false;
        _enabledFrame = Time.frameCount;
        RefreshFrameData();
        RefreshCursors();
    }

    private void Update()
    {
        if (_model == null)
            return;

        if (_isRebinding || Time.frameCount == _rebindDoneFrame)
            return;

        // 다른 뷰에서 전환된 프레임에는 입력 무시 (같은 입력이 중복 처리되는 것 방지)
        if (Time.frameCount == _enabledFrame)
            return;

        if (Input.GetKeyDown(GameManager.Instance.upKey))
            HandleArrow(0, -1);
        if (Input.GetKeyDown(GameManager.Instance.downKey))
            HandleArrow(0, +1);
        if (Input.GetKeyDown(GameManager.Instance.leftKey))
            HandleArrow(-1, 0);
        if (Input.GetKeyDown(GameManager.Instance.rightKey))
            HandleArrow(+1, 0);
        if (Input.GetKeyDown(GameManager.Instance.escKey))
            HandleEsc();
    }

    private void OnGUI()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown)
            return;

        // Shift/Ctrl/Alt 단독 입력은 keyCode 가 None 으로 오고 modifiers 에만 표시된다
        KeyCode keyCode = e.keyCode != KeyCode.None ? e.keyCode : GetModifierKeyCode(e);
        if (keyCode == KeyCode.None)
            return;

        // 다른 뷰에서 전환된 프레임에는 입력 무시 (같은 Enter가 중복 처리되는 것 방지)
        if (Time.frameCount == _enabledFrame)
            return;

        if (!_isRebinding)
        {
            if (keyCode == GameManager.Instance.enterKey)
            {
                HandleEnter();
                e.Use();
            }
            return;
        }

        // 대기 중 키 입력 처리
        if (keyCode is KeyCode.Escape or KeyCode.Return)
        {
            _isRebinding     = false;
            _rebindDoneFrame = Time.frameCount;
            RefreshFrameData();
            RefreshCursors();
        }
        else
        {
            ApplyKeyWithSwap(keyCode);
            _isRebinding     = false;
            _rebindDoneFrame = Time.frameCount;
            _commonActions?.PlaySelectSound?.Invoke();
            RefreshCursors();
        }

        e.Use();
    }

    // 수식키 단독 입력을 KeyCode 로 환산한다.
    // IMGUI 는 좌/우를 구분하지 않으므로 왼쪽 키로 바인딩한다(표시도 "Shift" 등으로 통일되어 있다)
    private static KeyCode GetModifierKeyCode(Event e)
    {
        if (e.shift)   return KeyCode.LeftShift;
        if (e.control) return KeyCode.LeftControl;
        if (e.alt)     return KeyCode.LeftAlt;
        return KeyCode.None;
    }

    // 다른 프레임에 같은 키가 있으면 스왑, 없으면 단순 변경
    private void ApplyKeyWithSwap(KeyCode newKeyCode)
    {
        KeyCode prevKeyCode = _grid[_cursor].GetComponent<KeySettingFrame>().CurrentKeyCode;

        for (int i = 0; i < _grid.Length; i++)
        {
            if (i == _cursor) continue;

            // defaultButtons 등 키 프레임이 아닌 항목은 건너뜀
            var frame = _grid[i].GetComponent<KeySettingFrame>();
            if (!frame) continue;

            if (frame.CurrentKeyCode == newKeyCode)
            {
                frame.KeyChange(prevKeyCode);
                break;
            }
        }

        _grid[_cursor].GetComponent<KeySettingFrame>().KeyChange(newKeyCode);
    }

    private void HandleEnter()
    {
        if (_cursor == _grid.Length - 2) // defaultButtons[0]
        {
            GameManager.Instance.SetDefaultKeyboard();
            RefreshFrameData();
            _commonActions?.PlaySelectSound?.Invoke();
            return;
        }

        if (_cursor == _grid.Length - 1) // defaultButtons[1]
        {
            HandleEsc();
            return;
        }

        _isRebinding = true;
        _grid[_cursor].GetComponent<KeySettingFrame>().SetWaiting();
        _commonActions?.PlaySelectSound?.Invoke();
    }

    // dirX: 좌우(-1/+1), dirY: 상하(-1/+1)
    private void HandleArrow(int dirX, int dirY)
    {
        int potionIdx   = _grid.Length - 3; // potionKey
        int defaultIdx0 = _grid.Length - 2; // defaultButtons[0]
        int defaultIdx1 = _grid.Length - 1; // defaultButtons[1]

        if (_cursor == potionIdx)
        {
            if (dirX != 0)
                return;

            // potionKey: ↓ defaultButtons[0] / ↑ 일반 그리드(스킬3)
            _cursor = dirY > 0 ? defaultIdx0 : potionIdx - Cols;
        }
        else if (_cursor == defaultIdx0)
        {
            if (dirX != 0)
                return;

            // defaultButtons[0]: ↓ defaultButtons[1] / ↑ potionKey
            _cursor = dirY > 0 ? defaultIdx1 : potionIdx;
        }
        else if (_cursor == defaultIdx1)
        {
            if (dirX != 0)
                return;

            // defaultButtons[1]: ↓ leftKey로 워프 / ↑ defaultButtons[0]
            _cursor = dirY > 0 ? 0 : defaultIdx0;
        }
        else if (_cursor < Cols && dirY < 0)
        {
            // leftKey/rightKey: ↑ defaultButtons[1]로 워프
            _cursor = defaultIdx1;
        }
        else
        {
            // 키 영역(2열 그리드) 일반 이동
            int rows = (defaultIdx0 + Cols) / Cols; // potionKey 행까지 포함
            int col  = _cursor % Cols;
            int row  = _cursor / Cols;

            col = (col + dirX + Cols) % Cols;
            row = (row + dirY + rows) % rows;

            _cursor = row * Cols + col;
        }

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
        potionKey.SetData(ConstValues.PotionKey, GameManager.Instance.potionKey);
        
        if (defaultButtons.Length > 0)
            defaultButtons[0].SetText(GameManager.Instance.GetTalk(30069));
        if (defaultButtons.Length > 1)
            defaultButtons[1].SetText(GameManager.Instance.GetTalk(30070));
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

    public void SetAction(PopupKeyboardModel model)
    {
        _model         = model;
        _commonActions = model.commonActions;
    }

    private void HandleEsc()
    {
        _model.closeAction?.Invoke();
        _commonActions?.PlayCancelSound?.Invoke();
    }

    // ── 마우스 상호작용 (보류) ── 재활성화 시 아래 주석 해제
    /*
    // 키 프레임과 하단 버튼에 마우스 호버/클릭 연결
    private void SetMouseInteraction()
    {
        _ownerPopup = GetComponentInParent<UIBase>();

        for (int i = 0; i < _grid.Length; i++)
        {
            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(_grid[i],
                onHover: () => MoveCursorTo(index),
                onClick: () => ClickFrame(index));
        }
    }

    // 마우스 호버로 커서 이동 (키보드 커서 이동과 동일한 연출)
    private void MoveCursorTo(int index)
    {
        if (!CanMouseInput() || _isRebinding)
            return;

        if (_cursor == index)
            return;

        _cursor = index;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    // 클릭: 키 프레임은 재바인딩 시작, 하단 버튼은 Enter와 동일
    // 키 입력 대기 중에 다시 클릭하면 대기 취소 (Esc/Enter 취소와 동일)
    private void ClickFrame(int index)
    {
        if (!CanMouseInput())
            return;

        if (_isRebinding)
        {
            _isRebinding     = false;
            _rebindDoneFrame = Time.frameCount;
            RefreshFrameData();
            RefreshCursors();
            return;
        }

        MoveCursorTo(index);
        HandleEnter();
    }

    // 팝업 열림 연출이 끝난 뒤에만 마우스 입력 허용
    private bool CanMouseInput() => _model != null && _ownerPopup && _ownerPopup.OpenComplete;
    */
}
