using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text saveSelectText;

    [SerializeField] private TMP_Text selectText;
    [SerializeField] private TMP_Text backText;
    [SerializeField] private TMP_Text deleteText;
    [SerializeField] private TMP_Text copyText;

    [SerializeField] private GameObject titleObject;
    [SerializeField] private GameObject saveSelectObject;

    [SerializeField] private ExpansionUiObject[] titleButtons;
    [SerializeField] private SaveFrame[] saveFrames;

    private CancellationTokenSource fadeCancellation;

    [SerializeField] private int  _cursor           = 0;
    [SerializeField] private int  _saveSelectCursor = 0;
    [SerializeField] private int  _copySrcCursor    = 0;

    [SerializeField] private bool _isSceneChange    = false;
    [SerializeField] private bool _isSaveSelect     = false;
    [SerializeField] private bool _isConfirmActive  = false;
    [SerializeField] private bool _isCopyMode       = false;

    private async void Start()
    {
        Time.timeScale = 1.0f;
        // 저장된 해상도가 모니터보다 크면 모니터가 수용하는 가장 큰 해상도로 보정해서 적용 (저장값은 유지)
        Vector2Int resolution = PopupVideoView.ClampToDisplay(GameManager.Instance.resolutionX, GameManager.Instance.resolutionY);
        Screen.SetResolution(resolution.x, resolution.y,
            GameManager.Instance.fullScreen == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        QualitySettings.vSyncCount = GameManager.Instance.vSync;
        GameManager.Instance.InGame = false;
        if (SceneChanger.Instance)
            SceneChanger.Instance.TitleScene = true;

        StartSetting();
        //SetMouseInteraction(); // 마우스 상호작용 (보류)

        if (await GameManager.Instance.Fading(1, 0, 0.5f, true, ConstValues.BlackColor).SuppressCancellationThrow())
            return;
        
        StartBGM();
    }

    private void Update()
    {
        if (_isSceneChange)
            return;

        if (_isConfirmActive)
            return;

        if (_isSaveSelect)
        {
            if (Input.GetKeyDown(GameManager.Instance.leftKey))
                HandleArrow(-1);
            if (Input.GetKeyDown(GameManager.Instance.rightKey))
                HandleArrow(+1);
            if (Input.GetKeyDown(KeyCode.X) && !_isCopyMode)
                HandleDelete();
            if (Input.GetKeyDown(KeyCode.C) && !_isCopyMode)
                EnterCopyMode();
        }
        else
        {
            if (Input.GetKeyDown(GameManager.Instance.upKey))
                HandleArrow(-1);
            if (Input.GetKeyDown(GameManager.Instance.downKey))
                HandleArrow(+1);
        }
        if (InputHelper.GetEnterDown())
            HandleEnter();
        if (Input.GetKeyDown(GameManager.Instance.escKey))
            HandleEsc();
    }

    // ── 마우스 상호작용 (보류) ── 재활성화 시 아래 주석 해제
    /*
    // 타이틀 버튼과 세이브 슬롯에 마우스 호버/클릭 연결
    private void SetMouseInteraction()
    {
        for (int i = 0; i < titleButtons.Length; i++)
        {
            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(titleButtons[i],
                onHover: () => MoveCursorTo(index),
                onClick: () =>
                {
                    if (_isSceneChange || _isConfirmActive || _isSaveSelect)
                        return;

                    MoveCursorTo(index);
                    HandleEnter();
                });
        }

        for (int i = 0; i < saveFrames.Length; i++)
        {
            int index = i; // 클로저 캡처용
            MouseSelectable.Attach(saveFrames[i],
                onHover: () => MoveSaveCursorTo(index),
                onClick: () =>
                {
                    if (_isSceneChange || _isConfirmActive || !_isSaveSelect)
                        return;

                    // 복사 모드에서 원본 슬롯은 선택 불가
                    if (_isCopyMode && index == _copySrcCursor)
                        return;

                    MoveSaveCursorTo(index);
                    HandleEnter();
                });
        }
    }

    // 마우스 호버로 타이틀 커서 이동 (키보드 커서 이동과 동일한 연출)
    private void MoveCursorTo(int index)
    {
        if (_isSceneChange || _isConfirmActive || _isSaveSelect)
            return;

        if (_cursor == index)
            return;

        _cursor = index;
        SoundManager.Instance.PlaySound(ConstValues.Jump1);
        RefreshCursors();
    }

    // 마우스 호버로 세이브 슬롯 커서 이동
    private void MoveSaveCursorTo(int index)
    {
        if (_isSceneChange || _isConfirmActive || !_isSaveSelect)
            return;

        // 복사 모드에서 원본 슬롯은 선택 불가
        if (_isCopyMode && index == _copySrcCursor)
            return;

        if (_saveSelectCursor == index)
            return;

        _saveSelectCursor = index;
        SoundManager.Instance.PlaySound(ConstValues.Jump1);
        RefreshSaveFrameCursors();
    }
    */

    private void HandleArrow(int dir)
    {
        var targets = _isSaveSelect ? saveFrames.Length : titleButtons.Length;
        if (targets == 0)
            return;

        if (_isSaveSelect)
        {
            int next = (_saveSelectCursor + dir + targets) % targets;
            if (_isCopyMode && next == _copySrcCursor)
                next = (next + dir + targets) % targets;
            _saveSelectCursor = next;
        }
        else
        {
            _cursor = (_cursor + dir + targets) % targets;
        }

        SoundManager.Instance.PlaySound(ConstValues.Jump1);

        if (_isSaveSelect)
            RefreshSaveFrameCursors();
        else
            RefreshCursors();
    }

    private void HandleEnter()
    {
        if (_isSaveSelect)
        {
            if (_isCopyMode)
            {
                GameManager.Instance.CopyData(_copySrcCursor + 1, _saveSelectCursor + 1);
                _isCopyMode = false;
                for (int i = 0; i < saveFrames.Length; i++)
                    saveFrames[i].SetData(GameManager.Instance.SaveFileName(i + 1), i + 1);
                RefreshSaveFrameCursors();
                SoundManager.Instance.PlaySound(ConstValues.NormalButton2);
                saveSelectText.text = GameManager.Instance.GetTalk(30062);
                return;
            }
            _isSceneChange = true;
            GameManager.Instance.SaveFileName(_saveSelectCursor + 1);
            GameManager.Instance.GameStart();
            return;
        }

        if (_cursor == 0)
            OpenSaveSelect();
        if (_cursor == 1)
            OpenSettingPopup();
        if (_cursor == 2)
            QuitGame();

        SoundManager.Instance.PlaySound(ConstValues.NormalButton2);
    }

    private void HandleDelete()
    {
        if (string.IsNullOrEmpty(GameManager.Instance.SaveFileName(_saveSelectCursor + 1)))
            return;

        _isConfirmActive = true;
        GameManager.Instance.SpawnSelect(
            GameManager.Instance.GetTalk(41002),
            null,
            0,
            yesAction: () =>
            {
                GameManager.Instance.DeleteData();
                for (int i = 0; i < saveFrames.Length; i++)
                    saveFrames[i].SetData(GameManager.Instance.SaveFileName(i + 1), i + 1);
                RefreshSaveFrameCursors();
                _isConfirmActive = false;
                SoundManager.Instance.PlaySound(ConstValues.NormalButton2);
            },
            noAction: () =>
            {
                _isConfirmActive = false;
            }
        );
    }

    private void HandleEsc()
    {
        if (_isCopyMode)
        {
            _isCopyMode       = false;
            _saveSelectCursor = _copySrcCursor;
            RefreshSaveFrameCursors();
            saveSelectText.text = GameManager.Instance.GetTalk(30062);
            SoundManager.Instance.PlaySound(ConstValues.NormalButton);
            return;
        }

        if (!_isSaveSelect)
            return;

        titleObject.SetActive(true);
        saveSelectObject.SetActive(false);
        _isSaveSelect     = false;
        _saveSelectCursor = 0;
        SoundManager.Instance.PlaySound(ConstValues.NormalButton);
    }

    private void EnterCopyMode()
    {
        if (string.IsNullOrEmpty(GameManager.Instance.SaveFileName(_saveSelectCursor + 1)))
            return;

        _isCopyMode       = true;
        _copySrcCursor    = _saveSelectCursor;

        // 소스 슬롯 바로 다음으로 커서 이동 (소스 슬롯 건너뜀)
        int targets       = saveFrames.Length;
        _saveSelectCursor = (_copySrcCursor + 1) % targets;

        saveSelectText.text = GameManager.Instance.GetTalk(30063);
        SoundManager.Instance.PlaySound(ConstValues.NormalButton2);
        RefreshSaveFrameCursors();
    }

    private void QuitGame()
    {
        _isConfirmActive = true;
        GameManager.Instance.SpawnSelect(
            GameManager.Instance.GetTalk(41003),
            null,
            0,
            yesAction: () =>
            {
                Application.Quit();
            },
            noAction: () =>
            {
                _isConfirmActive = false;
            },
            false
        );
    }

    private void OpenSettingPopup()
    {
        _isConfirmActive = true;
        var popup = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Setting, Vector3.zero).GetComponent<Popup_Setting>();
        popup.FadeOpen(false, false, 0.2f, false).Forget();
        popup.InitPresenters(() => { _isConfirmActive = false; }, LanguageSetting, null);
    }

    private void OpenSaveSelect()
    {
        titleObject.SetActive(false);
        saveSelectObject.SetActive(true);
        _isSaveSelect = true;

        for (int i = 0; i < saveFrames.Length; i++)
        {
            saveFrames[i].gameObject.SetActive(true);
            saveFrames[i].SetData(GameManager.Instance.SaveFileName(i + 1), i + 1);
        }

        _saveSelectCursor = 0;
        RefreshSaveFrameCursors();
    }

    private void RefreshCursors()
    {
        for (int i = 0; i < titleButtons.Length; i++)
        {
            if (i == _cursor)
            {
                titleButtons[i].SelectObjectActive(true);
                titleButtons[i].Expansion(1.1f);
            }
            else
            {
                titleButtons[i].SelectObjectActive(false);
                titleButtons[i].Reduction();
            }
        }
    }

    private void RefreshSaveFrameCursors()
    {
        for (int i = 0; i < saveFrames.Length; i++)
        {
            bool isSelected = i == _saveSelectCursor;
            bool isSrc      = _isCopyMode && i == _copySrcCursor;

            if (isSelected)
            {
                saveFrames[i].SelectObjectActive(true);
                saveFrames[i].Expansion(1.05f);
            }
            else if (isSrc)
            {
                saveFrames[i].SelectObjectActive(true);
                saveFrames[i].Reduction();
            }
            else
            {
                saveFrames[i].SelectObjectActive(false);
                saveFrames[i].Reduction();
            }
        }
    }

    private void StartSetting()
    {
        LanguageSetting();
        _cursor = 0;
        RefreshCursors();
    }

    private void LanguageSetting()
    {
        titleText.text = GameManager.Instance.GetTalk(10000);
        saveSelectText.text = GameManager.Instance.GetTalk(30062);
        selectText.text = string.Format(GameManager.Instance.GetTalk(30103), GameManager.Instance.GetKeyCode(GameManager.Instance.enterKey));
        backText.text = string.Format(GameManager.Instance.GetTalk(30104), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey));
        deleteText.text = string.Format(GameManager.Instance.GetTalk(30111), GameManager.Instance.GetKeyCode(GameManager.Instance.deleteKey));
        copyText.text = string.Format(GameManager.Instance.GetTalk(30112), GameManager.Instance.GetKeyCode(GameManager.Instance.copyKey));

        titleButtons[0].SetText(GameManager.Instance.GetTalk(30025));
        titleButtons[1].SetText(GameManager.Instance.GetTalk(30026));
        titleButtons[2].SetText(GameManager.Instance.GetTalk(30027));
    }

    private void StartBGM()
    {
        BgmManager.Instance.PlayBgm($"{ConstValues.BGM}_{ConstValues.Title}", true);
    }

    // private async void TextFade()
    // {
    //     fadeCancellation = new CancellationTokenSource();
    //     float fadeTime = 1.0f;
    //     while (true)
    //     {
    //         startText.DOFade(0, fadeTime);
    //         if (await NormalDelay(fadeTime, fadeCancellation).SuppressCancellationThrow())
    //             return;
    //
    //         startText.DOFade(1, fadeTime);
    //         if (await NormalDelay(fadeTime, fadeCancellation).SuppressCancellationThrow())
    //             return;
    //     }
    // }

    // private void AnyKeyStart()
    // {
    //     if (Input.GetKeyDown(KeyCode.F1))
    //     {
    //         //PlayerPrefs.DeleteAll();
    //         GameManager.Instance.DefaultSkillKeySetting();
    //         GameManager.Instance.FirstStart();
    //     }
    //
    //     // 아무 키 누르기
    //     if (!Input.anyKeyDown)
    //         return;
    //
    //     // 마우스 클릭은 제외
    //     if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
    //         return;
    //
    //     GameManager.Instance.GoScene(ConstValues.BattleScene);
    // }

    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
