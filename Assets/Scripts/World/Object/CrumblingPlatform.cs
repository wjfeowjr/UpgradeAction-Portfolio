using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CrumblingPlatform : Platform
{
    [SerializeField] private float crumbleDelay;
    [SerializeField] private float restoreDelay;
    [SerializeField] private GameObject shakeObject;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Shake")]
    [SerializeField] private float idleShakeStrength = 0.02f;
    [SerializeField] private float idleShakeDuration = 0.6f;
    [SerializeField] private float crumbleShakeStrength = 0.1f;
    [SerializeField] private float crumbleShakeDuration = 0.15f;
    
    private CancellationTokenSource crumbleCancellation;
    private Tween shakeTween;
    private Vector3 originLocalPosition;
    private bool isCrumbling;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originLocalPosition = shakeObject.transform.localPosition;
        
        crumbleDelay = 1.0f;
        restoreDelay = 2.5f;
    }

    private void Start()
    {
        StartShake(idleShakeStrength, idleShakeDuration);
    }

    private async UniTask Crumble()
    {
        isCrumbling = true;
        crumbleCancellation = new CancellationTokenSource();

        StartShake(crumbleShakeStrength, crumbleShakeDuration);
        SoundManager.Instance.PlaySound(ConstValues.WallFragments);
        foreach (var spriteRenderer in spriteRenderers)
            SpawnObject(ConstValues.PlatformDust, spriteRenderer.transform.position);
        
        if (await UniTask.Delay(TimeSpan.FromSeconds(crumbleDelay), cancellationToken: crumbleCancellation.Token).SuppressCancellationThrow())
            return;
        
        SoundManager.Instance.PlaySound(ConstValues.Fireimpact03);
        foreach (var spriteRenderer in spriteRenderers)
        {
            SpawnObject(ConstValues.PlatformExplosion, spriteRenderer.transform.position);
            var fragmentObj = SpawnObject(ConstValues.PlatformFragments, spriteRenderer.transform.position);

            var spriteChanger = fragmentObj.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(spriteRenderer.sprite);
            
            var rigidBody = fragmentObj.GetComponent<Rigidbody2D>();
            var spin = fragmentObj.GetComponent<Spin>();
            
            if(rigidBody.linearVelocityX > 0)
                spin.SetSpinSpeed(false);
            else
                spin.SetSpinSpeed(true);
        }
        KillShake();
        myBoxCollider.enabled = false;
        SetSpritesEnabled(false);

        if (await UniTask.Delay(TimeSpan.FromSeconds(restoreDelay), cancellationToken: crumbleCancellation.Token).SuppressCancellationThrow())
            return;
        
        SoundManager.Instance.PlaySound(ConstValues.MonsterBigTreeRootAttack);
        foreach (var spriteRenderer in spriteRenderers)
            SpawnObject(ConstValues.PlatformDestroyDust, spriteRenderer.transform.position);
        
        myBoxCollider.enabled = true;
        SetSpritesEnabled(true);
        StartShake(idleShakeStrength, idleShakeDuration);
        isCrumbling = false;
    }

    // 각 스프라이트를 독립적으로 흔드는 루프 트윈 생성
    private void StartShake(float strength, float duration)
    {
        KillShake();
        var trans = shakeObject.transform;
        trans.localPosition = originLocalPosition;
        shakeTween = trans.DOShakePosition(duration, strength, 10, 90f, false, false).SetLoops(-1, LoopType.Restart);
    }

    private void KillShake()
    {
        if (shakeTween == null)
            return;

        shakeTween?.Kill();
        shakeObject.transform.localPosition = originLocalPosition;
        shakeTween = null;
    }

    private void SetSpritesEnabled(bool enabled)
    {
        foreach (var sr in spriteRenderers)
            sr.enabled = enabled;
    }

    private void OnDestroy()
    {
        crumbleCancellation?.Cancel();
        crumbleCancellation?.Dispose();
        KillShake();
    }
    
    private void OnCollisionStay2D(Collision2D col)
    {
        if (isCrumbling)
            return;

        var character = col.gameObject.GetComponentInParent<Character>();
        if (!character)
            return;

        if (character.LandingState == ELandingState.Air)
            return;

        Crumble().Forget();
    }
}
