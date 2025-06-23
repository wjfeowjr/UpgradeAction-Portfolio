using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Reduction : MonoBehaviour
{
    [SerializeField] private float delay;
    [SerializeField] private float speed;
    [SerializeField] private Vector3 startScale;
    [SerializeField] private Vector3 endScale;
    private CancellationTokenSource reductionCancellation;

    public async void PlayReduction()
    {
        reductionCancellation = new CancellationTokenSource();
        transform.localScale = startScale;

        float scaleX = startScale.x;
        float scaleY = startScale.y;
        float scaleZ = startScale.z;

        if (await NormalDelay(delay, reductionCancellation).SuppressCancellationThrow())
            return;

        while(scaleX > endScale.x || scaleY > endScale.y || scaleZ > endScale.z)
        {
            if (scaleX > endScale.x)
                scaleX -= speed * Time.deltaTime;
            if (scaleY > endScale.y)
                scaleY -= speed * Time.deltaTime;
            if (scaleZ > endScale.z)
                scaleZ -= speed * Time.deltaTime;

            transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            if (await YieldDelay(reductionCancellation).SuppressCancellationThrow())
                return;
        }        
    }

    public void Stop()
    {
        reductionCancellation?.Cancel();
    }
    
    public void StopAndReset()
    {
        reductionCancellation?.Cancel();
        transform.localScale = startScale;
    }
    
    // 일반 딜레이
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    // 1프레임 딜레이
    private async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }
}
