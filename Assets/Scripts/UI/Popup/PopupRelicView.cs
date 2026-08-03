using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public interface IPopupRelicView
{
    void SetModel(string playerId, List<PlayerInfo> playerInfo);
    void SetPlayerInfo();
    void SetAction(PopupCommonActions commonActions, Action closeAction, Action<int> changeCharAction);
}

public class PopupRelicModel
{
    public string playerId;
    public List<PlayerInfo> playerInfoList;
    public PopupCommonActions commonActions;
    public Action closeAction;
    public Action<int> changeCharAction; // dir: +1(E) / -1(Q)
}

public class PopupRelicPresenter
{
    private readonly IPopupRelicView _view;
    private readonly PopupRelicModel _model;

    public PopupRelicPresenter(IPopupRelicView view, PopupRelicModel model)
    {
        _view = view;
        _model = model;
    }

    public void UpdatePlayerInfo(string newId)
    {
        _model.playerId = newId;
        _view.SetModel(newId, _model.playerInfoList);
        _view.SetAction(_model.commonActions, _model.closeAction, _model.changeCharAction);
        _view.SetPlayerInfo();
    }
}

public class PopupRelicView : MonoBehaviour, IPopupRelicView
{
    // ── 외부 주입 ──────────────────────────────────────
    private PopupCommonActions _actions;
    private Action _closeAction;
    private Action<int> _changeCharAction;

    private List<PlayerInfo> _playerInfoList = new List<PlayerInfo>();
    [SerializeField] private string _curPlayerId;
    [SerializeField] private string _curRelicId;

    [SerializeField] private TMP_Text relicNameText;
    [SerializeField] private TMP_Text relicExplainText;
    [SerializeField] private TextUiObject[] relicValueFrames;
    
    [SerializeField] private RelicFrame[] relicFrames;        // 유물 선택 그리드
    [SerializeField] private RelicFrame[] equippedRelicFrames; // 장착 슬롯

    // ── 내부 상태 ──────────────────────────────────────
    // zone: "grid" = 유물 그리드, "slot" = 장착 슬롯
    private enum Zone { Grid, Slot }
    private Zone _zone = Zone.Grid;

    private int _gridCols = 4;
    private int _gridRows = 3;
    [SerializeField] private int _slotCount;   // equippedRelicFrames.Length
    [SerializeField] private int _relicCount;  // 실제 보유 유물 수

    [SerializeField] private int _gridCursor = 0;  // 그리드 내 커서 인덱스
    [SerializeField] private int _slotCursor = 0;  // 슬롯 내 커서 인덱스

    // HTML 로직 대응
    // selectedRelic : 그리드에서 Enter로 선택된 유물 인덱스 (-1 = 없음)
    // pendingSlot   : 슬롯에서 빈칸 Enter 후 지정된 슬롯 인덱스 (-1 = 없음)
    // stolenFromChar: 다른 캐릭터에게서 뺐은 경우 그 playerId ("" = 없음)
    // stolenRelicId : 뺐은 유물 Id
    [SerializeField] private int    _selectedRelicIdx  = -1;
    [SerializeField] private int    _pendingSlot       = -1;
    [SerializeField] private string _stolenFromChar    = "";

    // 확인 팝업 (다른 캐릭터 유물 뺏기) - SpawnSelect로 처리하므로 플래그만 유지
    private bool   _confirmActive = false;
    private int    _confirmTargetRelicIdx = -1;
    private string _confirmOwnerPlayerId  = "";

    // 현재 보유 유물 Id 목록 (그리드에 표시)
    private List<string> _ownedRelicIds = new List<string>();

    // ── IPopupRelicView 구현 ───────────────────────────
    public void SetModel(string playerId, List<PlayerInfo> playerInfo)
    {
        _curPlayerId  = playerId;
        _playerInfoList = playerInfo;
    }

    public void SetAction(PopupCommonActions commonActions, Action closeAction, Action<int> changeCharAction)
    {
        _actions          = commonActions;
        _closeAction      = closeAction;
        _changeCharAction = changeCharAction;
    }

