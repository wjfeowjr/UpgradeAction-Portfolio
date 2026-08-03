using UnityEngine;

public class Popup_Guide : UIBase
{
    // 가이드 팝업
    public IPopupGuideView GuideView => guideView;
    
    [SerializeField] private PopupGuideView guideView;
    private PopupGuidePresenter popupGuidePresenter;

    public void SetGuidePresenter(PopupGuidePresenter presenter)
    {
        popupGuidePresenter = presenter;
    }
    
    private void Update()
    {
        if(openComplete)
            popupGuidePresenter?.Close();
    }
}
