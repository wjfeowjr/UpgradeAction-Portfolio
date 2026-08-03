using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IPopupStoreView
{
    void SetModel(string storeId);
    void SetItem();
    void SetAction(PopupCommonActions commonActions);
}

public class PopupStoreModel
{
    public PopupCommonActions commonActions;
    public Action closeAction;
}

public class PopupStorePresenter
{
    private IPopupStoreView _view;
    private PopupStoreModel _model;

    public PopupStorePresenter(IPopupStoreView view, PopupStoreModel model)
    {
        _view  = view;
        _model = model;
    }

    public void SetModel(string storeId)
    {
        _view.SetModel(storeId);
    }

    public void SetItem()
    {
        _view.SetItem();
    }

    public void SetAction()
    {
        _view.SetAction(_model.commonActions);
    }

    public void CloseStore()
    {
        if (Input.GetKeyDown(GameManager.Instance.escKey))
            _model.closeAction?.Invoke();
    }
}

public class PopupStoreView : MonoBehaviour, IPopupStoreView
{
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    private PopupStorePresenter presenter;
    
    public PopupStorePresenter Bind(PopupStoreModel model)
    {
        presenter = new PopupStorePresenter(this, model);
        return presenter;
    }

    [SerializeField] private TMP_Text popupText;
    [SerializeField] private TMP_Text goldText;

    [SerializeField] private TMP_Text itemTypeText;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemExplainText;

    [SerializeField] private TMP_Text selectText;
    [SerializeField] private TMP_Text backText;
    [SerializeField] private RectTransform scrollView;
    [SerializeField] private RectTransform scrollContents;
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    
    [SerializeField] private TextUiObject[] statObjects;
    [SerializeField] private TMP_Text itemCostText;
    [SerializeField] private StoreItemFrame[] storeItemFrames;

    [SerializeField] private List<StoreItemData> storeItemTableData     = new List<StoreItemData>();
    [SerializeField] private List<StoreItemData> sortStoreItemTableData = new List<StoreItemData>();

    [SerializeField] private List<ItemCopy> itemInfoList = new List<ItemCopy>();

    private CancellationTokenSource delayCancellation;

    private string curItemId;
    private string curStoreId;

    private PopupCommonActions popupCommonActions;

    // %% 내부 상태 %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    [SerializeField] private int  _cursor          = 0;
    private int  _itemCount       = 0;
    private bool _isConfirmActive = false; // SpawnSelect 팝업 활성 여부

    [SerializeField] private int unitFrameCount;

    public bool IsConfirmActive => _isConfirmActive;

    // %% 매 프레임 입력 처리 %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    private void Update()
    {
        if (!gameObject.activeSelf)
            return;

        // 골드 표시는 _isConfirmActive 여부와 무관하게 항상 최신 상태로 동기화
        RefreshGold();
    }

    // 입력 처리는 소유 Popup_Store의 Update에서 openComplete일 때만 호출됨
    public void HandleInput()
    {
        if (_isConfirmActive)
            return;

        if (Input.GetKeyDown(GameManager.Instance.upKey))
            HandleArrow(-1);
        if (Input.GetKeyDown(GameManager.Instance.downKey))
            HandleArrow(+1);
        if (InputHelper.GetEnterDown())
            HandleEnter();
    }

    // %% 방향키 처리 (위아래 이동) %%%%%%%%%%%%%%%%%%%%%%%%%%%%
    private void HandleArrow(int dir)
    {
        if (_itemCount == 0)
            return;

        _cursor   = (_cursor + dir + _itemCount) % _itemCount;
        curItemId = sortStoreItemTableData[_cursor].id;

        popupCommonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
        DisplayItemInfo();
        SortFrames();
    }

    // %% 커서 Expansion/Reduction 갱신 %%%%%%%%%%%%%%%%%
    private void RefreshCursors()
    {
        for (int i = 0; i < _itemCount && i < storeItemFrames.Length; i++)
        {
            if (i == _cursor)
            {
                storeItemFrames[i].Expansion(1.05f);
                storeItemFrames[i].SelectObjectActive(true);
            }
            else
            {
                storeItemFrames[i].Reduction();
                storeItemFrames[i].SelectObjectActive(false);
            }
        }
    }

    private void SortFrames()
    {
        float unitSizeY = (storeItemFrames[0].VerticalSize() + layoutGroup.spacing);
        float unitCursor = unitSizeY * (_cursor + 1); // 570
        float interval = unitSizeY * unitFrameCount;
        int n = 0;
        if (unitCursor > interval)
        {
            float sizeY = unitCursor;
            while (sizeY > interval)
            {
                sizeY -= interval;
                n++;
            }
        }
        scrollContents.anchoredPosition = new Vector2(scrollContents.anchoredPosition.x, interval * n);
    }