    public void SetPlayerInfo()
    {
        _slotCount = equippedRelicFrames.Length;

        // ── 보유 유물 목록 구성 ─────────────────────────
        _ownedRelicIds.Clear();
        var allRelics = GameManager.Instance.RelicList; // List<string> relicId 목록
        foreach (var rid in allRelics)
            _ownedRelicIds.Add(rid);

        _relicCount  = _ownedRelicIds.Count;
        _gridRows    = Mathf.CeilToInt((float)_relicCount / _gridCols);

        // ── 그리드 프레임 초기화 ────────────────────────
        // 규칙 3: 최초 모두 비활성, RelicList 크기만큼만 SetData
        for (int i = 0; i < relicFrames.Length; i++)
        {
            relicFrames[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < _relicCount && i < relicFrames.Length; i++)
        {
            relicFrames[i].gameObject.SetActive(true);
            relicFrames[i].ViewSlot();
            relicFrames[i].SetData(_ownedRelicIds[i]);
        }

        // ── 장착 슬롯 초기화 ────────────────────────────
        RefreshSlotFrames();

        _stolenFromChar   = "";
        _confirmActive    = false;

        RefreshCursors();
    }

    // ── 매 프레임 입력 처리 ───────────────────────────
    private void Update()
    {
        if (!gameObject.activeSelf) return;

        // SpawnSelect 팝업이 떠 있는 동안 입력 차단
        if (_confirmActive)
            return;

        HandleKeyInput();
    }

    // ── 일반 키 입력 ──────────────────────────────────
    private void HandleKeyInput()
    {
        // Q/E : 캐릭터 변경 (pendingSlot 상태에서는 무시)
        if (_pendingSlot < 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _changeCharAction?.Invoke(-1);
                _actions?.PlayMoveSound?.Invoke();
                return;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                _changeCharAction?.Invoke(+1);
                _actions?.PlayMoveSound?.Invoke();
                return;
            }
        }

        // Esc
        if (Input.GetKeyDown(GameManager.Instance.escKey))
        {
            HandleEscape();
            return;
        }

        // Enter
        if (InputHelper.GetEnterDown())
        {
            HandleEnter();
            return;
        }

        // 방향키
        HandleArrows();
    }

    // ── Esc 처리 ──────────────────────────────────────
    private void HandleEscape()
    {
        if (_pendingSlot >= 0)
        {
            // ③단계 취소 → 슬롯으로 복귀
            int ps = _pendingSlot;
            _pendingSlot = -1;
            _zone        = Zone.Slot;
            _slotCursor  = ps;
            _actions?.PlayCancelSound?.Invoke();
            RefreshCursors();
        }
        else if (_zone == Zone.Slot && _selectedRelicIdx >= 0)
        {
            ResetToGrid();
            _actions?.PlayCancelSound?.Invoke();
            RefreshCursors();
        }
        else
        {
            // 뒤로가기
            _actions?.PlayCancelSound?.Invoke();
            CloseAction();
        }
    }

