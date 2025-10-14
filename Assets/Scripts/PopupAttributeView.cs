using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IPopupAttributeView
{
    void SetModel(SkillCollection playerSkill);
    void SetAction(Action closeAction);
    void CloseAction();
}

public class PopupAttributeModel
{
    public SkillCollection playerSkill;
    public Action closeAction;
}

public class PopupAttributePresenter
{
    private IPopupAttributeView _attributeView;
    private PopupAttributeModel _model;

    public PopupAttributePresenter(IPopupAttributeView guideView, PopupAttributeModel model)
    {
        _attributeView = guideView;
        _model = model;
    }

    private void CloseAction()
    {
        _attributeView.CloseAction();
    }
    
    public void CloseAttribute()
    {
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Escape))
            CloseAction();
    }

    public void SetModel(SkillCollection playerSkill)
    {
        _model.playerSkill = playerSkill;
        _attributeView.SetModel(_model.playerSkill);
    }
    
    public void SetAction(Action closeAction)
    {
        _model.closeAction = closeAction;
        _attributeView.SetAction(_model.closeAction);
    }
}


public class PopupAttributeView : MonoBehaviour, IPopupAttributeView
{
    [SerializeField] private string targetPlayer;
    [SerializeField] private string targetSkill;
    
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text leftPointText;
    [SerializeField] private TMP_Text leftPoint;
    [SerializeField] private TMP_Text resetText;
    [SerializeField] private Button closeButton;

    [SerializeField] private AttributeFrame[] attributeFrameArray;

    private Action closeAction;
    
    private SkillCollection skillCollection;
    
    public void SetModel(SkillCollection playerSkill)
    {
        targetPlayer = ConstValues.Berserker;
        targetSkill = playerSkill.berserkerSkillSetting.skillList[0].skillId;
        skillCollection = playerSkill;
        
        foreach (var attributeFrame in attributeFrameArray)
            attributeFrame.gameObject.SetActive(false);

        for (int i = 0; i < skillCollection.berserkerSkillSetting.skillList.Count; i++)
        {
            attributeFrameArray[i].gameObject.SetActive(true);
            attributeFrameArray[i].SetSkillInfo(skillCollection.berserkerSkillSetting.skillList[i].skillId, targetSkill);
        }
    }
    
    public void SetAction(Action action)
    {
        closeAction = action;
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            closeAction();
        });
    }

    public void CloseAction()
    {
        Time.timeScale = 1.0f;
        closeAction();
    }
}
