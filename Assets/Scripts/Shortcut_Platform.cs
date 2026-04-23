using UnityEngine;

public class Shortcut_Platform : ShortcutObject
{
    [SerializeField] private MovingPlatform movingPlatform;
    
    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        SetInteractionAction();
        
        if (!opened)
        {
            movingPlatform.IsMoving = false;
            AnimTrigger(ConstValues.Left);
        }
    }

    // 열리는 연출
    public override void OpenProduct()
    {
        AnimTrigger(ConstValues.SwitchRight);
        movingPlatform.IsMoving = true;
        base.OpenImmediate();
    }
    
    // 즉시 오픈
    protected override void OpenImmediate()
    {
        AnimTrigger(ConstValues.Right);
        movingPlatform.IsMoving = true;
        base.OpenImmediate();
    }
    
    public override void SpawnInteractionObject()
    {
        if (opened)
            return;

        base.SpawnInteractionObject();
    }

    private void SetInteractionAction()
    {
        SetInteractionAction(InteractionAction, GameManager.Instance.GetTalk(30003), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey));
    }

    private void InteractionAction()
    {
        if (opened)
            return;

        OpenProduct();
        ReduceInteractionObject();
    }
}
