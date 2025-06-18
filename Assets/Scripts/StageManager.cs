using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private FollowCamera mainCamera;
    [SerializeField] private GameObject[] stageArray;
    [SerializeField] private Stage currentStage;
    
    protected override void Awake()
    {
        if (!SceneChanger.Instance)
            SceneManager.LoadScene(ConstValues.TitleScene);

        if (GameManager.Instance)
            GameManager.Instance.InitCamera(mainCamera);
    }

    public void Start()
    {
        currentStage = Instantiate(stageArray[0]).GetComponent<Stage>();
    }

    public int GetStageDialogStep()
    {
        return currentStage.EpisodeStep.dialogStep;
    }
}
