using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public interface IPopupStoreView
{
    void SetModel(string storeId);
    void SetItem();
    void SetAction(PopupCommonActions commonActions);
}

public class PopupStoreModel
{
    // TODO: 상점에 표시할 아이템 목록 및 관련 데이터 필드 추가
    public PopupCommonActions commonActions;
}

public class PopupStorePresenter
{
    private IPopupStoreView _view;
    private PopupStoreModel _model;

    public PopupStorePresenter(IPopupStoreView view, PopupStoreModel model)
    {
        _view = view;
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
}

public class PopupStoreView : MonoBehaviour, IPopupStoreView
{
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private TMP_Text goldText;
    
    [SerializeField] private TMP_Text itemTypeText;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemExplainText;
    [SerializeField] private TextUiObject[] statObjects;
    [SerializeField] private TMP_Text itemCostText;
    [SerializeField] private StoreItemFrame[] storeItemFrames;

    [SerializeField] private List<StoreItemData> storeItemTableData = new List<StoreItemData>();
    [SerializeField] private List<StoreItemData> sortStoreItemTableData = new List<StoreItemData>();
    
    [SerializeField] private List<ItemInfo> itemInfoList = new List<ItemInfo>();

    private string curItemId;
    
    private PopupCommonActions _actions;

    public void SetModel(string storeId)
    {
        popupText.text = "상점";
        
        foreach (var storeItemFrame in storeItemFrames)
            storeItemFrame.gameObject.SetActive(false);
        
        storeItemTableData = TableManager.Instance.storeItemTable.StoreItem.FindAll(x => x.storeId == storeId);
        sortStoreItemTableData.Clear();
        
    }

    public void SetItem()
    {
        // TODO: 선택된 아이템 상세 정보 표시
        foreach (var sortStoreItem in sortStoreItemTableData)
        {
            var sortItemData = GameManager.Instance.itemList.Find(x => x.id == sortStoreItem.id);
            itemInfoList.Add(sortItemData);
        }

        curItemId = itemInfoList[0].id;
    }

    private void DisplayItemInfo()
    {
        var itemInfo = GameManager.Instance.itemList.Find(x => x.id == curItemId);
        var relicInfo = GameManager.Instance.relicList.Find(x => x.id == curItemId);
        var storeItemData = TableManager.Instance.storeItemTable.StoreItem.Find(x => x.id == curItemId);
        
        if (itemInfo != null)
        {
            itemTypeText.text = itemInfo.type.ToString();
            itemNameText.text = GameManager.Instance.GetTalk(itemInfo.name);
            itemExplainText.text = GameManager.Instance.GetTalk(itemInfo.explain);
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

    public void SetAction(PopupCommonActions actions) => _actions = actions;
}
