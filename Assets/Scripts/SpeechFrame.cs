using TMPro;
using UnityEngine;

public class SpeechFrame : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogText; 
    
    public void Speech(string dialog)
    {
        dialogText.text = dialog;
    }
}
