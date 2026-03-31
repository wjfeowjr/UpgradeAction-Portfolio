using System;
using UnityEngine;

public class Popup_Character : UIBase
{
    // 특성 팝업
    public IPopupAttributeView AttributeView => attributeView;
    
    [SerializeField] private PopupAttributeView attributeView;
    private PopupAttributePresenter popupAttributePresenter;
    
    public PopupAttributePresenter PopupAttributePresenter => popupAttributePresenter;

    public void SetAttributePresenter(PopupAttributePresenter presenter)
    {
        popupAttributePresenter = presenter;
    }
}
