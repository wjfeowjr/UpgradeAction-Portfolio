using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [SerializeField] private FollowCamera mainCamera;
    [SerializeField] private GameObject[] stageArray;
    
    private void Awake()
    {
        if (!SceneChanger.Instance)
            SceneManager.LoadScene(ConstValues.TitleScene);
        
        if (GameManager.Instance)
            GameManager.Instance.InitCamera(mainCamera);
    }

    private void Start()
    {
        Instantiate(stageArray[1]);
    }
}
