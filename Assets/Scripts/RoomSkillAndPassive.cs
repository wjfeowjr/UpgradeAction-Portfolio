using System;
using UnityEngine;

public class RoomSkillAndPassive : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mySpriteRenderer;
    private Action action;
    private string skillId;

    public void SetSprite(string id, bool alreadyGet)
    {
        mySpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(id);
        skillId = id;
        
        gameObject.SetActive(!alreadyGet);
    }

    public void SetAction(Action getAction)
    {
        action = getAction;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(ConstValues.Player) && col.isTrigger)
        {
            action();
            gameObject.SetActive(false);
        }
    }
}
