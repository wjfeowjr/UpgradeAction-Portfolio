using UnityEngine;

public class Shortcut_Lever : ShortcutObject
{
    [SerializeField] private SpriteRenderer mySpriteRenderer;
    [SerializeField] private Sprite[] leverSprites;

    // 즉시 오픈
    protected override void OpenImmediate()
    {
        mySpriteRenderer.sprite = leverSprites[1];
        base.OpenImmediate();
    }
}
