using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Arena : MonoBehaviour
{
    [SerializeField] private Transform monsterPool;
    [SerializeField] private Transform[] monsterPos;
    [SerializeField] private Transform[] limitPos;
    [SerializeField] private TileFactory tileFactory;
    private CancellationTokenSource arenaCancellation;
    
    private List<List<Monster>> roundList = new List<List<Monster>>();

    private void Awake()
    {
        arenaCancellation = new CancellationTokenSource();
    }

    private void Start()
    {
        SettingData();
    }
    
    private void OnDestroy()
    {
        arenaCancellation?.Cancel();
        arenaCancellation?.Dispose();
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
            if (await YieldDelay().SuppressCancellationThrow())
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
    
    public async UniTask RoundStart(CancellationTokenSource tokenSource)
    {
        var monsterDelay = 0.15f;
        var roundDelay = 1.5f;

        int count = 0;
        foreach (var round in roundList)
        {
            for (var i = 0; i < round.Count; i++)
            {
                SpawnMonster(round[i], -2 - i);
                if (await NormalDelay(monsterDelay, tokenSource).SuppressCancellationThrow())
                    return;
            }

            if (await WaitUntilDelay(() => RoundMonsterAllDead(round), tokenSource).SuppressCancellationThrow())
                return;

            count += 1;
            if (count == roundList.Count)
                break;
            
            if (await NormalDelay(roundDelay, tokenSource).SuppressCancellationThrow())
                return;
        }
    }

    public async UniTask RoundEnd(CancellationTokenSource tokenSource)
    {
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.Immortal = true;

        GameManager.Instance.Flow.BaseTimeScale = 0.2f;
        
        var finishDelay = 0.5f;
        var endDelay = 2.0f;
        var productDelay = 2.0f;
        
        BgmManager.Instance.DelayStop(0.1f);
        if (await NormalDelay(finishDelay, tokenSource).SuppressCancellationThrow())
            return;

        GameManager.Instance.Flow.BaseTimeScale = 1.0f;
        GameManager.Instance.CurPlayer.ForceProduct();
        if (await NormalDelay(endDelay, tokenSource).SuppressCancellationThrow())
            return;

        SoundManager.Instance.PlaySound(ConstValues.Star3);
        
        if (await NormalDelay(productDelay, tokenSource).SuppressCancellationThrow())
            return;

        tileFactory.Crash();
        tileFactory.gameObject.SetActive(false);
        GameManager.Instance.ControlStart = true;
        GameManager.Instance.CurPlayer.Immortal = false;
    }

    private void SpawnMonster(Monster monster, int value)
    {
        monster.MonsterType = EMonsterType.Normal;
        monster.AlwaysAgro = true;
        monster.gameObject.SetActive(true);
        monster.Appear(null);
        monster.SetSortingGroup(value);
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
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }

    private async UniTask YieldDelay()
    {
        await UniTask.Yield(cancellationToken: arenaCancellation.Token);
    }
    
    private async UniTask WaitUntilDelay(Func<bool> condition, CancellationTokenSource tokenSource)
    {
        await UniTask.WaitUntil(condition, cancellationToken: tokenSource.Token);
    }
}