    // %% Enter: 아이템 구매 확인 %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    private void HandleEnter()
    {
        if (_itemCount == 0)
            return;

        // SoldOut 아이템이면 선택 불가
        bool isSoldOut = GameManager.Instance.RelicList.Contains(curItemId);

        if (isSoldOut)
        {
            popupCommonActions?.PlaySelectSound?.Invoke();
            GameManager.Instance.SpawnWarningPopup(GameManager.Instance.GetTalk(30211)).Forget();
            return;
        }

        var storeItemData = TableManager.Instance.GetStoreItem(curItemId);
        if (storeItemData == null)
            return;

        string itemName = GameManager.Instance.GetItemTalk(curItemId);
        string message  = string.Format(GameManager.Instance.GetTalk(41001), itemName);

        _isConfirmActive = true;
        GameManager.Instance.SpawnSelect
        (
            message,
            GameManager.Instance.GetAtlasSprite(ConstValues.Gold),
            storeItemData.cost,
            yesAction: () =>
            {
                // 구매 성공 시에만 아이템 목록을 갱신 (실패 시 _cursor가 0으로 리셋되는 것 방지)
                bool success = GameManager.Instance.BuyItem(storeItemData);
                if (success)
                {
                    RefreshGold();
                    RefreshItemInfo(curStoreId);
                    DisplayItemInfo();
                }
                _isConfirmActive = false;
            },
            noAction: () =>
            {
                _isConfirmActive = false;
                popupCommonActions?.PlayCancelSound?.Invoke();
            }
        );
    }

    private void RefreshGold()
    {
        goldText.text = GameManager.Instance.GetThousandCommaText(GameManager.Instance.Gold);
        GameManager.Instance.RefreshGoods();
    }

    // %% IPopupStoreView 구현 %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    public async void SetModel(string storeId)
    {
        popupText.text = GameManager.Instance.GetTalk(30024);

        curStoreId = storeId;

        // 아이템 프레임 비활성화
        foreach (var storeItemFrame in storeItemFrames)
            storeItemFrame.gameObject.SetActive(false);

        RefreshItemInfo(curStoreId);
        RefreshCursors();

        for (var i = 0; i < storeItemFrames.Length; i++)
        {
            float unitSizeY = (storeItemFrames[0].VerticalSize() + layoutGroup.spacing) * i;
            if (unitSizeY >= scrollView.sizeDelta.y)
                break;

            unitFrameCount = i;
        }

        delayCancellation = new CancellationTokenSource();
        await GameManager.Instance.YieldDelay(delayCancellation);
        scrollContents.anchoredPosition = Vector2.zero;
    }

    private void RefreshItemInfo(string storeId)
    {
        // storeId에 해당하는 아이템 목록 조회
        storeItemTableData = TableManager.Instance.storeItemTable.StoreItem.FindAll(x => x.storeId == storeId);
        sortStoreItemTableData.Clear();
        itemInfoList.Clear();

        // SoldOut 정렬 규칙: 미구매 먼저, 구매한 것은 뒤로 (RelicList에 id가 있으면 SoldOut)
        var ownedRelics = GameManager.Instance.RelicList;
        var unsold = storeItemTableData.FindAll(x => !ownedRelics.Contains(x.id));
        var sold   = storeItemTableData.FindAll(x =>  ownedRelics.Contains(x.id));
        sortStoreItemTableData.AddRange(unsold);
        sortStoreItemTableData.AddRange(sold);

        _itemCount = sortStoreItemTableData.Count;
        _cursor    = 0;

        // 프레임 활성화 및 SetData 처리
        var curGold = GameManager.Instance.Gold;
        for (int i = 0; i < _itemCount && i < storeItemFrames.Length; i++)
        {
            bool isSoldOut = sold.Contains(sortStoreItemTableData[i]);
            bool canAfford = curGold >= sortStoreItemTableData[i].cost;
            storeItemFrames[i].gameObject.SetActive(true);
            storeItemFrames[i].SetData(sortStoreItemTableData[i], isSoldOut, canAfford);
        }

        if (_itemCount > 0)
            curItemId = sortStoreItemTableData[0].id;
    }

    public void SetItem()
    {
        foreach (var sortStoreItem in sortStoreItemTableData)
        {
            // 아이템 itemList에 있는지 null 확인
            var sortItemData = GameManager.Instance.GetItemCopy(sortStoreItem.id);
            if (sortItemData != null)
                itemInfoList.Add(sortItemData);
        }

        DisplayItemInfo();
        RefreshGold();
    }

    // %% 우측 아이템 정보창 갱신 %%%%%%%%%%%%%%%%%%%%%%%%%%%
    private void DisplayItemInfo()
    {
        var itemInfo      = GameManager.Instance.GetItemCopy(curItemId);
        var relicInfo     = GameManager.Instance.GetRelicCopy(curItemId);
        var storeItemData = TableManager.Instance.GetStoreItem(curItemId);

        if (itemInfo != null)
        {
            switch (itemInfo.type)
            {
                case eItemType.Relic:
                    itemTypeText.text = GameManager.Instance.GetTalk(30023);
                    break;
                default:
                    itemTypeText.text = itemInfo.type.ToString();
                    break;
            }

            itemNameText.text    = GameManager.Instance.GetTalk(itemInfo.name);
            itemExplainText.text = GameManager.Instance.GetTalk(itemInfo.explain);

            selectText.text = string.Format(GameManager.Instance.GetTalk(30110), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey), GameManager.Instance.GetKeyCode(GameManager.Instance.downKey));
            backText.text   = string.Format(GameManager.Instance.GetTalk(30104), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey));
        }

        foreach (var statObject in statObjects)
            statObject.gameObject.SetActive(false);

        if (relicInfo != null)
        {
            for (int i = 0; i < relicInfo.statList.Count; i++)
            {
                statObjects[i].gameObject.SetActive(true);
                statObjects[i].SetText(GameManager.Instance.GetRelicStat(relicInfo, i));
            }
        }

        if (storeItemData != null)
            itemCostText.text = GameManager.Instance.GetThousandCommaText(storeItemData.cost);
    }
    
    public void SetAction(PopupCommonActions actions)
    {
        popupCommonActions = actions;
    }
}
