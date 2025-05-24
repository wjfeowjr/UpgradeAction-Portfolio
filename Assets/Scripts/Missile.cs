using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class MissileInfo
{
    public string id;
    public float speed;
    public bool piercingBullet;
    public float limitLength;
    public List<string> hitLayerList;
    public string spawnObject;
    public bool hitSpawn;
    public Action<string, Transform, int> explosionAction;
}
public class Missile : MonoBehaviour
{
    private Vector2 dir;
    private float limitPosX;
    private bool isDelete;
    private Collider2D myCollider;
    private SpriteRenderer missileSprite;
    [SerializeField] private MissileInfo missileInfo;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        missileSprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        isDelete = false;
        myCollider.enabled = true;
        if (missileSprite)
            missileSprite.enabled = true;

        // var leftRay = Physics2D.Raycast(transform.position, Vector2.left, missileInfo.limitLength, moveLayerMask);
        // Debug.DrawRay(transform.position, Vector2.left * missileInfo.limitLength, ConstValues.RedColor, 0.1f);
        //
        // if (leftRay.collider != null)
        //     Debug.Log(leftRay.point);
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void SetupData(MissileData missileData, Vector2 missileDir, Action<string, Transform, int> action)
    {
        if (missileInfo == null)
        {
            missileInfo = new MissileInfo();
            missileInfo.id = missileData.id;
            missileInfo.speed = missileData.speed;
            missileInfo.piercingBullet = missileData.piercingBullet;
            missileInfo.limitLength = missileData.limitLength;
        
            var hitLayerSplit = missileData.hitLayer.Split(',');
            missileInfo.hitLayerList = new List<string>();
            foreach (var hitLayer in hitLayerSplit)
                missileInfo.hitLayerList.Add(hitLayer);
            
            missileInfo.spawnObject = missileData.spawnObject;
            missileInfo.hitSpawn = missileData.hitSpawn;
            missileInfo.explosionAction = action;
        }
        dir = missileDir;
        SetLimit();
    }

    private void SetLimit()
    {
        if (missileInfo.limitLength == 0)
            return;

        limitPosX = transform.position.x;
        if (dir == Vector2.left)
        {
            limitPosX -= missileInfo.limitLength;
            if(missileSprite)
                missileSprite.flipX = true;
        }
        else if (dir == Vector2.right)
        {
            limitPosX += missileInfo.limitLength;
            if(missileSprite)
                missileSprite.flipX = false;
        }
    }
    
    private void Move()
    {
        if (isDelete)
            return;
        
        transform.Translate(dir * (missileInfo.speed * Time.deltaTime));
        
        if (limitPosX == 0)
            return;
        
        if (dir == Vector2.left)
        {
            if (transform.position.x <= limitPosX)
            {
                Delete(false);
            }
        }
        else if (dir == Vector2.right)
        {
            if (transform.position.x >= limitPosX)
            {
                Delete(false);
            }
        }
    }

    private async void Delete(bool isCollision)
    {
        if (isDelete)
            return;
        isDelete = true;

        if (missileInfo.spawnObject != ConstValues.None)
        {
            if (isCollision)
            {
                missileInfo.explosionAction(missileInfo.spawnObject, transform, 0);
            }
            else
            {
                if (!missileInfo.hitSpawn)
                    missileInfo.explosionAction(missileInfo.spawnObject, transform, 0);
            }
        }

        myCollider.enabled = false;
        if(missileSprite)
            missileSprite.enabled = false;
        
        // 잔상 남기기 용도
        await UniTask.WaitForSeconds(1.0f);
        gameObject.SetActive(false);
    }

    // 미사일 소멸에만 관여(공격판정은 여기서 정하지 않는다)
    private void OnTriggerEnter2D(Collider2D col)
    {
        foreach (var hitTag in missileInfo.hitLayerList)
        {
            if (!col.gameObject.CompareTag(hitTag))
                continue;

            // 캐릭터들이 무적상태라면 무시한다
            if (hitTag is ConstValues.Player or ConstValues.Monster)
            {
                var character = col.GetComponent<Character>();
                if (character != null)
                {
                    if (character.Immortal)
                        return;
                }
            }
            
            // 미사일의 방향에 따라 충돌한 지점 기준으로 미사일의 위치에 따른 충돌무시(벽을 등질 때 오작동 방지)
            if (missileInfo.piercingBullet && hitTag == ConstValues.Wall)
            {
                Vector2 contactPoint = col.ClosestPoint(transform.position);
                Vector2 myPoint = transform.position;

                if (dir == Vector2.right && myPoint.x > contactPoint.x)
                    return;
                
                if (dir == Vector2.left && myPoint.x < contactPoint.x)
                    return;
                
                // 수정될 수 있음. 벽 위에서 투사체를 날린 경우
                if(Math.Abs(contactPoint.x - myPoint.x) < 0.01f)
                    return;
            }

            Delete(true);
            return;
        }
    }
}
