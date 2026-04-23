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

    [SerializeField] private GameObject titleObject;
    [SerializeField] private GameObject saveSelectObject;
    
    [SerializeField] private ExpansionUiObject[] titleButtons;
    [SerializeField] private SaveFrame[] saveFrames; 
    
    private CancellationTokenSource fadeCancellation;

    private int _cursor = 0;
    private bool _isSaveSelect = false;
    private bool _isConfirmActive = false;
    private bool _isCopyMode = false;
    private int _copySrcCursor = 0;

    private async void Start()
    {
        Time.timeScale = 1.0f;
        GameManager.Instance.InGame = false;
        if (SceneChanger.Instance)
            SceneChanger.Instance.TitleScene = true;

        StartSetting();
        
        await GameManager.Instance.Fading(1, 0, 0.5f, true, ConstValues.BlackColor);
        StartBGM();
    }

    private void Update()
    {
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
        if (Input.GetKeyDown(KeyCode.Return))
            HandleEnter();
        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEsc();
    }

    private void HandleArrow(int dir)
    {
        var targets = _isSaveSelect ? saveFrames.Length : titleButtons.Length;
        if (targets == 0)
            return;

        _cursor = (_cursor + dir + targets) % targets;
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
                GameManager.Instance.CopyData(_copySrcCursor + 1, _cursor + 1);
                _isCopyMode = false;
                for (int i = 0; i < saveFrames.Length; i++)
                    saveFrames[i].SetData(GameManager.Instance.SaveFileName(i + 1), i + 1);
                RefreshSaveFrameCursors();
                SoundManager.Instance.PlaySound(ConstValues.NormalButton2);
                return;
            }
            GameManager.Instance.SaveFileName(_cursor + 1);
            GameManager.Instance.GameStart();
            return;
        }

        if (_cursor == 0)
            OpenSaveSelect();
        if (_cursor == 1)
            OpenSettingPopup();

        SoundManager.Instance.PlaySound(ConstValues.NormalButton2);
    }

    private void HandleDelete()
    {
        if (string.IsNullOrEmpty(GameManager.Instance.SaveFileName(_cursor + 1)))
            return;

        _isConfirmActive = true;
        GameManager.Instance.SpawnSelect(
            "선택된 세이브 데이터를 삭제하시겠습니까?_",
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
            _isCopyMode = false;
            _cursor = _copySrcCursor;
            RefreshSaveFrameCursors();
            SoundManager.Instance.PlaySound(ConstValues.NormalButton);
            return;
        }

        if (!_isSaveSelect)
            return;

        titleObject.SetActive(true);
        saveSelectObject.SetActive(false);
        _isSaveSelect = false;
        SoundManager.Instance.PlaySound(ConstValues.NormalButton);
    }

    private void EnterCopyMode()
    {
        if (string.IsNullOrEmpty(GameManager.Instance.SaveFileName(_cursor + 1)))
            return;

        _isCopyMode = true;
        _copySrcCursor = _cursor;
        SoundManager.Instance.PlaySound(ConstValues.NormalButton2);
        RefreshSaveFrameCursors();
    }

    private void OpenSettingPopup()
    {
        _isConfirmActive = true;
        var popup = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Setting, Vector3.zero).GetComponent<Popup_Setting>();
        popup.ExpansionOpen(false, false);
        popup.InitPresenters(() =>
        {
            _isConfirmActive = false;
        });
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

        _cursor = 0;
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
            bool isSelected = i == _cursor;
            if (isSelected)
            {
                saveFrames[i].SelectObjectActive(true);
                saveFrames[i].Expansion(1.05f);
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
        titleText.text = "망할 모험_";
        titleButtons[0].SetText("게임 시작_");
        titleButtons[1].SetText("설정_");
        titleButtons[2].SetText("게임 종료_");

        _cursor = 0;
        RefreshCursors();
    }

    private void StartBGM()
    {
        BgmManager.Instance.PlayBgm(ConstValues.BGMTitle, true);
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
