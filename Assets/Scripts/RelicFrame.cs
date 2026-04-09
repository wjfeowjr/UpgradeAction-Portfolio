using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RelicFrame : ExpansionUiObject
{
    [SerializeField] private Image relicImage;
    [SerializeField] private Image equipCharacterImage;
    
    [SerializeField] private GameObject insideObject;
    [SerializeField] private GameObject lockObject;
    [SerializeField] private GameObject equipObject;
    [SerializeField] private bool isNotEquipView;

    public void SetData(string relicId)
    {
        if (isNotEquipView)
        {
            equipObject.SetActive(false);
        }
        else
        {
            string equipPlayer = GameManager.Instance.GetEquipRelicPlayer(relicId);
            if (string.IsNullOrWhiteSpace(equipPlayer))
            {
                equipObject.SetActive(false);
            }
            else
            {
                equipObject.SetActive(true);
                equipCharacterImage.sprite = GameManager.Instance.GetAtlasSprite($"{equipPlayer}_{ConstValues.Face}");
            }
        }

        if (string.IsNullOrWhiteSpace(relicId))
        {
            relicImage.gameObject.SetActive(false);
        }
        else
        {
            relicImage.gameObject.SetActive(true);
            relicImage.sprite = GameManager.Instance.GetAtlasSprite(relicId);
        }
    }
    
    public void ViewSlot()
    {
        insideObject.SetActive(true);
        lockObject.SetActive(false);
    }
    public void LockSlot()
    {
        insideObject.SetActive(false);
        lockObject.SetActive(true);
    }
}
