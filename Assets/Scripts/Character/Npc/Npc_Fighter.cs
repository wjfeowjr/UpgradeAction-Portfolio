using System;
using UnityEngine;

public class Npc_Fighter : Npc
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
