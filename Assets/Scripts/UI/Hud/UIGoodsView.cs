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
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    // 호출부가 인터페이스 변환 -> Model 생성 -> Presenter 생성 -> 역주입을
    // 매번 반복하던 것을 한 줄로 줄인다.
    private UIGoodsPresenter presenter;
    public UIGoodsPresenter Presenter => presenter;

    public UIGoodsPresenter Bind(UIGoodsModel model)
    {
        presenter = new UIGoodsPresenter(this, model);
        return presenter;
    }

    [SerializeField] private TMP_Text goldText;

    public void SetGoldText(int gold)
    {
        goldText.text = $"{GameManager.Instance.GetThousandCommaText(gold)}";
    }
}
