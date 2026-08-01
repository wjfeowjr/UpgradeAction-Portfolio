using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DemoText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    private void OnEnable()
    {
        // 방을 떠났다 돌아오면 Start는 다시 불리지 않으므로 여기서 텍스트까지 갱신한다
        GameManager.Instance.RefreshBossCount();
        RefreshTalkText();
    }

    private void Start()
    {
        GameManager.Instance.RefreshBossCount();
        RefreshTalkText();
    }

    public void RefreshTalkText()
    {
        textMesh.text = string.Format(GameManager.Instance.GetTalk(30215), GameManager.Instance.CurBossCount, GameManager.Instance.BossCount);
    }
}
