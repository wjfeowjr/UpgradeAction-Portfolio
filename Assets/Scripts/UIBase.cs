using UnityEngine;

public class UIBase : MonoBehaviour
{
    [SerializeField] private eUIType uiType;

    public eUIType GetUIType()
    {
        return uiType;
    }
    public void Setup(eUIType type)
    {
        uiType = type;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
