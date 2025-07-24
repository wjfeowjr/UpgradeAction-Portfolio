using System;
using UnityEngine;

public class Popup_Minimap : UIBase
{
    // 미니맵 팝업
    public IPopupMinimapView MinimapView => minimapView;
    
    [SerializeField] private PopupMinimapView minimapView;
    private PopupMinimapPresenter popupMinimapPresenter;
    public PopupMinimapPresenter PopupMinimapPresenter => popupMinimapPresenter;
    
    public void SetMinimapPresenter(PopupMinimapPresenter presenter)
    {
        popupMinimapPresenter = presenter;
    }

    private void Update()
    {
        popupMinimapPresenter.CloseMinimap();
        popupMinimapPresenter.MoveAction();
        popupMinimapPresenter.CheckAction();
    }
}
