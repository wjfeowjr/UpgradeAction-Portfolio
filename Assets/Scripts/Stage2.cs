using UnityEngine;

public class Stage2 : Stage
{
    
    protected override void SetEpisodeName()
    {
        episodeName = ConstValues.Episode2;
    }
    protected override async void DialogStep()
    {
        // 대화 진행
        switch (myEventStep)
        {
            
        }
    }
    protected override void StageClearButtonAction()
    {
        Application.Quit();
    }
}
