using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rt;
    private CanvasGroup cg;
    private Canvas canvas;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        cg.alpha = 0.6f;
        cg.blocksRaycasts = false; // Raycast 대상 제외
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Canvas 스케일에 맞춰 이동
        rt.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.alpha = 1f;
        cg.blocksRaycasts = true;

        // 드래그 종료 지점에 있는 UI를 RaycastAll로 찾기
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var res in results)
        {
            // 떨어진 곳의 GameObject에서 UISkillView를 탐색
            var view = res.gameObject.GetComponentInParent<UISkillView>();
            if (view != null)
            {
                view.ExecuteSkillAction(ConstValues.BerserkerUpperSlash);
                break;
            }
        }
    }
}