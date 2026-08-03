using UnityEngine;

public class Popup_FastTravel : UIBase
{
    // 패스트 트래블(세이브 포인트 이동) 팝업
    public IPopupFastTravelView FastTravelView => fastTravelView;

    [SerializeField] private PopupFastTravelView fastTravelView;
    private PopupFastTravelPresenter popupFastTravelPresenter;
    public PopupFastTravelPresenter PopupFastTravelPresenter => popupFastTravelPresenter;

    public void SetFastTravelPresenter(PopupFastTravelPresenter presenter)
    {
        popupFastTravelPresenter = presenter;
    }

    private void Update()
    {
        if(openComplete)
            popupFastTravelPresenter?.Tick();
    }
}
