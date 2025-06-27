using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillDragHandler : MonoBehaviour, IPointerMoveHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private UISkillView mySkillView;
    private UISkillView targetSkillView;
    private UISkillView skillTooltipView;

    private Canvas canvas;            // 드래그 대상이 속한 Canvas
    private RectTransform canvasRect;
    private Camera canvasCamera;
    
    private RectTransform skillImageTransform;
    private SkillTooltip toolTip;
    private bool isDrag;
    
    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        canvasCamera = canvas.worldCamera;
    }

    private void Update()
    {
        if (!GameManager.Instance.ControlStart && skillImageTransform)
        {
            skillImageTransform.gameObject.SetActive(false);
            skillImageTransform = null;
        }
    }
    
    public void OnPointerMove(PointerEventData eventData)
    {
        skillTooltipView = GetSkillView(eventData, false);
        if (skillTooltipView == null || string.IsNullOrEmpty(skillTooltipView.GetSkillId()) || !GameManager.Instance.ControlStart || isDrag)
        {
            if (toolTip != null)
                toolTip.gameObject.SetActive(false);
            return;
        }
        
        var uiInterfaceObj = GameManager.Instance.GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;
        
        var uiInterface = uiInterfaceObj.GetComponent<UI_Interface>();
        if (toolTip == null)
        {
            Debug.Log($"{skillTooltipView.GetSkillId()}스킬 툴팁 최초 생성");
            toolTip = GameManager.Instance.SpawnToUIPool(ConstValues.SkillTooltip, uiInterface.GetTooltipPos()).GetComponent<SkillTooltip>();
        }
        toolTip.SetTooltip(GameManager.Instance.CurPlayer.GetSkill(skillTooltipView.GetSkillId()));
        
        if (toolTip.gameObject.activeSelf)
            return;
        
        // 툴팁 표시 및 설명 추가
        toolTip.gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        mySkillView = GetSkillView(eventData);
        if (mySkillView == null || string.IsNullOrEmpty(mySkillView.GetSkillId()) || !GameManager.Instance.ControlStart)
            return;

        isDrag = true;
        
        skillImageTransform = GameManager.Instance.SpawnToHighestPool(ConstValues.SkillImage, eventData.position).GetComponent<RectTransform>();
        skillImageTransform.GetComponent<Image>().sprite = mySkillView.GetSprite();
        Vector2 screenPoint = eventData.position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvasCamera, out Vector2 localPoint))
        {
            skillImageTransform.anchoredPosition = localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Canvas 스케일에 맞춰 이동
        //Debug.Log("드래그 중");

        if (mySkillView == null || string.IsNullOrEmpty(mySkillView.GetSkillId()) || !GameManager.Instance.ControlStart)
            return;

        Vector2 screenPoint = eventData.position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvasCamera, out Vector2 localPoint))
        {
            skillImageTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("드래그 끝");

        if (skillImageTransform == null)
            return;
        
        isDrag = false;
        
        skillImageTransform.gameObject.SetActive(false);
        skillImageTransform = null;

        if (mySkillView == null || string.IsNullOrEmpty(mySkillView.GetSkillId()))
            return;

        targetSkillView = GetSkillView(eventData);

        if (targetSkillView == null)
            return;
        
        var temp = mySkillView.GetSkillId();
        mySkillView.ExecuteSkillAction(targetSkillView.GetSkillId());
        targetSkillView.ExecuteSkillAction(temp);
        
        // 툴팁 표시 및 설명 추가
        toolTip.gameObject.SetActive(true);
    }

    private UISkillView GetSkillView(PointerEventData eventData, bool canDrag = true)
    {
        // 드래그 종료 지점에 있는 UI를 RaycastAll로 찾기
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var res in results)
        {
            // 떨어진 곳의 GameObject에서 UISkillView를 탐색
            var view = res.gameObject.GetComponentInParent<UISkillView>();
            if (view != null)
            {
                switch (canDrag)
                {
                    case true:
                        if (!view.IsChangeCharacter() && !view.IsDash())
                            return view;
                        break;
                    
                    case false:
                        return view;
                        break;
                }
            }
        }
        return null;
    }
}