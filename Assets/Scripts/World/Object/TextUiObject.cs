using TMPro;
using UnityEngine;

public class TextUiObject : MonoBehaviour
{
    [SerializeField] protected TMP_Text mainText;
    
    public void SetText(string inputText)
    {
        if(mainText)
            mainText.text = inputText;
    }
}
