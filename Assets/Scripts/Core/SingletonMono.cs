using UnityEngine;

public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static object _lock = new object();

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (null == instance)
                {
                    GameObject singleton = new GameObject();
                    instance = singleton.AddComponent<T>();
                    singleton.name = "(SingletonMono) " + typeof(T).ToString();

                    if (Application.isPlaying)
                        DontDestroyOnLoad(singleton);
                }

                return instance;
            }
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }
}