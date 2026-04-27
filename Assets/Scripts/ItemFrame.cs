using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemFrame : ExpansionUiObject
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private bool hideCount;

    public void SetData(HaveItemInfo itemInfo)
    {
        if (itemInfo == null)
        {
            itemImage.gameObject.SetActive(false);
            countText.gameObject.SetActive(false);
        }
        else
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = GameManager.Instance.GetAtlasSprite(itemInfo.id);
            countText.gameObject.SetActive(!hideCount && itemInfo.count > 1);
            countText.text = $"x {itemInfo.count}";
        }
    }
}
