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
        //currentStage = Instantiate(stageArray[GameManager.Instance.LoadStage()]).GetComponent<Stage>();
        //currentStage = Instantiate(stageArray[1]).GetComponent<Stage>();
        BgmManager.Instance.Stop();
        
        GameManager.Instance.SetPlayerOrder(ConstValues.Berserker, default); // default

        //GameManager.Instance.SpawnToUIPool(eUIType.UI_Interface, Vector2.zero);
        GameManager.Instance.SetGroundVector();
        GameManager.Instance.InitPlayerStat();
    }
    
    protected virtual void Update()
    {
        GameManager.Instance.ReduceSkillPlayer();
    }
    

    public int GetStageDialogStep()
    {
        return currentStage.EpisodeStep.dialogStep;
    }
}
