using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public enum eSettingState
{
    Setting,
    Game,
    Audio,
    Video,
    Keyboard,
}

public class Popup_Setting : UIBase
{
    [SerializeField] private TMP_Text settingText;
    [SerializeField] private TMP_Text selectText;
    [SerializeField] private TMP_Text backText;
    
    [SerializeField] private PopupSettingView  settingView;
    [SerializeField] private PopupGameView     gameView;
    [SerializeField] private PopupAudioView    audioView;
    [SerializeField] private PopupVideoView    videoView;
    [SerializeField] private PopupKeyboardView keyboardView;
    
    private Action languageChangeAction;
    private Action keyChangeAction;
    private Action closeAction;
    private eSettingState settingState;

    private void OnEnable()
    {
        LanguageChange();
    }

    private void LanguageChange()
    {
        SetSettingText();
        selectText.text = string.Format(GameManager.Instance.GetTalk(30103), GameManager.Instance.GetKeyCode(GameManager.Instance.confirmKey));
        backText.text = string.Format(GameManager.Instance.GetTalk(30104), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey));
    }

    private void SetSettingText()
    {
        switch (settingState)
        {
            case eSettingState.Setting:
                settingText.text = GameManager.Instance.GetTalk(30026);
                break;
            
            case eSettingState.Game:
                settingText.text = GameManager.Instance.GetTalk(30053);
                break;
            
            case eSettingState.Audio:
                settingText.text = GameManager.Instance.GetTalk(30054);
                break;

            case eSettingState.Video:
                settingText.text = GameManager.Instance.GetTalk(30055);
                break;

            case eSettingState.Keyboard:
                settingText.text = GameManager.Instance.GetTalk(30056);
                break;
        }
    }

    private void SetState(eSettingState state)
    {
        settingState = state;
        settingView.gameObject.SetActive(state == eSettingState.Setting);
        gameView.gameObject.SetActive(state == eSettingState.Game);
        audioView.gameObject.SetActive(state == eSettingState.Audio);
        videoView.gameObject.SetActive(state == eSettingState.Video);
        keyboardView.gameObject.SetActive(state == eSettingState.Keyboard);
        SetSettingText();
    }

    public void InitPresenters(Action close, Action languageChange, Action keyChange)
    {
        languageChangeAction = languageChange;
        keyChangeAction = keyChange;
        closeAction = close;
        SetState(eSettingState.Setting);

        var common = new PopupCommonActions
        {
            PlayMoveSound   = () => SoundManager.Instance.PlaySound(ConstValues.Jump1,        true),
            PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true),
            PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton,  true),
        };

        // GameView
        var gameModel = new PopupGameModel
        {
            languageChangeAction = () =>
            {
                languageChangeAction?.Invoke();
                LanguageChange();
            },
            closeAction   = () => SetState(eSettingState.Setting),
            commonActions = common,
        };
        var gamePresenter = new PopupGamePresenter(gameView.ConvertTo<IPopupGameView>(), gameModel);
        gamePresenter.SetAction();

        // AudioView
        var audioModel = new PopupAudioModel
        {
            closeAction   = () => SetState(eSettingState.Setting),
            commonActions = common,
        };
        var audioPresenter = new PopupAudioPresenter(audioView.ConvertTo<IPopupAudioView>(), audioModel);
        audioPresenter.SetAction();

        // VideoView
        var videoModel = new PopupVideoModel
        {
            closeAction   = () => SetState(eSettingState.Setting),
            commonActions = common,
        };
        var videoPresenter = new PopupVideoPresenter(videoView.ConvertTo<IPopupVideoView>(), videoModel);
        videoPresenter.SetAction();

        // KeyboardView
        var keyboardModel = new PopupKeyboardModel
        {
            closeAction   = () =>
            {
                keyChangeAction?.Invoke();
                SetState(eSettingState.Setting);
            },
            commonActions = common,
        };
        var keyboardPresenter = new PopupKeyboardPresenter(keyboardView.ConvertTo<IPopupKeyboardView>(), keyboardModel);
        keyboardPresenter.SetAction();

        // SettingView
        var settingModel = new PopupSettingModel
        {
            openGameAction     = () => SetState(eSettingState.Game),
            openAudioAction    = () => SetState(eSettingState.Audio),
            openVideoAction    = () => SetState(eSettingState.Video),
            openKeyboardAction = () => SetState(eSettingState.Keyboard),
            commonActions      = common,
        };
        var settingPresenter = new PopupSettingPresenter(settingView.ConvertTo<IPopupSettingView>(), settingModel);
        settingPresenter.SetAction();
    }

    private async void Update()
    {
        if (openComplete && Input.GetKeyDown(KeyCode.Escape) && settingState == eSettingState.Setting)
        {
            await FadeClose(false, false, 0.2f, true);
            closeAction?.Invoke();
        }
    }
}
