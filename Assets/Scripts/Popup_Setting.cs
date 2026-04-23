using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public enum eSettingState
{
    Setting,
    Game,
    Audio,
    Keyboard,
}

public class Popup_Setting : UIBase
{
    [SerializeField] private TMP_Text settingText;

    [SerializeField] private PopupSettingView  settingView;
    [SerializeField] private PopupGameView     gameView;
    [SerializeField] private PopupAudioView    audioView;
    [SerializeField] private PopupKeyboardView keyboardView;

    private Action        closeAction;
    private eSettingState settingState;

    public void SetState(eSettingState state)
    {
        settingState = state;
        settingView.gameObject.SetActive(state == eSettingState.Setting);
        gameView.gameObject.SetActive(state == eSettingState.Game);
        audioView.gameObject.SetActive(state == eSettingState.Audio);
        keyboardView.gameObject.SetActive(state == eSettingState.Keyboard);
    }

    public void InitPresenters(Action close)
    {
        closeAction = close;
        settingText.text = "설정_";

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

        // KeyboardView
        var keyboardModel = new PopupKeyboardModel
        {
            closeAction   = () => SetState(eSettingState.Setting),
            commonActions = common,
        };
        var keyboardPresenter = new PopupKeyboardPresenter(keyboardView.ConvertTo<IPopupKeyboardView>(), keyboardModel);
        keyboardPresenter.SetAction();

        // SettingView
        var settingModel = new PopupSettingModel
        {
            openGameAction     = () => SetState(eSettingState.Game),
            openAudioAction    = () => SetState(eSettingState.Audio),
            openKeyboardAction = () => SetState(eSettingState.Keyboard),
            commonActions      = common,
        };
        var settingPresenter = new PopupSettingPresenter(settingView.ConvertTo<IPopupSettingView>(), settingModel);
        settingPresenter.SetAction();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && settingState == eSettingState.Setting)
        {
            ReductionClose(false, false);
            closeAction?.Invoke();
        }
    }
}
