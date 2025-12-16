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
    
    [SerializeField] private Transform yesButton;
    [SerializeField] private GameObject yesSelectObject;
    
    [SerializeField] private Transform noButton;
    [SerializeField] private GameObject noSelectObject;
    
    private bool isYes;
    private Action startAction;
    private Action yesAction;
    private Action noAction;
    private Action moveSoundAction;

    private Tween selectTween;

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
        selectTween?.Kill(false);
        selectTween = yesButton.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
        selectTween = noButton.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
        yesSelectObject.SetActive(true);
        noSelectObject.SetActive(false);
    }

    private void No()
    {
        isYes = false;
        selectTween?.Kill(false);
        selectTween = yesButton.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
        selectTween = noButton.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
        yesSelectObject.SetActive(false);
        noSelectObject.SetActive(true);
    }
    
    protected void OnDisable()
    {
        selectTween?.Kill(false);
        selectTween = null;
    }
}
