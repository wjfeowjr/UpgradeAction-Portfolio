using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private Sprite[] spriteFirst;
    [SerializeField] private Sprite[] spriteMiddle;
    [SerializeField] private Sprite[] spriteEnd;
    [SerializeField] private int idx;
    private SpriteRenderer[] platformSpriteList;
    
    private MovingPlatform movingPlatform;

    private void Awake()
    {
        platformSpriteList = GetComponentsInChildren<SpriteRenderer>();
        for (var i = 0; i < platformSpriteList.Length; i++)
        {
            if (i == 0)
                platformSpriteList[i].sprite = spriteFirst[idx];
            else if(i == platformSpriteList.Length - 1)
                platformSpriteList[i].sprite = spriteEnd[idx];
            else
                platformSpriteList[i].sprite = spriteMiddle[idx];
        }

        movingPlatform = GetComponent<MovingPlatform>();
        if (movingPlatform)
        {
            movingPlatform.IsMoving = true;
            movingPlatform.IsRepeat = true;
            movingPlatform.Delay = 2.5f;
        }
    }

    public void DestroyBomb()
    {
        SoundManager.Instance.PlaySound(ConstValues.FighterStrongPunch);
        GameManager.Instance.CameraShake(0.3f, 0.3f, 0.3f);
        foreach (var platform in platformSpriteList)
        {
            SpawnObject(ConstValues.PlatformExplosion, platform.transform.position);
            var fragmentObj = SpawnObject(ConstValues.PlatformFragments, platform.transform.position);

            var spriteChanger = fragmentObj.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(platform.sprite);
            
            var rigidBody = fragmentObj.GetComponent<Rigidbody2D>();
            var spin = fragmentObj.GetComponent<Spin>();
            
            if(rigidBody.linearVelocityX > 0)
                spin.SetSpinSpeed(false);
            else
                spin.SetSpinSpeed(true);
        }
        gameObject.SetActive(false);
    }

    private GameObject SpawnObject(string id, Vector2 pos)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
            
        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();

        var grenadeData = TableManager.Instance.grenadeTable.Grenade.Find(x => x.id == id);
        if (grenadeData != null)
        {
            var grenade = obj.GetComponent<Grenade>();
            if (!grenade)
                grenade = obj.AddComponent<Grenade>();
            
            var dir = Vector2.right;
            grenade.SetupData(grenadeData, dir, SpawnDust);
            grenade.Throw();
        }
        
        return obj;
    }

    private void SpawnDust(string id, Transform dustTransform, int zAngle = 0, Vector2 targetVector = default)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, dustTransform);
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (objectData == null)
            return;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
            
        spawnedObject.SetupData(objectData, 1);
        spawnedObject.EnableSetting();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.DestroyPlatform))
            DestroyBomb();
    }
}
