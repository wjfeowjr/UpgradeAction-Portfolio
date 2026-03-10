using DG.Tweening;
using UnityEngine;

public class RepeatMove : MonoBehaviour
{
    [SerializeField] private Transform targetPosition; // 이동할 목표 좌표
    [SerializeField] private float duration = 0.2f;    // 편도 이동 시간

    void OnEnable()
    {
        transform.DOLocalMove(targetPosition.localPosition, duration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
    }
}
