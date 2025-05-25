using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private UISkillView mySkillView;
    private UISkillView targetSkillView;

    private Canvas canvas;            // 드래그 대상이 속한 Canvas
    private RectTransform canvasRect;
    private Camera canvasCamera;
    
    private RectTransform skillImageTransform;
    
    
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("드래그 시작");
        
        mySkillView = GetSkillView(eventData);
        if (mySkillView == null || string.IsNullOrEmpty(mySkillView.GetSkillId()) || !GameManager.Instance.ControlStart)
            return;
        
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
    }

    private UISkillView GetSkillView(PointerEventData eventData)
    {
        // 드래그 종료 지점에 있는 UI를 RaycastAll로 찾기
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var res in results)
        {
            // 떨어진 곳의 GameObject에서 UISkillView를 탐색
            var view = res.gameObject.GetComponentInParent<UISkillView>();
            if (view != null && !view.IsChangeCharacter() && !view.IsDash())
            {
                return view;
                //view.ExecuteSkillAction(ConstValues.BerserkerUpperSlash);
                //break;
            }
        }
        return null;
    }
}