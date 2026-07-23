using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DemoText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    private void OnEnable()
    {
        GameManager.Instance.RefreshBossCount();
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
