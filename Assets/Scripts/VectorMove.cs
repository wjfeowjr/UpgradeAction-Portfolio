using DG.Tweening;
using UnityEngine;

public class VectorMove : MonoBehaviour
{
    [SerializeField] private Vector3 targetPosition; // 이동할 목표 좌표 (World Space)
    [SerializeField] private float duration = 1f;    // 이동에 걸리는 시간 (초)

    private Tween moveTween;

    // 인스펙터에 설정된 값으로 이동 시작
    public void StartMove()
    {
        StartMove(targetPosition, duration);
    }

    // 외부에서 타겟 좌표와 이동 시간을 직접 지정해 이동 시작
    public void StartMove(Vector3 target, float moveDuration)
    {
        moveTween?.Kill();
        moveTween = transform.DOMove(target, moveDuration).SetEase(Ease.Linear);
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        moveTween = null;
    }
}
