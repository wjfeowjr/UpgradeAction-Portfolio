using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class GoldObject : MonoBehaviour
{
    [SerializeField] private Animator myAnimator;
    
    private CancellationTokenSource delayCancellation;
    private BoxCollider2D myBoxCollider;
    protected Action<int, Vector2> goldAction;
    
    [SerializeField] private int hp;
    private int gold;
    private bool isBreak;

    private void Awake()
    {
        myBoxCollider = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        hp = 3;
        gold = Random.Range(6, 10);
        myBoxCollider.enabled = !isBreak;
        if (isBreak)
            myAnimator.SetTrigger(ConstValues.BreakImmediate);
    }

    public void SetAction(Action<int, Vector2> action)
    {
        goldAction ??= action;
    }
    
    private async void HitAndBreak()
    {
        if (hp <= 0)
        {
            delayCancellation?.Cancel();
            myAnimator.SetTrigger(ConstValues.Break);
            isBreak = true;
            var objectPos = transform.position;
            SpawnObject(ConstValues.PlatformExplosion, objectPos);
            SoundManager.Instance.PlaySound(ConstValues.DestroyDoor);
            GameManager.Instance.CameraShake(0.1f, 0.1f, 0.1f);
            goldAction?.Invoke(gold, objectPos);
        }
        else
        {
            float delay1 = 0.2f;

            var randomX = Random.Range(-0.5f, 0.5f);
            var randomY = Random.Range(-0.5f, 0.5f);
            var randomVector = new Vector2(transform.position.x + randomX, transform.position.y + randomY);
            SpawnObject(ConstValues.ShortcutCrashEffect, randomVector);
            GameManager.Instance.CameraShake(0.05f, 0.05f, 0.05f);
            myAnimator.SetTrigger(ConstValues.Hit);
            
            delayCancellation ??= new CancellationTokenSource();
            if(await NormalDelay(delay1, delayCancellation).SuppressCancellationThrow())
                return;
            
            myAnimator.SetTrigger(ConstValues.Idle);
        }
    }
    
    // 일반 딜레이
    protected async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    // 오브젝트 소환 (오버로딩)
    public void SpawnObject(string id, Vector2 pos, int zAngle = 0)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        SetSpawnedObjectData(id, obj, zAngle);
    }
    
    // 오브젝트 데이터 삽입
    private void SetSpawnedObjectData(string id, GameObject obj, int zAngle, Transform traceTransform = null)
    {
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData != null)
        {
            var spawnedObject = obj.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = obj.AddComponent<SpawnedObject>();

            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
            spawnedObject.AttributeCheck();
            if (zAngle != 0)
            {
                var finalAngle = zAngle;
                if (transform.localScale.x < 0)
                    finalAngle = -zAngle;
            
                var objectAngle = spawnedObject.transform.eulerAngles;
                spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
            }

            if (traceTransform != null)
            {
                if (spawnedObject.GetTrace())
                {
                    var trace = obj.GetComponent<Trace>();
                    if (!trace)
                        trace = obj.AddComponent<Trace>();

                    trace.SetTarget(traceTransform);
                }
            }
        }
    }
    
    // 공격판정(Attack 오브젝트)와 충돌했을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hp == 0)
            return;

        // Attack 컴포넌트로 판정 (프로젝트 구조 기준)
        var atk = other.GetComponent<Attack>();
        if (!atk)
            return;

        if (!atk.CastChar.GetComponent<Player>())
            return;

        // 파괴 처리
        hp -= 1;
        HitAndBreak();
    }
}
