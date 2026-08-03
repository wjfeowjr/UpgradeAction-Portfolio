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
}
