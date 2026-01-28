using UnityEngine;

public class Lever : InteractionController
{
    protected Animator myAnimator;

    protected void AnimTrigger(string id)
    {
        myAnimator.SetTrigger(id);
    }
}