    // ── Enter 처리 ────────────────────────────────────
    private void HandleEnter()
    {
        // ③ pendingSlot 상태: 슬롯 지정 후 유물 선택 중
        if (_pendingSlot >= 0)
        {
            int rid  = _gridCursor;
            string relicId = GetRelicIdAt(rid);
            string owner   = GameManager.Instance.GetEquipRelicPlayer(relicId);

            if (owner == _curPlayerId)
            {
                // (이곳에 유물 장착 코드 추가)
                GameManager.Instance.EquipRelic(_curPlayerId, relicId);

                _actions?.PlaySelectSound?.Invoke();
                ResetToGrid();
            }
            else if (!string.IsNullOrEmpty(owner))
            {
                // 다른 캐릭터 유물 → 확인 팝업
                // (캐릭터 유물 뺐는 경고문 추가)
                ShowConfirm(rid, owner);
            }
            else
            {
                // (이곳에 유물 장착 코드 추가)
                GameManager.Instance.EquipRelic(_curPlayerId, relicId);

                _actions?.PlaySelectSound?.Invoke();
                ResetToGrid();
            }

            RefreshSlotFrames();
            RefreshCursors();
            return;
        }

        if (_zone == Zone.Grid)
        {
            string relicId   = GetRelicIdAt(_gridCursor);
            bool isEquippedRelic = GameManager.Instance.GetIsEquippedRelic(_curPlayerId, relicId);

            // 이미 장착하고 있는가?
            if (isEquippedRelic)
            {
                // 내 장착 유물 → 해제
                GameManager.Instance.UnEquipRelic(_curPlayerId, relicId);
                _actions?.PlaySelectSound?.Invoke();
                ResetToGrid();
            }
            else
            {
                string owner = GameManager.Instance.GetEquipRelicPlayer(relicId);
                if (!string.IsNullOrEmpty(owner) && owner != _curPlayerId)
                {
                    // 다른 캐릭터 유물 → 확인 팝업
                    // (캐릭터 유물 뺐는 경고문 추가)
                    ShowConfirm(_gridCursor, owner);
                }
                else
                {
                    // 미장착 유물 → 빈 슬롯 있으면 자동 장착, 없으면 슬롯 선택
                    EquipRelic(_gridCursor, relicId);
                }
            }
        }
        else // Zone.Slot
        {
            // 슬롯이 꽉차서 바꿔껴야 할 때
            if (_selectedRelicIdx >= 0)
            {
                // 슬롯 선택 확정 → 장착
                string relicId = GetRelicIdAt(_selectedRelicIdx);
                
                // (이곳에 유물 장착 코드 추가)
                GameManager.Instance.TargetEquipRelic(_curPlayerId, relicId, _slotCursor);
                if(!string.IsNullOrWhiteSpace(_stolenFromChar))
                    GameManager.Instance.UnEquipRelic(_stolenFromChar, relicId);

                _stolenFromChar = "";
                _actions?.PlaySelectSound?.Invoke();
                ResetToGrid();
            }
            else
            {
                string slotRelic = GameManager.Instance.GetEquippedRelicId(_curPlayerId, _slotCursor);
                if (!string.IsNullOrEmpty(slotRelic))
                {
                    if (slotRelic == ConstValues.Lock)
                        return;
                    
                    // 유물 있는 슬롯 → 해제
                    GameManager.Instance.UnEquipRelic(_curPlayerId, slotRelic);
                    _actions?.PlaySelectSound?.Invoke();
                    // 모두 해제되면 그리드로 복귀
                    if (!GameManager.Instance.GetIsEmptyRelicList(_curPlayerId))
                        ResetToGrid();
                }
                else
                {
                    // 빈 슬롯 Enter → ③단계: 슬롯 지정 후 그리드로
                    _pendingSlot = _slotCursor;
                    _zone        = Zone.Grid;
                    _actions?.PlayMoveSound?.Invoke();
                }
            }
        }

        RefreshSlotFrames();
        RefreshCursors();
    }

    // ── 방향키 처리 ───────────────────────────────────
    private void HandleArrows()
    {
        Zone prevZone       = _zone;
        int  prevGridCursor = _gridCursor;
        int  prevSlotCursor = _slotCursor;

        if (_zone == Zone.Grid || _pendingSlot >= 0)
        {
            int row    = _gridCursor / _gridCols;
            int col    = _gridCursor % _gridCols;
            int maxIdx = _relicCount - 1;

            if (Input.GetKeyDown(GameManager.Instance.rightKey))
            {
                // 현재 행에서 실제로 존재하는 마지막 col 계산
                int rowLastIdx = Mathf.Min(row * _gridCols + (_gridCols - 1), maxIdx);
                int rowLastCol = rowLastIdx % _gridCols;

                if (col == rowLastCol && _pendingSlot < 0)
                {
                    // 현재 행의 실제 마지막 유물에서 → 슬롯 첫칸 warp
                    _zone       = Zone.Slot;
                    _slotCursor = 0;
                }
                else
                {
                    int next = row * _gridCols + col + 1;
                    _gridCursor = Mathf.Min(next, maxIdx);
                }
            }
            else if (Input.GetKeyDown(GameManager.Instance.leftKey))
            {
                if (col == 0 && _pendingSlot < 0)
                {
                    // 그리드 왼쪽 끝 → 슬롯 마지막칸 warp
                    _zone       = Zone.Slot;
                    _slotCursor = _slotCount - 1;
                }
                else
                {
                    _gridCursor = row * _gridCols + col - 1;
                    _gridCursor = Mathf.Max(_gridCursor, row * _gridCols); // 행 밖으로 안 나가게
                }
            }
            else if (Input.GetKeyDown(GameManager.Instance.downKey))
            {
                int next = ((row + 1) % _gridRows) * _gridCols + col;
                _gridCursor = Mathf.Min(next, maxIdx);
            }
            else if (Input.GetKeyDown(GameManager.Instance.upKey))
            {
                int next = ((row - 1 + _gridRows) % _gridRows) * _gridCols + col;
                _gridCursor = Mathf.Min(next, maxIdx);
            }
        }
        else // Zone.Slot
        {
            if (Input.GetKeyDown(GameManager.Instance.rightKey))
            {
                if (_slotCursor == _slotCount - 1)
                {
                    // 슬롯 마지막칸 → 그리드 첫칸 warp
                    _zone       = Zone.Grid;
                    _gridCursor = 0;
                }
                else
                {
                    _slotCursor++;
                }
            }
            else if (Input.GetKeyDown(GameManager.Instance.leftKey))
            {
                if (_slotCursor == 0)
                {
                    // 슬롯 첫칸 → 그리드 오른쪽 끝 warp
                    _zone       = Zone.Grid;
                    _gridCursor = (_gridRows - 1) * _gridCols + (_gridCols - 1);
                    _gridCursor = Mathf.Min(_gridCursor, _relicCount - 1);
                }
                else
                {
                    _slotCursor--;
                }
            }
        }

        bool moved = (_zone != prevZone) || (_gridCursor != prevGridCursor) || (_slotCursor != prevSlotCursor);

        if (moved)
        {
            _actions?.PlayMoveSound?.Invoke();
            RefreshCursors();
        }
    }

