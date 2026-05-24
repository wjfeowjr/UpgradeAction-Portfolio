using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Npc_Miner : Npc, IQuestClearAction
{
    protected override void StartDialogue()
    {
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        base.StartDialogue();
    }

    // 퀘스트 클리어 연출: 광부가 속한 Room의 Testing() 호출
    public async UniTask QuestClearAction()
    {
        var room = GetComponentInParent<Room>();
        if (room == null)
            return;

        await room.MinerQuestClearEvent();
    }
    
    
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
