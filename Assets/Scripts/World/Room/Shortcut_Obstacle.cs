using UnityEngine;
using UnityEngine.Serialization;

public class Shortcut_Obstacle : ShortcutObject
{
    [SerializeField] private Transform destroyPos;
    [SerializeField] private SpriteRenderer[] obstacleSpriteRenderers;
    
    public Transform DestroyPos => destroyPos;
    
    public override void OpenProduct()
    {
        Crash();
        base.OpenProduct();
    }

    private void Crash()
    {
        foreach (var spriteRenderer in obstacleSpriteRenderers)
            SpawnObject(ConstValues.ShortcutCrashExplosion, spriteRenderer.transform.position);

        foreach (var spriteRenderer in obstacleSpriteRenderers)
        {
            var explosion = SpawnObject(ConstValues.ObstacleExplosion, spriteRenderer.transform.position);
            explosion.transform.localScale = spriteRenderer.transform.localScale;

            var fragmentObj = SpawnObject(ConstValues.PlatformFragments, spriteRenderer.transform.position);
            var spriteChanger = fragmentObj.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(spriteRenderer.sprite);

            var rigidBody = fragmentObj.GetComponent<Rigidbody2D>();
            float randX = Random.Range(-10.0f, 10.0f);
            float randY = Random.Range(15.0f, 25.0f);
            rigidBody.linearVelocity = new Vector2(randX, randY);
                
            var spin = fragmentObj.GetComponent<Spin>();
            
            if(rigidBody.linearVelocityX > 0)
                spin.SetSpinSpeed(false);
            else
                spin.SetSpinSpeed(true);
        }
    }
    
    private GameObject SpawnObject(string id, Vector2 pos)
    {
        var obj = GameManager.Instance.SpawnToObjectPool(id, pos);
        
        var objectData = TableManager.Instance.GetSpawnedObject(id);
        if(objectData == null)
            return obj;

        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();

        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();

        return obj;
    }
}
