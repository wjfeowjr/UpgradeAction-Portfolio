using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IPopupSelectView
{
    void SetModel(string message, Sprite goods, int cost, bool yes);
    void SetAction(Action start, Action yes, Action no, Action escAction, PopupCommonActions commonActions);
}

public class PopupSelectModel
{
    public bool yes;
    public string message;
    public Sprite goods;
    public int cost;
    
    public Action startAction;
    public Action yesAction;
    public Action noAction;
    public Action escAction;
    public PopupCommonActions commonActions;
}

public class PopupSelectPresenter
{
    private IPopupSelectView _selectView;
    private PopupSelectModel _model;

    public PopupSelectPresenter(IPopupSelectView selectView, PopupSelectModel model)
    {
        _selectView = selectView;
        _model = model;
    }
    
    public void Expansion(Action action)
    {
        action?.Invoke();
    }

    public void SetModel()
    {
        _selectView.SetModel(_model.message, _model.goods, _model.cost, _model.yes);
    }
    
    public void SetAction()
    {
        _selectView.SetAction(_model.startAction, _model.yesAction, _model.noAction, _model.escAction, _model.commonActions);
    }
}

public class PopupSelectView : MonoBehaviour, IPopupSelectView
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text costText;
    
    [SerializeField] private Image goodsIcon;
    [SerializeField] private GameObject goodsObject;

    [SerializeField] private ExpansionUiObject yesObject;
    [SerializeField] private ExpansionUiObject noObject;

    private float expansionValue = 1.1f;
    private bool isYes;
    private Action startAction;
    private Action yesAction;
    private Action noAction;
    private Action escAction;
    private PopupCommonActions popupCommonActions;

    private void OnEnable()
    {
        startAction?.Invoke();
    }

    // 입력 처리는 소유 Popup_Select의 Update에서 openComplete일 때만 호출됨
    public void HandleInput()
    {
        if (Input.GetKeyDown(GameManager.Instance.leftKey))
        {
            // 왼쪽이동
            if (isYes)
                return;
            
            Yes();
            popupCommonActions?.PlayMoveSound.Invoke();
        }
        else if (Input.GetKeyDown(GameManager.Instance.rightKey))
        {
            // 오른쪽이동
            if (!isYes)
                return;
            
            No();
            popupCommonActions?.PlayMoveSound.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // 선택 확정
            if (isYes)
                yesAction();
            else
                noAction();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            escAction();
        }
    }

    public void SetModel(string message, Sprite goods, int cost, bool yes)
    {
        if (yes)
            Yes();
        else
            No();
        
        messageText.text = message;
        yesObject.SetText(GameManager.Instance.GetTalk(30059));
        noObject.SetText(GameManager.Instance.GetTalk(30060));
        if (goods == null)
        {
            goodsObject.SetActive(false);
        }
        else
        {
            goodsObject.SetActive(true);
            goodsIcon.sprite = goods;
            costText.text = cost.ToString();
        }
    }
    
    public void SetAction(Action start, Action yes, Action no, Action esc, PopupCommonActions common)
    {
        startAction = start;
        yesAction = yes;
        noAction = no;
        escAction = esc;
        popupCommonActions = common;
    }
    
    private void Yes()
    {
        isYes = true;
        
        yesObject.SelectObjectActive(true);
        yesObject.Expansion(expansionValue);
        
        noObject.SelectObjectActive(false);
        noObject.Reduction();
    }

    private void No()
    {
        isYes = false;
        
        noObject.SelectObjectActive(true);
        noObject.Expansion(expansionValue);
        
        yesObject.SelectObjectActive(false);
        yesObject.Reduction();
    }
}
