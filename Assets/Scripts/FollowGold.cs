using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class FollowGold : MonoBehaviour
{
    private CancellationTokenSource followCancellation;
    private BoxCollider2D boxCollider2D;
    private Action getAction;
    private bool isFollow;

    [SerializeField] private float startDelay;
    [SerializeField] private float speed;
    
    private void Awake()
    {
        followCancellation = new CancellationTokenSource();
        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        Follow();
    }

    private void Update()
    {
        UpdateFollow();
    }

    public void SetAction(Action action)
    {
        getAction = action;
    }

    private async void Follow()
    {
        isFollow = false;
        boxCollider2D.enabled = false;
        await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: followCancellation.Token);
        isFollow = true;
        boxCollider2D.enabled = true;
    }

    private void UpdateFollow()
    {
        if (isFollow)
        {
            transform.position = Vector3.MoveTowards(transform.position, GameManager.Instance.CurPlayer.CenterPos.position, Time.deltaTime * speed);
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.Player))
        {
            getAction?.Invoke();
            Debug.Log($"현재 골드: {GameManager.Instance.Gold}");
            gameObject.SetActive(false);
        }
    }
}
