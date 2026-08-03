using Cysharp.Threading.Tasks;
using UnityEngine;

public class Npc_Merchant : Npc
{
    private void Start()
    {
        Flip(-1);
    }

    protected override void StartDialogue()
    {
        LookAt(GameManager.Instance.CurPlayer.transform.position.x);
        base.StartDialogue();
    }
}
