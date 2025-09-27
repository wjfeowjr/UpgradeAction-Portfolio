using TMPro;
using UnityEngine;

public interface IUIGoodsView
{
    void SetGoldText(int gold);
    void SetPassiveText(int passivePoint);
}

public class UIGoodsModel
{
    public int getGold;
    public int totalGold;
    
    public int getPassivePoint;
    public int totalPassivePoint;
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
    
    public void SetPassiveText()
    {
        _goodsview.SetPassiveText(_model.totalPassivePoint);
    }
    
    public void PlusGoldText()
    {
        Debug.Log(_model.getGold);
        _goodsview.SetGoldText(_model.totalGold);
    }
    
    public void PlusPassiveText()
    {
        Debug.Log(_model.getPassivePoint);
        _goodsview.SetPassiveText(_model.totalPassivePoint);
    }
}

public class UIGoodsView : MonoBehaviour, IUIGoodsView
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text passivePointText;
    
    public void SetGoldText(int gold)
    {
        goldText.text = $"골드: {GameManager.Instance.GetThousandCommaText(gold)}";
    }
    
    public void SetPassiveText(int passivePoint)
    {
        passivePointText.text = $"특성 포인트: {GameManager.Instance.GetThousandCommaText(passivePoint)}";
    }
}
