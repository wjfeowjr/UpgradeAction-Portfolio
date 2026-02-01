using System;
using System.Collections.Generic;
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
                Monster spawnedMonster = GameManager.Instance.ActiveAndHideMonster(monster.monster, monsterPool, monsterPos[monster.posIdx].position);
                monsterList.Add(spawnedMonster);
            }
            roundList.Add(monsterList);
        }
    }
}
