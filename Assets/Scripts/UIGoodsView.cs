using TMPro;
using UnityEngine;

public interface IUIGoodsView
{
    void SetGoldText(int gold);
}

public class UIGoodsModel
{
    public int getGold;
    public int totalGold;
}

public class UIGoodsPresenter
{
    private readonly IUIGoodsView _goodsview;
    private UIGoodsModel _model;

    public UIGoodsPresenter(IUIGoodsView goodsView, UIGoodsModel model)
    {
        _goodsview = goodsView;
        _model = model;
    }

    public void SetGoldText()
    {
        _goodsview.SetGoldText(_model.totalGold);
    }

    public void PlusGoldText()
    {
        Debug.Log(_model.getGold);
        _goodsview.SetGoldText(_model.totalGold);
    }
}

public class UIGoodsView : MonoBehaviour, IUIGoodsView
{
    [SerializeField] private TMP_Text goldText;

    public void SetGoldText(int gold)
    {
        goldText.text = $"{GameManager.Instance.GetThousandCommaText(gold)}";
    }
}
