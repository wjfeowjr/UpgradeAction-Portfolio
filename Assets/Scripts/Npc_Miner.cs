using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Npc_Miner : Npc
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        var attack = col.GetComponent<Attack>();

        if (attack != null && attack.CastChar.GetComponent<Monster_Bomb>())
        {
            float randX = Random.Range(-1.5f, 0f);
            float randY = Random.Range(5.0f, 7.0f);
            Airborne(randX, randY, true);

            int randIdx = Random.Range(1, 6);
            PlaySound($"{ConstValues.PlayerDamaged}{randIdx}");
        }
    }
}
