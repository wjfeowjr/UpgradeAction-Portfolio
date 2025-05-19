using UnityEngine;

public class UI_Episode : UIBase
{
    public UIEpisodeView EpisodeView => episodeView;
    [SerializeField] private UIEpisodeView episodeView;
    private UIEpisodePresenter uiEpisodePresenter;
    public UIEpisodePresenter EpisodePresenter => uiEpisodePresenter;
    
    public void SetEpisodePresenter(UIEpisodePresenter presenter)
    {
        uiEpisodePresenter = presenter;
    }
}
