using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Popup_GameOver : UIBase
{
    // 게임오버
    public PopupGameOverView GameOverView => gameOverView;
    
    [SerializeField] private PopupGameOverView gameOverView;
    private PopupGameOverPresenter popupGameOverPresenter;

    public void SetGuidePresenter(PopupGameOverPresenter presenter)
    {
        popupGameOverPresenter = presenter;
    }

    public override async UniTask FadeOpen(bool timeStop, bool controlStop, float time, bool fadeSound = true)
    {
        popupGameOverPresenter?.BgActive(false);
        await base.FadeOpen(timeStop, controlStop, time, fadeSound);
        popupGameOverPresenter?.BgActive(true);
    }

    private void Update()
    {
        if(openComplete)
            popupGameOverPresenter?.Restart();
    }
}
