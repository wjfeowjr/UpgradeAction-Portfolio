using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupFastTravelView
{
    void SetFastTravelText(string move, string select, string cancel);
    void SetEntries(List<string> placeNames);
    void RefreshSelect(int index, bool smooth);
}

public class PopupFastTravelModel
{
    public string moveText;
    public string selectText;
    public string cancelText;
    
    public List<Vector3> targetPositions = new List<Vector3>();   // 각 세이브 포인트의 SaveObject 위치
    public List<string> placeNames = new List<string>();          // 각 세이브 포인트의 place 이름
    public int startIndex;                                        // 시작 인덱스(현재 방)
    public Transform miniMapCamera;                               // 이동시킬 미니맵 카메라
    public Action closeAction;                                    // ESC 닫기
    public Action<int> selectAction;                             // Enter 확정(선택 인덱스 전달)
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
    
    public void SetFastTravelText()
    {
        _view.SetFastTravelText(_model.moveText, _model.selectText, _model.cancelText);
    }

    public void Fade(Action action)
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
        // 3) 현재 인덱스로 선택 상태 동기화 (스크롤도 트윈 없이 즉시 맞춘다)
        _view.RefreshSelect(_index, false);
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

        // Enter 입력 시 선택한 세이브 포인트로 확정(이동)
        if (InputHelper.GetEnterDown() && _model.targetPositions.Count > 0)
        {
            _moveTween?.Kill();
            _model.selectAction?.Invoke(_index);
            return;
        }

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
            _view.RefreshSelect(_index, true);
        }
        else if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            _index = (_index + 1) % count;
            MoveCamera(true);
            _view.RefreshSelect(_index, true);
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

    [SerializeField] private TMP_Text moveText;
    [SerializeField] private TMP_Text selectText;
    [SerializeField] private TMP_Text cancelText;

    [Header("스크롤")]
    // 보이는 영역. RectMask2D(또는 ScrollRect의 Viewport)가 붙은 RectTransform
    [SerializeField] private RectTransform scrollViewport;
    // 항목들이 들어있는 영역(VerticalLayoutGroup이 붙은 PlaceFrame). scrollViewport의 자식이어야 한다
    [SerializeField] private RectTransform scrollContent;
    [SerializeField] private float scrollDuration = 0.15f;

    private Tween scrollTween;

    private bool UseScroll => scrollViewport && scrollContent;

    public void SetFastTravelText(string move, string select, string cancel)
    {
        moveText.text = move;
        selectText.text = select;
        cancelText.text = cancel;
    }

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

        // 팝업은 풀에서 재사용되므로 이전에 내려가 있던 스크롤 위치를 초기화한다
        if (UseScroll)
        {
            scrollTween?.Kill(false);
            scrollTween = null;
            scrollContent.anchoredPosition = new Vector2(scrollContent.anchoredPosition.x, 0f);
        }
    }

    // 현재 인덱스와 버튼 선택 상태 동기화
    public void RefreshSelect(int index, bool smooth)
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

        ScrollToIndex(index, smooth);
    }

    // 선택 항목이 보이는 영역을 벗어난 만큼만 콘텐츠를 밀어 스크롤한다
    private void ScrollToIndex(int index, bool smooth)
    {
        if (!UseScroll)
            return;

        // 슬롯 수보다 세이브 포인트가 많으면 해당 인덱스에 대응하는 항목이 없다
        if (index < 0 || index >= expansionUiObjects.Length)
            return;

        var target = expansionUiObjects[index].transform as RectTransform;
        if (!target || !target.gameObject.activeInHierarchy)
            return;

        // LayoutGroup 배치가 아직 반영되지 않은 프레임에서도 정확한 위치를 얻기 위해 강제 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

        var viewCorners = new Vector3[4];
        var itemCorners = new Vector3[4];
        scrollViewport.GetWorldCorners(viewCorners);
        target.GetWorldCorners(itemCorners);

        // GetWorldCorners: 0 = 좌하단, 1 = 좌상단. 뷰포트 로컬 기준으로 환산해 비교한다
        float viewBottom = scrollViewport.InverseTransformPoint(viewCorners[0]).y;
        float viewTop    = scrollViewport.InverseTransformPoint(viewCorners[1]).y;
        float itemBottom = scrollViewport.InverseTransformPoint(itemCorners[0]).y;
        float itemTop    = scrollViewport.InverseTransformPoint(itemCorners[1]).y;

        float delta;
        if (itemTop > viewTop)
            delta = viewTop - itemTop;          // 위로 벗어남 → 벗어난 만큼 콘텐츠를 내린다
        else if (itemBottom < viewBottom)
            delta = viewBottom - itemBottom;    // 아래로 벗어남 → 벗어난 만큼 콘텐츠를 올린다
        else
            return;                             // 이미 다 보이면 스크롤하지 않는다

        float destY = scrollContent.anchoredPosition.y + delta;

        scrollTween?.Kill(false);
        if (smooth)
            scrollTween = scrollContent.DOAnchorPosY(destY, scrollDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        else
            scrollContent.anchoredPosition = new Vector2(scrollContent.anchoredPosition.x, destY);
    }

    private void OnDisable()
    {
        scrollTween?.Kill(false);
        scrollTween = null;
    }
}
