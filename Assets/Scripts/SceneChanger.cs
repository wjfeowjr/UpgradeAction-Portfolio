using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : Singleton<SceneChanger>
{
    public bool titleScene;

    public bool TitleScene
    {
        get => titleScene;
        set => titleScene = value;
    }
    
    public void SceneControl()
    {
        if (!titleScene)
        {
            SceneManager.LoadScene(ConstValues.TitleScene);
            titleScene = true;
        }
    }
}
