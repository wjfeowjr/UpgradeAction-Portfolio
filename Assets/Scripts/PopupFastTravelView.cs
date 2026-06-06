using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public interface IPopupFastTravelView
{
    void SetEntries(List<string> placeNames);
    void RefreshSelect(int index);
}

public class PopupFastTravelModel
{
    public List<Vector3> targetPositions = new List<Vector3>();   // 각 세이브 포인트의 SaveObject 위치
    public List<string> placeNames = new List<string>();          // 각 세이브 포인트의 place 이름
    public int startIndex;                                        // 시작 인덱스(현재 방)
    public Transform miniMapCamera;                               // 이동시킬 미니맵 카메라
    public Action closeAction;
}

public class PopupFastTravelPresenter
{
    private readonly IPopupFastTravelView _view;
    private PopupFastTravelModel _model;
    // 팝업이 스폰된 프레임의 잔여 입력(GetKeyDown)을 무시하기 위한 플래그
    private bool _inputReady;
    private int _index;
    private Tween _moveTween;
    private readonly float _moveDuration = 0.3f;

    public PopupFastTravelPresenter(IPopupFastTravelView fastTravelView, PopupFastTravelModel model)
    {
        _view = fastTravelView;
        _model = model;
        _inputReady = false;
        _index = model.startIndex;
    }

    public void Expansion(Action action)
    {
        action?.Invoke();
    }

    // 시작 위치로 카메라를 즉시 이동하고 목록/선택 상태를 초기화
    public void Open()
    {
        if (_model.targetPositions.Count == 0)
            return;

        // 1) place 이름 채우기 + 미사용 버튼 비활성화
        _view.SetEntries(_model.placeNames);
        // 2) 시작 위치로 카메라 즉시 이동
        MoveCamera(false);
        // 3) 현재 인덱스로 선택 상태 동기화
        _view.RefreshSelect(_index);
    }

    // 매 프레임 입력 처리 (위/아래 이동, ESC 닫기)
    public void Tick()
    {
        // 스폰된 프레임에서는 입력을 받지 않고 한 프레임 뒤부터 감지
        if (!_inputReady)
        {
            _inputReady = true;
            return;
        }

        NavigateAction();

        // ESC 입력 시 닫기
        if (Input.GetKeyDown(GameManager.Instance.escKey))
        {
            _moveTween?.Kill();
            _model.closeAction?.Invoke();
        }
    }

    private void NavigateAction()
    {
        var count = _model.targetPositions.Count;
        if (count == 0)
            return;

        // 위: 이전(idx 작은) 세이브 포인트 / 아래: 다음(idx 큰) 세이브 포인트, 양 끝에서 워프
        if (Input.GetKeyDown(GameManager.Instance.upKey))
        {
            _index = (_index - 1 + count) % count;
            MoveCamera(true);
            _view.RefreshSelect(_index);
        }
        else if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            _index = (_index + 1) % count;
            MoveCamera(true);
            _view.RefreshSelect(_index);
        }
    }

    private void MoveCamera(bool smooth)
    {
        var target = _model.targetPositions[_index];
        var camPos = _model.miniMapCamera.position;
        var dest = new Vector3(target.x, target.y, camPos.z);

        _moveTween?.Kill();
        if (smooth)
            _moveTween = _model.miniMapCamera.DOMove(dest, _moveDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        else
            _model.miniMapCamera.position = dest;
    }

}

public class PopupFastTravelView : MonoBehaviour, IPopupFastTravelView
{
    [SerializeField] private ExpansionUiObject[] expansionUiObjects;

    // 세이브 포인트 개수만큼 place 이름을 채우고, 남는 버튼은 비활성화
    public void SetEntries(List<string> placeNames)
    {
        for (int i = 0; i < expansionUiObjects.Length; i++)
        {
            if (i < placeNames.Count)
            {
                expansionUiObjects[i].gameObject.SetActive(true);
                expansionUiObjects[i].SetText(placeNames[i]);
            }
            else
            {
                expansionUiObjects[i].gameObject.SetActive(false);
            }
        }
    }

    // 현재 인덱스와 버튼 선택 상태 동기화
    public void RefreshSelect(int index)
    {
        for (int i = 0; i < expansionUiObjects.Length; i++)
        {
            if (!expansionUiObjects[i].gameObject.activeSelf)
                continue;

            if (i == index)
            {
                expansionUiObjects[i].SelectObjectActive(true);
                expansionUiObjects[i].Expansion(1.05f);
            }
            else
            {
                expansionUiObjects[i].SelectObjectActive(false);
                expansionUiObjects[i].Reduction();
            }
        }
    }
}
