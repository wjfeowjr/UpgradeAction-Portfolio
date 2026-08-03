using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private Sprite[] spriteFirst;
    [SerializeField] private Sprite[] spriteMiddle;
    [SerializeField] private Sprite[] spriteEnd;
    [SerializeField] private int idx;
    
    protected BoxCollider2D myBoxCollider;
    [SerializeField] private List<SpriteRenderer> platformSpriteList = new List<SpriteRenderer>();

    protected virtual void Awake()
    {
        myBoxCollider = GetComponent<BoxCollider2D>();

        if (spriteFirst.Length > 0)
        {
            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var spriteRenderer in spriteRenderers)
            {
                string split = spriteRenderer.name.Split(' ')[0];
                if(split == ConstValues.Sprite)
                    platformSpriteList.Add(spriteRenderer);
            }
            for (var i = 0; i < platformSpriteList.Count; i++)
            {
                if (i == 0)
                    platformSpriteList[i].sprite = spriteFirst[idx];
                else if(i == platformSpriteList.Count - 1)
                    platformSpriteList[i].sprite = spriteEnd[idx];
                else
                    platformSpriteList[i].sprite = spriteMiddle[idx];
            }
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

    protected GameObject SpawnObject(string id, Vector2 pos)
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

        var grenadeData = GameManager.Instance.grenadeCopyList.Find(x => x.id == id);
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
}
