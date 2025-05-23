using UnityEngine;

public class Popup_GameOver : UIBase
{
    // 게임오버
    public IUIGameOverView GameOverView => gameOverView;
    
    [SerializeField] private PopupGameOverView gameOverView;
}
