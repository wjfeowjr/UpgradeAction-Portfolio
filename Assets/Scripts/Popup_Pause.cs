using UnityEngine;

public class Popup_Pause : UIBase
{
    public PopupPauseView      PauseView      => pauseView;
    public PopupPausePresenter PausePresenter => pausePresenter;

    [SerializeField] private PopupPauseView pauseView;
    private PopupPausePresenter pausePresenter;

    public void SetPausePresenter(PopupPausePresenter presenter)
    {
        pausePresenter = presenter;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseView._IsSettingOpen)
                return;
            
            pausePresenter.HandleEsc();
        }
    }
}
