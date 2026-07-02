using UnityEngine;

public enum EExpandDir
{
    Left,
    Right,
}

// 플레이어가 구역 안에 있는 동안 카메라 리밋을 한쪽 방향으로 넓혀 시야를 확장하는 존.
// 콜라이더는 구역 범위 정의(씬 뷰 시각화)용이며, 판정은 Room에서 OverlapPoint로 계산한다.
// 확장 판정(expandCollider)과 복귀 판정(returnCollider)을 분리해서,
// 나갈 때와 돌아올 때의 경계 좌표를 다르게 둘 수 있다(히스테리시스).
// 두 콜라이더는 자식 오브젝트에 붙여서 인스펙터로 할당한다.
public class CameraExpandZone : MonoBehaviour
{
    [Header("확장 방향과 거리")]
    [SerializeField] private EExpandDir expandDir;
    [SerializeField] private float expandDistance = 2.56f;   // 카메라 리밋이 넓어지는 거리
    [SerializeField] private float expandSpeed = 5.0f;       // 초당 확장/복귀 속도

    [Header("판정 콜라이더")]
    [SerializeField] private BoxCollider2D expandCollider;   // 들어오면 확장 시작
    [SerializeField] private BoxCollider2D returnCollider;   // 확장 상태에서 여기에 닿으면 복귀 (비우면 확장 존을 벗어날 때 복귀)

    private bool isExpanded;
    private float curOffset;

    // 방향에 맞는 쪽에만 현재 오프셋을 반환
    public float LeftOffset  => expandDir == EExpandDir.Left ? curOffset : 0;
    public float RightOffset => expandDir == EExpandDir.Right ? curOffset : 0;

    private void Awake()
    {
        if (!expandCollider)
        {
            Debug.LogError($"{name}: expandCollider가 할당되지 않았습니다.");
            return;
        }

        expandCollider.isTrigger = true;
        if (returnCollider)
            returnCollider.isTrigger = true;
    }

    // 플레이어 위치 기준으로 오프셋을 갱신하고, 값이 변했으면 true를 반환
    public bool UpdateExpand(Vector2 playerPos)
    {
        if (!expandCollider)
            return false;

        // 평상시에는 확장 존 진입으로 확장한다.
        // 확장 상태에서는 어디에 있든 유지되다가, 복귀 존에 닿아야만 복귀한다.
        // (확장 존 안에 있을 때는 복귀 존과 겹쳐 있어도 확장을 유지)
        if (isExpanded)
        {
            if (returnCollider)
            {
                if (returnCollider.OverlapPoint(playerPos) && !expandCollider.OverlapPoint(playerPos))
                    isExpanded = false;
            }
            else
            {
                isExpanded = expandCollider.OverlapPoint(playerPos);
            }
        }
        else
        {
            isExpanded = expandCollider.OverlapPoint(playerPos);
        }

        float targetOffset = isExpanded ? expandDistance : 0;
        if (Mathf.Approximately(curOffset, targetOffset))
            return false;

        curOffset = Mathf.MoveTowards(curOffset, targetOffset, expandSpeed * Time.deltaTime);
        return true;
    }

    // 방 재진입 등 리밋이 초기화될 때 오프셋도 즉시 복구
    public void ResetExpand()
    {
        isExpanded = false;
        curOffset = 0;
    }
}
