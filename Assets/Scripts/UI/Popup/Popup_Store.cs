using Unity.VisualScripting;
using UnityEngine;

public class Popup_Store : UIBase
{
    public PopupStoreView StoreView => storeView;

    [SerializeField] private PopupStoreView storeView;
    private PopupStorePresenter popupStorePresenter;

    public void SetStorePresenter(PopupStorePresenter presenter)
    {
        popupStorePresenter = presenter;
    }
    
    private void Update()
    {
        if (!openComplete)
            return;

        // 기존 PopupStoreView.Update의 입력(방향키/Enter)을 이곳에서 처리
        storeView.HandleInput();

        if (Input.GetKeyDown(GameManager.Instance.escKey))
        {
            // 구매 확인 팝업이 열려 있으면 ESC를 SpawnSelect가 처리하도록 넘김
            if (storeView.IsConfirmActive)
                return;

            popupStorePresenter.CloseStore();
        }
    }
}
