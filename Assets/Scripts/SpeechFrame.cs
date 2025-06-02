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
        if(!gameObject.activeSelf)
            gameObject.SetActive(true);

        Trace(null);
        PlaySound();
        dialogText.text = dialog;
    }

    public void Trace(Transform targetTransform)
    {
        GetComponent<Trace>().SetTarget(targetTransform);
    }

    private void PlaySound()
    {
        SoundManager.Instance.PlaySound(ConstValues.SpeechFrame);
    }
}
