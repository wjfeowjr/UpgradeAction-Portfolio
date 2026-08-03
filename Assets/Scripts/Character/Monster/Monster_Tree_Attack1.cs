using DG.Tweening;
using UnityEngine;

public class Monster_Tree_Attack1 : MonoBehaviour
{
    [SerializeField] private Transform[] fragmentsPos;
    [SerializeField] private float startDownValue;
    
    public Transform[] FragmentsPos => fragmentsPos;

    // 19.5f 0.8f
    public void Grow(float height, float duration)
    {
        // 회전값이 활성화 이후에 적용되므로, 실제 동작은 트윈이 시작되는 다음 프레임에 처리
        // 1. 시작 시 현재 각도의 아래(로컬 y축) 방향으로 -4.5만큼 순간이동
        // 2. 그 지점에서 위쪽 방향을 따라 총 +9.0f만큼 이동
        float prevDistance = 0f;
        DOTween.To(() => prevDistance, distance =>
            {
                transform.position += transform.up * (distance - prevDistance);
                prevDistance = distance;
            }, height, duration)
            .OnStart(() => transform.position -= transform.up * startDownValue)
            .SetTarget(transform);
    }

    private void OnDisable()
    {
        transform.DOKill();
    }
}
