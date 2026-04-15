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
            ReductionClose(true, true);
        }
    }
}
