using System;
using UnityEngine;

public class UICanvas : MonoBehaviour
{
    [SerializeField] private RectTransform[] rectTransformArray;

    private void Start()
    {
        float width = Screen.width;
        float height = Screen.height;

        float heightRatio = (height / width) * 16f;
        Debug.Log($"현재 화면 비율: 16 : {heightRatio:F2}");

        foreach (var rectTransform in rectTransformArray)
        {
            if (heightRatio > 9)
            {
                Debug.Log("16:9보다 세로가 더 김 (예: 16:10)");
                rectTransform.anchorMin = Vector2.zero;        // (0,0)
                rectTransform.anchorMax = Vector2.one;         // (1,1)

                // Offset = 0 (Left, Right, Top, Bottom)
                rectTransform.offsetMin = Vector2.zero;        // Left, Bottom
                rectTransform.offsetMax = Vector2.zero;        // Right, Top
            }
            else if (heightRatio < 9)
            {
                Debug.Log("16:9보다 가로가 더 김 (예: 21:9)");
            }
            else
            {
                Debug.Log("정확히 16:9");
            }
        }
    }
}
