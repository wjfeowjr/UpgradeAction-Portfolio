using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IPopupSelectView
{
    void SetModel(string message, Sprite goods, int cost);
    void SetAction(Action start, Action yes, Action no, Action moveSoundAction);
}

public class PopupSelectModel
{
    public string message;
    public Sprite goods;
    public int cost;
    
    public Action startAction;
    public Action yesAction;
    public Action noAction;
    public Action moveSoundAction;
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
        _selectView.SetModel(_model.message, _model.goods, _model.cost);
    }
    
    public void SetAction()
    {
        _selectView.SetAction(_model.startAction, _model.yesAction, _model.noAction, _model.moveSoundAction);
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
    private Action moveSoundAction;

    private void OnEnable()
    {
        startAction?.Invoke();
        Yes();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // 왼쪽이동
            if (isYes)
                return;
            
            Yes();
            moveSoundAction?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // 오른쪽이동
            if (!isYes)
                return;
            
            No();
            moveSoundAction?.Invoke();
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
            noAction();
        }
    }

    public void SetModel(string message, Sprite goods, int cost)
    {
        messageText.text = message;
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
    
    public void SetAction(Action start, Action yes, Action no, Action move)
    {
        startAction = start;
        yesAction = yes;
        noAction = no;
        moveSoundAction = move;
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
