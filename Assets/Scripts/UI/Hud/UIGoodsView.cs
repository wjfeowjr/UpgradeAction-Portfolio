using TMPro;
using UnityEngine;

// 표시할 값 묶음. 로직은 없다.
public class UIGoodsModel
{
    public int getGold;
    public int totalGold;
}

// 받은 값을 그리기만 한다.
// 무엇을 그릴지 판단하는 부분이 없어 Presenter 를 두지 않았다.
public class UIGoodsView : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;

    public void SetGoldText(UIGoodsModel model)
    {
        SetGoldText(model.totalGold);
    }

    private void SetGoldText(int gold)
    {
        goldText.text = $"{GameManager.Instance.GetThousandCommaText(gold)}";
    }
}
