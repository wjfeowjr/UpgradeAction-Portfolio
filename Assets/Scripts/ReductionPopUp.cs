using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ReductionPopUp : MonoBehaviour
{
    private CancellationToken myCancellationToken;
    
    private void OnEnable()
    {
        Reduction();
    }

    private async void Reduction()
    {
        float speed = 6;
        var myTransform = transform;
        var localScale = new Vector2(myTransform.localScale.x, 0);

        float previousRealTime = Time.realtimeSinceStartup;

        while (localScale.y < 1)
        {
            float deltaRealTime = Time.realtimeSinceStartup - previousRealTime;
            localScale.y += speed * deltaRealTime;
            localScale.y = Mathf.Min(1, localScale.y); // y 값이 1보다 크지 않도록 함
            myTransform.localScale = localScale;

            previousRealTime = Time.realtimeSinceStartup;
            await UniTask.Yield(cancellationToken: myCancellationToken);
        }

        transform.localScale = new Vector3(1, 1, 1);

        float endTime = Time.realtimeSinceStartup + 1.5f;
        await UniTask.WaitUntil(() => Time.realtimeSinceStartup >= endTime, cancellationToken: myCancellationToken);

        previousRealTime = Time.realtimeSinceStartup;
        while (localScale.y > 0)
        {
            float deltaRealTime = Time.realtimeSinceStartup - previousRealTime;
            localScale.y -= speed * deltaRealTime;
            localScale.y = Mathf.Max(0, localScale.y); // y 값이 0보다 작아지지 않도록 함
            myTransform.localScale = localScale;

            previousRealTime = Time.realtimeSinceStartup;
            await UniTask.Yield(cancellationToken: myCancellationToken);
        }

        gameObject.SetActive(false);
    }
}
