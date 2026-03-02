using System;
using System.Collections.Generic;
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
    private float limitPosX;
    private float limitPosY;
    private bool isDelete;
    private Rigidbody2D myRigidbody;
    private BoxCollider2D myCollider;
    private SpriteRenderer missileSprite;
    private Spin mySpin;
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
        myCollider.enabled = true;
        if (missileSprite)
            missileSprite.enabled = true;
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

    public void SetupData(MissileData missileData, Vector2 missileDir,
        Action<string, Transform, int, Vector2> explosionAction, Action<string, Vector2> blockAction)
    {
        missileInfo = new MissileInfo();
        missileInfo.id = missileData.id;
        missileInfo.type = (MissileType)Enum.Parse(typeof(MissileType), missileData.type);
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
            // var colSize = myCollider.size;
            // var boxSize = new Vector2(0.1f, colSize.y * 0.8f); // 좌우 (세로로 긴 박스)
            // var boxVector = new Vector2(myCollider.transform.position.x + myCollider.offset.x, myCollider.transform.position.y + myCollider.offset.y);

            if (dir == Vector2.left)
            {
                var rayDir = -transform.right;
                var rayVector = new Vector2(transform.position.x, transform.position.y);
                var ray = Physics2D.Raycast(rayVector, rayDir, defaultLimit, missileLayerMask);
                Debug.DrawRay(rayVector, rayDir * defaultLimit, ConstValues.OrangeColor, 0.02f);
                //var ray = Physics2D.BoxCast(boxVector, boxSize, 0f, dir, defaultLimit, missileLayerMask);

                if (ray.collider == null)
                    missileInfo.limitLength = defaultLimit;
                else
                    missileInfo.limitLength =
                        Vector2.Distance(transform.position, ray.point) - (myCollider.size.x * 0.5f);
            }

            if (dir == Vector2.right)
            {
                var rayDir = transform.right;
                var rayVector = new Vector2(transform.position.x, transform.position.y);
                var ray = Physics2D.Raycast(rayVector, rayDir, defaultLimit, missileLayerMask);
                Debug.DrawRay(rayVector, rayDir * defaultLimit, ConstValues.OrangeColor, 0.02f);
                //var ray = Physics2D.BoxCast(boxVector, boxSize, 0f, dir, defaultLimit, missileLayerMask);

                if (ray.collider == null)
                    missileInfo.limitLength = defaultLimit;
                else
                    missileInfo.limitLength =
                        Vector2.Distance(transform.position, ray.point) - (myCollider.size.x * 0.5f);
            }
        }

        SetLimit();
    }

    // 미사일 데이터를 변경하는 특성은 여기서 관리
    public void AttributeCheck()
    {
        var passiveList = GameManager.Instance.PlayerSkill.GetAttributePassive(missileInfo.id);
        foreach (var passive in passiveList)
        {
            switch (passive)
            {
                // 사거리 끝에서 자동 폭발
                case ConstValues.LimitExplosion:
                    missileInfo.hitSpawn = false;
                    break;
            }
        }
    }

    public void BossCheck(bool isBoss)
    {
        missileInfo.isBossProjectile = isBoss;
    }

    private void SetLimit()
    {
        if (missileInfo.limitLength == 0)
            return;

        switch (missileInfo.type)
        {
            case MissileType.Horizontal:
                limitPosX = transform.position.x;
                if (dir == Vector2.left)
                {
                    limitPosX -= missileInfo.limitLength;
                    if (missileSprite)
                        missileSprite.flipX = true;
                    if (mySpin)
                        mySpin.SetSpinSpeed(false);
                }
                else if (dir == Vector2.right)
                {
                    limitPosX += missileInfo.limitLength;
                    if (missileSprite)
                        missileSprite.flipX = false;
                    if (mySpin)
                        mySpin.SetSpinSpeed(true);
                }

                break;
            case MissileType.Vertical:
                limitPosY = transform.position.y;
                if (missileInfo.speed > 0)
                {
                    limitPosY += missileInfo.limitLength;
                    if (missileSprite)
                        missileSprite.flipY = false;
                    if (mySpin)
                        mySpin.SetSpinSpeed(true);
                }
                else
                {
                    limitPosY -= missileInfo.limitLength;
                    if (missileSprite)
                        missileSprite.flipY = true;
                    if (mySpin)
                        mySpin.SetSpinSpeed(false);
                }

                break;
        }
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

                if (limitPosX == 0)
                    return;

                if (dir == Vector2.left)
                {
                    if (transform.position.x <= limitPosX)
                    {
                        transform.position = new Vector2(limitPosX, transform.position.y);
                        Explosion(false);
                    }
                }
                else if (dir == Vector2.right)
                {
                    if (transform.position.x >= limitPosX)
                    {
                        transform.position = new Vector2(limitPosX, transform.position.y);
                        Explosion(false);
                    }
                }

                break;
            case MissileType.Vertical:
                transform.Translate(Vector2.up * (missileInfo.speed * Time.deltaTime));

                if (limitPosY == 0)
                    return;

                if (missileInfo.speed > 0)
                {
                    if (transform.position.y >= limitPosY)
                    {
                        transform.position = new Vector2(transform.position.x, limitPosY);
                        Explosion(false);
                    }
                }
                else
                {
                    if (transform.position.y <= limitPosY)
                    {
                        transform.position = new Vector2(transform.position.x, limitPosY);
                        Explosion(false);
                    }
                }

                break;
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

        //myRigidbody.linearVelocity = Vector2.zero;
        myCollider.enabled = false;
        if (missileSprite)
            missileSprite.enabled = false;

        // 잔상 남기기 용도
        if (missileInfo.afterImage)
            await UniTask.WaitForSeconds(1.0f);
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

            // 캐릭터들이 무적상태라면 무시한다
            if (hitTag is ConstValues.Player or ConstValues.Monster)
            {
                var character = col.GetComponent<Character>();
                if (character != null)
                {
                    if (character.Immortal || character.IsDie)
                    {
                        if (character.Immortal)
                        {
                            Explosion(true);
                        }

                        return;
                    }
                }

                // 이 부분 기억 (플레이어의 물리 판정)
                if (!col.isTrigger)
                    return;
            }

            // 미사일의 방향에 따라 충돌한 지점 기준으로 미사일의 위치에 따른 충돌무시(벽을 등질 때 오작동 방지)
            Vector2 myPoint = transform.position;
            Vector2 contactPoint = col.ClosestPoint(myPoint);

            if (missileInfo.id.Split('_')[0] != ConstValues.Monster && hitTag == ConstValues.Ground)
            {
                if (dir == Vector2.right && myPoint.x > contactPoint.x)
                    return;

                if (dir == Vector2.left && myPoint.x < contactPoint.x)
                    return;

                // 수정될 수 있음. 벽 위에서 투사체를 날린 경우
                if (Math.Abs(contactPoint.x - myPoint.x) < 0.01f)
                    return;
            }

            Explosion(true);
            return;
        }
    }
}