    // ── 확인 팝업 ─────────────────────────────────────
    private void ShowConfirm(int relicIdx, string ownerPlayerId)
    {
        _confirmActive         = true;
        _confirmTargetRelicIdx = relicIdx;
        _confirmOwnerPlayerId  = ownerPlayerId;

        // 경고문 메시지 구성
        // ownerPlayerId에 해당하는 캐릭터 이름 talk 번호
        int ownerNameTalkIdx = ownerPlayerId switch
        {
            ConstValues.Berserker => 50000,
            ConstValues.Gunner    => 50001,
            ConstValues.Fighter   => 50002,
            _                     => 50000
        };
        string ownerName = GameManager.Instance.GetTalk(ownerNameTalkIdx);

        // TODO: 경고문 talk 번호 확정 후 교체 (현재 임시 문자열 사용)
        string message = string.Format(GameManager.Instance.GetTalk(41000), ownerName);
        GameManager.Instance.SpawnSelect(message, null, 0, yesAction: DoConfirmYes, noAction: DoConfirmNo);
    }

    private void DoConfirmYes()
    {
        _confirmActive = false;

        string relicId = GetRelicIdAt(_confirmTargetRelicIdx);

        // 빈 슬롯 있으면 자동 장착, 없으면 슬롯 선택 단계
        bool canEquipSlot = GameManager.Instance.GetCanEquipSlot(_curPlayerId);
        if (canEquipSlot)
        {
            // (이곳에 유물 장착 코드 추가)
            GameManager.Instance.EquipRelic(_curPlayerId, relicId);
            GameManager.Instance.UnEquipRelic(_confirmOwnerPlayerId, relicId);
            RefreshSlotFrames();

            _actions?.PlaySelectSound?.Invoke();
            ResetToGrid();
        }
        else
        {
            // 슬롯이 꽉 참 → 슬롯 선택 단계
            _stolenFromChar   = _confirmOwnerPlayerId;
            _selectedRelicIdx = _confirmTargetRelicIdx;
            _zone             = Zone.Slot;
            _slotCursor       = 0;
            _actions?.PlayMoveSound?.Invoke();
        }

        RefreshSlotFrames();
        RefreshCursors();
    }

    private void DoConfirmNo()
    {
        _confirmActive  = false;
        _stolenFromChar = "";
        _actions?.PlayCancelSound?.Invoke();
        RefreshCursors();
    }

    // ── 자동/수동 장착 ────────────────────────────────
    private void EquipRelic(int relicIdx, string relicId)
    {
        bool canEquip = GameManager.Instance.GetCanEquipSlot(_curPlayerId);
        if (canEquip)
        {
            // (이곳에 유물 장착 코드 추가)
            GameManager.Instance.EquipRelic(_curPlayerId, relicId);
            RefreshSlotFrames();

            _actions?.PlaySelectSound?.Invoke();
            ResetToGrid();
        }
        else
        {
            // 슬롯 꽉 참 → 슬롯 선택 단계
            _selectedRelicIdx = relicIdx;
            _zone             = Zone.Slot;
            _slotCursor       = 0;
            _actions?.PlayMoveSound?.Invoke();
        }
    }
    
