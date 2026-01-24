using System;
using TMPro;
using UnityEngine;

public class SpeechFrame : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private TMP_Text nextText;
    [SerializeField] private GameObject nextObject;

    private void Awake()
    {
        nextText.text = GameManager.Instance.GetKeyCode(GameManager.Instance.spaceKey);
    }

    public void SetPos(Vector2 pos)
    {
        transform.position = pos;
    }
    public void Speech(string dialog)
    {
        if(!gameObject.activeSelf)
            gameObject.SetActive(true);
        
        nextObject.SetActive(false);

        Trace(null);
        PlaySound();
        dialogText.text = dialog;
    }

    public void NextObjectActive()
    {
        nextObject.SetActive(true);
    }
    
    public void SpeechEnd()
    {
        gameObject.SetActive(false);
    }

    public void Trace(Transform targetTransform)
    {
        if (GetComponent<Trace>() == null)
            return;
        
        GetComponent<Trace>().SetTarget(targetTransform);
    }

    private void PlaySound()
    {
        SoundManager.Instance.PlaySound(ConstValues.SpeechFrame);
    }
}
