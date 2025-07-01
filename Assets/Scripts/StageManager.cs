using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private FollowCamera mainCamera;
    [SerializeField] private GameObject[] stageArray;
    [SerializeField] private Stage currentStage;

    // 프로퍼티
    public Stage CurrentStage => currentStage;
    
    protected override void Awake()
    {
        if (!SceneChanger.Instance)
            SceneManager.LoadScene(ConstValues.TitleScene);

        if (GameManager.Instance)
            GameManager.Instance.InitCamera(mainCamera);
    }

    public void Start()
    {
        currentStage = Instantiate(stageArray[GameManager.Instance.LoadStage()]).GetComponent<Stage>();
    }
    

    public int GetStageDialogStep()
    {
        return currentStage.EpisodeStep.dialogStep;
    }
}