    // ── 상태 초기화 ───────────────────────────────────
    private void ResetToGrid()
    {
        _selectedRelicIdx = -1;
        _pendingSlot      = -1;
        _stolenFromChar   = "";
        _zone             = Zone.Grid;
    }

    // ── UI 갱신 ──────────────────────────────────────
    // 규칙 6: 커서에 따라 Expansion/Reduction, SelectObjectActive 갱신
    private void RefreshCursors()
    {
        // 그리드 커서
        for (int i = 0; i < _relicCount && i < relicFrames.Length; i++)
        {
            bool isCursor = (_zone == Zone.Grid || _pendingSlot >= 0) && (i == _gridCursor);
            bool isSelected = (i == _selectedRelicIdx);

            if (isCursor || isSelected)
            {
                relicFrames[i].Expansion(1.05f);
                relicFrames[i].SelectObjectActive(true);
            }
            else
            {
                relicFrames[i].Reduction();
                relicFrames[i].SelectObjectActive(false);
            }
        }

        // 슬롯 커서
        for (int i = 0; i < _slotCount; i++)
        {
            bool isCursor = (_zone == Zone.Slot && _pendingSlot < 0) && (i == _slotCursor);
            bool isPending = (i == _pendingSlot);

            if (isCursor || isPending)
            {
                equippedRelicFrames[i].Expansion(1.05f);
                equippedRelicFrames[i].SelectObjectActive(true);
            }
            else
            {
                equippedRelicFrames[i].Reduction();
                equippedRelicFrames[i].SelectObjectActive(false);
            }
        }
        
        // 현재 선택된 슬롯의 RelicId 갱신
        if (_zone == Zone.Grid || _pendingSlot >= 0)
            _curRelicId = GetRelicIdAt(_gridCursor);
        else // Zone.Slot
            _curRelicId = GameManager.Instance.GetEquippedRelicId(_curPlayerId, _slotCursor);
        
        // 능력치 표현창은 우선 전부 비활성화
        foreach (var relicValueFrame in relicValueFrames)
            relicValueFrame.gameObject.SetActive(false);

        if (string.IsNullOrWhiteSpace(_curRelicId))
        {
            relicNameText.text = GameManager.Instance.GetTalk(102000);
            relicExplainText.text = GameManager.Instance.GetTalk(103000);
        }
        else if (_curRelicId == ConstValues.Lock)
        {
            relicNameText.text = GameManager.Instance.GetTalk(102001);
            relicExplainText.text = GameManager.Instance.GetTalk(103001);
        }
        else
        {
            relicNameText.text = GameManager.Instance.GetItemTalk(_curRelicId);
            relicExplainText.text = GameManager.Instance.GetItemExplain(_curRelicId);
            
            var relicInfo = GameManager.Instance.relicCopyList.Find(x => x.id == _curRelicId);
            for (int i = 0; i < relicInfo.statList.Count; i++)
            {
                relicValueFrames[i].gameObject.SetActive(true);
                relicValueFrames[i].SetText(GameManager.Instance.GetRelicStat(relicInfo, i));
            }
        }
    }

    // 슬롯 프레임 ViewSlot/LockSlot 및 SetData 갱신
    private void RefreshSlotFrames()
    {
        var curInfo       = GetPlayerInfo(_curPlayerId);
        int unlockedCount = curInfo.relicList.Count;
        
        for (int i = 0; i < _relicCount && i < relicFrames.Length; i++)
            relicFrames[i].SetData(_ownedRelicIds[i]);

        for (int i = 0; i < _slotCount; i++)
        {
            if (i < unlockedCount)
            {
                equippedRelicFrames[i].ViewSlot();
                equippedRelicFrames[i].SetData(curInfo.relicList[i]);
            }
            else
            {
                equippedRelicFrames[i].LockSlot();
            }
        }
    }

    // ── 유틸 ─────────────────────────────────────────
    private PlayerInfo GetPlayerInfo(string playerId)
    {
        if (_playerInfoList == null)
            return null;
        
        return _playerInfoList.Find(p => p.playerId == playerId);
    }

    private string GetRelicIdAt(int gridIdx)
    {
        if (gridIdx < 0 || gridIdx >= _ownedRelicIds.Count)
            return "";
        
        return _ownedRelicIds[gridIdx];
    }

    public void CloseAction()
    {
        if (_closeAction == null)
            return;
        _closeAction.Invoke();
    }
}