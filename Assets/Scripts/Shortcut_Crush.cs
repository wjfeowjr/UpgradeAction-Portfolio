using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public  class Shortcut_Crush : ShortcutObject
{
    [SerializeField] private int targetIdx;
    [SerializeField] private GameObject shakeObject;
    [SerializeField] private Transform[] fragmentsPos;
    [SerializeField] private Sprite[] fragmentSprites;
    private Room targetRoom;

    private CancellationTokenSource shakeCancellation;
    private Vector2 firstVector;
    private Vector2 centerVector;

    public Room TargetRoom
    {
        get => targetRoom;
        set => targetRoom = value;
    }

    private void Awake()
    {
        firstVector = shakeObject.transform.position;
        centerVector = new Vector2(transform.position.x + myCollider.offset.x, transform.position.y + myCollider.offset.y);
    }

    public override void OpenProduct()
    {
        SpawnObject(ConstValues.ShortcutCrashExplosion, centerVector);
        
        foreach (var fragment in fragmentsPos)
        {
            SpawnObject(ConstValues.PlatformExplosion, fragment.transform.position);
            var fragmentObj = SpawnObject(ConstValues.PlatformFragments, fragment.transform.position);

            var spriteChanger = fragmentObj.GetComponent<SpriteChanger>();
            int randIdx = Random.Range(0, fragmentSprites.Length);
            spriteChanger.ChangeSprite(fragmentSprites[randIdx]);
            
            var rigidBody = fragmentObj.GetComponent<Rigidbody2D>();
            float randX = Random.Range(-4.0f, 4.0f);
            float randY = Random.Range(10.0f, 12.0f);
            rigidBody.linearVelocity = new Vector2(randX, randY);
                
            var spin = fragmentObj.GetComponent<Spin>();
            
            if(rigidBody.linearVelocityX > 0)
                spin.SetSpinSpeed(false);
            else
                spin.SetSpinSpeed(true);
        }
        
        base.OpenProduct();
    }

    protected override void OpenImmediate()
    {
        base.OpenImmediate();
        if(targetRoom)
            targetRoom.ShortcutOpen(targetRoom.GetWallShortCutName(targetIdx));
    }

    protected override async void HitProduct()
    {
        // 대략적인 연출과정
        float randomEffectX = Random.Range(-0.2f, 0.2f);
        float randomEffectY = Random.Range(-0.2f, 0.2f);
        Vector2 randomVector = new Vector2(centerVector.x + randomEffectX, centerVector.y + randomEffectY);
        SpawnObject(ConstValues.ShortcutCrashEffect, randomVector);
        
        shakeCancellation = new CancellationTokenSource();
        for (int i = 0; i < 5; i++)
        {
            float randomX = Random.Range(-0.1f, 0.1f);
            float randomY = Random.Range(-0.1f, 0.1f);
            Vector2 shakeVector = new Vector2(firstVector.x + randomX, firstVector.y + randomY);
            shakeObject.transform.position = shakeVector;
            if (await NormalDelay(0.02f, shakeCancellation).SuppressCancellationThrow())
                return;
        }
        shakeObject.transform.position = firstVector;
    }

    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}