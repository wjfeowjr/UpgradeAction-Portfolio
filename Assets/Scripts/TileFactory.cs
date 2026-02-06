using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[Serializable]
public class TileFragment
{
    public Vector2 tilePos;
    public Sprite tileSprite;
}

public class TileFactory : MonoBehaviour
{
    [SerializeField] private Tilemap spriteTilemap;
    [SerializeField] private Transform[] explosionPos;
    [SerializeField] private GameObject shakeObject;
    [SerializeField] private List<TileFragment> fragmentList = new List<TileFragment>();
    
    private Vector2 firstVector;
    private Vector2 centerVector;
    private CancellationTokenSource shakeCancellation;

    private void Awake()
    {
        if (shakeObject)
        {
            var pos = shakeObject.transform.position;
            firstVector = pos;
            centerVector = pos;
        }
        
        SetFragmentList();
    }

    private void SetFragmentList()
    {
        foreach (Vector3Int cellPos in spriteTilemap.cellBounds.allPositionsWithin)
        {
            if (spriteTilemap.GetTile(cellPos) == null)
                continue;

            TileFragment fragment = new TileFragment
            {
                tilePos = spriteTilemap.GetCellCenterWorld(cellPos),
                tileSprite = spriteTilemap.GetSprite(cellPos)
            };
            fragmentList.Add(fragment);
        }
    }

    public async void HitProduct()
    {
        // 대략적인 연출과정
        float randomEffectX = Random.Range(-0.2f, 0.2f);
        float randomEffectY = Random.Range(-0.2f, 0.2f);

        Vector2 randomVector = Vector2.zero;
        foreach (var pos in explosionPos)
            randomVector = new Vector2(pos.position.x + randomEffectX, pos.position.y + randomEffectY);

        SpawnObject(ConstValues.ShortcutCrashEffect, randomVector);
        
        shakeCancellation = new CancellationTokenSource();
        for (int i = 0; i < 5; i++)
        {
            float randomX = Random.Range(-0.1f, 0.1f);
            float randomY = Random.Range(-0.1f, 0.1f);
            Vector2 shakeVector = new Vector2(firstVector.x + randomX, firstVector.y + randomY);
            shakeObject.transform.position = shakeVector;
            if (await NormalDelay(0.02f, shakeCancellation).SuppressCancellationThrow())
                return;
        }
        shakeObject.transform.position = firstVector;
    }
    
    public void Build()
    {
        foreach (var fragment in fragmentList)
        {
            SpawnObject(ConstValues.ShortcutCrashEffect, fragment.tilePos);
        }
    }

    public void Crash()
    {
        foreach (var pos in explosionPos)
            SpawnObject(ConstValues.ShortcutCrashExplosion, pos.position);

        foreach (var fragment in fragmentList)
        {
            SpawnObject(ConstValues.PlatformExplosion, fragment.tilePos);
            var fragmentObj = SpawnObject(ConstValues.PlatformFragments, fragment.tilePos);

            var spriteChanger = fragmentObj.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(fragment.tileSprite);
            
            var rigidBody = fragmentObj.GetComponent<Rigidbody2D>();
            float randX = Random.Range(-4.0f, 4.0f);
            float randY = Random.Range(10.0f, 12.0f);
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
        
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if(objectData == null)
            return obj;

        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();

        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();

        return obj;
    }

    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
}
