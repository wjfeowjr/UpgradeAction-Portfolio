using UnityEngine;

public class SpriteChanger : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public void ChangeSprite(Sprite sprite)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
    }
}
