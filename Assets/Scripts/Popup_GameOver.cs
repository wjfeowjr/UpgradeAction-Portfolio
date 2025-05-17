using UnityEngine;

public class Popup_GameOver : UIBase
{
    // 게임오버
    public IUIGameOverView GameOverView => gameOverView;
    
    [SerializeField] private PopupGameOverView gameOverView;
    private PopupGameOverPresenter popupGameOverPresenter;
    
    // 프로퍼티
    public PopupGameOverPresenter PopupGameOverPresenter => popupGameOverPresenter;
    
    public void SetGameOverPresenter(PopupGameOverPresenter presenter)
    {
        popupGameOverPresenter = presenter;
    }
}
