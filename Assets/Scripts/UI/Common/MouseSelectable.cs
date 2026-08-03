using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 마우스 호버/클릭을 외부 콜백으로 연결하는 범용 컴포넌트
// 키보드 커서 기반 UI에 마우스 상호작용을 덧붙일 때 런타임에 부착해서 사용한다
public class MouseSelectable : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public Action onHover;
    public Action onClick;

    // 대상에 MouseSelectable을 부착(또는 재사용)하고 콜백 연결
    // 마우스 이벤트 수신에 필요한 GraphicRaycaster가 캔버스에 없으면 함께 추가한다
    public static MouseSelectable Attach(Component target, Action onHover, Action onClick)
    {
        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas && !canvas.GetComponent<GraphicRaycaster>())
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        var mouse = target.GetComponent<MouseSelectable>();
        if (!mouse)
            mouse = target.gameObject.AddComponent<MouseSelectable>();

        mouse.onHover = onHover;
        mouse.onClick = onClick;
        return mouse;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            onClick?.Invoke();
    }
}
