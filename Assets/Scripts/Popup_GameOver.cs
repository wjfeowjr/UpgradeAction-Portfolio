using System;
using UnityEngine;

public class Popup_GameOver : UIBase
{
    // 게임오버
    public IPopupGameOverView GameOverView => gameOverView;
    
    [SerializeField] private PopupGameOverView gameOverView;
    private PopupGameOverPresenter popupGameOverPresenter;

    public void SetGuidePresenter(PopupGameOverPresenter presenter)
    {
        popupGameOverPresenter = presenter;
    }
    
    private void Update()
    {
        if(openComplete)
            popupGameOverPresenter?.Restart();
    }
}
