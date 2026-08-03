using System;
using UnityEngine;

public class UI_StageClear : UIBase
{
    public UIStageClearView StageClearView => stageClearView;
    [SerializeField] private UIStageClearView stageClearView;
    private UIStageClearPresenter uiStageClearPresenter;
    public UIStageClearPresenter StageClearPresenter => uiStageClearPresenter;
    private Action action;
    
    public void SetStageClearPresenter(UIStageClearPresenter presenter)
    {
        uiStageClearPresenter = presenter;
    }

    public void ViewActive()
    {
        stageClearView.gameObject.SetActive(true);
    }
    
    private void Update()
    {
        if (stageClearView.IsNextButtonActive() && Input.GetKeyDown(KeyCode.Space))
            stageClearView.InvokeAction();
    }
}
