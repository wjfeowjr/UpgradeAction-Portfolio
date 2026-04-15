using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public interface IPopupStoreView
{
    void SetModel();
    void SetItem();
    void SetAction(PopupCommonActions commonActions);
}

public class PopupStoreModel
{
    // TODO: 상점에 표시할 아이템 목록 및 관련 데이터 필드 추가
    public PopupCommonActions commonActions;
}

public class PopupStorePresenter
{
    private IPopupStoreView _view;
    private PopupStoreModel _model;

    public PopupStorePresenter(IPopupStoreView view, PopupStoreModel model)
    {
        _view = view;
        _model = model;
    }

    public void SetModel()
    {
        _view.SetModel();
    }

    public void SetItem()
    {
        _view.SetItem();
    }

    public void SetAction()
    {
        _view.SetAction(_model.commonActions);
    }
}

public class PopupStoreView : MonoBehaviour, IPopupStoreView
{
    [SerializeField] private TMP_Text popupText;
    private PopupCommonActions _actions;

    public void SetModel()
    {
        // TODO: 상점 아이템 목록 UI 초기화
    }

    public void SetItem()
    {
        // TODO: 선택된 아이템 상세 정보 표시
    }

    public void SetAction(PopupCommonActions actions) => _actions = actions;
}
