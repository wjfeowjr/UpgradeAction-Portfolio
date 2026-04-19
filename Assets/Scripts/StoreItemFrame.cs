using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemFrame : ExpansionUiObject
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text cost;
    [SerializeField] private GameObject soldOutObject;

    private bool isSoldOut;

    public float VerticalSize()
    {
        return GetComponent<RectTransform>().sizeDelta.y;
    }
    
    public void SetData(StoreItemData storeItemData, bool soldOut)
    {
        mainText.text = GameManager.Instance.GetItemTalk(storeItemData.id);
        itemImage.sprite = GameManager.Instance.GetAtlasSprite(storeItemData.id);
        // 아이템 가격 작성
        cost.text = GameManager.Instance.GetThousandCommaText(storeItemData.cost);

        isSoldOut = soldOut;
        soldOutObject.SetActive(isSoldOut);
    }
}
