using UnityEngine;

public class Popup_Guide : UIBase
{
    // 가이드 팝업
    public IUIGuideView GuideView => guideView;
    
    [SerializeField] private PopupGuideView guideView;
    private PopupGuidePresenter popupGuidePresenter;
    
    // 프로퍼티
    public PopupGuidePresenter PopupGameOverPresenter => popupGuidePresenter;
    
    public void SetGuidePresenter(PopupGuidePresenter presenter)
    {
        popupGuidePresenter = presenter;
    }
}
