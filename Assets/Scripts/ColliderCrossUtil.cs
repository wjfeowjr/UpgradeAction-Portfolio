using UnityEngine;

// 존 판정 공용 유틸.
// 현재 위치가 콜라이더 안에 있거나, 이전 프레임 위치에서 현재 위치까지의 이동 선분이
// 콜라이더 영역을 지나갔으면 true. 빠른 이동으로 한 프레임에 뚫고 지나가도(터널링) 놓치지 않는다.
public static class ColliderCrossUtil
{
    // ignoreHeight가 true면 세로 범위를 무시하고 x축만으로 판정한다.
    // 콜라이더가 세로선 마커가 되어, 플레이어가 어떤 높이로 지나가든(점프 포함) x만 걸치면 통과로 본다
    public static bool CrossedOrInside(Collider2D collider, Vector2 prevPos, Vector2 curPos, bool ignoreHeight = false)
    {
        if (ignoreHeight)
            return CrossedX(collider.bounds, prevPos, curPos);

        if (collider.OverlapPoint(curPos))
            return true;

        return SegmentIntersectsBounds(prevPos, curPos, collider.bounds);
    }

    // 이동 구간의 x 범위가 콜라이더의 x 범위와 겹치면 통과로 판정 (y 무시)
    private static bool CrossedX(Bounds bounds, Vector2 prevPos, Vector2 curPos)
    {
        float lo = Mathf.Min(prevPos.x, curPos.x);
        float hi = Mathf.Max(prevPos.x, curPos.x);
        return hi >= bounds.min.x && lo <= bounds.max.x;
    }

    // 선분과 AABB의 교차 검사 (슬랩 방식)
    private static bool SegmentIntersectsBounds(Vector2 p0, Vector2 p1, Bounds bounds)
    {
        Vector2 d = p1 - p0;
        float tMin = 0f;
        float tMax = 1f;

        // x축
        if (Mathf.Approximately(d.x, 0f))
        {
            if (p0.x < bounds.min.x || p0.x > bounds.max.x)
                return false;
        }
        else
        {
            float t1 = (bounds.min.x - p0.x) / d.x;
            float t2 = (bounds.max.x - p0.x) / d.x;
            if (t1 > t2)
                (t1, t2) = (t2, t1);

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax)
                return false;
        }

        // y축
        if (Mathf.Approximately(d.y, 0f))
        {
            if (p0.y < bounds.min.y || p0.y > bounds.max.y)
                return false;
        }
        else
        {
            float t1 = (bounds.min.y - p0.y) / d.y;
            float t2 = (bounds.max.y - p0.y) / d.y;
            if (t1 > t2)
                (t1, t2) = (t2, t1);

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax)
                return false;
        }

        return true;
    }
}
