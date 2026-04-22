using UnityEngine;

public class Popup_Pause : UIBase
{
    public PopupPauseView PauseView => pauseView;
    [SerializeField] private PopupPauseView pauseView;

    private PopupPausePresenter pausePresenter;

    public void SetPausePresenter(PopupPausePresenter presenter)
    {
        pausePresenter = presenter;
    }
}
