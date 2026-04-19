using Unity.VisualScripting;
using UnityEngine;

public class Popup_Store : UIBase
{
    public IPopupStoreView StoreView => storeView;

    [SerializeField] private PopupStoreView storeView;
    private PopupStorePresenter popupStorePresenter;

    public void SetStorePresenter(PopupStorePresenter presenter)
    {
        popupStorePresenter = presenter;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 구매 확인 팝업이 열려 있으면 ESC를 SpawnSelect가 처리하도록 넘김
            if (storeView.IsConfirmActive)
                return;
    
            popupStorePresenter.CloseStore();
        }
    }
}
