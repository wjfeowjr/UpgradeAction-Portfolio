using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private Transform playerPos;
    
    private void Start()
    {
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Skill, Vector2.zero);
    }
}
