using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private FollowCamera mainCamera;
    [SerializeField] private Transform playerPos;
    [SerializeField] private List<Collider2D> platformColliderList;
    
    private void Start()
    {
        GameManager.Instance.InitCamera(mainCamera);
        GameManager.Instance.SpawnPlayer(GameManager.Instance.FirstPlayer, playerPos);
        GameManager.Instance.SpawnToUIPool(eUIType.UI_Skill, Vector2.zero);
        GameManager.Instance.PlatformColliderList = platformColliderList;
    }
}
