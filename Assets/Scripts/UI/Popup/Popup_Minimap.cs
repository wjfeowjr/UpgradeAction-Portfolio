using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Popup_Minimap : UIBase
{
    // 미니맵 팝업
    public IPopupMinimapView MinimapView => minimapView;

    [SerializeField] private PopupMinimapView minimapView;
    private PopupMinimapPresenter popupMinimapPresenter;
    public PopupMinimapPresenter PopupMinimapPresenter => popupMinimapPresenter;

    private Action closeAction;
    private bool isClosing;

    public void SetMinimapPresenter(PopupMinimapPresenter presenter)
    {
        popupMinimapPresenter = presenter;
    }

    // 미니맵 열기: CanvasGroup 페이드 인 (FadeOpen)
    public void OpenAction(Action close)
    {
        closeAction = close;
        isClosing = false;
        minimapView.SetMinimapActive(true);
        FadeOpen(true, false, 0.1f, false).Forget();
    }

    // 미니맵 닫기: CanvasGroup 페이드 아웃 (FadeClose)
    public async void CloseAction()
    {
        if (isClosing)
            return;

        isClosing = true;
        await FadeClose(true, false, 0.1f);
        closeAction?.Invoke();
    }

    private void Update()
    {
        if (!openComplete)
            return;

        popupMinimapPresenter.MoveAction();
        popupMinimapPresenter.CheckAction();

        // 기존 PopupMinimapView.Update의 닫기 입력을 openComplete 게이팅하여 이곳에서 처리
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(GameManager.Instance.escKey))
            CloseAction();
    }
}
