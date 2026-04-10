using Cysharp.Threading.Tasks;
using UnityEngine;

public class Npc_Merchant : Npc, IFirstDialogAction
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
    
    public async UniTask DialogAction()
    {
        await GameManager.Instance.DialogueMove(-1.5f);
        
        
    }
}
