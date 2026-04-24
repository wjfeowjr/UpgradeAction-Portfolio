using System;
using UnityEngine;

// ── Model ─────────────────────────────────────────────────────────────────────
public class PopupAudioModel
{
    public Action closeAction;
    public PopupCommonActions commonActions;
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IPopupAudioView
{
    void SetAction(PopupAudioPresenter presenter, PopupCommonActions commonActions);
}

// ── Presenter ─────────────────────────────────────────────────────────────────
public class PopupAudioPresenter
{
    private readonly IPopupAudioView _view;
    private readonly PopupAudioModel _model;

    public PopupAudioPresenter(IPopupAudioView view, PopupAudioModel model)
    {
        _view  = view;
        _model = model;
    }

    public void SetAction() => _view.SetAction(this, _model.commonActions);
    public void HandleEsc()
    {
        _model.closeAction?.Invoke();
        _model.commonActions.PlayCancelSound?.Invoke();
    }
}

// ── View ──────────────────────────────────────────────────────────────────────
public class PopupAudioView : MonoBehaviour, IPopupAudioView
{
    private const float VolumeStep = 0.1f;
    private const float VolumeMin  = 0.0f;
    private const float VolumeMax  = 1.0f;

    [SerializeField] private VolumeFrame[] volumeFrames;

    private PopupAudioPresenter _presenter;
    private PopupCommonActions  _commonActions;
    private int _cursor = 0;

    private void OnEnable()
    {
        _cursor = 0;
        SetTextVolumeFrames();
        RefreshVolumeData();
        RefreshCursors();
    }

    private void Update()
    {
        if (_presenter == null)
            return;

        if (Input.GetKeyDown(GameManager.Instance.upKey))
            HandleArrow(-1);
        if (Input.GetKeyDown(GameManager.Instance.downKey))
            HandleArrow(+1);
        if (Input.GetKeyDown(GameManager.Instance.leftKey))
            HandleVolume(-1);
        if (Input.GetKeyDown(GameManager.Instance.rightKey))
            HandleVolume(+1);
        if (Input.GetKeyDown(KeyCode.Escape))
            _presenter.HandleEsc();
    }
    
    private void SetTextVolumeFrames()
    {
        if (volumeFrames.Length > 0)
            volumeFrames[0].SetText(GameManager.Instance.GetTalk(30030));
        if (volumeFrames.Length > 1)
            volumeFrames[1].SetText(GameManager.Instance.GetTalk(30031));
        if (volumeFrames.Length > 2)
            volumeFrames[2].SetText(GameManager.Instance.GetTalk(30032));
    }

    private void HandleArrow(int dir)
    {
        if (volumeFrames.Length == 0)
            return;

        _cursor = (_cursor + dir + volumeFrames.Length) % volumeFrames.Length;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    private void HandleVolume(int dir)
    {
        if (volumeFrames.Length == 0)
            return;

        var frame      = volumeFrames[_cursor];
        float newVolume = Mathf.Clamp(Mathf.Round((frame.CurrentVolume + dir * VolumeStep) * 10f) / 10f, VolumeMin, VolumeMax);

        if (Mathf.Approximately(newVolume, frame.CurrentVolume))
            return;

        frame.ChangeVolume(newVolume);
        ApplyVolume(_cursor, newVolume);
        _commonActions?.PlayMoveSound?.Invoke();
    }

    // 인덱스에 따라 VolumeManager 호출 + GameManager 필드 갱신 + 저장
    private void ApplyVolume(int index, float volume)
    {
        switch (index)
        {
            case 0:
                VolumeManager.Instance.SetMasterVolume(volume);
                GameManager.Instance.masterVolume = volume;
                VolumeBinding.SaveVolume(ConstValues.MasterVolume, volume);
                break;
            case 1:
                VolumeManager.Instance.SetSfxVolume(volume);
                GameManager.Instance.sfxVolume = volume;
                VolumeBinding.SaveVolume(ConstValues.SFXVolume, volume);
                break;
            case 2:
                VolumeManager.Instance.SetBGMVolume(volume);
                GameManager.Instance.bgmVolume = volume;
                VolumeBinding.SaveVolume(ConstValues.BGMVolume, volume);
                break;
        }
    }

    private void RefreshVolumeData()
    {
        if (volumeFrames.Length > 0)
            volumeFrames[0].SetData(GameManager.Instance.masterVolume);
        if (volumeFrames.Length > 1)
            volumeFrames[1].SetData(GameManager.Instance.sfxVolume);
        if (volumeFrames.Length > 2)
            volumeFrames[2].SetData(GameManager.Instance.bgmVolume);
    }

    private void RefreshCursors()
    {
        for (int i = 0; i < volumeFrames.Length; i++)
        {
            if (i == _cursor)
            {
                volumeFrames[i].SelectObjectActive(true);
                volumeFrames[i].Expansion(1.1f);
            }
            else
            {
                volumeFrames[i].SelectObjectActive(false);
                volumeFrames[i].Reduction();
            }
        }
    }

    // IPopupAudioView
    public void SetAction(PopupAudioPresenter presenter, PopupCommonActions commonActions)
    {
        _presenter     = presenter;
        _commonActions = commonActions;
    }
}
