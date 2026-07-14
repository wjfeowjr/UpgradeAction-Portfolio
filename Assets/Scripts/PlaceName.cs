using TMPro;
using UnityEngine;

public enum ePlace
{
    Forest,
    BaseCamp,
    Dungeon,
    Mine,
    SnowField,
}

public class PlaceName : MonoBehaviour
{
    [SerializeField] private TextMeshPro nameText;
    [SerializeField] private ePlace ePlace;

    public string Place => ePlace.ToString();

    public void SetText()
    {
        switch (ePlace)
        {
            case ePlace.Forest:
                nameText.text = GameManager.Instance.GetTalk(130000);
                break;
            
            case ePlace.BaseCamp:
                nameText.text = GameManager.Instance.GetTalk(130001);
                break;
            
            case ePlace.Dungeon:
                nameText.text = GameManager.Instance.GetTalk(130002);
                break;
            
            case ePlace.Mine:
                nameText.text = GameManager.Instance.GetTalk(130003);
                break;
            
            case ePlace.SnowField:
                nameText.text = GameManager.Instance.GetTalk(130004);
                break;
        }
    }
}
