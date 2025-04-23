using UnityEngine;

public class BattleManager : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.uiManager.OpenSkillUI();
    }
}
