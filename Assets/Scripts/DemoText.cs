using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DemoText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    private void Start()
    {
        textMesh.text = GameManager.Instance.GetTalk(30215);
    }
}
