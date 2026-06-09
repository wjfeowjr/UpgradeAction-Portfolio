using UnityEngine;
using UnityEngine.Serialization;

public class Popup_Select : UIBase
{
    // 가이드 팝업
    public IPopupSelectView SelectView => selectView;
    
    [SerializeField] private PopupSelectView selectView;
    private PopupSelectPresenter popupSelectPresenter;

    public void SetSelectPresenter(PopupSelectPresenter presenter)
    {
        popupSelectPresenter = presenter;
    }

    private void Update()
    {
        if (openComplete)
            selectView.HandleInput();
    }
}
