using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public enum MissileType
{
    Horizontal,
    Vertical,
}

// 보스인지 확인하기
public interface IProjectile
{
    public bool IsBoss();
    public void Delete();
}

[Serializable]
public class MissileInfo
{
    public string id;
    public MissileType type;
    public float speed;
    public float limitLength;
    public List<string> hitTagList;
    public List<string> spawnObjectList;
    public bool hitSpawn;
    public bool afterImage;
    public Action<string, Transform, int, Vector2> explosionAction;
    public Action<string, Vector2> blockAction;
    public bool isBossProjectile;
}

public class Missile : MonoBehaviour, IProjectile
{
    [SerializeField] private Vector2 dir;
    private Vector2 limitStartPos;   // 사거리 측정 기준점 (출발 위치)
    private bool hasLimit;
    private bool isDelete;
    private Rigidbody2D myRigidbody;
    private BoxCollider2D myCollider;
    private SpriteRenderer missileSprite;
    private Spin mySpin;
    private CancellationTokenSource missileCancellation;
    
    [SerializeField] private MissileInfo missileInfo;

    private int missileLayerMask;
    private float defaultLimit;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        myCollider = GetComponent<BoxCollider2D>();
        missileSprite = GetComponentInChildren<SpriteRenderer>();
        mySpin = GetComponentInChildren<Spin>();

