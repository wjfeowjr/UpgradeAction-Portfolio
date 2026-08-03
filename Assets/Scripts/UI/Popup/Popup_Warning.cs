using UnityEngine;

public class Popup_Warning : MonoBehaviour
{
    // 경고 팝업
    public IPopupWarningView WarningView => warningView;
    
    [SerializeField] private PopupWarningView warningView;
    private PopupWarningPresenter popupWarningPresenter;
    public PopupWarningPresenter PopupWarningPresenter => popupWarningPresenter;
    
    public void SetWarningPresenter(PopupWarningPresenter presenter)
    {
        popupWarningPresenter = presenter;
    }
}
