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

    [SerializeField] private ExpansionUiObject[] volumeFrames;

    private PopupAudioPresenter _presenter;
    private PopupCommonActions  _commonActions;
    //private UIBase _ownerPopup; // 마우스 상호작용 (보류)
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
        if (InputHelper.GetEnterDown() || InputHelper.GetKeypadEnterDown())
            HandleEnter();
        if (Input.GetKeyDown(GameManager.Instance.escKey))
            _presenter.HandleEsc();
    }

    private void HandleEnter()
    {
        switch (_cursor)
        {
            case 3:
                GameManager.Instance.SetDefaultAudio();
                RefreshVolumeData();

                float newVolume1 = GameManager.Instance.masterVolume;
                volumeFrames[0].GetComponent<VolumeFrame>().ChangeVolume(newVolume1);
                ApplyVolume(0, newVolume1);
                
                float newVolume2 = GameManager.Instance.sfxVolume;
                volumeFrames[1].GetComponent<VolumeFrame>().ChangeVolume(newVolume2);
                ApplyVolume(1, newVolume2);
                
                float newVolume3 = GameManager.Instance.bgmVolume;
                volumeFrames[2].GetComponent<VolumeFrame>().ChangeVolume(newVolume3);
                ApplyVolume(2, newVolume3);
                _commonActions?.PlaySelectSound?.Invoke();
                break;

            case 4:
                _presenter.HandleEsc();
                break;
        }
    }
    
    private void SetTextVolumeFrames()
    {
        if (volumeFrames.Length > 0)
            volumeFrames[0].SetText(GameManager.Instance.GetTalk(30030));
        if (volumeFrames.Length > 1)
            volumeFrames[1].SetText(GameManager.Instance.GetTalk(30031));
        if (volumeFrames.Length > 2)
            volumeFrames[2].SetText(GameManager.Instance.GetTalk(30032));
        if (volumeFrames.Length > 3)
            volumeFrames[3].SetText(GameManager.Instance.GetTalk(30069));
        if (volumeFrames.Length > 4)
            volumeFrames[4].SetText(GameManager.Instance.GetTalk(30070));
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

        // 볼륨 항목이 아닌 커서(닫기 등)에서는 무시
        var volumeFrame = volumeFrames[_cursor].GetComponent<VolumeFrame>();
        if (!volumeFrame)
            return;

        float newVolume = Mathf.Clamp(Mathf.Round((volumeFrame.CurrentVolume + dir * VolumeStep) * 10f) / 10f, VolumeMin, VolumeMax);

        if (Mathf.Approximately(newVolume, volumeFrame.CurrentVolume))
            return;

        volumeFrame.ChangeVolume(newVolume);
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
            volumeFrames[0].GetComponent<VolumeFrame>().SetData(GameManager.Instance.masterVolume);
        if (volumeFrames.Length > 1)
            volumeFrames[1].GetComponent<VolumeFrame>().SetData(GameManager.Instance.sfxVolume);
        if (volumeFrames.Length > 2)
            volumeFrames[2].GetComponent<VolumeFrame>().SetData(GameManager.Instance.bgmVolume);
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
        //SetMouseInteraction(); // 마우스 상호작용 (보류)
    }

    // ── 마우스 상호작용 (보류) ── 재활성화 시 아래 주석 해제
    /*
    // 항목 호버/클릭과 좌우 화살표 클릭(볼륨 조절) 연결
    private void SetMouseInteraction()
    {
        _ownerPopup = GetComponentInParent<UIBase>();

        for (int i = 0; i < volumeFrames.Length; i++)
        {
            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(volumeFrames[i],
                onHover: () => MoveCursorTo(index),
                onClick: () =>
                {
                    if (!CanMouseInput())
                        return;

                    MoveCursorTo(index);
                    HandleEnter(); // 버튼 항목만 동작 (볼륨 항목은 케이스 없음)
                });

            // 볼륨 항목의 좌/우 화살표 클릭 → 볼륨 증감
            var frame = volumeFrames[i].GetComponent<VolumeFrame>();
            if (!frame)
                continue;

            MouseSelectable.Attach(frame.LeftArrow,  onHover: null, onClick: () => ClickVolume(index, -1));
            MouseSelectable.Attach(frame.RightArrow, onHover: null, onClick: () => ClickVolume(index, +1));
        }
    }

    // 화살표 클릭으로 해당 항목의 볼륨 증감
    private void ClickVolume(int index, int dir)
    {
        if (!CanMouseInput())
            return;

        MoveCursorTo(index);
        HandleVolume(dir);
    }

    // 마우스 호버로 커서 이동 (키보드 커서 이동과 동일한 연출)
    private void MoveCursorTo(int index)
    {
        if (!CanMouseInput())
            return;

        if (_cursor == index)
            return;

        _cursor = index;
        _commonActions?.PlayMoveSound?.Invoke();
        RefreshCursors();
    }

    // 팝업 열림 연출이 끝난 뒤에만 마우스 입력 허용
    private bool CanMouseInput() => _presenter != null && _ownerPopup && _ownerPopup.OpenComplete;
    */
}
