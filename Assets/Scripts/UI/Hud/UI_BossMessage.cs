using UnityEngine;

public class UI_BossMessage : UIBase
{
    public UIBossMessageView BossMessageView => bossMessageView;
    [SerializeField] private UIBossMessageView bossMessageView;
    private UIBossMessagePresenter uiBossMessagePresenter;
    public UIBossMessagePresenter BossMessagePresenter => uiBossMessagePresenter;
    
    public void SetEpisodePresenter(UIBossMessagePresenter presenter)
    {
        uiBossMessagePresenter = presenter;
    }
    
    public void ViewActive()
    {
        bossMessageView.gameObject.SetActive(true);
    }
}
