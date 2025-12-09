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

[Serializable]
public class MissileInfo
{
    public string id;
    public MissileType type;
    public float speed;
    public float limitLength;
    public List<string> hitTagList;
    public string spawnObject;
    public bool hitSpawn;
    public bool afterImage;
    public Action<string, Transform, int> explosionAction;
}
public class Missile : MonoBehaviour
{
    [SerializeField] private Vector2 dir;
    private float limitPosX;
    private float limitPosY;
    private bool isDelete;
    private Rigidbody2D myRigidbody;
    private Collider2D myCollider;
    private SpriteRenderer missileSprite;
    private Spin mySpin;
    [SerializeField] private MissileInfo missileInfo;

    private int missileLayerMask;
    private float defaultLimit;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        myCollider = GetComponent<Collider2D>();
        missileSprite = GetComponentInChildren<SpriteRenderer>();
        mySpin = GetComponentInChildren<Spin>();
        
        missileLayerMask = (1 << LayerMask.NameToLayer(ConstValues.Ground)) | (1 << LayerMask.NameToLayer(ConstValues.Platform));
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

    // private void FixedUpdate()
    // {
    //     Move2();
    // }

    public void SetupData(MissileData missileData, Vector2 missileDir, Action<string, Transform, int> action)
    {
        if (missileInfo == null)
        {
            missileInfo = new MissileInfo();
            missileInfo.id = missileData.id;
            missileInfo.type = (MissileType)Enum.Parse(typeof(MissileType), missileData.type);
            missileInfo.speed = missileData.speed;
            defaultLimit = missileData.limitLength;
            
            missileInfo.hitTagList = new List<string>();
            var hitTagSplit = missileData.hitTag.Split(',');
            if (hitTagSplit.Length > 0)
            {
                foreach (var hitLayer in hitTagSplit)
                    missileInfo.hitTagList.Add(hitLayer);
            }

            missileInfo.spawnObject = missileData.spawnObject;
            missileInfo.hitSpawn = missileData.hitSpawn;
            missileInfo.afterImage = missileData.afterImage;
        }
        
        missileInfo.explosionAction = action;
        dir = missileDir;
        
        // 레이캐스트
        if (dir == Vector2.left)
        {
            var rayDir = -transform.right;
            var rayVector = new Vector2(transform.position.x, transform.position.y);
            var ray = Physics2D.Raycast(rayVector, rayDir, defaultLimit, missileLayerMask);
            Debug.DrawRay(rayVector, rayDir * defaultLimit, ConstValues.OrangeColor, 0.02f);
            if (ray.collider == null)
                missileInfo.limitLength = defaultLimit;
            else
                missileInfo.limitLength = Vector2.Distance(transform.position, ray.point);
        }
        if (dir == Vector2.right)
        {
            var rayDir = transform.right;
            var rayVector = new Vector2(transform.position.x, transform.position.y);
            var ray = Physics2D.Raycast(rayVector, rayDir, defaultLimit, missileLayerMask);
            Debug.DrawRay(rayVector, rayDir * defaultLimit, ConstValues.OrangeColor, 0.02f);
            if (ray.collider == null)
                missileInfo.limitLength = defaultLimit;
            else
                missileInfo.limitLength = Vector2.Distance(transform.position, ray.point);
        }
        
        SetLimit();
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
                        Delete(false);
                    }
                }
                else if (dir == Vector2.right)
                {
                    if (transform.position.x >= limitPosX)
                    {
                        transform.position = new Vector2(limitPosX, transform.position.y);
                        Delete(false);
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
                        Delete(false);
                    }
                }
                else
                {
                    if (transform.position.y <= limitPosY)
                    {
                        transform.position = new Vector2(transform.position.x, limitPosY);
                        Delete(false);
                    }
                }
                break;
        }
    }
    
    // 물리값 이동(FixedUpdate에서 사용)
    private void Move2()
    {
        if (isDelete)
            return;
        
        float targetSpeedX = missileInfo.speed * dir.x;
        float targetSpeedY = myRigidbody.linearVelocity.y;
    
        if (limitPosX == 0)
            return;
        
        if (dir == Vector2.left)
        {
            if (transform.position.x <= limitPosX)
            {
                Delete(false);
            }
            else
            {
                myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
            }
        }
        else if (dir == Vector2.right)
        {
            if (transform.position.x >= limitPosX)
            {
                Delete(false);
            }
            else
            {
                myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
            }
        }
    }

    private async void Delete(bool isCollision)
    {
        if (isDelete)
            return;
        isDelete = true;

        await UniTask.Yield();
        
        if (missileInfo.spawnObject != ConstValues.None)
        {
            if (isCollision)
            {
                missileInfo.explosionAction(missileInfo.spawnObject, transform, 0);
            }
            else
            {
                if (!missileInfo.hitSpawn || missileInfo.limitLength < defaultLimit)
                    missileInfo.explosionAction(missileInfo.spawnObject, transform, 0);
            }
        }

        //myRigidbody.linearVelocity = Vector2.zero;
        myCollider.enabled = false;
        if(missileSprite)
            missileSprite.enabled = false;
        
        // 잔상 남기기 용도
        if(missileInfo.afterImage)
            await UniTask.WaitForSeconds(1.0f);
        gameObject.SetActive(false);
    }
    
    public void LookAtTarget(Vector2 target)
    {
        if(dir == Vector2.left)
            transform.LookAt2D(target, -180);
        else if(dir == Vector2.right)
            transform.LookAt2D(target);
    }

    // 미사일 소멸에만 관여(공격판정은 여기서 정하지 않는다)
    private void OnTriggerEnter2D(Collider2D col)
    {
        foreach (var hitTag in missileInfo.hitTagList)
        {
            if (!col.gameObject.CompareTag(hitTag))
                continue;

            // 캐릭터들이 무적상태라면 무시한다
            if (hitTag is ConstValues.Player or ConstValues.Monster)
            {
                var character = col.GetComponent<Character>();
                if (character != null)
                {
                    if (character.Immortal || character.IsDie)
                        return;
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
                if(Math.Abs(contactPoint.x - myPoint.x) < 0.01f)
                    return;
            }
            
            // if (missileInfo.id.Split('_')[0] != ConstValues.Monster && hitTag == ConstValues.Platform)
            // {
            //     if(Math.Abs(contactPoint.x - myPoint.x) < 0.01f)
            //         return;
            // }
            
            Delete(true);
            return;
        }
    }
}
