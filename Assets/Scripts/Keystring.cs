using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class Keystring : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    public void SetText(string text)
    {
        textMesh.text = text;
    }
}
