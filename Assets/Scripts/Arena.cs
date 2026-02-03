using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Arena : MonoBehaviour
{
    [SerializeField] private Transform monsterPool;
    [SerializeField] private Transform[] monsterPos;
    private List<List<Monster>> roundList = new List<List<Monster>>();

    private void Start()
    {
        SettingData();
    }

    private void SettingData()
    {
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
}
