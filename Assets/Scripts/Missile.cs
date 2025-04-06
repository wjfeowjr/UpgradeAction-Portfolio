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
    public List<string> hitTagList;
    public string spawnObject;
    public Action<string, Transform> explosionAction;
}
public class Missile : MonoBehaviour
{
    private Vector2 dir;
    private float limitPosX;
    private bool isDelete;
    private Collider2D myCollider;
    [SerializeField] private MissileInfo missileInfo;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        isDelete = false;
        myCollider.enabled = true;
    }

    private void Update()
    {
        Move();
    }

    public void SetupData(MissileData missileData, Vector2 missileDir, Action<string, Transform> action)
    {
        if (missileInfo == null)
        {
            missileInfo = new MissileInfo();
            missileInfo.id = missileData.id;
            missileInfo.speed = missileData.speed;
            missileInfo.piercingBullet = missileData.piercingBullet;
            missileInfo.limitLength = missileData.limitLength;
        
            var hitTagSplit = missileData.hitTag.Split(',');
            missileInfo.hitTagList = new List<string>();
            foreach (var hitTag in hitTagSplit)
                missileInfo.hitTagList.Add(hitTag);
        
            missileInfo.spawnObject = missileData.spawnObject;
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
            limitPosX -= missileInfo.limitLength;
        else if (dir == Vector2.right)
            limitPosX += missileInfo.limitLength;
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
                Delete();
            }
        }
        else if (dir == Vector2.right)
        {
            if (transform.position.x >= limitPosX)
            {
                Delete();
            }
        }
    }

    private async void Delete()
    {
        if (isDelete)
            return;
        isDelete = true;
        
        if (missileInfo.spawnObject != ConstValues.None)
            missileInfo.explosionAction(missileInfo.spawnObject, transform);
        
        myCollider.enabled = false;

        // 잔상 남기기 용도
        await UniTask.WaitForSeconds(1.0f);
        gameObject.SetActive(false);
    }

    // 미사일 소멸에만 관여(공격판정은 여기서 정하지 않는다)
    private void OnTriggerEnter2D(Collider2D col)
    {
        foreach (var hitTag in missileInfo.hitTagList)
        {
            if (!col.CompareTag(hitTag))
                continue;
            
            // 미사일의 방향에 따라 충돌한 지점 기준으로 미사일의 위치에 따른 충돌무시(벽을 등질 때 오작동 방지)
            if (hitTag == ConstValues.Wall)
            {
                Vector2 contactPoint = col.ClosestPoint(transform.position);
                Vector2 myPoint = transform.position;
                
                if (dir == Vector2.right && myPoint.x > contactPoint.x)
                    return;
                
                if (dir == Vector2.left && myPoint.x < contactPoint.x)
                    return;
            }

            Delete();
            return;
        }
    }
}
