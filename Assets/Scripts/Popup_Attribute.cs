using System;
using UnityEngine;

public class Popup_Attribute : UIBase
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

    private void Update()
    {
        PopupAttributePresenter?.CloseAttribute();
        PopupAttributePresenter?.UpdateNavigation(); // ▼ 추가: 방향키 입력/반복 처리
    }
}
