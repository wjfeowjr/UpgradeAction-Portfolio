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
    
    private async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseView._IsSettingOpen)
                return;
            
            await ReductionClose(true, true);
            pausePresenter.HandleEsc();
        }
    }
}
