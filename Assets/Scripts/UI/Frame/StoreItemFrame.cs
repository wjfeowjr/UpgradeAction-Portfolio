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
    
    public void SetData(StoreItemData storeItemData, bool soldOut, bool canAfford)
    {
        mainText.text = GameManager.Instance.GetItemTalk(storeItemData.id);
        itemImage.sprite = GameManager.Instance.GetAtlasSprite(storeItemData.id);
        // 아이템 가격 작성
        cost.text = GameManager.Instance.GetThousandCommaText(storeItemData.cost);

        isSoldOut = soldOut;
        soldOutObject.SetActive(isSoldOut);

        // 아이템명은 항상 흰색, 골드 부족 시 가격만 빨간색
        // SoldOut일 땐 soldOutObject가 별도 표시하므로 가격 색상은 흰색으로 복원
        cost.color = (!isSoldOut && !canAfford) ? ConstValues.RedColor : ConstValues.WhiteColor;
    }
}
