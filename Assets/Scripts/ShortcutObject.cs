using System;
using UnityEngine;

public class ShortcutObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mySpriteRenderer;
    [SerializeField] private Sprite[] leverSprites;
    [SerializeField] private GameObject shortcutBlocker;// 막고 있는 문/벽(콜라이더 포함)

    private Action openAction;
    private bool opened;

    public void CacheBlocker(GameObject blockerObject)
    {
        shortcutBlocker = blockerObject;
    }
    public void OpenSetting(int point, Action action)
    {
        if (point == 0)
        {
            openAction = action;
            return;
        }
        
        OpenShortcutImmediate();
    }

    private void BreakAndOpen()
    {
        openAction();
        Open();
    }

    // 저장 반영으로 이미 열린 경우 즉시 처리
    private void OpenShortcutImmediate()
    {
        Open();
    }

    private void Open()
    {
        mySpriteRenderer.sprite = leverSprites[1];
        opened = true;
        
        if (shortcutBlocker)
            shortcutBlocker.SetActive(false);
    }
    
    // 공격판정(Attack 오브젝트)와 충돌했을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened)
            return;

        // Attack 컴포넌트로 판정 (프로젝트 구조 기준)
        var atk = other.GetComponent<Attack>();
        if (!atk)
            return;

        if (!atk.CastChar.GetComponent<Player>())
            return;

        // 파괴 처리
        BreakAndOpen();
    }
}
