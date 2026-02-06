using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Arena : MonoBehaviour
{
    [SerializeField] private Transform monsterPool;
    [SerializeField] private Transform[] monsterPos;
    [SerializeField] private Transform[] limitPos;
    [SerializeField] private TileFactory tileFactory;
    
    private List<List<Monster>> roundList = new List<List<Monster>>();

    private void Start()
    {
        SettingData();
    }

    private void SettingData()
    {
        tileFactory.gameObject.SetActive(false);
        var arenaData = TableManager.Instance.arenaTable.Arena.FindAll(x => x.id == name);
        if (arenaData.Count == 0)
            return;

        int totalRound = arenaData[^1].round;
        for (int i = 0; i < totalRound; i++)
        {
            var roundMonster = arenaData.FindAll(x => x.round == i + 1);
            List<Monster> monsterList = new List<Monster>();
            foreach (var monster in roundMonster)
            {
                Monster spawnedMonster = GameManager.Instance.ActiveAndHideMonster(monster.monster, monsterPool, monsterPos[monster.posIdx].position, false);
                float rand = Random.Range(-monster.range, monster.range);
                GameObject monsterObject = spawnedMonster.gameObject;
                float pos = monsterObject.transform.position.x + rand;
                
                monsterObject.transform.position = new Vector2(pos, monsterPos[monster.posIdx].position.y);
                spawnedMonster.DataCaching();
                spawnedMonster.LimitLeft = limitPos[0].transform.position.x;
                spawnedMonster.LimitRight = limitPos[1].transform.position.x;
                monsterList.Add(spawnedMonster);
            }
            roundList.Add(monsterList);
        }
    }
    
    public async UniTask ReduceCameraLimitX(Vector2 firstMaxLimit, Vector2 firstMinLimit)
    {
        Vector2 targetMaxLimit = firstMaxLimit;
        Vector2 targetMinLimit = firstMinLimit;
        Vector2 centerVector = transform.position;
        float speed = 3.0f;
        
        GameManager.Instance.MainCamera.SetCameraCurrentPos(1.28f);
        
        while (targetMaxLimit.x > centerVector.x || targetMinLimit.x < centerVector.x)
        {
            if (targetMaxLimit.x > centerVector.x)
                targetMaxLimit = new Vector2(targetMaxLimit.x -= Time.deltaTime * speed, targetMaxLimit.y);
            if (targetMinLimit.x < centerVector.x)
                targetMinLimit = new Vector2(targetMinLimit.x += Time.deltaTime * speed, targetMinLimit.y);
            
            GameManager.Instance.MainCamera.SetCameraLimit(targetMaxLimit, targetMinLimit);
            if (await GameManager.Instance.YieldDelay(GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }
        
        Vector2 finalMaxLimit = new Vector2(centerVector.x, targetMaxLimit.y);
        Vector2 finalMinLimit = new Vector2(centerVector.x, targetMinLimit.y);
        GameManager.Instance.MainCamera.SetCameraLimit(finalMaxLimit, finalMinLimit);
    }

    public void CreateTile()
    {
        tileFactory.gameObject.SetActive(true);
        tileFactory.Build();
    }
    
    public async UniTask RoundStart()
    {
        var monsterDelay = 0.15f;
        var roundDelay = 1.5f;

        int count = 0;
        foreach (var round in roundList)
        {
            foreach (var monster in round)
            {
                SpawnMonster(monster);
                if (await GameManager.Instance.NormalDelay(monsterDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                    return;
            }
            if (await GameManager.Instance.WaitUntilDelay(()=> RoundMonsterAllDead(round), GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;

            count += 1;
            if (count == roundList.Count)
                break;
            
            if (await GameManager.Instance.NormalDelay(roundDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
                return;
        }
    }

    public async UniTask RoundEnd()
    {
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.Immortal = true;

        Time.timeScale = 0.2f;
        
        var finishDelay = 0.5f;
        var endDelay = 2.0f;
        var productDelay = 2.0f;
        
        BgmManager.Instance.DelayStop(0.01f);
        if (await GameManager.Instance.NormalDelay(finishDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        Time.timeScale = 1.0f;
        if (await GameManager.Instance.NormalDelay(endDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;
        
        SoundManager.Instance.PlaySound(ConstValues.Star3);
        
        if (await GameManager.Instance.NormalDelay(productDelay, GameManager.Instance.ProductCancellation).SuppressCancellationThrow())
            return;

        tileFactory.Crash();
        tileFactory.gameObject.SetActive(false);
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.CurPlayer.Immortal = false;
    }

    private void SpawnMonster(Monster monster)
    {
        monster.IsBoss = false;
        monster.AlwaysAgro = true;
        monster.gameObject.SetActive(true);
        monster.Appear(null);
    }

    private bool RoundMonsterAllDead(List<Monster> round)
    {
        bool allDead = true;
        foreach (var monster in round)
        {
            if (!monster.IsDie)
            {
                allDead = false;
                break;
            }
        }

        return allDead;
    }
}
