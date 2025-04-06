using UnityEngine;

public class UIBase : MonoBehaviour
{
    public eUIType uiType;

    public void Setup(eUIType type)
    {
        uiType = type;
    }
    
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