        missileLayerMask |= 1 << LayerMask.NameToLayer(ConstValues.Ground);
    }

    private void OnEnable()
    {
        isDelete = false;
        if(myCollider)
            myCollider.enabled = true;
        if (missileSprite)
            missileSprite.enabled = true;
        
        missileCancellation?.Cancel();
    }

    private void Update()
    {
        Move1();
    }

    // 인터페이스 함수
    public bool IsBoss()
    {
        return missileInfo.isBossProjectile;
    }

    public void Delete()
    {
        gameObject.SetActive(false);
        missileInfo.blockAction?.Invoke(ConstValues.ProjectileDestroyEffect, transform.position);
    }

    public void SetupData(MissileData missileData, Vector2 missileDir, Action<string, Transform, int, Vector2> explosionAction, Action<string, Vector2> blockAction)
    {
        missileInfo = new MissileInfo();
        missileInfo.id = missileData.id;
        missileInfo.type = TableParse.Enum<MissileType>(missileData.type);
        missileInfo.speed = missileData.speed;
        defaultLimit = missileData.limitLength;

        missileInfo.hitTagList = new List<string>();
        var hitTagSplit = missileData.hitTag.Split(',');
        foreach (var hitTag in hitTagSplit)
        {
            if (!string.IsNullOrWhiteSpace(hitTag))
            {
                missileInfo.hitTagList.Add(hitTag);
            }
        }

        missileInfo.spawnObjectList = new List<string>();
        var spawnObjectSplit = missileData.spawnObject.Split(',');
        foreach (var spawnObject in spawnObjectSplit)
        {
            if (!string.IsNullOrWhiteSpace(spawnObject))
            {
                missileInfo.spawnObjectList.Add(spawnObject);
            }
        }

        missileInfo.hitSpawn = missileData.hitSpawn;
        missileInfo.afterImage = missileData.afterImage;

        missileInfo.explosionAction = explosionAction;
        missileInfo.blockAction = blockAction;
        dir = missileDir;

        // 박스 캐스트
        if (missileInfo.type == MissileType.Horizontal)
        {

            if (dir == Vector2.left)
            {
                var rayDir = -transform.right;
                var rayVector = new Vector2(transform.position.x, transform.position.y);
                var ray = Physics2D.Raycast(rayVector, rayDir, defaultLimit, missileLayerMask);
                Debug.DrawRay(rayVector, rayDir * defaultLimit, ConstValues.OrangeColor, 0.02f);

                if (ray.collider == null || !myCollider)
                    missileInfo.limitLength = defaultLimit;
                else
                    missileInfo.limitLength = Vector2.Distance(transform.position, ray.point) - (myCollider.size.x * 0.5f);
            }

            if (dir == Vector2.right)
            {
                var rayDir = transform.right;
                var rayVector = new Vector2(transform.position.x, transform.position.y);
                var ray = Physics2D.Raycast(rayVector, rayDir, defaultLimit, missileLayerMask);
                Debug.DrawRay(rayVector, rayDir * defaultLimit, ConstValues.OrangeColor, 0.02f);

                if (ray.collider == null || !myCollider)
                    missileInfo.limitLength = defaultLimit;
                else
                    missileInfo.limitLength = Vector2.Distance(transform.position, ray.point) - (myCollider.size.x * 0.5f);
            }
        }
    }

    // 미사일 데이터를 변경하는 특성은 여기서 관리
    public void AttributeCheck()
    {
        var passiveList = GameManager.Instance.GetAttributePassive(missileInfo.id);
        foreach (var passive in passiveList)
        {
            switch (passive)
            {
                // 사거리 끝에서 자동 폭발
                case ConstValues.LimitExplosion:
                    missileInfo.hitSpawn = false;
                    break;
                // 적 관통
                case ConstValues.PiercingMissile:
                    if (missileInfo.hitTagList.Contains(ConstValues.Monster))
                        missileInfo.hitTagList.Remove(ConstValues.Monster);
                    break;
            }
        }
        var upgradeList = GameManager.Instance.GetAttributeUpgrade(missileInfo.id);
        foreach (var upgrade in upgradeList)
        {
            switch (upgrade.upgradeId)
            {
                // 사거리 증가
                case ConstValues.ReachUp:
                    missileInfo.limitLength *= (1 + (upgrade.upgradeValue * 0.01f));
                    break;
            }
        }
    }
    
    public void SetLimit()
    {
        // 사거리는 출발점 기준 누적 이동거리로 판정한다 (회전된 미사일도 정확)
        hasLimit = missileInfo.limitLength != 0;
        limitStartPos = transform.position;

        switch (missileInfo.type)
        {
            case MissileType.Horizontal:
                if (dir == Vector2.left)
                {
                    if (missileSprite)
                        missileSprite.flipX = true;
                    if (mySpin)
                        mySpin.SetSpinSpeed(false);
                }
                else if (dir == Vector2.right)
                {
                    if (missileSprite)
                        missileSprite.flipX = false;
                    if (mySpin)
                        mySpin.SetSpinSpeed(true);
                }

                break;
            case MissileType.Vertical:
                if (missileInfo.speed > 0)
                {
                    if (missileSprite)
                        missileSprite.flipY = false;
                    if (mySpin)
                        mySpin.SetSpinSpeed(true);
                }
                else
                {
                    if (missileSprite)
                        missileSprite.flipY = true;
                    if (mySpin)
                        mySpin.SetSpinSpeed(false);
                }

                break;
        }
    }

    public void BossCheck(bool isBoss)
    {
        missileInfo.isBossProjectile = isBoss;
    }

    // 좌표값 이동(Update에서 사용)
    private void Move1()
    {
        if (isDelete)
            return;

        switch (missileInfo.type)
        {
            case MissileType.Horizontal:
                transform.Translate(dir * (missileInfo.speed * Time.deltaTime));
                break;
            case MissileType.Vertical:
                transform.Translate(Vector2.up * (missileInfo.speed * Time.deltaTime));
                break;
        }

        if (!hasLimit)
            return;

        // 사거리 체크: 출발점 기준 누적 이동거리 (회전된 미사일도 각도와 무관하게 정확)
        Vector2 moved = (Vector2)transform.position - limitStartPos;
        if (moved.magnitude >= missileInfo.limitLength)
        {
            // 사거리 끝 지점으로 스냅 후 폭발
            transform.position = limitStartPos + moved.normalized * missileInfo.limitLength;
            Explosion(false);
        }
    }

    public void AddSpawnObject(string id)
    {
        missileInfo.spawnObjectList.Add(id);
    }

    private async void Explosion(bool isCollision)
    {
        if (isDelete)
            return;
        isDelete = true;

        await UniTask.Yield();

        foreach (var spawnObject in missileInfo.spawnObjectList)
        {
            if (!string.IsNullOrWhiteSpace(spawnObject))
            {
                if (isCollision)
                {
                    missileInfo.explosionAction(spawnObject, transform, 0, default);
                }
                else
                {
                    if (!missileInfo.hitSpawn || missileInfo.limitLength < defaultLimit)
                        missileInfo.explosionAction(spawnObject, transform, 0, default);
                }
            }
        }

        if(myCollider)
            myCollider.enabled = false;
        if (missileSprite)
            missileSprite.enabled = false;

        // 잔상 남기기 용도
        if (missileInfo.afterImage)
        {
            missileCancellation = new CancellationTokenSource();
            if (await NormalDelay(1.0f, missileCancellation).SuppressCancellationThrow())
                return;
        }

        if(gameObject)
            gameObject.SetActive(false);
    }

    public void LookAtTarget(Vector2 target)
    {
        if (dir == Vector2.left)
            transform.LookAt2D(target, -180);
        else if (dir == Vector2.right)
            transform.LookAt2D(target);
    }

    // 미사일 소멸에만 관여(공격판정은 여기서 정하지 않는다)
    private void OnTriggerEnter2D(Collider2D col)
    {
        // 충돌 처리
        foreach (var hitTag in missileInfo.hitTagList)
        {
            if (string.IsNullOrEmpty(hitTag) || !col.gameObject.CompareTag(hitTag))
                continue;

            // 캐릭터들이 회피상태라면 무시한다
            if (hitTag is ConstValues.Player or ConstValues.Monster)
            {
                var character = col.GetComponent<Character>();
                if (character != null)
                {
                    if (character.Dodge || character.IsDie)
                        return;
                }

                // 이 부분 기억 (플레이어의 물리 판정)
                if (!col.isTrigger)
                    return;
            }

            // 충돌 지점이 미사일의 실제 진행 방향 앞쪽일 때만 폭발로 인정한다
            // (뒤쪽 벽에 겹쳐 생성된 경우, 벽 위에서 발사해 발밑 지형에 스치는 경우 오작동 방지.
            //  회전(zAngle)된 미사일도 진행 방향 기준이라 올바르게 판정된다)
            Vector2 myPoint = transform.position;
            Vector2 contactPoint = col.ClosestPoint(myPoint);

            if (missileInfo.id.Split('_')[0] != ConstValues.Monster && hitTag == ConstValues.Ground)
            {
                // Move1의 Translate(Space.Self)와 동일한 월드 기준 진행 방향
                Vector2 moveDir = missileInfo.type == MissileType.Horizontal
                    ? (Vector2)transform.TransformDirection(dir)
                    : (Vector2)transform.TransformDirection(Vector2.up * Mathf.Sign(missileInfo.speed));

                // 진행 방향 성분이 없거나 뒤쪽이면 무시 (수직 아래 스침 포함)
                if (Vector2.Dot(contactPoint - myPoint, moveDir.normalized) < 0.01f)
                    return;
            }

            Explosion(true);
            return;
        }
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}