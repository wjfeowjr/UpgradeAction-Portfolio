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
        if (!openComplete)
            return;

        // 기존 PopupPauseView.Update의 입력(방향키/Enter)을 이곳에서 처리
        pauseView.HandleInput();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseView._IsSettingOpen)
                return;

            pausePresenter.HandleEsc();
        }
    }
}
