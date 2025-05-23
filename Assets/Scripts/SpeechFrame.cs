using TMPro;
using UnityEngine;

public class SpeechFrame : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogText; 
    
    public void SetPos(Vector2 pos)
    {
        transform.position = pos;
    }
    public void Speech(string dialog)
    {
        PlaySound();
        dialogText.text = dialog;
    }

    private void PlaySound()
    {
        SoundManager.Instance.PlaySound(ConstValues.SpeechFrame);
    }
}
