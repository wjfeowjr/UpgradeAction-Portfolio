using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IPopupItemView
{
    void SetModel(List<HaveItemInfo> itemList);
    void SetItemInfo();
    void SetAction(PopupCommonActions common, Action close);
}

public class PopupItemModel
{
    public List<HaveItemInfo> itemList = new List<HaveItemInfo>();
    public PopupCommonActions commonActions;
    public Action closeAction;
}

public class PopupItemPresenter
{
    private readonly IPopupItemView _view;
    private readonly PopupItemModel _model;

    public PopupItemPresenter(IPopupItemView view, PopupItemModel model)
    {
        _view = view;
        _model = model;
    }

    public void UpdateItemInfo()
    {
        _view.SetModel(_model.itemList);
        _view.SetAction(_model.commonActions, _model.closeAction);
    }
}

public class PopupItemView : MonoBehaviour, IPopupItemView
{
    // ── 외부 주입 ──────────────────────────────────────
    private PopupCommonActions _actions;
    private Action _closeAction;

    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemExplainText;

    [SerializeField] private ItemFrame selectedItem;   // 선택 아이템 미리보기 프레임
    [SerializeField] private ItemFrame[] itemFrames;   // 아이템 그리드 (5x5 = 25개)

    // ── 내부 상태 ──────────────────────────────────────
    private const int Cols = 5;

    private List<HaveItemInfo> _itemIds = new List<HaveItemInfo>();
    private int _itemCount;   // 실제 보유 아이템 수
    private int _rows;        // 실제 사용 행 수

    [SerializeField] private int _cursor;
    private HaveItemInfo _curItemInfo;

    // ── IPopupItemView 구현 ───────────────────────────
    public void SetModel(List<HaveItemInfo> itemList)
    {
        _itemIds   = itemList ?? new List<HaveItemInfo>();
        _itemCount = _itemIds.Count;
        _rows      = (_itemCount > 0) ? Mathf.CeilToInt((float)_itemCount / Cols) : 0;

        // 전체 비활성화 후 보유 수량만큼 활성화
        for (int i = 0; i < itemFrames.Length; i++)
            itemFrames[i].gameObject.SetActive(false);

        if (_itemCount == 0)
        {
            // 아이템이 없을 때 빈 칸 1개만 표시
            itemFrames[0].gameObject.SetActive(true);
            itemFrames[0].SetData(null);
        }
        else
        {
            for (int i = 0; i < _itemCount && i < itemFrames.Length; i++)
            {
                itemFrames[i].gameObject.SetActive(true);
                itemFrames[i].SetData(_itemIds[i]);
            }
        }

        _cursor = 0;
        RefreshCursors();
    }

    public void SetItemInfo()
    {
        if (_itemCount == 0 || _curItemInfo == null)
        {
            itemNameText.text    = GameManager.Instance.GetTalk(100000);
            itemExplainText.text = GameManager.Instance.GetTalk(101000);
            if (selectedItem)
                selectedItem.gameObject.SetActive(false);
            return;
        }

        if (selectedItem)
            selectedItem.gameObject.SetActive(true);
        itemNameText.text    = GameManager.Instance.GetItemTalk(_curItemInfo.id);
        itemExplainText.text = GameManager.Instance.GetItemExplain(_curItemInfo.id);
        if (selectedItem)
            selectedItem.SetData(_curItemInfo);
    }

    public void SetAction(PopupCommonActions common, Action close)
    {
        _actions     = common;
        _closeAction = close;
    }

    // ── 매 프레임 입력 처리 ───────────────────────────
    private void Update()
    {
        if (!gameObject.activeSelf)
            return;

        HandleKeyInput();
    }

    private void HandleKeyInput()
    {
        if (Input.GetKeyDown(GameManager.Instance.escKey))
        {
            HandleEscape();
            return;
        }

        if (_itemCount == 0)
            return;
        
        if (InputHelper.GetEnterDown())
        {
            HandleEnter();
            return;
        }

        HandleArrows();
    }

    private void HandleEscape()
    {
        _actions?.PlayCancelSound?.Invoke();
        _closeAction?.Invoke();
    }

    private void HandleEnter()
    {

    }

    // ── 방향키 처리 ───────────────────────────────────
    private void HandleArrows()
    {
        int prev   = _cursor;
        int row    = _cursor / Cols;
        int col    = _cursor % Cols;
        int maxIdx = _itemCount - 1;

        if (Input.GetKeyDown(GameManager.Instance.rightKey))
        {
            // 현재 행의 실제 마지막 열 계산
            int rowLastIdx = Mathf.Min(row * Cols + (Cols - 1), maxIdx);
            int rowLastCol = rowLastIdx % Cols;

            if (col >= rowLastCol)
            {
                // 다음 행 첫 칸으로 이동, 마지막 행이면 처음으로 wrap
                int nextRowFirst = (row + 1) * Cols;
                _cursor = (nextRowFirst <= maxIdx) ? nextRowFirst : 0;
            }
            else
            {
                _cursor++;
            }
        }
        else if (Input.GetKeyDown(GameManager.Instance.leftKey))
        {
            if (col == 0)
            {
                // 이전 행 마지막 칸으로 이동, 첫 행이면 끝으로 wrap
                _cursor = (row == 0) ? maxIdx : Mathf.Min(row * Cols - 1, maxIdx);
            }
            else
            {
                _cursor--;
            }
        }
        else if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            int nextRow = (row + 1) % _rows;
            _cursor = Mathf.Min(nextRow * Cols + col, maxIdx);
        }
        else if (Input.GetKeyDown(GameManager.Instance.upKey))
        {
            int prevRow = (row - 1 + _rows) % _rows;
            _cursor = Mathf.Min(prevRow * Cols + col, maxIdx);
        }

        if (_cursor != prev)
        {
            _actions?.PlayMoveSound?.Invoke();
            RefreshCursors();
        }
    }

    // ── UI 갱신 ──────────────────────────────────────
    private void RefreshCursors()
    {
        for (int i = 0; i < _itemCount && i < itemFrames.Length; i++)
        {
            if (i == _cursor)
            {
                itemFrames[i].Expansion(1.05f);
                itemFrames[i].SelectObjectActive(true);
            }
            else
            {
                itemFrames[i].Reduction();
                itemFrames[i].SelectObjectActive(false);
            }
        }

        _curItemInfo = (_cursor >= 0 && _cursor < _itemIds.Count) ? _itemIds[_cursor] : default;
        SetItemInfo();
    }
}
